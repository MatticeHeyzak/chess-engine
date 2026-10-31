using ChessEngine.Core.Main;

namespace ChessEngine.Core.MoveGen;

/// <summary>White's direction and rank constants for the colour-generic move generator.</summary>
public readonly struct White : IColor
{
    public static int Us => Piece.White;
    public static int Up => Square.BoardSize;
    public static int UpRight => Square.BoardSize + 1;
    public static int UpLeft => Square.BoardSize - 1;
    public static int Home => Square.A1;
    public static ulong PromotionRank => Bitboard.Rank8;
    public static ulong ThirdRank => Bitboard.Rank3;
    public static int KingSideRight => CastlingRights.WhiteKingSide;
    public static int QueenSideRight => CastlingRights.WhiteQueenSide;

    public static ulong ShiftUp(ulong bb) => Bitboard.North(bb);
    public static ulong ShiftUpRight(ulong bb) => Bitboard.NorthEast(bb);
    public static ulong ShiftUpLeft(ulong bb) => Bitboard.NorthWest(bb);
}

