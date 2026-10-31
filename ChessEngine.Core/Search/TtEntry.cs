using ChessEngine.Core.Main;

namespace ChessEngine.Core.Search;

/// <summary>
/// One transposition-table slot. The layout is kept at exactly 16 bytes
/// (<see cref="TranspositionTable.EntrySizeBytes"/>) so slots never straddle a cache line.
/// </summary>
public struct TtEntry
{
    public ulong Key;
    public Move Move;
    public short Score;
    public short Depth;
    public Bound Bound;
    public byte Age;
}

