using ChessEngine.Core.BoardState;
using ChessEngine.Core.Main;

namespace ChessEngine.Core.MoveGen;

public static class Perft
{
    public static ulong Run(Board board, int depth)
    {
        if (depth == 0)
            return 1;

        var list = new MoveList();
        MoveGenerator.Generate(board, ref list);
        
        // Bulk counting: at depth 1 the number of legal moves is the answer.
        if (depth == 1)
            return (ulong)list.Count;

        ulong nodes = 0;

        for (var i = 0; i < list.Count; i++)
        {
            var move = list[i];
            board.MakeMove(move);
            nodes += Run(board, depth - 1);
            board.UnmakeMove(move);
        }

        return nodes;
    }
}