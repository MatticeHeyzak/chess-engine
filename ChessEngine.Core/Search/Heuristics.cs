using ChessEngine.Core.Main;

namespace ChessEngine.Core.Search;

/// <summary>
/// Learned move-ordering data for quiet moves: killer moves (quiet moves that recently caused a
/// beta cutoff at the same ply) and a history table (how often a from/to pair has caused cutoffs).
/// </summary>
public sealed class Heuristics
{
    /// <summary>History scores stay below this, so they can never outrank killers or captures.</summary>
    public const int MaxHistory = 16_384;

    private const int KillersPerPly = 2;
    private const int SideCount = 2;
    private const int MaxHistoryBonus = 400;

    private readonly Move[] _killers = new Move[Searcher.MaxPly * KillersPerPly];
    private readonly int[] _history = new int[SideCount * Square.Count * Square.Count];

    /// <summary>2 for the newest killer at <paramref name="ply"/>, 1 for the older one, 0 if the move is not a killer.</summary>
    public int KillerRank(int ply, Move move)
        => move == _killers[ply * KillersPerPly] ? 2
            : move == _killers[ply * KillersPerPly + 1] ? 1
            : 0;

    public void AddKiller(int ply, Move move)
    {
        if (_killers[ply * KillersPerPly] == move)
            return;

        _killers[ply * KillersPerPly + 1] = _killers[ply * KillersPerPly];
        _killers[ply * KillersPerPly] = move;
    }

    public int History(int side, Move move) => _history[Index(side, move)];

    public void UpdateHistory(int side, Move move, int depth)
    {
        var bonus = Math.Min(depth * depth, MaxHistoryBonus);
        ref var entry = ref _history[Index(side, move)];

        // The "gravity" term shrinks the bonus as the entry fills up, which keeps it below MaxHistory.
        entry += bonus - entry * bonus / MaxHistory;
    }

    private static int Index(int side, Move move)
        => (side * Square.Count + move.From) * Square.Count + move.To;
}

