using ChessEngine.Core.BoardState;
using ChessEngine.Core.Main;
using ChessEngine.Core.MoveGen;

namespace ChessEngine.Core.Search;

/// <summary>
/// Assigns each move an ordering score so alpha-beta tries the most promising moves first,
/// which is what makes cutoffs happen early. Higher scores are searched first.
/// </summary>
public static class MoveOrdering
{
    private const int PrincipalMoveScore = 1_000_000;
    private const int PromotionBase = 150_000;
    private const int CaptureBase = 100_000;

    /// <summary>Above every history score (see <see cref="Heuristics.MaxHistory"/>) and below every capture.</summary>
    private const int KillerBase = 90_000;

    // MVV-LVA: prefer the most valuable victim, then the least valuable attacker.
    private const int VictimWeight = 10;
    private const int PromotionPieceWeight = 100;

    /// <summary>
    /// Scores every move in <paramref name="list"/>. The principal move (PV / hash move) goes first,
    /// then tactical moves, then killers and history.
    /// Without <paramref name="heuristics"/> quiet moves score exactly 0, which quiescence search relies on
    /// to recognise where the tactical moves end.
    /// </summary>
    public static void ScoreMoves(
        Board board, ref MoveList list, Move principalMove, Heuristics? heuristics = null, int ply = 0)
    {
        var side = board.SideToMove;

        for (var i = 0; i < list.Count; i++)
        {
            var move = list[i];
            var score = TacticalScore(board, move);

            if (score == 0 && heuristics is not null)
                score = QuietScore(heuristics, side, ply, move);

            if (move == principalMove)
                score = PrincipalMoveScore;

            list.SetScore(i, score);
        }
    }

    private static int TacticalScore(Board board, Move move)
    {
        var score = 0;

        if (move.IsCapture)
        {
            var victim = move.IsEnPassant ? Piece.Pawn : Piece.TypeOf(board.PieceAt(move.To));
            var attacker = Piece.TypeOf(board.PieceAt(move.From));
            score = CaptureBase + VictimWeight * victim - attacker;
        }

        if (move.IsPromotion)
            score += PromotionBase + PromotionPieceWeight * move.PromotionPieceType;

        return score;
    }

    private static int QuietScore(Heuristics heuristics, int side, int ply, Move move)
    {
        var killerRank = heuristics.KillerRank(ply, move);
        return killerRank > 0 ? KillerBase + killerRank : heuristics.History(side, move);
    }
}

