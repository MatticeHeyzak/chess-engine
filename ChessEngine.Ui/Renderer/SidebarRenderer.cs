using Raylib_cs;

namespace ChessEngine.Ui.Renderer;

public class SidebarRenderer(GameSession session, BoardLayout layout) : IRenderer
{
    private const int IconSize = 30, IconStep = 18, CapturedHeight = 40, RowHeight = 27;
    private readonly PieceTextures _textures = new();

    public void Load() => _textures.Load(Path.Combine(AppContext.BaseDirectory, "Assets"));
    public void Unload() => _textures.Unload();

    public void Draw()
    {
        if (session.Screen == Screen.Menu)
            return;

        var r = layout.SidebarRect;
        Raylib.DrawRectangleRounded(r, 0.03f, 10, Theme.Panel);

        const float pad = 16;
        var inner = new Rectangle(r.X + pad, r.Y + pad, r.Width - pad * 2, r.Height - pad * 2);
        var topRow = new Rectangle(inner.X, inner.Y, inner.Width, CapturedHeight);
        var bottomRow = new Rectangle(inner.X, inner.Y + inner.Height - CapturedHeight, inner.Width, CapturedHeight);
        DrawCaptured(session.PlayerColor ^ 1, topRow);
        DrawCaptured(session.PlayerColor, bottomRow);

        var listBottom = bottomRow.Y - 12;
        if (session.Screen == Screen.Replay)
        {
            var footerTop = bottomRow.Y - 12 - 100;
            DrawReplayControls(new Rectangle(inner.X, footerTop, inner.Width, 100));
            listBottom = footerTop - 12;
        }

        var listTop = topRow.Y + CapturedHeight + 12;
        DrawMoveList(new Rectangle(inner.X, listTop, inner.Width, listBottom - listTop));
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

    private void DrawMoveList(Rectangle area)
    {
        var history = session.History;
        var rows = Math.Max(1, (int)(area.Height / RowHeight));
        var totalRows = (history.Count + 1) / 2;
        var focusRow = Math.Max(0, (Math.Max(session.VisiblePlies, 1) - 1) / 2);
        var first = Math.Max(0, focusRow - rows + 1); // keeps the current move in view
        var replay = session.Screen == Screen.Replay;
        var cellWidth = (area.Width - 40) / 2;

        for (var i = 0; i < rows && first + i < totalRows; i++)
        {
            var row = first + i;
            var y = area.Y + i * RowHeight;
            Raylib.DrawText($"{row + 1}.", (int)area.X, (int)y + 4, 20, Theme.TextDim);

            for (var k = 0; k < 2; k++)
            {
                var ply = row * 2 + k;
                if (ply >= history.Count) break;

                var cell = new Rectangle(area.X + 40 + k * cellWidth, y, cellWidth - 4, RowHeight - 2);
                if (replay && ply + 1 == session.ReplayIndex)
                    Raylib.DrawRectangleRounded(cell, 0.3f, 6, Theme.Accent);
                if (replay && Gui.Clicked(cell))
                    session.SetReplay(ply + 1);

                Raylib.DrawText(session.MoveText(ply), (int)cell.X + 8, (int)y + 4, 20, Theme.Text);
            }
        }
    }

    private void DrawReplayControls(Rectangle area)
    {
        const float gap = 8;
        var bw = (area.Width - gap * 3) / 4;
        string[] labels = ["|<", "<", ">", ">|"];
        int[] targets = [0, session.ReplayIndex - 1, session.ReplayIndex + 1, session.History.Count];

        for (var i = 0; i < 4; i++)
            if (Gui.Button(new Rectangle(area.X + i * (bw + gap), area.Y, bw, 48), labels[i]))
                session.SetReplay(targets[i]);

        if (Gui.Button(new Rectangle(area.X, area.Y + 56, area.Width, 44), "Back to results", fontSize: 20))
            session.ExitReplay();

        if (Raylib.IsKeyPressed(KeyboardKey.Left))
            session.SetReplay(session.ReplayIndex - 1);
        if (Raylib.IsKeyPressed(KeyboardKey.Right))
            session.SetReplay(session.ReplayIndex + 1);
        if (Raylib.IsKeyPressed(KeyboardKey.Home))
            session.SetReplay(0);
        if (Raylib.IsKeyPressed(KeyboardKey.End))
            session.SetReplay(session.History.Count);
    }
}
