using System.Diagnostics;
using System.Runtime.CompilerServices;
using ChessEngine.Core.BoardState;
using ChessEngine.Core.Main;
using ChessEngine.Core.MoveGen;

namespace ChessEngine.Core.Search;

/// <summary>
/// Iterative-deepening negamax with alpha-beta pruning, principal-variation search, a transposition
/// table, null-move / reverse-futility pruning, late-move reductions and a quiescence search.
/// One instance searches one position; it is not thread-safe except for <see cref="Stop"/>.
/// </summary>
public class Searcher(Board board, TranspositionTable tt)
{
    public const int Infinity = 32_000;
    public const int MateScore = 30_000;
    public const int MaxPly = 128;

    /// <summary>Scores at or beyond this magnitude are forced mates (mate scores shrink by one per ply).</summary>
    private const int MateBound = MateScore - MaxPly;

    private const int TimeCheckInterval = 2048; // nodes between clock reads (must be a power of two)
    private const int MillisecondsPerSecond = 1000;

    // Draw detection
    private const int FiftyMoveRuleHalfmoves = 100;
    private const int RepetitionDrawCount = 2; // one earlier occurrence is enough to score a draw inside the search

    // Aspiration windows
    private const int AspirationMinDepth = 5;
    private const int AspirationWindow = 40;

    // Reverse futility pruning
    private const int ReverseFutilityMaxDepth = 6;
    private const int ReverseFutilityMargin = 100; // centipawns per remaining ply

    // Null-move pruning
    private const int NullMoveMinDepth = 3;
    private const int NullMoveBaseReduction = 2;
    private const int NullMoveDepthDivisor = 3;

    // Late move reductions
    private const int LmrMinDepth = 3;
    private const int LmrMinMoveIndex = 3;
    private const int LmrTableSize = 64;
    private const double LmrBase = 0.75;
    private const double LmrDivisor = 2.25;

    // Quiescence: skip captures that cannot raise alpha even with this much extra margin
    private const int DeltaMargin = 200;

    private static readonly int[,] Lmr = BuildLmr();

    private readonly Heuristics _heuristics = new();

    private long _deadline;
    private ulong _nodes;
    private bool _stopped;
    private volatile bool _abort;
    private Move _rootBest;

    /// <summary>Asks a running search to finish; safe to call from another thread.</summary>
    public void Stop() => _abort = true;

    /// <summary>
    /// Searches the position, deepening one ply at a time until a limit is hit.
    /// The result is the best move of the last fully completed iteration (or a partially searched one,
    /// which is still at least as good because the previous best move is always searched first).
    /// </summary>
    public SearchResult Search(SearchLimits limits)
    {
        BeginSearch(limits);

        var best = Move.Null;
        var bestScore = 0;
        var completedDepth = 0;

        for (var depth = 1; depth <= limits.MaxDepth; depth++)
        {
            var score = SearchWithAspiration(depth, bestScore);

            if (_stopped)
            {
                // The previous best move is searched first, so _rootBest is never worse than it.
                if (!_rootBest.IsNull)
                    best = _rootBest;
                break;
            }

            best = _rootBest;
            bestScore = score;
            completedDepth = depth;

            if (Math.Abs(score) >= MateBound)
                break; // forced mate found, deeper search can't improve it
        }

        if (best.IsNull)
            best = FirstLegalMove();

        return new SearchResult(best, bestScore, completedDepth, _nodes);
    }

    private void BeginSearch(SearchLimits limits)
    {
        _nodes = 0;
        _stopped = false;
        _abort = false;
        _rootBest = Move.Null;
        _deadline = Stopwatch.GetTimestamp() + limits.TimeMs * Stopwatch.Frequency / MillisecondsPerSecond;
        tt.NewSearch();
    }

    private Move FirstLegalMove()
    {
        var list = new MoveList();
        MoveGenerator.Generate(board, ref list);
        return list.Count > 0 ? list[0] : Move.Null;
    }

    /// <summary>
    /// Searches the root with a narrow window around the previous score, which prunes far more than a full
    /// window. If the result falls outside the window, the window is widened (doubling each time) and re-searched.
    /// </summary>
    private int SearchWithAspiration(int depth, int previousScore)
    {
        int alpha = -Infinity, beta = Infinity, window = AspirationWindow;

        if (depth >= AspirationMinDepth)
        {
            alpha = Math.Max(previousScore - window, -Infinity);
            beta = Math.Min(previousScore + window, Infinity);
        }

        while (true)
        {
            var score = Negamax(depth, 0, alpha, beta, false);

            if (_stopped)
                return score;

            if (score <= alpha)
            {
                beta = (alpha + beta) / 2;
                alpha = Math.Max(score - window, -Infinity);
            }
            else if (score >= beta)
            {
                beta = Math.Min(score + window, Infinity);
            }
            else
            {
                return score;
            }

            window *= 2;
        }
    }

    /// <summary>Scores the position for the side to move, searching <paramref name="depth"/> plies.</summary>
    /// <param name="allowNull">False directly after a null move, so two are never played in a row.</param>
    private int Negamax(int depth, int ply, int alpha, int beta, bool allowNull)
    {
        if (ShouldStop())
            return 0;

        var isRoot = ply == 0;
        var pvNode = beta - alpha > 1;

        if (!isRoot && TryTerminalScore(ply, ref alpha, ref beta, out var terminalScore))
            return terminalScore;

        var inCheck = MoveGenerator.IsInCheck(board);
        if (inCheck)
            depth++; // check extension

        if (ply >= MaxPly - 1)
            return Evaluation.Evaluate(board);

        if (depth <= 0)
            return Quiescence(ply, alpha, beta);

        if (TryProbeTt(depth, ply, alpha, beta, pvNode, out var ttMove, out var ttScore))
            return ttScore;

        if (!pvNode && !inCheck && TryPrune(depth, ply, beta, allowNull, out var prunedScore))
            return prunedScore;

        var list = new MoveList();
        MoveGenerator.Generate(board, ref list);

        if (list.Count == 0)
            return inCheck ? -MateScore + ply : 0; // checkmate (prefer shorter mates) or stalemate

        // At the root the previous best move is the principal move; elsewhere it is the TT move.
        MoveOrdering.ScoreMoves(board, ref list, isRoot ? _rootBest : ttMove, _heuristics, ply);

        return SearchMoves(ref list, depth, ply, alpha, beta, inCheck);
    }

    /// <summary>The alpha-beta move loop of an interior node; also stores the result in the transposition table.</summary>
    private int SearchMoves(ref MoveList list, int depth, int ply, int alpha, int beta, bool inCheck)
    {
        var originalAlpha = alpha;
        var bestScore = -Infinity;
        var bestMove = Move.Null;
        var side = board.SideToMove;

        for (var i = 0; i < list.Count; i++)
        {
            list.PickBest(i);
            var move = list[i];
            var quiet = !move.IsCapture && !move.IsPromotion;

            board.MakeMove(move);
            var score = SearchChild(depth, ply, alpha, beta, i, quiet && !inCheck);
            board.UnmakeMove(move);

            if (_stopped)
                return 0;

            if (score <= bestScore)
                continue;

            bestScore = score;
            bestMove = move;
            if (ply == 0)
                _rootBest = move;

            if (score <= alpha)
                continue;

            alpha = score;
            if (alpha < beta)
                continue;

            if (quiet)
                RewardQuietCutoff(side, ply, move, depth);
            break; // beta cutoff
        }

        tt.Store(board.Hash, bestMove, ScoreToTt(bestScore, ply), depth, ClassifyBound(bestScore, originalAlpha, beta));
        return bestScore;
    }

    /// <summary>
    /// Principal-variation search: the first move gets the full window, later moves a zero-width window
    /// (cheap to refute) that is only widened if the move turns out to be better than expected.
    /// Late quiet moves are additionally searched to a reduced depth first.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int SearchChild(int depth, int ply, int alpha, int beta, int moveIndex, bool reducible)
    {
        var childDepth = depth - 1;

        if (moveIndex == 0)
            return -Negamax(childDepth, ply + 1, -beta, -alpha, true);

        var reduction = 0;
        if (reducible && depth >= LmrMinDepth && moveIndex >= LmrMinMoveIndex)
            reduction = Lmr[Math.Min(depth, LmrTableSize - 1), Math.Min(moveIndex, LmrTableSize - 1)];

        var score = -Negamax(childDepth - reduction, ply + 1, -alpha - 1, -alpha, true);

        if (score > alpha && reduction > 0)
            score = -Negamax(childDepth, ply + 1, -alpha - 1, -alpha, true);

        if (score > alpha && score < beta)
            score = -Negamax(childDepth, ply + 1, -beta, -alpha, true);

        return score;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void RewardQuietCutoff(int side, int ply, Move move, int depth)
    {
        _heuristics.AddKiller(ply, move);
        _heuristics.UpdateHistory(side, move, depth);
    }

    /// <summary>
    /// Draws by rule, and mate-distance pruning: no line from here can beat a mate already found
    /// closer to the root, so the window is clamped to the best possible mate at this ply.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool TryTerminalScore(int ply, ref int alpha, ref int beta, out int score)
    {
        if (IsDrawByRule())
        {
            score = 0;
            return true;
        }

        alpha = Math.Max(alpha, -MateScore + ply);
        beta = Math.Min(beta, MateScore - ply - 1);
        score = alpha;
        return alpha >= beta;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool IsDrawByRule()
        => board.HalfmoveClock >= FiftyMoveRuleHalfmoves
           || board.CountRepetitions() >= RepetitionDrawCount
           || Evaluation.IsInsufficientMaterial(board);

    /// <summary>
    /// Looks the position up in the transposition table. Returns true if the stored result already decides
    /// this node; otherwise <paramref name="ttMove"/> still carries the stored best move for move ordering.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool TryProbeTt(int depth, int ply, int alpha, int beta, bool pvNode, out Move ttMove, out int score)
    {
        ttMove = Move.Null;
        score = 0;
        if (!tt.Probe(board.Hash, out var entry))
            return false;
        ttMove = entry.Move;
        return !pvNode && TryTtCutoff(entry, depth, ply, alpha, beta, out score);
    }
    /// <summary>Uses a stored result when it was searched at least as deep and its bound settles the window.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool TryTtCutoff(in TtEntry entry, int depth, int ply, int alpha, int beta, out int score)
    {
        score = 0;

        if (entry.Depth < depth)
            return false;

        score = ScoreFromTt(entry.Score, ply);

        return entry.Bound == Bound.Exact
               || (entry.Bound == Bound.Lower && score >= beta)
               || (entry.Bound == Bound.Upper && score <= alpha);
    }

    /// <summary>
    /// Reverse futility and null-move pruning: both give up on a node whose static evaluation is already
    /// so far above beta that the opponent is very unlikely to recover.
    /// </summary>
    private bool TryPrune(int depth, int ply, int beta, bool allowNull, out int score)
    {
        score = 0;

        if (Math.Abs(beta) >= MateBound)
            return false;

        var staticEval = Evaluation.Evaluate(board);

        if (depth <= ReverseFutilityMaxDepth && staticEval - ReverseFutilityMargin * depth >= beta)
        {
            score = staticEval;
            return true;
        }

        // Skipped with only pawns left, where passing the move (zugzwang) can genuinely be the worst option.
        return allowNull
               && depth >= NullMoveMinDepth
               && staticEval >= beta
               && board.HasNonPawnMaterial(board.SideToMove)
               && TryNullMove(depth, ply, beta, out score);
    }

    /// <summary>Lets the opponent move twice in a row; if they still can't get below beta, this node fails high.</summary>
    private bool TryNullMove(int depth, int ply, int beta, out int score)
    {
        var reducedDepth = depth - 1 - (NullMoveBaseReduction + depth / NullMoveDepthDivisor);

        board.MakeNullMove();
        var nullScore = -Negamax(reducedDepth, ply + 1, -beta, -beta + 1, false);
        board.UnmakeNullMove();

        if (_stopped)
        {
            score = 0;
            return true;
        }

        // Never trust a mate score from a null-move search: the "mate" may only exist because we passed.
        score = nullScore >= MateBound ? beta : nullScore;
        return nullScore >= beta;
    }

    /// <summary>
    /// Searches only tactical moves until the position is quiet, so the static evaluation
    /// is never taken in the middle of a capture sequence (the "horizon effect").
    /// </summary>
    private int Quiescence(int ply, int alpha, int beta)
    {
        if (ShouldStop())
            return 0;

        var standPat = Evaluation.Evaluate(board);
        if (ply >= MaxPly - 1 || standPat >= beta)
            return standPat;

        if (standPat > alpha)
            alpha = standPat;

        var list = new MoveList();
        MoveGenerator.Generate(board, ref list);

        MoveOrdering.ScoreMoves(board, ref list, Move.Null);

        for (var i = 0; i < list.Count; i++)
        {
            list.PickBest(i);

            if (list.GetScore(i) <= 0)
                break; // only quiet moves remain

            var move = list[i];
            if (IsSkippedInQuiescence(move, standPat, alpha))
                continue;

            board.MakeMove(move);
            var score = -Quiescence(ply + 1, -beta, -alpha);
            board.UnmakeMove(move);

            if (_stopped)
                return 0;

            if (score >= beta)
                return score;

            if (score > alpha)
                alpha = score;
        }

        return alpha;
    }

    /// <summary>Skips under-promotions, and (delta pruning) captures that cannot lift the score to alpha.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool IsSkippedInQuiescence(Move move, int standPat, int alpha)
    {
        if (move.IsPromotion)
            return move.PromotionPieceType != Piece.Queen;

        var victim = move.IsEnPassant ? Piece.Pawn : Piece.TypeOf(board.PieceAt(move.To));
        return standPat + Evaluation.PieceValue[victim] + DeltaMargin < alpha;
    }

    /// <summary>Counts the node and decides whether to stop; the clock is only read every <see cref="TimeCheckInterval"/> nodes.</summary>
    private bool ShouldStop()
    {
        if (_stopped)
            return true;

        _nodes++;

        if (_abort || ((_nodes & (TimeCheckInterval - 1)) == 0 && Stopwatch.GetTimestamp() >= _deadline))
            _stopped = true;

        return _stopped;
    }

    private static Bound ClassifyBound(int score, int originalAlpha, int beta)
        => score >= beta ? Bound.Lower
            : score > originalAlpha ? Bound.Exact
            : Bound.Upper;

    // Mate scores are relative to the root. In the table they must be relative to the stored node,
    // otherwise the same position found at a different ply would report the wrong mate distance.

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int ScoreToTt(int score, int ply)
        => score >= MateBound ? score + ply : score <= -MateBound ? score - ply : score;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int ScoreFromTt(int score, int ply)
        => score >= MateBound ? score - ply : score <= -MateBound ? score + ply : score;

    private static int[,] BuildLmr()
    {
        var table = new int[LmrTableSize, LmrTableSize];

        for (var depth = 1; depth < LmrTableSize; depth++)
            for (var move = 1; move < LmrTableSize; move++)
                table[depth, move] = (int)(LmrBase + Math.Log(depth) * Math.Log(move) / LmrDivisor);

        return table;
    }
}

