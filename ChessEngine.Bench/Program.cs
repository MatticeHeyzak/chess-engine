using System.Diagnostics;
using BenchmarkDotNet.Running;
using ChessEngine.Core.BoardState;
using ChessEngine.Core.MoveGen;
using ChessEngine.Core.Search;
var mode = args.Length > 0 ? args[0] : "all";
if (mode == "bdn")
{
    BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args[1..]);
    return;
}
if (mode is "all" or "perft")
    RunPerft();
if (mode is "all" or "search")
    RunSearch();
static void RunPerft()
{
    (string Name, string Fen, int Depth)[] positions =
    [
        ("Start",      "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1", 6),
        ("Kiwipete",   "r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R w KQkq - 0 1", 5),
        ("Position 3", "8/2p5/3p4/KP5r/1R3p1k/8/4P1P1/8 w - - 0 1", 7),
        ("Position 4", "r3k2r/Pppp1ppp/1b3nbN/nP6/BBP1P3/q4N2/Pp1P2PP/R2Q1RK1 w kq - 0 1", 5),
        ("Position 5", "rnbq1k1r/pp1Pbppp/2p5/8/2B5/8/PPP1NnPP/RNBQK2R w KQ - 1 8", 5),
    ];
    Console.WriteLine("== Perft ==");
    Console.WriteLine($"{"Position",-12} {"Depth",5} {"Nodes",14} {"Time (ms)",10} {"Mnps",8}");
    foreach (var (name, fen, depth) in positions)
    {
        var board = Fen.Parse(fen);
        Perft.Run(board, depth - 2); // warm-up: JIT + tiered compilation
        var sw = Stopwatch.StartNew();
        var nodes = Perft.Run(board, depth);
        sw.Stop();
        var mnps = nodes / sw.Elapsed.TotalSeconds / 1_000_000;
        Console.WriteLine($"{name,-12} {depth,5} {nodes,14:N0} {sw.ElapsedMilliseconds,10:N0} {mnps,8:F1}");
    }
}
// Fixed-depth searches with an effectively infinite clock, so the result is deterministic:
// node counts, scores and best moves must not change across refactors.
static void RunSearch()
{
    (string Name, string Fen, int Depth)[] positions =
    [
        ("Start",      "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1", 12),
        ("Kiwipete",   "r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R w KQkq - 0 1", 11),
        ("Position 3", "8/2p5/3p4/KP5r/1R3p1k/8/4P1P1/8 w - - 0 1", 14),
        ("Position 4", "r3k2r/Pppp1ppp/1b3nbN/nP6/BBP1P3/q4N2/Pp1P2PP/R2Q1RK1 w kq - 0 1", 11),
        ("Position 5", "rnbq1k1r/pp1Pbppp/2p5/8/2B5/8/PPP1NnPP/RNBQK2R w KQ - 1 8", 11),
        ("Middlegame", "r4rk1/1pp1qppp/p1np1n2/2b1p1B1/2B1P1b1/P1NP1N2/1PP1QPPP/R4RK1 w - - 0 10", 11),
    ];
    const int infiniteTimeMs = int.MaxValue;
    new Searcher(Fen.Parse(Fen.StartPosition), new TranspositionTable(16))
        .Search(new SearchLimits(MaxDepth: 6, TimeMs: infiniteTimeMs)); // warm-up
    Console.WriteLine();
    Console.WriteLine("== Search ==");
    Console.WriteLine($"{"Position",-12} {"Depth",5} {"Nodes",14} {"Time (ms)",10} {"Knps",8} {"Score",7} {"Best",6}");
    ulong totalNodes = 0;
    long totalMs = 0;
    foreach (var (name, fen, depth) in positions)
    {
        var searcher = new Searcher(Fen.Parse(fen), new TranspositionTable(64));
        var sw = Stopwatch.StartNew();
        var result = searcher.Search(new SearchLimits(MaxDepth: depth, TimeMs: infiniteTimeMs));
        sw.Stop();
        totalNodes += result.Nodes;
        totalMs += sw.ElapsedMilliseconds;
        var knps = result.Nodes / sw.Elapsed.TotalSeconds / 1_000;
        Console.WriteLine(
            $"{name,-12} {result.Depth,5} {result.Nodes,14:N0} {sw.ElapsedMilliseconds,10:N0} {knps,8:F0} {result.Score,7} {result.BestMove,6}");
    }
    Console.WriteLine($"{"Total",-12} {"",5} {totalNodes,14:N0} {totalMs,10:N0}");
}
