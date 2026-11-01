using ChessEngine.Ui.Updates;
using Raylib_cs;

namespace ChessEngine.Ui.Renderer;

public class LastMoveRenderer(BoardLayout layout, LastMoveTracker tracker) : IRenderer
{
    private static readonly Color HighlightColor = new(246, 246, 105, 110);

    public void Draw()
    {
        if (!tracker.HasMove)
            return;

        var move = tracker.LastMove;
        Raylib.DrawRectangleRec(layout.GetSquareRect(move.From), HighlightColor);
        Raylib.DrawRectangleRec(layout.GetSquareRect(move.To), HighlightColor);
    }

    public void Load() { }
    public void Unload() { }
}
