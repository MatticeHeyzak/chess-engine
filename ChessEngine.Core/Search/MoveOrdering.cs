using ChessEngine.Core.BoardState;
using ChessEngine.Core.Main;
using ChessEngine.Core.MoveGen;

namespace ChessEngine.Core.Search;

public static class MoveOrdering
{
    private const int PrincipalMoveScore = 1_000_000;
    private const int CaptureBase = 100_000;
    private const int PromotionBase = 150_000;
    private const int KillerBase = 90_000;
    
    // Tactical moves score above 0, quiet moves score exactly 0.
    public static void ScoreMoves(Board board, ref MoveList list, Move principalMove, Heuristics? heuristic = null, int ply = 0)
    {
        var side = board.SideToMove;
        for (var i = 0; i < list.Count; i++)
        {
            var move = list[i];
            var score = 0;

            if (move.IsCapture)
            {
                var victim = move.IsEnPassant ? Piece.Pawn : Piece.TypeOf(board.PieceAt(move.To));
                var attacker = Piece.TypeOf(board.PieceAt(move.From));
                score = CaptureBase + 10 * victim - attacker;
            }

            if (move.IsPromotion)
                score += PromotionBase + 100 * move.PromotionPieceType;

            if (score == 0 && heuristic is not null)
            {
                var k = heuristic.KillerRank(ply, move);
                score = k > 0 ? KillerBase + k : heuristic.History(side, move); 
            }
            
            if (move == principalMove)
                score = PrincipalMoveScore;

            list.SetScore(i, score);
        }
    }
}