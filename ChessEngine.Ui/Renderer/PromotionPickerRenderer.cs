using System.Numerics;
using ChessEngine.Core.BoardState;
using ChessEngine.Core.Main;
using ChessEngine.Ui.Updates;
using Raylib_cs;

namespace ChessEngine.Ui.Renderer;

public class PromotionPickerRenderer(Board board, BoardLayout layout, BoardInteraction interaction) : IRenderer
{
    private static readonly Color DimColor = new(0, 0, 0, 120);
    private static readonly Color ShadowColor = new(0, 0, 0, 90);
    private static readonly Color PanelColor = new(250, 250, 250, 255);
    private static readonly Color HoverColor = new(129, 182, 76, 160);

    private const float PanelRoundness = 0.08f;
    private const int RoundSegments = 12;
    private const float ShadowOffset = 6f;
    private const float IconPadding = 0.06f; // fraction of the square left empty around each icon

    private readonly PieceTextures _textures = new();

    public void Load() => _textures.Load(Path.Combine(AppContext.BaseDirectory, "Assets"));
    public void Unload() => _textures.Unload();

    public void Draw()
    {
        if (!interaction.PromotionPending)
            return;

        DrawBoardDim();

        var panel = GetPanelRect();
        var shadow = new Rectangle(panel.X + ShadowOffset, panel.Y + ShadowOffset, panel.Width, panel.Height);
        Raylib.DrawRectangleRounded(shadow, PanelRoundness, RoundSegments, ShadowColor);
        Raylib.DrawRectangleRounded(panel, PanelRoundness, RoundSegments, PanelColor);

        var mouse = new Vector2(Raylib.GetMouseX(), Raylib.GetMouseY());
        var color = board.SideToMove;

        for (var i = 0; i < BoardInteraction.PromotionChoices.Length; i++)
        {
            var rect = interaction.PromotionRect(i);

            if (Raylib.CheckCollisionPointRec(mouse, rect))
                Raylib.DrawRectangleRounded(rect, PanelRoundness, RoundSegments, HoverColor);

            var piece = Piece.MakePiece(color, BoardInteraction.PromotionChoices[i]);
            _textures.Draw(piece, Inset(rect, rect.Width * IconPadding));
        }
    }

    private void DrawBoardDim()
        => Raylib.DrawRectangle(layout.OriginX, layout.OriginY, layout.BoardPixelSize, layout.BoardPixelSize, DimColor);

    // Bounding box of all picker squares, whichever direction the picker expands.
    private Rectangle GetPanelRect()
    {
        var first = interaction.PromotionRect(0);
        var last = interaction.PromotionRect(BoardInteraction.PromotionChoices.Length - 1);
        var top = Math.Min(first.Y, last.Y);
        var height = first.Height * BoardInteraction.PromotionChoices.Length;
        return new Rectangle(first.X, top, first.Width, height);
    }

    private static Rectangle Inset(Rectangle rect, float amount)
        => new(rect.X + amount, rect.Y + amount, rect.Width - amount * 2, rect.Height - amount * 2);
}
