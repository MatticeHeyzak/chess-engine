using System.Numerics;
using System.Runtime.CompilerServices;
using ChessEngine.Core.Main;

namespace ChessEngine.Core.MoveGen;

public static class Attacks
{
    private struct Magic
    {
        public ulong Mask; 
        public ulong Number;
        public int Shift;
        public int Offset;
    }

    // Sizes follow from the masks: sum over squares of 2^(relevent bits).
    private const int RookTableSize = 102400;
    private const int BishopTableSize = 5248;
    
    private static readonly (int Df, int Dr)[] KnightDeltas =
        [(1, 2), (2, 1), (2, -1), (1, -2), (-1, -2), (-2, -1), (-2, 1), (-1, 2)];
    
    private static readonly (int Df, int Dr)[] KingDeltas =
        [(1, 0), (1, 1), (0, 1), (-1, 1), (-1, 0), (-1, -1), (0, -1), (1, -1)];
    
    private static readonly (int Df, int Dr)[] RookDirs = [(1, 0), (-1, 0), (0, 1), (0, -1)];
    private static readonly (int Df, int Dr)[] BishopDirs = [(1, 1), (1, -1), (-1, 1), (-1, -1)];

    private static readonly ulong[] KnightTable = BuildLeaperTable(KnightDeltas);
    private static readonly ulong[] KingTable = BuildLeaperTable(KingDeltas);

    private static readonly Magic[] RookMagics = new Magic[Square.Count];
    private static readonly Magic[] BishopMagics = new Magic[Square.Count];
    private static readonly ulong[] RookTable = new ulong[RookTableSize];
    private static readonly ulong[] BishopTable = new ulong[BishopTableSize];

    static Attacks()
    {
        InitMagics(RookMagics, RookTable, RookDirs, new XorShift64(0x1234ABCD5678EF01UL));
        InitMagics(BishopMagics, BishopTable, BishopDirs, new XorShift64(0x0FEDCBA987654321UL));
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong Knight(int square) => KnightTable[square];
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong King(int square) => KingTable[square];

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

    internal static ulong SlowRook(int square, ulong occupancy) => Slide(square, occupancy, RookDirs);
    internal static ulong SlowBishop(int square, ulong occupancy) => Slide(square, occupancy, BishopDirs);

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

                if ((uint)f < 8 && (uint)r < 8)
                    table[square] |= Bitboard.FromSquare(Square.Make(f, r));
            }
        }

        return table;
    }

    private static ulong Slide(int square, ulong occupancy, (int Df, int Dr)[] dirs)
    {
        ulong result = 0;

        foreach (var (df, dr) in dirs)
        {
            var f = Square.FileOf(square) + df;
            var r = Square.RankOf(square) + dr;

            while ((uint)f < 8 && (uint)r < 8)
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

    // Finds a magic number per squaere by trial. Every blocker subset must map to a slot
    // that either is unused or already holds the same attack set (harmless collision)
    private static void InitMagics(Magic[] magics, ulong[] table, (int Df, int Dr)[] dirs, XorShift64 rng)
    {
        var occupancies = new ulong[4096];
        var references = new ulong[4096];
        var used = new ulong[4096];
        var epoch = new int[4096]; // avoids clearing `used` between attempts
        var attempt = 0;
        var offset = 0;

        for (var square = 0; square < Square.Count; square++)
        {
            // Edge squares never change the attack set, so they are not relevant blockers.
            var edges = ((Bitboard.Rank1 | Bitboard.Rank8) & ~Bitboard.Ranks[Square.RankOf(square)])
                        | ((Bitboard.FileA | Bitboard.FileH) & ~Bitboard.Files[Square.FileOf(square)]);
            var mask = Slide(square, 0, dirs) & ~edges;
            var bits = BitOperations.PopCount(mask);
            var shift = 64 - bits;
            
            // Enumerate every subset of the mask (carry-rippler trick)
            var count = 0;
            ulong subset = 0;
            do
            {
                occupancies[count] = subset;
                references[count] = Slide(square, subset, dirs);
                count++;
                subset = (subset - mask) & mask;
            } while (subset != 0);

            while (true)
            {
                var magic = rng.NextSparse();
                
                // Cheap rejection: good magics spread the mask bits into the top byte
                if (BitOperations.PopCount((mask * magic) >> 56) < 6)
                    continue;

                attempt++;
                var valid = true;

                for (var i = 0; i < count && valid; i++)
                {
                    var index = (int)((occupancies[i] * magic) >> shift);

                    if (epoch[index] != attempt)
                    {
                        epoch[index] = attempt;
                        used[index] = references[i];
                    }
                    else
                    {
                        valid = used[index] == references[i];
                    }
                }

                if (!valid)
                    continue;
                
                magics[square] = new Magic { Mask = mask, Number = magic, Shift = shift, Offset = offset };

                for (var i = 0; i < count; i++)
                    table[offset + (int)((occupancies[i] * magic) >> shift)] = references[i];

                offset += 1 << bits;
                break;
            }
        }
    }
}