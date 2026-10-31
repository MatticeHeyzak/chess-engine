using System.Runtime.CompilerServices;
using ChessEngine.Core.Main;

namespace ChessEngine.Core.BoardState;

/// <summary>
/// Zobrist hashing: every (piece, square), castling-rights value, en passant file and the side to move
/// gets a random 64-bit key, and a position's hash is the XOR of its keys. XOR makes the hash
/// updatable in O(1) per move. It identifies positions for repetition detection and the transposition table.
/// </summary>
public static class Zobrist
{
    /// <summary>Fixed seed so hashes are identical on every run and machine.</summary>
    public const ulong Seed = 0x9E3779B97F4A7C15UL;

    private static readonly ulong[] PieceKeys = new ulong[Piece.CodeCount * Square.Count];
    private static readonly ulong[] CastlingKeys = new ulong[CastlingRights.Count];
    private static readonly ulong[] EnPassantKeys = new ulong[Square.BoardSize];

    public static readonly ulong SideToMove;

    static Zobrist()
    {
        var rng = new XorShift64(Seed);

        Fill(PieceKeys, ref rng);
        Fill(CastlingKeys, ref rng);
        Fill(EnPassantKeys, ref rng);
        SideToMove = rng.Next();
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

    private static void Fill(Span<ulong> keys, ref XorShift64 rng)
    {
        for (var i = 0; i < keys.Length; i++)
            keys[i] = rng.Next();
    }
}

