using System.Runtime.CompilerServices;
using ChessEngine.Core.Main;

namespace ChessEngine.Core.MoveGen;

public static class Attacks
{
    private static readonly (int Df, int Dr)[] KnightDeltas =
        [(1, 2), (2, 1), (2, -1), (1, -2), (-1, -2), (-2, -1), (-2, 1), (-1, 2)];
    
    private static readonly (int Df, int Dr)[] KingDeltas =
        [(1, 0), (1, 1), (0, 1), (-1, 1), (-1, 0), (-1, -1), (0, -1), (1, -1)];

    private static readonly ulong[] KnightTable = BuildLeaperTable(KnightDeltas);
    private static readonly ulong[] KingTable = BuildLeaperTable(KingDeltas);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong Knight(int square) => KnightTable[square];
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong King(int square) => KingTable[square];
    
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
}