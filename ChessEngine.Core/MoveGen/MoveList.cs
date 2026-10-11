using System.Runtime.CompilerServices;
using ChessEngine.Core.Main;

namespace ChessEngine.Core.MoveGen;

// 218 is the highest known legal-move count in any position; 256 is the nearest power of 2.
[InlineArray(256)]
public struct MoveBuffer
{
    private Move _element0;
}

// Lives on the stack: zero allocations per node
public ref struct MoveList
{
    private MoveBuffer _buffer;
    
    public int Count { get; private set; }

    public void Add(Move move) => _buffer[Count++] = move;

    public Move this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _buffer[index];
    }
}