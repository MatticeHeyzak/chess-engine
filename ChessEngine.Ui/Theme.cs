using Raylib_cs;

namespace ChessEngine.Ui;

public static class Theme
{
    public static readonly Color Background = new(24, 26, 32, 255);
    public static readonly Color Panel = new(36, 39, 48, 255);
    public static readonly Color Text = new(235, 237, 242, 255);
    public static readonly Color TextDim = new(150, 156, 170, 255);
    public static readonly Color Accent = new(88, 140, 255, 255);
    public static readonly Color AccentHover = new(120, 165, 255, 255);
    public static readonly Color Button = new(52, 56, 68, 255);
    public static readonly Color ButtonHover = new(66, 71, 86, 255);
    public static readonly Color Overlay = new(0, 0, 0, 170);
    public static readonly Color LightSquare = new(222, 227, 230, 255);
    public static readonly Color DarkSquare = new(140, 162, 173, 255);
    public static readonly Color LastMove = new(255, 213, 79, 95);
    public static readonly Color Check = new(235, 64, 52, 170);
}
