using System.Runtime.CompilerServices;
using ChessEngine.Core.Main;
namespace ChessEngine.Core.MoveGen;
/// <summary>
/// The legal moves of one position plus their ordering scores.
/// It is a ref struct so both buffers live on the stack: zero allocations per node.
/// </summary>
public ref struct MoveList
{
    /// <summary>218 is the highest known legal-move count in any position; 256 is the nearest power of 2.</summary>
    public const int Capacity = 256;
    private MoveBuffer _moves;
    private ScoreBuffer _scores;
    public int Count { get; private set; }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add(Move move) => _moves[Count++] = move;
    public readonly Move this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _moves[index];
    }
    public readonly int GetScore(int index) => _scores[index];
    public void SetScore(int index, int score) => _scores[index] = score;
    /// <summary>
    /// Moves the best-scoring remaining move to position <paramref name="start"/>.
    /// A lazy selection sort: most nodes cut off after a few moves, so a full sort would be wasted work.
    /// </summary>
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
