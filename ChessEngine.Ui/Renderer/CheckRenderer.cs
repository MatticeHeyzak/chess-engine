using System.Numerics;
using ChessEngine.Core.BoardState;
using ChessEngine.Core.MoveGen;
using Raylib_cs;

namespace ChessEngine.Ui.Renderer;

public class CheckRenderer(BoardLayout layout, Board board) : IRenderer
{
    public void Draw()
    {
        if (!MoveGenerator.IsInCheck(board))
            return;

        var rect = layout.GetSquareRect(board.KingSquare(board.SideToMove));
        var center = new Vector2(rect.X + rect.Width / 2, rect.Y + rect.Height / 2);
        Raylib.DrawRectangleRec(rect, new Color(235, 64, 52, 70));
        Raylib.DrawRing(center, rect.Width * 0.45f, rect.Width * 0.5f, 0, 360, 48, Theme.Check);
    }
    public void Load() { }
    public void Unload() { }
}
