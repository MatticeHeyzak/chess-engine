using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace ChessEngine.Core;

// Hashes a single board state to detect threefold repetition
public static class Zobrist
{
    private static readonly ulong[] PieceKeys = new ulong[Piece.CodeCount * Square.Count];
    private static readonly ulong[] CastlingKeys = new ulong[CastlingRights.Count];
    private static readonly ulong[] EnPassentKeys = new ulong[8];

    public static readonly ulong SideToMove;
    
    static Zobrist()
    {
        FillRandom(PieceKeys);
        FillRandom(CastlingKeys);
        FillRandom(EnPassentKeys);

        Span<ulong> single = stackalloc ulong[1];
        FillRandom(single);
        SideToMove = single[0];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong PieceSquare(int piece, int square)
        => PieceKeys[piece * Square.Count + square];
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong Castling(int rights)
        => CastlingKeys[rights];
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong EnPassantFile(int file)
        => EnPassentKeys[file];
    
    private static void FillRandom(Span<ulong> keys)
        => RandomNumberGenerator.Fill(MemoryMarshal.AsBytes(keys));
}