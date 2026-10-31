namespace ChessEngine.Core.Search;

/// <summary>How a stored transposition-table score relates to the true score of the position.</summary>
public enum Bound : byte
{
    /// <summary>The slot is empty.</summary>
    None,

    /// <summary>The score is exact (it fell inside the search window).</summary>
    Exact,

    /// <summary>The score is a lower bound (the search failed high).</summary>
    Lower,

    /// <summary>The score is an upper bound (the search failed low).</summary>
    Upper
}

