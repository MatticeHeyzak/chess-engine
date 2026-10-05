using System.Runtime.CompilerServices;
using System.Text;

namespace ChessEngine.Core;

public static class CastlingRights
{
    public const int None = 0;
    public const int WhiteKingSide = 1; // 0001
    public const int WhiteQueenSide = 2; // 0010
    public const int BlackKingSide = 4; // 0100
    public const int BlackQueenSide = 8; // 1000
    public const int All = WhiteKingSide | WhiteQueenSide | BlackKingSide | BlackQueenSide; // 1111

    public const int Count = 16; // Size for arrays indexed by the rights value (e.g. Zobrist keys)
    
    // For each square: the mask to AND the rights with when a piece moves from or to it.
    // Most squares keep everything (All). Only the king and rook home squares remove rights.
    private static readonly int[] UpdateMask = BuildUpdateMask();
    
    private static int[] BuildUpdateMask()
    {
        var mask = new int[Square.Count];

        // White
        mask[Square.E1] = All & ~(WhiteKingSide | WhiteQueenSide);
        mask[Square.A1] = All & ~WhiteQueenSide;
        mask[Square.H1] = All & ~WhiteKingSide;
        
        // Black
        mask[Square.E8] = All & ~(BlackKingSide | BlackQueenSide);
        mask[Square.A8] = All & ~BlackQueenSide;
        mask[Square.H8] = All & ~BlackKingSide;
        
        return mask;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Update(int rights, int from, int to)
        => rights & UpdateMask[from] & UpdateMask[to];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Has(int rights, int right)
        => (rights & right) != 0;

    // Parses the FEN castling field: "KQkq", "Kq", "-", ...
    public static int Parse(string field)
    {
        var rights = None;

        foreach (var c in field)
        {
            rights |= c switch
            {
                'K' => WhiteKingSide,
                'Q' => WhiteQueenSide,
                'k' => BlackKingSide,
                'q' => BlackQueenSide,
                _ => None
            };
        }

        return rights;
    }
    
    // Produces the FEN castling field. Always in the order K, Q, k, q, or "-" for none
    public static string ToFen(int rights)
    {
        if (rights == None)
            return "-";

        var sb = new StringBuilder(4);
        if (Has(rights, WhiteKingSide)) sb.Append('K');
        if (Has(rights, WhiteQueenSide)) sb.Append('Q');
        if (Has(rights, BlackKingSide)) sb.Append('k');
        if (Has(rights, BlackQueenSide)) sb.Append('q');

        return sb.ToString();
    }
}