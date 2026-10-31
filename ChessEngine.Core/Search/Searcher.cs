using System.Diagnostics;
using ChessEngine.Core.BoardState;
using ChessEngine.Core.Main;
using ChessEngine.Core.MoveGen;

namespace ChessEngine.Core.Search;

public class Searcher(Board board, TranspositionTable tt)
{
    public const int Infinity = 32_000;
    public const int MateScore = 30_000;
    public const int MaxPly = 128;
    private const int MateBound = MateScore - MaxPly;
    private const int TimeCheckInterval = 2048; // nodes between clock reads

    private static readonly int[,] Lmr = BuildLmr();
    private static int[,] BuildLmr()
    {
        var t = new int[64, 64];
        for (var d = 1; d < 64; d++)
        for (var m = 1; m < 64; m++)
            t[d, m] = (int)(0.75 + Math.Log(d) * Math.Log(m) / 2.25);
        return t;
    }

    private readonly Heuristics _heur = new();
    
    private long _deadline;
    private ulong _nodes;
    private bool _stopped;
    private volatile bool _abort;
    private Move _rootBest;
    
    private static int ToTt(int s, int ply) => s >= MateBound ? s + ply : s <= -MateBound ? s - ply : s;
    private static int FromTt(int s, int ply) => s >= MateBound ? s - ply : s <= -MateBound ? s + ply : s;
    public void Stop() => _abort = true;

    public SearchResult Search(SearchLimits limits)
    {
        _nodes = 0; _stopped = false; _abort = false; _rootBest = Move.Null;
        _deadline = Stopwatch.GetTimestamp() + limits.TimeMs * Stopwatch.Frequency / 1000;
        tt.NewSearch();

        var best = Move.Null;
        var bestScore = 0;
        var completedDepth = 0;

        for (var depth = 1; depth <= limits.MaxDepth; depth++)
        {
            int alpha = -Infinity, beta = Infinity, window = 40;
            if (depth >= 5)
            {
                alpha = Math.Max(bestScore - window, -Infinity);
                beta = Math.Min(bestScore + window, Infinity);
            }

            int score;
            while (true)
            {
                score = Negamax(depth, 0, alpha, beta, false);
                if (_stopped) break;

                if (score <= alpha)      { beta = (alpha + beta) / 2; alpha = Math.Max(score - window, -Infinity); }
                else if (score >= beta)  { beta = Math.Min(score + window, Infinity); }
                else break;
                window *= 2;
            }

            if (_stopped)
            {
                // The previous best move is searched first, so _rootBest is never worse than it.
                if (!_rootBest.IsNull) best = _rootBest;
                break;
            }

            best = _rootBest;
            bestScore = score;
            completedDepth = depth;

            if (Math.Abs(score) >= MateBound) break;
        }

        if (best.IsNull)
        {
            var list = new MoveList();
            MoveGenerator.Generate(board, ref list);
            if (list.Count > 0)
                best = list[0];
        }
        
        return new SearchResult(best, bestScore, completedDepth, _nodes);
    }

    private int Negamax(int depth, int ply, int alpha, int beta, bool allowNull)
    {
        if (ShouldStop()) return 0;

        var isRoot = ply == 0;
        var pvNode = beta - alpha > 1;

        if (!isRoot)
        {
            if (board.HalfmoveClock >= 100 || board.CountRepetitions() >= 2
                || Evaluation.IsInsufficientMaterial(board))
                return 0;

            // mate distance pruning
            alpha = Math.Max(alpha, -MateScore + ply);
            beta = Math.Min(beta, MateScore - ply - 1);
            if (alpha >= beta) return alpha;
        }

        var inCheck = MoveGenerator.IsInCheck(board);
        if (inCheck) depth++;

        if (ply >= MaxPly - 1) return Evaluation.Evaluate(board);
        if (depth <= 0) return Quiescence(ply, alpha, beta);

        // Transposition table
        var ttMove = Move.Null;
        if (tt.Probe(board.Hash, out var e))
        {
            ttMove = e.Move;
            if (!pvNode && e.Depth >= depth)
            {
                var s = FromTt(e.Score, ply);
                if (e.Bound == Bound.Exact
                    || (e.Bound == Bound.Lower && s >= beta)
                    || (e.Bound == Bound.Upper && s <= alpha))
                    return s;
            }
        }

        var staticEval = inCheck ? -Infinity : Evaluation.Evaluate(board);

        if (!pvNode && !inCheck && Math.Abs(beta) < MateBound)
        {
            // reverse futility pruning
            if (depth <= 6 && staticEval - 100 * depth >= beta)
                return staticEval;

            // null-move pruning
            if (allowNull && depth >= 3 && staticEval >= beta && board.HasNonPawnMaterial(board.SideToMove))
            {
                board.MakeNullMove();
                var ns = -Negamax(depth - 1 - (2 + depth / 3), ply + 1, -beta, -beta + 1, false);
                board.UnmakeNullMove();
                if (_stopped) return 0;
                if (ns >= beta) return ns >= MateBound ? beta : ns;
            }
        }

        var list = new MoveList();
        MoveGenerator.Generate(board, ref list);
        if (list.Count == 0) return inCheck ? -MateScore + ply : 0;

        // At the root the previous best move is the principal move; elsewhere it is the TT move.
        MoveOrdering.ScoreMoves(board, ref list, isRoot ? _rootBest : ttMove, _heur, ply);

        var origAlpha = alpha;
        var bestScore = -Infinity;
        var bestMove = Move.Null;
        var side = board.SideToMove;

        for (var i = 0; i < list.Count; i++)
        {
            list.PickBest(i);
            var move = list[i];
            var quiet = !move.IsCapture && !move.IsPromotion;

            board.MakeMove(move);

            int score;
            var newDepth = depth - 1;
            if (i == 0)
            {
                score = -Negamax(newDepth, ply + 1, -beta, -alpha, true);
            }
            else
            {
                var reduction = 0;
                if (depth >= 3 && i >= 3 && quiet && !inCheck)
                    reduction = Lmr[Math.Min(depth, 63), Math.Min(i, 63)];

                score = -Negamax(newDepth - reduction, ply + 1, -alpha - 1, -alpha, true);
                if (score > alpha && reduction > 0)
                    score = -Negamax(newDepth, ply + 1, -alpha - 1, -alpha, true);
                if (score > alpha && score < beta)
                    score = -Negamax(newDepth, ply + 1, -beta, -alpha, true);
            }

            board.UnmakeMove(move);
            if (_stopped) return 0;

            if (score <= bestScore) continue;

            bestScore = score;
            bestMove = move;
            if (isRoot) _rootBest = move;

            if (score > alpha)
            {
                alpha = score;
                if (alpha >= beta)
                {
                    if (quiet)
                    {
                        _heur.AddKiller(ply, move);
                        _heur.UpdateHistory(side, move, depth);
                    }
                    break;
                }
            }
        }

        var bound = bestScore >= beta ? Bound.Lower : bestScore > origAlpha ? Bound.Exact : Bound.Upper;
        tt.Store(board.Hash, bestMove, ToTt(bestScore, ply), depth, bound);

        return bestScore;
    }
    
    // Searches only tactical moves until the position is quiet, so the static evaluation
    // is never taken in the middle of a capture sequence (the "horizon effect").
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
            if (move.IsPromotion && move.PromotionPieceType != Piece.Queen)
                continue;
            
            if (!move.IsPromotion)
            {
                var victim = move.IsEnPassant ? Piece.Pawn : Piece.TypeOf(board.PieceAt(move.To));
                if (standPat + Evaluation.PieceValue[victim] + 200 < alpha)
                    continue;
            }

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

    private bool ShouldStop()
    {
        if (_stopped)
            return true;

        _nodes++;

        if (_abort || ((_nodes & (TimeCheckInterval - 1)) == 0 && Stopwatch.GetTimestamp() >= _deadline))
            _stopped = true;

        return _stopped;
    }
}