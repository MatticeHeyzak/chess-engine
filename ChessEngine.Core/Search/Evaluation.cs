using System.Numerics;
using ChessEngine.Core.BoardState;
using ChessEngine.Core.Main;

namespace ChessEngine.Core.Search;

public static class Evaluation
{
    private const int TotalPhase = 24;

    public static readonly int[] PieceValue = [0, 100, 320, 330, 500, 900, 0];
    private static readonly int[] PhaseWeight = [0, 0, 1, 1, 2, 4, 0];
    
    // Indexed [pieceCode * 64 + square]. Material is already included.
    private static readonly int[] Middlegame = new int[Piece.CodeCount * Square.Count];
    private static readonly int[] Endgame = new int[Piece.CodeCount * Square.Count];

    private static readonly int[] Pawn =
    [
        0,  0,  0,  0,  0,  0,  0,  0,
        50, 50, 50, 50, 50, 50, 50, 50,
        10, 10, 20, 30, 30, 20, 10, 10,
        5,  5, 10, 25, 25, 10,  5,  5,
        0,  0,  0, 20, 20,  0,  0,  0,
        5, -5,-10,  0,  0,-10, -5,  5,
        5, 10, 10,-20,-20, 10, 10,  5,
        0,  0,  0,  0,  0,  0,  0,  0
    ];
    
        private static readonly int[] Knight =
    [
        -50,-40,-30,-30,-30,-30,-40,-50,
        -40,-20,  0,  0,  0,  0,-20,-40,
        -30,  0, 10, 15, 15, 10,  0,-30,
        -30,  5, 15, 20, 20, 15,  5,-30,
        -30,  0, 15, 20, 20, 15,  0,-30,
        -30,  5, 10, 15, 15, 10,  5,-30,
        -40,-20,  0,  5,  5,  0,-20,-40,
        -50,-40,-30,-30,-30,-30,-40,-50
    ];

    private static readonly int[] Bishop =
    [
        -20,-10,-10,-10,-10,-10,-10,-20,
        -10,  0,  0,  0,  0,  0,  0,-10,
        -10,  0,  5, 10, 10,  5,  0,-10,
        -10,  5,  5, 10, 10,  5,  5,-10,
        -10,  0, 10, 10, 10, 10,  0,-10,
        -10, 10, 10, 10, 10, 10, 10,-10,
        -10,  5,  0,  0,  0,  0,  5,-10,
        -20,-10,-10,-10,-10,-10,-10,-20
    ];

    private static readonly int[] Rook =
    [
          0,  0,  0,  0,  0,  0,  0,  0,
          5, 10, 10, 10, 10, 10, 10,  5,
         -5,  0,  0,  0,  0,  0,  0, -5,
         -5,  0,  0,  0,  0,  0,  0, -5,
         -5,  0,  0,  0,  0,  0,  0, -5,
         -5,  0,  0,  0,  0,  0,  0, -5,
         -5,  0,  0,  0,  0,  0,  0, -5,
          0,  0,  0,  5,  5,  0,  0,  0
    ];

    private static readonly int[] Queen =
    [
        -20,-10,-10, -5, -5,-10,-10,-20,
        -10,  0,  0,  0,  0,  0,  0,-10,
        -10,  0,  5,  5,  5,  5,  0,-10,
         -5,  0,  5,  5,  5,  5,  0, -5,
          0,  0,  5,  5,  5,  5,  0, -5,
        -10,  5,  5,  5,  5,  5,  0,-10,
        -10,  0,  5,  0,  0,  0,  0,-10,
        -20,-10,-10, -5, -5,-10,-10,-20
    ];

    private static readonly int[] KingMiddlegame =
    [
        -30,-40,-40,-50,-50,-40,-40,-30,
        -30,-40,-40,-50,-50,-40,-40,-30,
        -30,-40,-40,-50,-50,-40,-40,-30,
        -30,-40,-40,-50,-50,-40,-40,-30,
        -20,-30,-30,-40,-40,-30,-30,-20,
        -10,-20,-20,-20,-20,-20,-20,-10,
         20, 20,  0,  0,  0,  0, 20, 20,
         20, 30, 10,  0,  0, 10, 30, 20
    ];

    private static readonly int[] KingEndgame =
    [
        -50,-40,-30,-20,-20,-30,-40,-50,
        -30,-20,-10,  0,  0,-10,-20,-30,
        -30,-10, 20, 30, 30, 20,-10,-30,
        -30,-10, 30, 40, 40, 30,-10,-30,
        -30,-10, 30, 40, 40, 30,-10,-30,
        -30,-10, 20, 30, 30, 20,-10,-30,
        -30,-30,  0,  0,  0,  0,-30,-30,
        -50,-30,-30,-30,-30,-30,-30,-50
    ];

    static Evaluation()
    {
        int[][] middle = [[], Pawn, Knight, Bishop, Rook, Queen, KingMiddlegame];
        int[][] end = [[], Pawn, Knight, Bishop, Rook, Queen, KingEndgame];
        
        for (var type = Piece.Pawn; type <= Piece.King; type++)
        for (var square = 0; square < Square.Count; square++)
        {
            var white = Piece.MakePiece(Piece.White, type) * Square.Count + square;
            var black = Piece.MakePiece(Piece.Black, type) * Square.Count + square;
            
            Middlegame[white] = PieceValue[type] + middle[type][square ^ 56];
            Endgame[white] = PieceValue[type] + end[type][square ^ 56];
            Middlegame[black] = PieceValue[type] + middle[type][square];
            Endgame[black] = PieceValue[type] + end[type][square];
        }
    }

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
        
        phase = Math.Min(phase, TotalPhase); // promotions can push it over
        var score = (middlegame * phase + endgame * (TotalPhase - phase)) / TotalPhase;
        
        if (BitOperations.PopCount(board.GetBitboard(Piece.White, Piece.Bishop)) >= 2)
            score += 30;
        if (BitOperations.PopCount(board.GetBitboard(Piece.Black, Piece.Bishop)) >= 2)
            score -= 30;
        score += board.SideToMove == Piece.White ? 10 : -10;
        
        return board.SideToMove == Piece.White ? score : -score;
    }
    
    public static bool IsInsufficientMaterial(Board b)
    {
        var heavy = b.GetBitboard(Piece.White, Piece.Pawn) | b.GetBitboard(Piece.Black, Piece.Pawn)
                       | b.GetBitboard(Piece.White, Piece.Rook) | b.GetBitboard(Piece.Black, Piece.Rook)
                       | b.GetBitboard(Piece.White, Piece.Queen) | b.GetBitboard(Piece.Black, Piece.Queen);
        
        if (heavy != 0)
            return false;

        var minors = b.GetBitboard(Piece.White, Piece.Knight) | b.GetBitboard(Piece.Black, Piece.Knight)
                        | b.GetBitboard(Piece.White, Piece.Bishop) | b.GetBitboard(Piece.Black, Piece.Bishop);
        return System.Numerics.BitOperations.PopCount(minors) <= 1;
    }
}