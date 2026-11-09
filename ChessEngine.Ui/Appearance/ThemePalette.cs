using Raylib_cs;

namespace ChessEngine.Ui.Appearance;

public record ThemePalette(
    string Name,
    Color Background,
    Color Panel,
    Color Text,
    Color TextDim,
    Color Accent,
    Color AccentHover,
    Color Button,
    Color ButtonHover,
    Color Overlay,
    Color LightSquare,
    Color DarkSquare,
    Color LastMove,
    Color Check);
