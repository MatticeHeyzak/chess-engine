using System.Numerics;
using ChessEngine.Core.Main;

namespace ChessEngine.Core.Search;

public enum Bound : byte
{
    None,
    Exact,
    Lower,
    Upper
}

public struct TtEntry
{
    public ulong Key;
    public Move Move;
    public short Score;
    public short Depth;
    public Bound Bound;
    public byte Age;
}

public sealed class TranspositionTable
{
    private readonly TtEntry[] _entries;
    private readonly ulong _mask;
    public byte Age { get; private set; }

    public TranspositionTable(int megabytes = 64)
    {
        var count = BitOperations.RoundUpToPowerOf2((uint)(megabytes * 1024L * 1024L / 16));
        _entries = new TtEntry[count];
        _mask = count - 1;
    }

    public void NewSearch() => Age++;

    public void Clear()
    {
        Array.Clear(_entries);
        Age = 0;
    }

    public bool Probe(ulong key, out TtEntry entry)
    {
        entry = _entries[key & _mask];
        return entry.Bound != Bound.None && entry.Key == key;
    }

    public void Store(ulong key, Move move, int score, int depth, Bound bound)
    {
        ref var e = ref _entries[key & _mask];
        var sameKey = e.Key == key;
        
        // Keep a clearly deeper entry from the same search for this position.
        if (sameKey && e.Age == Age && e.Depth > depth + 2 && bound != Bound.Exact)
            return;

        e.Move = move.IsNull && sameKey ? e.Move : move;
        e.Key = key;
        e.Score = (short)score;
        e.Depth = (short)depth;
        e.Bound = bound;
        e.Age = Age;
    }
}