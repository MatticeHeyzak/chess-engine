namespace ChessEngine.Core.MoveGen;
/// <summary>
/// Compile-time description of a side. The move generator is instantiated once per colour
/// (<see cref="White"/> and <see cref="Black"/>) so every direction and rank is a constant
/// the JIT can fold, instead of a branch executed per piece.
/// </summary>
public interface IColor
{
    static abstract int Us { get; }
    /// <summary>Square-index delta of one step towards the opponent's side.</summary>
    static abstract int Up { get; }
    static abstract int UpRight { get; }
    static abstract int UpLeft { get; }
    /// <summary>First square (a-file) of this side's back rank.</summary>
    static abstract int Home { get; }
    static abstract ulong PromotionRank { get; }
    /// <summary>The rank a pawn lands on after a single push from its start rank.</summary>
    static abstract ulong ThirdRank { get; }
    static abstract int KingSideRight { get; }
    static abstract int QueenSideRight { get; }
    static abstract ulong ShiftUp(ulong bb);
    static abstract ulong ShiftUpRight(ulong bb);
    static abstract ulong ShiftUpLeft(ulong bb);
}
