using ChessEngine.Core.BoardState;
using ChessEngine.Core.MoveGen;

namespace ChessEngine.Test;

public class LegalityTests
{
    private const string Start = Fen.StartPosition;
    private const string Kiwipete = "r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R w KQkq - 0 1";

    [Theory]
    [InlineData(Start, 1, 20UL)]
    [InlineData(Start, 4, 197281UL)]
    [InlineData(Start, 5, 4865609UL)]
    [InlineData(Kiwipete, 3, 97862UL)]
    [InlineData(Kiwipete, 4, 4085603UL)]
    [InlineData("8/2p5/3p4/KP5r/1R3p1k/8/4P1P1/8 w - - 0 1", 5, 674624UL)]
    [InlineData("r3k2r/Pppp1ppp/1b3nbN/nP6/BBP1P3/q4N2/Pp1P2PP/R2Q1RK1 w kq - 0 1", 4, 422333UL)]
    [InlineData("rnbq1k1r/pp1Pbppp/2p5/8/2B5/8/PPP1NnPP/RNBQK2R w KQ - 1 8", 4, 2103487UL)]
    [InlineData("r4rk1/1pp1qppp/p1np1n2/2b1p1B1/2B1P1b1/P1NP1N2/1PP1QPPP/R4RK1 w - - 0 10", 4, 3894594UL)]
    public void PerftMatchesKnownValues(string fen, int depth, ulong expected)
        => Assert.Equal(expected, Perft.Run(Fen.Parse(fen), depth));

    [Fact]
    public void HashAndStateSurviveMakeUnmake()
        => Walk(Fen.Parse(Kiwipete), 3);

    private static void Walk(Board board, int depth)
    {
        if (depth == 0)
            return;

        var list = new MoveList();
        MoveGenerator.Generate(board, ref list);

        for (var i = 0; i < list.Count; i++)
        {
            var move = list[i];
            var before = Fen.Export(board);

            board.MakeMove(move);
            Assert.Equal(board.ComputeHashFromScratch(), board.Hash);
            Walk(board, depth - 1);
            board.UnmakeMove(move);

            Assert.Equal(before, Fen.Export(board));
        }
    }
}

