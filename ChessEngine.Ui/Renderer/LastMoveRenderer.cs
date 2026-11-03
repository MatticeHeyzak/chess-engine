using ChessEngine.Core.Main;
using ChessEngine.Ui.Updates;
using Raylib_cs;

namespace ChessEngine.Ui.Renderer;

public class LastMoveRenderer(BoardLayout layout, GameSession session) : IRenderer
{
    private static readonly Color HighlightColor = new(246, 246, 105, 110);

    public void Draw()
    {
        var move = session.LastMove;
        if (move.IsNull)
            return;

        Raylib.DrawRectangleRec(layout.GetSquareRect(move.From), HighlightColor);
        Raylib.DrawRectangleRec(layout.GetSquareRect(move.To), HighlightColor);
    }

    public void Load() { }
    public void Unload() { }
}
