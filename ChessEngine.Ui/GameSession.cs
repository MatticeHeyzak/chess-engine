using System.Net.Mime;
using ChessEngine.Core.BoardState;
using ChessEngine.Core.Main;
using ChessEngine.Core.MoveGen;
using ChessEngine.Core.Search;

namespace ChessEngine.Ui;

public enum Screen
{
    Menu,
    Playing,
    GameOver,
    Replay
}

public enum SideChoice
{
    White,
    Black,
    Random
}

public readonly record struct PlayedMove(Move Move, int Captured);

public sealed class GameSession(string startFen)
{
    public Screen Screen { get; private set; } = Screen.Menu;
    public Board Board { get; private set; } = Fen.Parse(startFen);
    public int PlayerColor { get; private set; } = Piece.White;
    public int BotColor => PlayerColor ^ 1;
    private static readonly int[] PieceValues = [0, 1, 3, 3, 5, 9, 0];
    private Board _replayBoard = Fen.Parse(startFen);

    public int ReplayIndex { get; private set; }

    public Board DisplayBoard => Screen == Screen.Replay ? _replayBoard : Board;
    public int VisiblePlies => Screen == Screen.Replay ? ReplayIndex : History.Count;
    public Move DisplayLastmove => VisiblePlies > 0 ? History[VisiblePlies - 1].Move : Move.Null;

    public List<PlayedMove> History { get; } = [];

    public string ResultTitle { get; private set; } = "";
    public string ResultReason { get; private set; } = "";

    public int GameId { get; private set; }

    public IEnumerable<int> CapturedBy(int color)
        => History.Take(VisiblePlies).Select(x => x.Captured)
            .Where(p => p != Piece.None && Piece.ColorOf(p) != color)
            .OrderByDescending(Piece.TypeOf);

    public int MaterialAdvantage(int color)
        => CapturedBy(color).Sum(p => PieceValues[Piece.TypeOf(p)])
         - CapturedBy(color ^ 1).Sum(p => PieceValues[Piece.TypeOf(p)]);

    public void Restart() => StartGame(PlayerColor == Piece.White ? SideChoice.White : SideChoice.Black);
    public void ToMenu() => Screen = Screen.Menu;

    private void CheckGameOver()
    {
        var list = new MoveList();
        MoveGenerator.Generate(Board, ref list);

        if (list.Count == 0)
        {
            if (MoveGenerator.IsInCheck(Board))
                Finish(Board.SideToMove == Piece.White ? "Black wins" : "White wins", "Checkmate");
            else
                Finish("Draw", "Stalemate");
            return;
        }

        if (Evaluation.IsInsufficientMaterial(Board))
        {
            Finish("Draw", "Insufficient material");
            return;
        }

        if (Board.CountRepetitions() >= 3)
        {
            Finish("Draw", "Threefold repetition");
            return;
        }

        if (Board.HalfmoveClock >= 100)
            Finish("Draw", "Fifty-move rule");
    }

    private void Finish(string title, string reason)
    {
        ResultTitle = title;
        ResultReason = reason;
        Screen = Screen.GameOver;
    }

    public void StartGame(SideChoice choice)
    {
        PlayerColor = choice switch
        {
            SideChoice.White => Piece.White,
            SideChoice.Black => Piece.Black,
            _ => Random.Shared.Next(2) == 0 ? Piece.White : Piece.Black
        };

        Board = Fen.Parse(startFen);
        GameId++;
        Screen = Screen.Playing;
        ResultTitle = ResultReason = "";
        History.Clear();
    }

    public void Play(Move move)
    {
        var captured = move.IsEnPassant
            ? Piece.MakePiece(Board.SideToMove ^ 1, Piece.Pawn)
            : Board.PieceAt(move.To);

        Board.MakeMove(move);
        History.Add(new PlayedMove(move, captured));
        CheckGameOver();
    }

    public void EnterReplay()
    {
        Screen = Screen.Replay;
        SetReplay(0);
    }
    public void ExitReplay() => Screen = Screen.GameOver;

    public void SetReplay(int index)
    {
        ReplayIndex = Math.Clamp(index, 0, History.Count);
        _replayBoard = Fen.Parse(startFen);
        for (var i = 0; i < ReplayIndex; i++)
            _replayBoard.MakeMove(History[i].Move);
    }

    public string MoveText(int ply)
    {
        var m = History[ply].Move;
        var text = Name(m.From) + Name(m.To);
        return m.IsPromotion ? text + " pnbrqk"[m.PromotionPieceType] : text;
    }

    private static string Name(int sq) => $"{(char)('a' + Square.FileOf(sq))}{Square.RankOf(sq) + 1}";
}
