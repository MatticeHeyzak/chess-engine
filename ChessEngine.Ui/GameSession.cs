using ChessEngine.Core.BoardState;
using ChessEngine.Core.Main;
using ChessEngine.Core.MoveGen;
using ChessEngine.Core.Search;

namespace ChessEngine.Ui;

public enum Screen
{
    Menu,
    Playing,
    GameOver
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
    public Move LastMove { get; private set; } = Move.Null;
    private static readonly int[] PieceValues = [0, 1, 3, 3, 5, 9, 0];
    public List<PlayedMove> History { get; } = [];

    public string ResultTitle { get; private set; } = "";
    public string ResultReason { get; private set; } = "";

    public int GameId { get; private set; }

    public IEnumerable<int> CapturedBy(int color)
        => History.Select(x => x.Captured)
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
        LastMove = Move.Null;
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
        LastMove = move;
        CheckGameOver();
    }
}
