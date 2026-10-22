using ChessEngine.Core.Main;

namespace ChessEngine.Core.MoveGen;

public interface IColor
{
    static abstract int Us { get; }
    static abstract int Up { get; }
    static abstract int UpRight { get; }
    static abstract int UpLeft { get; }
    static abstract int Home { get; }
    static abstract ulong PromotionRank { get; }
    static abstract ulong ThirdRank { get; }
    static abstract int KingSideRight { get; }
    static abstract int QueenSideRight { get; }

    static abstract ulong ShiftUp(ulong bb);
    static abstract ulong ShiftUpRight(ulong bb);
    static abstract ulong ShiftUpLeft(ulong bb);
}

public readonly struct White : IColor
{
    public static int Us => Piece.White;
    public static int Up => 8;
    public static int UpRight => 9;
    public static int UpLeft => 7;
    public static int Home => Square.A1;
    public static ulong PromotionRank => Bitboard.Rank8;
    public static ulong ThirdRank => Bitboard.Rank3;
    public static int KingSideRight => CastlingRights.WhiteKingSide;
    public static int QueenSideRight => CastlingRights.WhiteQueenSide;

    public static ulong ShiftUp(ulong bb) => Bitboard.North(bb);
    public static ulong ShiftUpRight(ulong bb) => Bitboard.NorthEast(bb);
    public static ulong ShiftUpLeft(ulong bb) => Bitboard.NorthWest(bb);
}

public readonly struct Black : IColor
{
    public static int Us => Piece.Black;
    public static int Up => -8;
    public static int UpRight => -7;
    public static int UpLeft => -9;
    public static int Home => Square.A8;
    public static ulong PromotionRank => Bitboard.Rank1;
    public static ulong ThirdRank => Bitboard.Rank6;
    public static int KingSideRight => CastlingRights.BlackKingSide;
    public static int QueenSideRight => CastlingRights.BlackQueenSide;

    public static ulong ShiftUp(ulong bb) => Bitboard.South(bb);
    public static ulong ShiftUpRight(ulong bb) => Bitboard.SouthEast(bb);
    public static ulong ShiftUpLeft(ulong bb) => Bitboard.SouthWest(bb);
}
