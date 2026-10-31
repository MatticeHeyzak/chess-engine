using ChessEngine.Core.Main;

namespace ChessEngine.Core.Search;

public sealed class Heuristics
{
    private const int MaxHistory = 16_384;
    private readonly Move[] _killers = new Move[Searcher.MaxPly * 2];
    private readonly int[] _history = new int[2 * 64 * 64];

    public int KillerRank(int ply, Move m)
        => m == _killers[ply * 2] ? 2 : m == _killers[ply * 2 + 1] ? 1 : 0;

    public void AddKiller(int ply, Move m)
    {
        if (_killers[ply * 2] == m)
            return;

        _killers[ply * 2 + 1] = _killers[ply * 2];
        _killers[ply * 2] = m;
    }

    private static int Index(int side, Move m) => (side * 64 + m.From) * 64 + m.To;
    public int History(int side, Move m) => _history[Index(side, m)];

    public void UpdateHistory(int side, Move m, int depth)
    {
        var bonus = Math.Min(depth * depth, 400);
        ref var h = ref _history[Index(side, m)];
        h += bonus - h * bonus / MaxHistory;
    }
}