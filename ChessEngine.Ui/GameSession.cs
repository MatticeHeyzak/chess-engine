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

public sealed class GameSession(string startFen)
{
    public Screen Screen { get; private set; } = Screen.Menu;
    public Board Board { get; private set; } = Fen.Parse(startFen);
    public int PlayerColor { get; private set; } = Piece.White;
    public int BotColor => PlayerColor ^ 1;
    public Move LastMove { get; private set; } = Move.Null;

    public int GameId { get; private set; }

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
        Board.MakeMove(move);
        LastMove = move;
    }
}
