using System.Diagnostics;
using BenchmarkDotNet.Running;
using ChessEngine.Core.BoardState;
using ChessEngine.Core.MoveGen;

if (args.Length > 0 && args[0] == "bdn")
{
    BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args[1..]);
    return;
}

(string Name, string Fen, int Depth)[] positions =
[
    ("Start",      "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1", 6),
    ("Kiwipete",   "r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R w KQkq - 0 1", 5),
    ("Position 3", "8/2p5/3p4/KP5r/1R3p1k/8/4P1P1/8 w - - 0 1", 7),
    ("Position 4", "r3k2r/Pppp1ppp/1b3nbN/nP6/BBP1P3/q4N2/Pp1P2PP/R2Q1RK1 w kq - 0 1", 5),
    ("Position 5", "rnbq1k1r/pp1Pbppp/2p5/8/2B5/8/PPP1NnPP/RNBQK2R w KQ - 1 8", 5),
];

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