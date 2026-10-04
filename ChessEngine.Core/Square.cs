using System.Runtime.CompilerServices;

namespace ChessEngine.Core;

public static class Square
{
    // a1 = 0, h1 = 7, a8 = 56, h8 = 63  ->  index = rank * 8 + file
    public const int A1 = 0,  B1 = 1,  C1 = 2,  D1 = 3,  E1 = 4,  F1 = 5,  G1 = 6,  H1 = 7;
    public const int A2 = 8,  B2 = 9,  C2 = 10, D2 = 11, E2 = 12, F2 = 13, G2 = 14, H2 = 15;
    public const int A3 = 16, B3 = 17, C3 = 18, D3 = 19, E3 = 20, F3 = 21, G3 = 22, H3 = 23;
    public const int A4 = 24, B4 = 25, C4 = 26, D4 = 27, E4 = 28, F4 = 29, G4 = 30, H4 = 31;
    public const int A5 = 32, B5 = 33, C5 = 34, D5 = 35, E5 = 36, F5 = 37, G5 = 38, H5 = 39;
    public const int A6 = 40, B6 = 41, C6 = 42, D6 = 43, E6 = 44, F6 = 45, G6 = 46, H6 = 47;
    public const int A7 = 48, B7 = 49, C7 = 50, D7 = 51, E7 = 52, F7 = 53, G7 = 54, H7 = 55;
    public const int A8 = 56, B8 = 57, C8 = 58, D8 = 59, E8 = 60, F8 = 61, G8 = 62, H8 = 63;

    public const int Count = 64;
    
    // Sentinel for "no square" (e.g. no en passent target)
    public const int None = -1;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Make(int file, int rank)
        => rank * 8 + file;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int FileOf(int square)
        => square & 7;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int RankOf(int square)
        => square >> 3;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsValid(int square)
        => (uint)square < Count;

    public static string ToName(int square)
    {
        if (!IsValid(square))
            return "-";
        
        return $"{(char)('a' + FileOf(square))}{(char)('1' + RankOf(square))}";
    }

    public static int Parse(string name)
    {
        if (name.Length != 2)
            return None;

        var file = name[0] - 'a';
        var rank = name[1] - '1';

        if ((uint)file > 7 || (uint)rank > 7)
            return None;

        return Make(file, rank);
    }
}