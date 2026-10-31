using ChessEngine.Core.BoardState;
using ChessEngine.Core.Search;

namespace ChessEngine.Test;

/// <summary>Behavioural checks that must hold whatever the search internals look like.</summary>
public class SearchTests
{
    private const int NoTimeLimitMs = int.MaxValue;

    [Fact]
    public void FindsMateInOne()
    {
        // Back-rank mate: Ra1-a8#.
        var board = Fen.Parse("6k1/5ppp/8/8/8/8/8/R5K1 w - - 0 1");

        var result = Search(board, depth: 4);

        Assert.Equal("a1a8", result.BestMove.ToString());
        Assert.True(result.Score >= Searcher.MateScore - Searcher.MaxPly);
    }

    [Fact]
    public void TakesAFreeQueen()
    {
        var board = Fen.Parse("4k3/8/8/3q4/8/8/8/3RK3 w - - 0 1");

        var result = Search(board, depth: 4);

        Assert.Equal("d1d5", result.BestMove.ToString());
    }

    [Fact]
    public void SearchLeavesTheBoardUnchanged()
    {
        var board = Fen.Parse("r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R w KQkq - 0 1");
        var before = Fen.Export(board);
        var hash = board.Hash;

        Search(board, depth: 5);

        Assert.Equal(before, Fen.Export(board));
        Assert.Equal(hash, board.Hash);
    }

    [Fact]
    public void SearchIsDeterministicAtFixedDepth()
    {
        const string fen = "r4rk1/1pp1qppp/p1np1n2/2b1p1B1/2B1P1b1/P1NP1N2/1PP1QPPP/R4RK1 w - - 0 10";

        var first = Search(Fen.Parse(fen), depth: 6);
        var second = Search(Fen.Parse(fen), depth: 6);

        Assert.Equal(first, second);
    }

    private static SearchResult Search(Board board, int depth)
        => new Searcher(board, new TranspositionTable(megabytes: 4))
            .Search(new SearchLimits(MaxDepth: depth, TimeMs: NoTimeLimitMs));
}

