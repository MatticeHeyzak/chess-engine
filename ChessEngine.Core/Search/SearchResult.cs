using ChessEngine.Core.Main;

namespace ChessEngine.Core.Search;

/// <summary>Outcome of a search: the best move, its score (centipawns, side-to-move relative) and statistics.</summary>
public readonly record struct SearchResult(Move BestMove, int Score, int Depth, ulong Nodes);

