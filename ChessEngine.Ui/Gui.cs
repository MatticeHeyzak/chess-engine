using Raylib_cs;

namespace ChessEngine.Ui;

public static class Gui
{
    private static bool _clickUsed;

    public static void BeginFrame() => _clickUsed = false;

    public static bool Clicked(Rectangle r)
    {
        if (_clickUsed || !Raylib.CheckCollisionPointRec(Raylib.GetMousePosition(), r)
                       || !Raylib.IsMouseButtonPressed(MouseButton.Left))
            return false;

        _clickUsed = true;
        return true;
    }

    public static void TextCentered(string text, Rectangle r, int size, Color color)
    {
        var w = Raylib.MeasureText(text, size);
        Raylib.DrawText(text, (int)(r.X + (r.Width - w) / 2), (int)(r.Y + (r.Height - size) / 2), size, color);
    }

    public static bool Button(Rectangle r, string text, bool primary = false, int fontSize = 22)
    {
        var hover = Raylib.CheckCollisionPointRec(Raylib.GetMousePosition(), r);
        var color = primary ? (hover ? Theme.AccentHover : Theme.Accent)
                            : (hover ? Theme.ButtonHover : Theme.Button);
        Raylib.DrawRectangleRounded(r, 0.25f, 10, color);
        TextCentered(text, r, fontSize, Theme.Text);
        return Clicked(r);
    }
}
