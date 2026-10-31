using ChessEngine.Core.BoardState;
using ChessEngine.Core.MoveGen;
using ChessEngine.Core.Search;

namespace ChessEngine.Ui.Updates;

/// <summary>
/// Plays one side: starts a background search when it is the bot's turn and makes the move once it finishes.
/// </summary>
public class BotController(Board board, int botColor, int thinkTimeMs) : IUpdatable
{
    private const int TranspositionTableMegabytes = 64;

    private readonly TranspositionTable _tt = new(TranspositionTableMegabytes);

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

        // Search a private copy: the UI keeps drawing (and may change) the real board while the bot thinks.
        var copy = board.Clone();
        _searchedHash = board.Hash; // lets Update() discard the result if the position changed meanwhile
        var limits = new SearchLimits(TimeMs: thinkTimeMs);
        _search = Task.Run(() => new Searcher(copy, _tt).Search(limits));
    }
}

