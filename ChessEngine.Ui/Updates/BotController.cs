using ChessEngine.Core.BoardState;
using ChessEngine.Core.MoveGen;
using ChessEngine.Core.Search;

namespace ChessEngine.Ui.Updates;

/// <summary>
/// Plays one side: starts a background search when it is the bot's turn and makes the move once it finishes.
/// </summary>
public class BotController(GameSession session, int thinkTimeMs) : IUpdatable
{
    private const int TranspositionTableMegabytes = 64;

    private readonly TranspositionTable _tt = new(TranspositionTableMegabytes);

    private Task<SearchResult>? _search;
    private ulong _searchedHash;
    private int _searchedGame;

    public bool IsThinking => _search is not null;

    public void Update()
    {
        var board = session.Board;

        if (_search is null)
        {
            if (session.Screen == Screen.Playing && board.SideToMove == session.BotColor)
                StartSearch();
            return;
        }

        if (!_search.IsCompleted)
            return;

        var result = _search.Result;
        _search = null;

        if (session.Screen != Screen.Playing || _searchedGame != session.GameId
            || board.SideToMove != session.BotColor || board.Hash != _searchedHash || result.BestMove.IsNull)
            return;

        session.Play(result.BestMove);
    }

    private void StartSearch()
    {
        var board = session.Board;
        var list = new MoveList();
        MoveGenerator.Generate(board, ref list);
        if (list.Count == 0)
            return;

        var copy = board.Clone();
        _searchedHash = board.Hash;
        _searchedGame = session.GameId;
        var limits = new SearchLimits(TimeMs: thinkTimeMs);
        _search = Task.Run(() => new Searcher(copy, _tt).Search(limits));
    }
}

