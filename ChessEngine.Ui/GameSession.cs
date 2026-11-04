using ChessEngine.Core.BoardState;
using ChessEngine.Core.Main;

namespace ChessEngine.Ui;

public enum Screen
{
    Menu,
    Playing
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

    public int GameId { get; private set; }

    public IEnumerable<int> CapturedBy(int color)
        => History.Select(x => x.Captured)
            .Where(p => p != Piece.None && Piece.ColorOf(p) != color)
            .OrderByDescending(Piece.TypeOf);

    public int MaterialAdvantage(int color)
        => CapturedBy(color).Sum(p => PieceValues[Piece.TypeOf(p)])
         - CapturedBy(color ^ 1).Sum(p => PieceValues[Piece.TypeOf(p)]);

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
    }

    public void Play(Move move)
    {
        var captured = move.IsEnPassant
            ? Piece.MakePiece(Board.SideToMove ^ 1, Piece.Pawn)
            : Board.PieceAt(move.To);

        Board.MakeMove(move);
        History.Add(new PlayedMove(move, captured));
        LastMove = move;
    }
}
