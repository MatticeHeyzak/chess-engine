using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;

namespace ChessEngine.Core.Main;

/// <summary>
/// 64-bit set-of-squares helpers (bit <c>n</c> is square <c>n</c>, see <see cref="Square"/>).
/// Everything here is on the move-generation hot path, so the members are tiny and force-inlined.
/// </summary>
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

    public static readonly ulong[] Files =
    [
        FileA, FileB, FileC, FileD, FileE, FileF, FileG, FileH
    ];

    // Ranks
    public const ulong Rank1 = 0xFF;
    public const ulong Rank2 = Rank1 << Square.BoardSize;
    public const ulong Rank3 = Rank2 << Square.BoardSize;
    public const ulong Rank4 = Rank3 << Square.BoardSize;
    public const ulong Rank5 = Rank4 << Square.BoardSize;
    public const ulong Rank6 = Rank5 << Square.BoardSize;
    public const ulong Rank7 = Rank6 << Square.BoardSize;
    public const ulong Rank8 = Rank7 << Square.BoardSize;

    public static readonly ulong[] Ranks =
    [
        Rank1, Rank2, Rank3, Rank4, Rank5, Rank6, Rank7, Rank8
    ];

    // Masks that stop a shift from wrapping around the board edge
    public const ulong NotFileA = ~FileA;
    public const ulong NotFileH = ~FileH;

    public const ulong Empty = 0UL;
    public const ulong Full = ulong.MaxValue;

    // Bit shifts for one step in each direction
    private const int NorthStep = Square.BoardSize;
    private const int NorthEastStep = Square.BoardSize + 1;
    private const int NorthWestStep = Square.BoardSize - 1;

    // Single-square helpers

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong FromSquare(int sq)
        => 1UL << sq;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsSet(ulong bb, int sq)
        => (FromSquare(sq) & bb) != Empty;

    // Scanning and counting

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int PopCount(ulong bb)
        => BitOperations.PopCount(bb);

    /// <summary>Index of the lowest set bit. Undefined for an empty board (returns 64): check for non-empty first.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int LeastSignificantBit(ulong bb)
        => BitOperations.TrailingZeroCount(bb);

    /// <summary>Returns the index of the lowest set bit and removes it from <paramref name="bb"/>.</summary>
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
    public static ulong North(ulong bb) => bb << NorthStep;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong South(ulong bb) => bb >> NorthStep;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong NorthEast(ulong bb) => (bb << NorthEastStep) & NotFileA;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong NorthWest(ulong bb) => (bb << NorthWestStep) & NotFileH;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong SouthEast(ulong bb) => (bb >> NorthWestStep) & NotFileA;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong SouthWest(ulong bb) => (bb >> NorthEastStep) & NotFileH;

    /// <summary>
    /// Board-diagram rendering for debugging: rank 8 at the top, file A on the left.
    /// </summary>
    public static string ToDebugString(ulong bb)
    {
        var sb = new StringBuilder();

        for (var rank = Square.MaxIndex; rank >= 0; rank--)
        {
            sb.Append(rank + 1).Append("  ");

            for (var file = 0; file <= Square.MaxIndex; file++)
                sb.Append(IsSet(bb, Square.Make(file, rank)) ? '1' : '.').Append(' ');

            sb.AppendLine();
        }

        sb.AppendLine();
        sb.AppendLine("   a b c d e f g h");
        sb.Append("   0x").Append(bb.ToString("X16"));

        return sb.ToString();
    }
}

