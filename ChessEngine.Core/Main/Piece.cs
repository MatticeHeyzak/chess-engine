using System.Runtime.CompilerServices;

namespace ChessEngine.Core.Main;

public static class Piece
{
    // Piece types
    public const int None = 0;
    public const int Pawn = 1;
    public const int Knight = 2;
    public const int Bishop = 3;
    public const int Rook = 4;
    public const int Queen = 5;
    public const int King = 6;

    // colors
    public const int White = 0;
    public const int Black = 1;
    
    // Encoding layout: bits 0-2 = type, bit 3 = color
    private const int ColorShift = 3;
    private const int TypeMask = 0b0111;
    
    // Combined piece codes
    public const int WhitePawn = Pawn | (White << ColorShift);
    public const int WhiteKnight = Knight | (White << ColorShift);
    public const int WhiteBishop = Bishop | (White << ColorShift);
    public const int WhiteRook = Rook | (White << ColorShift);
    public const int WhiteQueen = Queen | (White << ColorShift);
    public const int WhiteKing = King | (White << ColorShift);

    public const int BlackPawn = Pawn | (Black << ColorShift);
    public const int BlackKnight = Knight | (Black << ColorShift);
    public const int BlackBishop = Bishop | (Black << ColorShift);
    public const int BlackRook = Rook | (Black << ColorShift);
    public const int BlackQueen = Queen | (Black << ColorShift);
    public const int BlackKing = King | (Black << ColorShift);
    
    // a piece is a 4 bit number thus could have 16 values (even though some are unused)
    public const int CodeCount = 16;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int MakePiece(int color, int type)
        => type | (color << ColorShift);

    // Should not be used for piece types of None
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int ColorOf(int piece)
        => piece >> ColorShift;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int TypeOf(int piece)
        => piece & TypeMask;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsNone(int piece)
        => piece == None;
}