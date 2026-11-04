using Raylib_cs;

namespace ChessEngine.Ui.Renderer;

public class SidebarRenderer(GameSession session, BoardLayout layout) : IRenderer
{
    private const int IconSize = 30, IconStep = 18, CapturedHeight = 40;
    private readonly PieceTextures _textures = new();

    public void Load() => _textures.Load(Path.Combine(AppContext.BaseDirectory, "Assets"));
    public void Unload() => _textures.Unload();

    public void Draw()
    {
        if (session.Screen == Screen.Menu)
            return;

        var r = layout.SidebarRect;
        Raylib.DrawRectangleRounded(r, 0.0f, 10, Theme.Panel);

        const float pad = 16;
        var inner = new Rectangle(r.X + pad, r.Y + pad, r.Width - pad * 2, r.Height - pad * 2);
        var topRow = new Rectangle(inner.X, inner.Y, inner.Width, CapturedHeight);
        var bottomRow = new Rectangle(inner.X, inner.Y + inner.Height - CapturedHeight, inner.Width, CapturedHeight);

        DrawCaptured(session.PlayerColor ^ 1, topRow);
        DrawCaptured(session.PlayerColor, bottomRow);
    }

    private void DrawCaptured(int color, Rectangle row)
    {
        var x = row.X;
        foreach (var piece in session.CapturedBy(color))
        {
            _textures.Draw(piece, new Rectangle(x, row.Y + (row.Height - IconSize) / 2, IconSize, IconSize));
            x += IconStep;
        }

        var advantage = session.MaterialAdvantage(color);
        if (advantage > 0)
            Raylib.DrawText($"+{advantage}", (int)(row.X + row.Width - 40), (int)(row.Y + 10), 20, Theme.TextDim);
    }
}
