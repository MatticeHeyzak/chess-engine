using Raylib_cs;

namespace ChessEngine.Ui.Appearance;

public static class Theme
{
    public static ThemePalette Current { get; internal set; } = ThemePalettes.Default;

    public static Color Background => Current.Background;
    public static Color Panel => Current.Panel;
    public static Color Text => Current.Text;
    public static Color TextDim => Current.TextDim;
    public static Color Accent => Current.Accent;
    public static Color AccentHover => Current.AccentHover;
    public static Color Button => Current.Button;
    public static Color ButtonHover => Current.ButtonHover;
    public static Color Overlay => Current.Overlay;
    public static Color LightSquare => Current.LightSquare;
    public static Color DarkSquare => Current.DarkSquare;
    public static Color LastMove => Current.LastMove;
    public static Color Check => Current.Check;
}
