namespace ChessEngine.Ui.Appearance;

public static class ThemePalettes
{
    public static IReadOnlyList<ThemePalette> All { get; } =
    [
        new("Slate",
            Background: new(24, 26, 32, 255), Panel: new(36, 39, 48, 255),
            Text: new(235, 237, 242, 255), TextDim: new(150, 156, 170, 255),
            Accent: new(88, 140, 255, 255), AccentHover: new(120, 165, 255, 255),
            Button: new(52, 56, 68, 255), ButtonHover: new(66, 71, 86, 255),
            Overlay: new(0, 0, 0, 170),
            LightSquare: new(222, 227, 230, 255), DarkSquare: new(140, 162, 173, 255),
            LastMove: new(255, 213, 79, 95), Check: new(235, 64, 52, 170)),

        new("Midnight Neon",
            Background: new(10, 10, 20, 255), Panel: new(20, 20, 38, 255),
            Text: new(230, 240, 255, 255), TextDim: new(120, 130, 170, 255),
            Accent: new(0, 245, 212, 255), AccentHover: new(120, 255, 235, 255),
            Button: new(34, 32, 64, 255), ButtonHover: new(52, 46, 96, 255),
            Overlay: new(5, 5, 15, 190),
            LightSquare: new(112, 104, 168, 255), DarkSquare: new(58, 50, 104, 255),
            LastMove: new(0, 245, 212, 80), Check: new(255, 45, 120, 180)),

        new("Walnut & Brass",
            Background: new(30, 22, 18, 255), Panel: new(46, 34, 28, 255),
            Text: new(245, 234, 218, 255), TextDim: new(166, 148, 128, 255),
            Accent: new(212, 164, 76, 255), AccentHover: new(235, 190, 100, 255),
            Button: new(66, 50, 40, 255), ButtonHover: new(86, 66, 52, 255),
            Overlay: new(15, 10, 8, 180),
            LightSquare: new(238, 215, 175, 255), DarkSquare: new(166, 112, 72, 255),
            LastMove: new(255, 220, 90, 100), Check: new(200, 50, 40, 175)),

        new("Forest Dusk",
            Background: new(18, 28, 26, 255), Panel: new(28, 42, 38, 255),
            Text: new(228, 240, 232, 255), TextDim: new(130, 156, 144, 255),
            Accent: new(120, 220, 140, 255), AccentHover: new(160, 240, 175, 255),
            Button: new(40, 60, 54, 255), ButtonHover: new(54, 80, 72, 255),
            Overlay: new(5, 12, 10, 180),
            LightSquare: new(226, 232, 206, 255), DarkSquare: new(106, 150, 110, 255),
            LastMove: new(250, 225, 100, 100), Check: new(230, 70, 60, 175)),

        new("Synthwave Sunset",
            Background: new(22, 8, 40, 255), Panel: new(40, 16, 68, 255),
            Text: new(255, 235, 250, 255), TextDim: new(182, 140, 200, 255),
            Accent: new(255, 56, 150, 255), AccentHover: new(255, 120, 190, 255),
            Button: new(64, 28, 104, 255), ButtonHover: new(92, 40, 140, 255),
            Overlay: new(12, 4, 24, 195),
            LightSquare: new(255, 170, 120, 255), DarkSquare: new(120, 40, 140, 255),
            LastMove: new(0, 240, 255, 105), Check: new(255, 30, 60, 190)),

        new("Volcanic Obsidian",
            Background: new(14, 12, 12, 255), Panel: new(28, 24, 24, 255),
            Text: new(245, 236, 228, 255), TextDim: new(150, 136, 128, 255),
            Accent: new(255, 110, 30, 255), AccentHover: new(255, 156, 70, 255),
            Button: new(48, 40, 40, 255), ButtonHover: new(72, 56, 52, 255),
            Overlay: new(6, 4, 4, 200),
            LightSquare: new(112, 100, 98, 255), DarkSquare: new(48, 42, 44, 255),
            LastMove: new(255, 120, 30, 110), Check: new(255, 40, 20, 200)),

        new("Deep Ocean",
            Background: new(4, 14, 28, 255), Panel: new(8, 28, 50, 255),
            Text: new(214, 244, 250, 255), TextDim: new(104, 160, 180, 255),
            Accent: new(40, 255, 200, 255), AccentHover: new(130, 255, 225, 255),
            Button: new(12, 48, 80, 255), ButtonHover: new(20, 70, 110, 255),
            Overlay: new(2, 8, 18, 200),
            LightSquare: new(40, 110, 140, 255), DarkSquare: new(10, 50, 82, 255),
            LastMove: new(40, 255, 200, 85), Check: new(255, 70, 110, 190)),

        new("Royal Amethyst",
            Background: new(20, 12, 34, 255), Panel: new(34, 22, 56, 255),
            Text: new(250, 240, 255, 255), TextDim: new(166, 146, 196, 255),
            Accent: new(255, 200, 70, 255), AccentHover: new(255, 222, 130, 255),
            Button: new(56, 38, 88, 255), ButtonHover: new(78, 54, 120, 255),
            Overlay: new(10, 5, 20, 195),
            LightSquare: new(226, 206, 250, 255), DarkSquare: new(124, 80, 190, 255),
            LastMove: new(255, 200, 70, 105), Check: new(230, 40, 90, 185)),

        new("Desert Mirage",
            Background: new(38, 26, 22, 255), Panel: new(58, 40, 34, 255),
            Text: new(255, 244, 228, 255), TextDim: new(190, 160, 138, 255),
            Accent: new(30, 200, 190, 255), AccentHover: new(110, 228, 218, 255),
            Button: new(84, 58, 48, 255), ButtonHover: new(110, 76, 62, 255),
            Overlay: new(20, 12, 10, 190),
            LightSquare: new(244, 218, 176, 255), DarkSquare: new(196, 108, 76, 255),
            LastMove: new(30, 200, 190, 100), Check: new(210, 40, 40, 190)),

        new("Winter Wonderland",
            Background: new(12, 36, 26, 255), Panel: new(20, 56, 40, 255),
            Text: new(248, 250, 246, 255), TextDim: new(160, 196, 172, 255),
            Accent: new(200, 38, 52, 255), AccentHover: new(235, 74, 84, 255),
            Button: new(30, 78, 56, 255), ButtonHover: new(42, 104, 74, 255),
            Overlay: new(6, 20, 14, 190),
            LightSquare: new(244, 240, 228, 255), DarkSquare: new(74, 128, 92, 255),
            LastMove: new(255, 205, 70, 110), Check: new(235, 40, 50, 190))
    ];

    public static ThemePalette Default => All[0];
}
