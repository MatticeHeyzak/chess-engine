using System.Runtime.CompilerServices;
using ChessEngine.Core.Main;

namespace ChessEngine.Core.MoveGen;

/// <summary>Fixed-size, stack-allocatable storage for the moves of a <see cref="MoveList"/>.</summary>
[InlineArray(MoveList.Capacity)]
public struct MoveBuffer
{
    private Move _element0;
}

