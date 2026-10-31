namespace ChessEngine.Core.Main;

/// <summary>
/// The 4-bit move flags stored in the top of a <see cref="Move"/>.
/// Bit 2 marks a capture and bit 3 marks a promotion; the low two bits of a promotion
/// flag select the piece (knight, bishop, rook, queen).
/// </summary>
public static class MoveFlag
{
    public const int Quiet = 0;
    public const int DoublePawnPush = 1;
    public const int KingCastle = 2;
    public const int QueenCastle = 3;
    public const int Capture = 4;
    public const int EnPassant = 5;

    public const int PromoKnight = 8;
    public const int PromoBishop = 9;
    public const int PromoRook = 10;
    public const int PromoQueen = 11;

    public const int PromoKnightCapture = 12;
    public const int PromoBishopCapture = 13;
    public const int PromoRookCapture = 14;
    public const int PromoQueenCapture = 15;
}

