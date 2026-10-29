using System.Diagnostics;
using ChessEngine.Core.BoardState;
using ChessEngine.Core.Main;
using ChessEngine.Core.MoveGen;

namespace ChessEngine.Core.Search;

public class Searcher(Board board)
{
    public const int Infinity = 32_000;
    public const int MateScore = 30_000;
    private const int MaxPly = 128;
    private const int TimeCheckInterval = 2048; // nodes between clock reads

    private long _deadline;
    private ulong _nodes;
    private bool _stopped;
    private volatile bool _abort;
    private Move _rootBest;
    
    public void Stop() => _abort = true;

    public SearchResult Search(SearchLimits limits)
    {
        _nodes = 0;
        _stopped = false;
        _abort = false;
        _rootBest = Move.Null;
        _deadline = Stopwatch.GetTimestamp() + limits.TimeMs * Stopwatch.Frequency / 1000;

        var best = Move.Null;
        var bestScore = 0;
        var completedDepth = 0;

        for (var depth = 1; depth <= limits.MaxDepth; depth++)
        {
            var score = Negamax(depth, 0, -Infinity, Infinity);

            if (_stopped)
                break; // the unfinished iteration is discarded
            
            best = _rootBest;
            bestScore = score;
            completedDepth = depth;

            if (Math.Abs(score) >= MateScore - MaxPly)
                break; // forced mate found, deeper search can't improve it
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

    private int Negamax(int depth, int ply, int alpha, int beta)
    {
        if (ShouldStop())
            return 0;

        if (ply > 0 && (board.HalfmoveClock >= 100 || board.CountRepetitions() >= 2))
            return 0; // draw by fifty-move rule or repetition

        var inCheck = MoveGenerator.IsInCheck(board);
        if (inCheck)
            depth++;

        if (ply >= MaxPly - 1)
            return Evaluation.Evaluate(board);

        if (depth <= 0)
            return Quiescence(ply, alpha, beta);

        var list = new MoveList();
        MoveGenerator.Generate(board, ref list);

        if (list.Count == 0)
            return inCheck ? -MateScore + ply : 0; // checkmate (prefer shorter mates) or stalemate

        MoveOrdering.ScoreMoves(board, ref list, ply == 0 ? _rootBest : Move.Null);
        
        var bestScore = -Infinity;

        for (var i = 0; i < list.Count; i++)
        {
            list.PickBest(i);
            var move = list[i];
            
            board.MakeMove(move);
            var score = -Negamax(depth - 1, ply + 1, -beta, -alpha);
            board.UnmakeMove(move);

            if (_stopped)
                return 0;

            if (score <= bestScore)
                continue;
            
            bestScore = score;
            if (ply == 0)
                _rootBest = move;

            if (score > alpha)
            {
                alpha = score;
                if (alpha >= beta)
                    break; // beta cutoff
            }
        }
        
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