using System.Numerics;
using ChessEngine.Core.Main;
using ChessEngine.Ui.Updates;
using Raylib_cs;

namespace ChessEngine.Ui.Renderer;

public class MoveHighlightRenderer(BoardLayout layout, BoardInteraction interaction) : IRenderer
{
    private static readonly Color SelectedColor = new(246, 246, 105, 150);
    private static readonly Color HintColor = new(0, 0, 0, 60);
    // Sizes as a fraction of the square size
    private const float CaptureRingInnerRadius = 0.44f;
    private const float CaptureRingOuterRadius = 0.48f;
    private const float QuietDotRadius = 0.1f;
    private const int RingSegments = 48;

    public void Draw()
    {
        if (interaction.Selected == Square.None)
            return;

        Raylib.DrawRectangleRec(layout.GetSquareRect(interaction.Selected), SelectedColor);

        var size = layout.SquareSize;
        foreach (var move in interaction.Moves)
        {
            var rect = layout.GetSquareRect(move.To);
            var center = new Vector2(rect.X + rect.Width / 2f, rect.Y + rect.Height / 2f);

            if (move.IsCapture)
                Raylib.DrawRing(center, size * CaptureRingInnerRadius, size * CaptureRingOuterRadius, 0, 360, RingSegments, HintColor);
            else
                Raylib.DrawCircleV(center, size * QuietDotRadius, HintColor);
        }
    }

    public void Load() { }
    public void Unload() { }
}
