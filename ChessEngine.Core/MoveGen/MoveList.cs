using System.Runtime.CompilerServices;
using ChessEngine.Core.Main;

namespace ChessEngine.Core.MoveGen;

// 218 is the highest known legal-move count in any position; 256 is the nearest power of 2.
[InlineArray(256)]
public struct MoveBuffer
{
    private Move _element0;
}

[InlineArray(256)]
public struct ScoreBuffer
{
    private int _element0;
}

// Lives on the stack: zero allocations per node
public ref struct MoveList
{
    private MoveBuffer _moves;
    private ScoreBuffer _scores;
    
    public int Count { get; private set; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add(Move move) => _moves[Count++] = move;

    public Move this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _moves[index];
    }

    public int GetScore(int index) => _scores[index];

    public void SetScore(int index, int score) => _scores[index] = score;
    
    // Moves the best-scoring remainng move to position `start`.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PickBest(int start)
    {
        var best = start;

        for (var i = start + 1; i < Count; i++)
        {
            if (_scores[i] > _scores[best])
                best = i;
        }

        if (best == start)
            return;
        
        (_moves[start], _moves[best]) = (_moves[best], _moves[start]);
        (_scores[start], _scores[best]) = (_scores[best], _scores[start]);
    }
}