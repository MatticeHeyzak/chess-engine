using System.Runtime.CompilerServices;
using ChessEngine.Core.Main;

namespace ChessEngine.Core.BoardState;

// Hashes a single board state to detect threefold repetition
public static class Zobrist
{
    /// <summary>Fixed seed so hashes are identical on every run and machine.</summary>
    public const ulong Seed = 0x9E3779B97F4A7C15UL;   
    
    private static readonly ulong[] PieceKeys = new ulong[Piece.CodeCount * Square.Count];
    private static readonly ulong[] CastlingKeys = new ulong[CastlingRights.Count];
    private static readonly ulong[] EnPassantKeys = new ulong[8];

    public static readonly ulong SideToMove;
    
    static Zobrist()
    {
        var state = Seed;

        Fill(PieceKeys, ref state);
        Fill(CastlingKeys, ref state);
        Fill(EnPassantKeys, ref state);
        SideToMove = Next(ref state);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong PieceSquare(int piece, int square)
        => PieceKeys[piece * Square.Count + square];
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong Castling(int rights)
        => CastlingKeys[rights];
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong EnPassantFile(int file)
        => EnPassantKeys[file];

    private static void Fill(Span<ulong> keys, ref ulong state)
    {
        for (var i = 0; i < keys.Length; i++)
            keys[i] = Next(ref state);
    }
    
    /// <summary>SplitMix64 (Steele, Lea, Flood).</summary>
    private static ulong Next(ref ulong state)
    {
        var z = state += 0x9E3779B97F4A7C15UL;
        z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
        z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
        return z ^ (z >> 31);
    }
}