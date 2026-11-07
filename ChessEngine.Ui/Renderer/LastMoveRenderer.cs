using Raylib_cs;

namespace ChessEngine.Ui.Renderer;

public class LastMoveRenderer(BoardLayout layout, GameSession session) : IRenderer
{
    public void Draw()
    {
        var move = session.DisplayLastmove;
        if (move.IsNull)
            return;

        Raylib.DrawRectangleRec(layout.GetSquareRect(move.From), Theme.LastMove);
        Raylib.DrawRectangleRec(layout.GetSquareRect(move.To), Theme.LastMove);
    }

    public void Load() { }
    public void Unload() { }
}
