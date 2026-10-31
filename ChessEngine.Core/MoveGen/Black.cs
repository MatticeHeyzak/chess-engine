using ChessEngine.Core.Main;

namespace ChessEngine.Core.MoveGen;

/// <summary>Black's direction and rank constants for the colour-generic move generator.</summary>
public readonly struct Black : IColor
{
    public static int Us => Piece.Black;
    public static int Up => -Square.BoardSize;
    public static int UpRight => -(Square.BoardSize - 1);
    public static int UpLeft => -(Square.BoardSize + 1);
    public static int Home => Square.A8;
    public static ulong PromotionRank => Bitboard.Rank1;
    public static ulong ThirdRank => Bitboard.Rank6;
    public static int KingSideRight => CastlingRights.BlackKingSide;
    public static int QueenSideRight => CastlingRights.BlackQueenSide;

    public static ulong ShiftUp(ulong bb) => Bitboard.South(bb);
    public static ulong ShiftUpRight(ulong bb) => Bitboard.SouthEast(bb);
    public static ulong ShiftUpLeft(ulong bb) => Bitboard.SouthWest(bb);
}

