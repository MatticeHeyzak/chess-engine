using System.Runtime.CompilerServices;

namespace ChessEngine.Core.MoveGen;

/// <summary>Fixed-size, stack-allocatable storage for the ordering scores of a <see cref="MoveList"/>.</summary>
[InlineArray(MoveList.Capacity)]
public struct ScoreBuffer
{
    private int _element0;
}

