using ChessEngine.Ui.Appearance;

namespace ChessEngine.Ui;

public sealed class UserSettings
{
    private int _themeIndex;

    public ThemePalette CurrentTheme => ThemePalettes.All[_themeIndex];

    public bool RotateBoard { get; private set; } = true;

    public void CycleTheme(int step)
    {
        var count = ThemePalettes.All.Count;
        _themeIndex = ((_themeIndex + step) % count + count) % count;
        Theme.Current = CurrentTheme;
    }

    public void ToggleRotateBoard() => RotateBoard = !RotateBoard;
}
