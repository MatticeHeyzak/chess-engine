using System.Runtime.CompilerServices;

namespace ChessEngine.Core;

public static class MoveFlag
{
    public const int Quiet = 0;
    public const int DoublePawnPush = 1;
    public const int KingCastle = 2;
    public const int QueenCastle = 3;
    public const int Capture = 4;
    public const int EnPassant = 5;

    public const int PromoKnight = 8;
    public const int PromoBishop = 9;
    public const int PromoRook = 10;
    public const int PromoQueen = 11;

    public const int PromoKnightCapture = 12;
    public const int PromoBishopCapture = 13;
    public const int PromoRookCapture = 14;
    public const int PromoQueenCapture = 15;
}

// Layout of the 16 bits:
//    bits 0-5 -> from square (0-63)
//    bits 6-11 -> to square (0-63)
//    bits 12-15 flags
public readonly struct Move : IEquatable<Move>
{
    private const int SquareMask = 0x3F;
    private const int ToShift = 6;
    private const int FlagsShift = 12;

    private const int CaptureBit = 4;
    private const int PromotionBit = 8;
    private const int PromotionTypeMask = 3;
    
    private readonly ushort _data;

    // The "no move" sentinal. All zeros: a1 -> a1 quiet, which is never legal.
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
    
    // Piece TYPE (not code) of the promotion piece. Only valid if IsPromotion.
    public int PromotionPieceType
        => Piece.Knight + (Flags & PromotionTypeMask);

    public override string ToString()
    {
        if (IsNull)
            return "0000";

        var text = Square.ToName(From) + Square.ToName(To);

        if (IsPromotion)
        {
            text += PromotionPieceType switch
            {
                Piece.Knight => 'n',
                Piece.Bishop => 'b',
                Piece.Rook => 'r',
                _ => 'q'
            };
        }

        return text;
    }
    public bool Equals(Move other) => _data == other._data;

    public override bool Equals(object? obj) => obj is Move other && Equals(other);

    public override int GetHashCode() => _data.GetHashCode();

    public static bool operator ==(Move left, Move right) => left._data == right._data;

    public static bool operator !=(Move left, Move right) => left._data != right._data;
}