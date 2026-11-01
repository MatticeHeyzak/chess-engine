using ChessEngine.Ui.Renderer;
using ChessEngine.Ui.Updates;
using Raylib_cs;

namespace ChessEngine.Ui;

public class ChessApplication(IEnumerable<IRenderer> renderers, List<IUpdatable> updatables, BoardLayout layout)
{
    private readonly List<IRenderer> _renderers = [.. renderers];
    private readonly List<IUpdatable> _updatables = [.. updatables];

    public void Run()
    {
        Raylib.SetConfigFlags(ConfigFlags.ResizableWindow | ConfigFlags.Msaa4xHint);
        Raylib.InitWindow(Settings.ScreenWidth, Settings.ScreenHeight, Settings.WindowTitle);
        Raylib.SetWindowSize(Settings.MinWidth, Settings.MinHeight);
        Raylib.SetTargetFPS(Settings.Fps);

        foreach (var renderer in _renderers)
            renderer.Load();

        while (!Raylib.WindowShouldClose())
        {
            Update();
            Draw();
        }

        foreach (var renderer in _renderers)
            renderer.Unload();

        Raylib.CloseWindow();
    }

    private void Update()
    {
        layout.Fit(Raylib.GetScreenWidth(), Raylib.GetScreenHeight());
        foreach (var updatable in _updatables)
            updatable.Update();
    }

    private void Draw()
    {
        Raylib.BeginDrawing();
        Raylib.ClearBackground(Color.DarkGray);

        foreach (var renderer in _renderers)
            renderer.Draw();

        Raylib.EndDrawing();
    }
}
