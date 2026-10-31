using ChessEngine.Core.BoardState;
using ChessEngine.Core.Main;

namespace ChessEngine.Core.Search;

/// <summary>
/// Static evaluation: material plus piece-square bonuses, blended between a middlegame and an
/// endgame table by how much material is left (a "tapered" evaluation), then a few small bonuses.
/// </summary>
public static class Evaluation
{
    /// <summary>Sum of <see cref="PhaseWeight"/> over the starting position; the phase of a full board.</summary>
    private const int TotalPhase = 24;

    private const int BishopPairBonus = 30;
    private const int TempoBonus = 10;

    /// <summary>XOR with this mirrors a square vertically (a1 &lt;-&gt; a8).</summary>
    private const int FlipRank = Square.Count - Square.BoardSize;

    public static readonly int[] PieceValue = [0, 100, 320, 330, 500, 900, 0];
    private static readonly int[] PhaseWeight = [0, 0, 1, 1, 2, 4, 0];

    // Indexed [pieceCode * 64 + square]. Material is already included.
    private static readonly int[] Middlegame = new int[Piece.CodeCount * Square.Count];
    private static readonly int[] Endgame = new int[Piece.CodeCount * Square.Count];

    static Evaluation()
    {
        int[][] middle =
        [
            [], PieceSquareTables.Pawn, PieceSquareTables.Knight, PieceSquareTables.Bishop,
            PieceSquareTables.Rook, PieceSquareTables.Queen, PieceSquareTables.KingMiddlegame
        ];

        int[][] end =
        [
            [], PieceSquareTables.Pawn, PieceSquareTables.Knight, PieceSquareTables.Bishop,
            PieceSquareTables.Rook, PieceSquareTables.Queen, PieceSquareTables.KingEndgame
        ];

        for (var type = Piece.Pawn; type <= Piece.King; type++)
            for (var square = 0; square < Square.Count; square++)
            {
                var white = Piece.MakePiece(Piece.White, type) * Square.Count + square;
                var black = Piece.MakePiece(Piece.Black, type) * Square.Count + square;

                // The tables are written from White's view (rank 8 first), so White flips and Black does not.
                Middlegame[white] = PieceValue[type] + middle[type][square ^ FlipRank];
                Endgame[white] = PieceValue[type] + end[type][square ^ FlipRank];
                Middlegame[black] = PieceValue[type] + middle[type][square];
                Endgame[black] = PieceValue[type] + end[type][square];
            }
    }

    /// <summary>
    /// Score in centipawns from the point of view of the side to move (what negamax needs).
    /// Called at every leaf, so it only does table lookups and popcounts.
    /// </summary>
    public static int Evaluate(Board board)
    {
        int middlegame = 0, endgame = 0, phase = 0;

        for (var color = Piece.White; color <= Piece.Black; color++)
        {
            var sign = color == Piece.White ? 1 : -1;

            for (var type = Piece.Pawn; type <= Piece.King; type++)
            {
                var pieces = board.GetBitboard(color, type);
                var offset = Piece.MakePiece(color, type) * Square.Count;

                while (pieces != 0)
                {
                    var square = Bitboard.PopLeastSignificantBit(ref pieces);
                    middlegame += sign * Middlegame[offset + square];
                    endgame += sign * Endgame[offset + square];
                    phase += PhaseWeight[type];
                }
            }
        }

        var whiteToMove = board.SideToMove == Piece.White;
        var score = Taper(middlegame, endgame, phase)
                    + BishopPairScore(board)
                    + (whiteToMove ? TempoBonus : -TempoBonus);

        return whiteToMove ? score : -score;
    }

    /// <summary>
    /// True when neither side can possibly deliver mate: bare kings, or a single minor piece.
    /// Checked at every interior node, so it must stay cheap.
    /// </summary>
    public static bool IsInsufficientMaterial(Board board)
    {
        var pawnsAndMajors = BothSides(board, Piece.Pawn)
                             | BothSides(board, Piece.Rook)
                             | BothSides(board, Piece.Queen);

        if (pawnsAndMajors != 0)
            return false;

        var minors = BothSides(board, Piece.Knight) | BothSides(board, Piece.Bishop);
        return Bitboard.PopCount(minors) <= 1;
    }

    private static ulong BothSides(Board board, int type)
        => board.GetBitboard(Piece.White, type) | board.GetBitboard(Piece.Black, type);

    /// <summary>Blends the two scores by game phase. Promotions can push the phase above the total, hence the clamp.</summary>
    private static int Taper(int middlegame, int endgame, int phase)
    {
        phase = Math.Min(phase, TotalPhase);
        return (middlegame * phase + endgame * (TotalPhase - phase)) / TotalPhase;
    }

    /// <summary>White-relative bonus for owning both bishops.</summary>
    private static int BishopPairScore(Board board)
    {
        var score = 0;

        if (HasBishopPair(board, Piece.White))
            score += BishopPairBonus;
        if (HasBishopPair(board, Piece.Black))
            score -= BishopPairBonus;

        return score;
    }

    private static bool HasBishopPair(Board board, int color)
        => Bitboard.PopCount(board.GetBitboard(color, Piece.Bishop)) >= 2;
}

