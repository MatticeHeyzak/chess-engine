using ChessEngine.Core.Main;

namespace ChessEngine.Core.Search;

public readonly record struct SearchLimits(int MaxDepth = 64, int TimeMs = 1000);

public readonly record struct SearchResult(Move BestMove, int Score, int Depth, ulong Nodes);
