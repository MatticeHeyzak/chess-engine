using System.Runtime.CompilerServices;
using System.Numerics;
using System.Text;

namespace ChessEngine.Core;

public static class Bitboard
{
    // Files
    public const ulong FileA = 0x0101010101010101;
    public const ulong FileB = FileA << 1;
    public const ulong FileC = FileB << 1;
    public const ulong FileD = FileC << 1;
    public const ulong FileE = FileD << 1;
    public const ulong FileF = FileE << 1;
    public const ulong FileG = FileF << 1;
    public const ulong FileH = FileG << 1;
    
    public static ulong[] Files = [
        FileA, FileB, FileC, FileD, FileE, FileF, FileG, FileH
    ];

    // Ranks
    public const ulong Rank1 = 0xFF;
    public const ulong Rank2 = Rank1 << 8;
    public const ulong Rank3 = Rank2 << 8;
    public const ulong Rank4 = Rank3 << 8;
    public const ulong Rank5 = Rank4 << 8;
    public const ulong Rank6 = Rank5 << 8;
    public const ulong Rank7 = Rank6 << 8;
    public const ulong Rank8 = Rank7 << 8;
    
    public static ulong[] Ranks = [
        Rank1, Rank2, Rank3, Rank4, Rank5, Rank6, Rank7, Rank8
    ];

    // Wraparound masks
    public const ulong NotFileA = ~FileA;
    public const ulong NotFileH = ~FileH;
    public const ulong NotFileAB = ~(FileA | FileB);
    public const ulong NotFileGH = ~(FileG | FileH);

    // Misc
    public const ulong Empty = 0UL;
    public const ulong Full = ulong.MaxValue;
    
    // a1 is a dark square, so bit 0 is set here.
    public const ulong DarkSquares = 0xAA55AA55AA55AA55;
    public const ulong LightSquares = ~DarkSquares;
    
    // Single-square helpers
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong FromSquare(int sq)
        => 1UL << sq;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsSet(ulong bb, int sq)
        => (FromSquare(sq) & bb) != Empty;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong Set(ulong bb, int sq)
        => bb | FromSquare(sq);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong Clear(ulong bb, int sq)
        => bb & ~FromSquare(sq);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong Toggle(ulong bb, int sq)
        => bb ^ FromSquare(sq);
    
    // Scanning and counting
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsEmpty(ulong bb)
        => bb == Empty;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsNotEmpty(ulong bb)
        => bb != Empty;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int PopCount(ulong bb)
        => BitOperations.PopCount(bb);
    
    // Undefined for bb == 0 (returns 64). Always check for non-empty first.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int LeastSignificantBit(ulong bb)
        => BitOperations.TrailingZeroCount(bb);
    
    // Returns the index of the lowest set bit and removes it from bb.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int PopLeastSignificantBit(ref ulong bb)
    {
        var sq = BitOperations.TrailingZeroCount(bb);
        bb &= bb - 1;
        return sq;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool HasMoreThanOneBit(ulong bb)
        => (bb & (bb - 1)) != Empty;
    
    // Directional shifts (edge-safe)
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong North(ulong bb) => bb << 8;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong South(ulong bb) => bb >> 8;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong East(ulong bb) => (bb << 1) & NotFileA;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong West(ulong bb) => (bb >> 1) & NotFileH;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong NorthEast(ulong bb) => (bb << 9) & NotFileA;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong NorthWest(ulong bb) => (bb << 7) & NotFileH;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong SouthEast(ulong bb) => (bb >> 7) & NotFileA;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong SouthWest(ulong bb) => (bb >> 9) & NotFileH;
    
    // Debugging
    // Rank 8 at the top, file A on the left, like a normal board diagram.
    public static string ToDebugString(ulong bb)
    {
        var sb = new StringBuilder();

        for (var rank = 7; rank >= 0; rank--)
        {
            sb.Append(rank + 1).Append("  ");
            for (var file = 0; file < 8; file++)
            {
                var sq = rank * 8 + file;
                sb.Append(IsSet(bb, sq) ? '1' : '.').Append(' ');
            }
            sb.AppendLine();
        }

        sb.AppendLine();
        sb.AppendLine("   a b c d e f g h");
        sb.Append("   0x").Append(bb.ToString("X16"));

        return sb.ToString();
    }
}