using System.Numerics;
using ChessEngine.Core.Main;

namespace ChessEngine.Core.Search;

/// <summary>
/// Hash table of previously searched positions, indexed by the low bits of the Zobrist key
/// (always-replace with a small depth/age guard). It lets the search reuse work across transpositions
/// and across iterative-deepening iterations, and supplies a best move for ordering.
/// </summary>
public sealed class TranspositionTable
{
    /// <summary>Size of one <see cref="TtEntry"/>; the table size is derived from it.</summary>
    public const int EntrySizeBytes = 16;

    private const long BytesPerMegabyte = 1024L * 1024L;

    /// <summary>A same-search entry this much deeper than the new one is not overwritten (unless the new one is exact).</summary>
    private const int DepthPreservationMargin = 2;

    private readonly TtEntry[] _entries;
    private readonly ulong _mask;

    /// <summary>Incremented per search so stale entries from earlier moves can be told apart.</summary>
    public byte Age { get; private set; }

    /// <summary>Creates a table of about <paramref name="megabytes"/> MB (rounded up to a power-of-two entry count).</summary>
    public TranspositionTable(int megabytes = 64)
    {
        var count = BitOperations.RoundUpToPowerOf2((uint)(megabytes * BytesPerMegabyte / EntrySizeBytes));
        _entries = new TtEntry[count];
        _mask = count - 1;
    }

    public void NewSearch() => Age++;

    public void Clear()
    {
        Array.Clear(_entries);
        Age = 0;
    }

    /// <summary>Copies the slot for <paramref name="key"/> into <paramref name="entry"/>; true if it holds that position.</summary>
    public bool Probe(ulong key, out TtEntry entry)
    {
        entry = _entries[key & _mask];
        return entry.Bound != Bound.None && entry.Key == key;
    }

    public void Store(ulong key, Move move, int score, int depth, Bound bound)
    {
        ref var entry = ref _entries[key & _mask];
        var sameKey = entry.Key == key;

        // Keep a clearly deeper entry from the same search for this position.
        if (sameKey && entry.Age == Age && entry.Depth > depth + DepthPreservationMargin && bound != Bound.Exact)
            return;

        // Keep the old move if this search found none (e.g. fail-low nodes).
        entry.Move = move.IsNull && sameKey ? entry.Move : move;
        entry.Key = key;
        entry.Score = (short)score;
        entry.Depth = (short)depth;
        entry.Bound = bound;
        entry.Age = Age;
    }
}

