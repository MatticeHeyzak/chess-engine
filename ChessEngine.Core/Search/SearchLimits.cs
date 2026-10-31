namespace ChessEngine.Core.Search;
/// <summary>Stop conditions for a search: whichever of depth or time is reached first.</summary>
public readonly record struct SearchLimits(
    int MaxDepth = SearchLimits.DefaultMaxDepth,
    int TimeMs = SearchLimits.DefaultTimeMs)
{
    public const int DefaultMaxDepth = 64;
    public const int DefaultTimeMs = 1000;
}
