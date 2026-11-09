using ChessEngine.Ui.Appearance;
using Raylib_cs;

namespace ChessEngine.Ui.Renderer;

public class MenuRenderer(GameSession session) : IRenderer
{
    private static readonly string[] Labels = ["Play as white", "Play as black", "Random"];

    public void Draw()
    {
        if (session.Screen != Screen.Menu)
            return;

        float w = Raylib.GetScreenWidth(), h = Raylib.GetScreenHeight();
        Raylib.DrawRectangle(0, 0, (int)w, (int)h, Theme.Background);

        var top = h * 0.18f;
        Gui.TextCentered("Chess", new Rectangle(0, top, w, 80), 64, Theme.Text);
        Gui.TextCentered("Bot", new Rectangle(0, top + 90, w, 30), 24, Theme.TextDim);

        const float bw = 260, bh = 64, gap = 18;
        for (var i = 0; i < Labels.Length; i++)
        {
            var r = new Rectangle((w - bw) / 2, top + 160 + i * (bh + gap), bw, bh);
            if (Gui.Button(r, Labels[i], primary: i == 2))
                session.StartGame((SideChoice)i);

            const float margin = 24;
            var settingsButton = new Rectangle(w - margin - 140, margin, 140, 44);
            if (Gui.Button(settingsButton, "Settings", fontSize: 20))
                session.OpenSettings();
        }
    }

    public void Load() { }
    public void Unload() { }
}
