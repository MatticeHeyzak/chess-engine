using System.Runtime.CompilerServices;

namespace ChessEngine.Core.Main;

/// <summary>
/// Piece types, colours and the packed piece code (<c>type | color &lt;&lt; 3</c>).
/// The code doubles as an array index (see <see cref="CodeCount"/>), which keeps lookups branch-free.
/// </summary>
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

    // Colors
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

    /// <summary>A piece code is a 4-bit number, so arrays indexed by it have 16 slots (some unused).</summary>
    public const int CodeCount = 16;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int MakePiece(int color, int type)
        => type | (color << ColorShift);

    /// <summary>Colour of a piece code. Meaningless for <see cref="None"/>.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int ColorOf(int piece)
        => piece >> ColorShift;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int TypeOf(int piece)
        => piece & TypeMask;
}

