using System.Runtime.CompilerServices;

namespace ChessEngine.Core.Main;

/// <summary>
/// A move packed into 16 bits:
/// <list type="bullet">
/// <item>bits 0-5: from square (0-63)</item>
/// <item>bits 6-11: to square (0-63)</item>
/// <item>bits 12-15: <see cref="MoveFlag"/></item>
/// </list>
/// The compact size keeps move lists and transposition-table entries small.
/// </summary>
public readonly struct Move : IEquatable<Move>
{
    private const int SquareMask = 0x3F;
    private const int ToShift = 6;
    private const int FlagsShift = 12;

    private const int CaptureBit = 4;
    private const int PromotionBit = 8;
    private const int PromotionTypeMask = 3;

    private readonly ushort _data;

    /// <summary>The "no move" sentinel. All zeros: a1 -> a1 quiet, which is never legal.</summary>
    public static readonly Move Null = default;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Move(int from, int to, int flags = MoveFlag.Quiet)
    {
        _data = (ushort)(from | (to << ToShift) | (flags << FlagsShift));
    }

    public int From
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _data & SquareMask;
    }

    public int To
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (_data >> ToShift) & SquareMask;
    }

    public int Flags
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _data >> FlagsShift;
    }

    public bool IsNull => _data == 0;

    public bool IsCapture => (Flags & CaptureBit) != 0;
    public bool IsPromotion => (Flags & PromotionBit) != 0;
    public bool IsEnPassant => Flags == MoveFlag.EnPassant;
    public bool IsDoublePawnPush => Flags == MoveFlag.DoublePawnPush;
    public bool IsKingCastle => Flags == MoveFlag.KingCastle;
    public bool IsCastle => Flags is MoveFlag.KingCastle or MoveFlag.QueenCastle;

    /// <summary>Piece TYPE (not code) of the promotion piece. Only valid if <see cref="IsPromotion"/>.</summary>
    public int PromotionPieceType
        => Piece.Knight + (Flags & PromotionTypeMask);

    /// <summary>Long algebraic (UCI) notation, e.g. "e2e4" or "e7e8q".</summary>
    public override string ToString()
    {
        if (IsNull)
            return "0000";

        var text = Square.ToName(From) + Square.ToName(To);

        if (IsPromotion)
            text += PromotionLetter(PromotionPieceType);

        return text;
    }

    private static char PromotionLetter(int pieceType) => pieceType switch
    {
        Piece.Knight => 'n',
        Piece.Bishop => 'b',
        Piece.Rook => 'r',
        _ => 'q'
    };

    public bool Equals(Move other) => _data == other._data;

    public override bool Equals(object? obj) => obj is Move other && Equals(other);

    public override int GetHashCode() => _data.GetHashCode();

    public static bool operator ==(Move left, Move right) => left._data == right._data;

    public static bool operator !=(Move left, Move right) => left._data != right._data;
}

