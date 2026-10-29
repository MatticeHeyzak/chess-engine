using ChessEngine.Core.BoardState;
using ChessEngine.Core.MoveGen;
using ChessEngine.Core.Search;

namespace ChessEngine.Ui.Updates;

public class BotController(Board board, int botColor, int thinkTimeMs) : IUpdatable
{
    private Task<SearchResult>? _search;
    private ulong _searchedHash;

    public bool IsThinking => _search is not null;

    public void Update()
    {
        if (_search is null)
        {
            if (board.SideToMove == botColor)
                StartSearch();
            return;
        }

        if (!_search.IsCompleted)
            return;
        
        var result = _search.Result;
        _search = null;
        
        if (board.SideToMove == botColor && board.Hash == _searchedHash && !result.BestMove.IsNull)
            board.MakeMove(result.BestMove);
    }

    private void StartSearch()
    {
        var list = new MoveList();
        MoveGenerator.Generate(board, ref list);
        if (list.Count == 0)
            return; // checkmate or stalemate
        
        // Note currently Round-tripping gets skipped
        var copy = Fen.Parse(Fen.Export(board));
        _searchedHash = board.Hash;
        
        var limits = new SearchLimits(MaxDepth: 64, TimeMs: thinkTimeMs);
        _search = Task.Run(() => new Searcher(copy).Search(limits));
    }
}