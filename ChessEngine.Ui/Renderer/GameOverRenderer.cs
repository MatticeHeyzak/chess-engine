using ChessEngine.Ui.Appearance;
using Raylib_cs;

namespace ChessEngine.Ui.Renderer;

public class GameOverRenderer(GameSession session) : IRenderer
{
    public void Draw()
    {
        if (session.Screen != Screen.GameOver)
            return;

        float w = Raylib.GetScreenWidth(), h = Raylib.GetScreenHeight();
        Raylib.DrawRectangle(0, 0, (int)w, (int)h, Theme.Overlay);

        var panel = new Rectangle((w - 460) / 2, (h - 340) / 2, 460, 340);
        Raylib.DrawRectangleRounded(panel, 0.06f, 12, Theme.Panel);
        Gui.TextCentered(session.ResultTitle, new Rectangle(panel.X, panel.Y + 24, panel.Width, 60), 44, Theme.Text);
        Gui.TextCentered(session.ResultReason, new Rectangle(panel.X, panel.Y + 84, panel.Width, 30), 24, Theme.TextDim);

        float bx = panel.X + 40, bw = panel.Width - 80;
        if (Gui.Button(new Rectangle(bx, panel.Y + 140, bw, 52), "Restart", primary: true))
            session.Restart();
        if (Gui.Button(new Rectangle(bx, panel.Y + 204, bw, 52), "Review game"))
            session.EnterReplay();
        if (Gui.Button(new Rectangle(bx, panel.Y + 268, bw, 52), "Back to menu"))
            session.ToMenu();
    }

    public void Load() { }
    public void Unload() { }
}
