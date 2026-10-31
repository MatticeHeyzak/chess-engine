using System.Runtime.CompilerServices;
using ChessEngine.Core.Main;

namespace ChessEngine.Core.MoveGen;

/// <summary>
/// Precomputed attack sets. Knights, kings and pawns use plain lookup tables; rooks and bishops use
/// magic bitboards (a multiply-and-shift perfect hash of the blockers), which answers "what does a slider
/// on this square attack, given this occupancy?" in a few instructions.
/// </summary>
public static class Attacks
{
    // Sizes follow from the masks: sum over squares of 2^(relevant bits).
    private const int RookTableSize = 102400;
    private const int BishopTableSize = 5248;

    private const int SquareBits = 6; // log2(Square.Count): lets (a << SquareBits) + b index a square-pair table

    private const ulong RookMagicSeed = 0x1234ABCD5678EF01UL;
    private const ulong BishopMagicSeed = 0x0FEDCBA987654321UL;

    private static readonly (int Df, int Dr)[] KnightDeltas =
        [(1, 2), (2, 1), (2, -1), (1, -2), (-1, -2), (-2, -1), (-2, 1), (-1, 2)];

    private static readonly (int Df, int Dr)[] KingDeltas =
        [(1, 0), (1, 1), (0, 1), (-1, 1), (-1, 0), (-1, -1), (0, -1), (1, -1)];

    private static readonly (int Df, int Dr)[] RookDirs = [(1, 0), (-1, 0), (0, 1), (0, -1)];
    private static readonly (int Df, int Dr)[] BishopDirs = [(1, 1), (1, -1), (-1, 1), (-1, -1)];

    private static readonly ulong[] KnightTable = BuildLeaperTable(KnightDeltas);
    private static readonly ulong[] KingTable = BuildLeaperTable(KingDeltas);
    private static readonly ulong[] PawnTable = BuildPawnTable();
    private static readonly ulong[] BetweenTable = new ulong[Square.Count * Square.Count];
    private static readonly ulong[] LineTable = new ulong[Square.Count * Square.Count];

    private static readonly Magic[] RookMagics = new Magic[Square.Count];
    private static readonly Magic[] BishopMagics = new Magic[Square.Count];
    private static readonly ulong[] RookTable = new ulong[RookTableSize];
    private static readonly ulong[] BishopTable = new ulong[BishopTableSize];

    static Attacks()
    {
        new MagicGenerator(RookDirs, new XorShift64(RookMagicSeed)).Generate(RookMagics, RookTable);
        new MagicGenerator(BishopDirs, new XorShift64(BishopMagicSeed)).Generate(BishopMagics, BishopTable);

        BuildBetweenAndLine(RookDirs);
        BuildBetweenAndLine(BishopDirs);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong Knight(int square) => KnightTable[square];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong King(int square) => KingTable[square];

    /// <summary>Squares attacked by a pawn of <paramref name="color"/> standing on <paramref name="square"/>.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong Pawn(int color, int square) => PawnTable[(color << SquareBits) + square];

    /// <summary>Squares strictly between a and b, or 0 if they are not on a shared rank, file or diagonal.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong Between(int a, int b) => BetweenTable[(a << SquareBits) + b];

    /// <summary>The full edge-to-edge line through a and b (including both), or 0 if not aligned.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong Line(int a, int b) => LineTable[(a << SquareBits) + b];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong Rook(int square, ulong occupancy)
    {
        ref readonly var m = ref RookMagics[square];
        return RookTable[m.Offset + (int)(((occupancy & m.Mask) * m.Number) >> m.Shift)];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong Bishop(int square, ulong occupancy)
    {
        ref readonly var m = ref BishopMagics[square];
        return BishopTable[m.Offset + (int)(((occupancy & m.Mask) * m.Number) >> m.Shift)];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong Queen(int square, ulong occupancy)
        => Rook(square, occupancy) | Bishop(square, occupancy);

    // Reference implementations that walk the rays; they validate the magic tables in tests.
    internal static ulong SlowRook(int square, ulong occupancy) => Slide(square, occupancy, RookDirs);
    internal static ulong SlowBishop(int square, ulong occupancy) => Slide(square, occupancy, BishopDirs);

    /// <summary>Attacks of a ray-walking piece: each ray runs until the board edge or the first blocker (included).</summary>
    internal static ulong Slide(int square, ulong occupancy, (int Df, int Dr)[] dirs)
    {
        ulong result = 0;

        foreach (var (df, dr) in dirs)
        {
            var f = Square.FileOf(square) + df;
            var r = Square.RankOf(square) + dr;

            while ((uint)f < Square.BoardSize && (uint)r < Square.BoardSize)
            {
                var bit = Bitboard.FromSquare(Square.Make(f, r));
                result |= bit;

                if ((occupancy & bit) != 0)
                    break;

                f += df;
                r += dr;
            }
        }

        return result;
    }

    private static ulong[] BuildLeaperTable((int Df, int Dr)[] deltas)
    {
        var table = new ulong[Square.Count];

        for (var square = 0; square < Square.Count; square++)
        {
            var file = Square.FileOf(square);
            var rank = Square.RankOf(square);

            foreach (var (df, dr) in deltas)
            {
                var f = file + df;
                var r = rank + dr;

                if ((uint)f < Square.BoardSize && (uint)r < Square.BoardSize)
                    table[square] |= Bitboard.FromSquare(Square.Make(f, r));
            }
        }

        return table;
    }

    private static ulong[] BuildPawnTable()
    {
        var table = new ulong[Square.Count * 2];
        BuildLeaperTable([(-1, 1), (1, 1)]).CopyTo(table, Piece.White * Square.Count);
        BuildLeaperTable([(-1, -1), (1, -1)]).CopyTo(table, Piece.Black * Square.Count);
        return table;
    }

    private static void BuildBetweenAndLine((int Df, int Dr)[] dirs)
    {
        for (var a = 0; a < Square.Count; a++)
            for (var b = 0; b < Square.Count; b++)
            {
                var bitA = Bitboard.FromSquare(a);
                var bitB = Bitboard.FromSquare(b);
                var raysA = Slide(a, 0, dirs);

                if (a == b || (raysA & bitB) == 0)
                    continue;

                BetweenTable[(a << SquareBits) + b] = Slide(a, bitB, dirs) & Slide(b, bitA, dirs);
                LineTable[(a << SquareBits) + b] = (raysA & Slide(b, 0, dirs)) | bitA | bitB;
            }
    }
}

