using ChessEngine.Ui.Appearance;
using Raylib_cs;

namespace ChessEngine.Ui.Renderer;

public class SettingsRenderer(GameSession session, UserSettings settings) : IRenderer
{
    private const float PanelWidth = 560, PanelHeight = 420, Padding = 32;
    private const float ArrowSize = 44, NameWidth = 210, ToggleWidth = 64, ToggleHeight = 34;

    public void Draw()
    {
        if (session.Screen != Screen.Settings)
            return;

        float w = Raylib.GetScreenWidth(), h = Raylib.GetScreenHeight();
        Raylib.DrawRectangle(0, 0, (int)w, (int)h, Theme.Background);

        var panel = new Rectangle((w - PanelWidth) / 2, Math.Max(24, (h - PanelHeight) / 2), PanelWidth, PanelHeight);
        Raylib.DrawRectangleRounded(panel, 0.05f, 12, Theme.Panel);
        Gui.TextCentered("Settings", new Rectangle(panel.X, panel.Y + 20, panel.Width, 50), 36, Theme.Text);

        var content = new Rectangle(panel.X + Padding, panel.Y + 90, panel.Width - Padding * 2, 0);
        DrawThemeRow(content with { Height = 110 });
        DrawRotateRow(content with { Y = content.Y + 130, Height = 60 });

        var back = new Rectangle(content.X, panel.Y + panel.Height - Padding - 48, content.Width, 48);
        if (Gui.Button(back, "Back", primary: true) || Raylib.IsKeyPressed(KeyboardKey.Escape))
            session.ToMenu();
    }

    private void DrawThemeRow(Rectangle row)
    {
        Raylib.DrawText("Theme", (int)row.X, (int)row.Y + 12, 24, Theme.Text);

        var right = row.X + row.Width;
        var next = new Rectangle(right - ArrowSize, row.Y, ArrowSize, ArrowSize);
        var name = new Rectangle(next.X - NameWidth, row.Y, NameWidth, ArrowSize);
        var previous = new Rectangle(name.X - ArrowSize, row.Y, ArrowSize, ArrowSize);

        if (Gui.Button(previous, "<")) settings.CycleTheme(-1);
        if (Gui.Button(next, ">")) settings.CycleTheme(1);
        Gui.TextCentered(settings.CurrentTheme.Name, name, 22, Theme.Text);

        DrawSwatches(new Rectangle(row.X, row.Y + 60, row.Width, 36));
    }

    /// <summary>Preview strip of the selected theme's key colours.</summary>
    private static void DrawSwatches(Rectangle area)
    {
        Color[] colors = [Theme.Background, Theme.DarkSquare, Theme.LightSquare, Theme.Accent, Theme.Check];
        const float gap = 8;
        var width = (area.Width - gap * (colors.Length - 1)) / colors.Length;

        for (var i = 0; i < colors.Length; i++)
        {
            var swatch = new Rectangle(area.X + i * (width + gap), area.Y, width, area.Height);
            Raylib.DrawRectangleRounded(swatch, 0.3f, 8, colors[i]);
            Raylib.DrawRectangleRoundedLinesEx(swatch, 0.3f, 8, 1.5f, Theme.TextDim);
        }
    }

    private void DrawRotateRow(Rectangle row)
    {
        Raylib.DrawText("Rotate board", (int)row.X, (int)row.Y + 4, 24, Theme.Text);
        Raylib.DrawText("Put your pieces at the bottom", (int)row.X, (int)row.Y + 34, 18, Theme.TextDim);

        var toggle = new Rectangle(row.X + row.Width - ToggleWidth, row.Y + 8, ToggleWidth, ToggleHeight);
        if (Gui.Toggle(toggle, settings.RotateBoard))
            settings.ToggleRotateBoard();
    }

    public void Load() { }
    public void Unload() { }
}
