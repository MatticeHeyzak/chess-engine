using ChessEngine.Ui.Renderer;
using ChessEngine.Ui.Updates;
using Raylib_cs;

namespace ChessEngine.Ui;

public class ChessApplication(IEnumerable<IRenderer> renderers, List<IUpdatable> updatables)
{
    private readonly List<IRenderer> _renderers = renderers.ToList();
    private readonly List<IUpdatable> _updatables = updatables.ToList();
    
    public void Run()
    {
        Raylib.InitWindow(Settings.ScreenWidth, Settings.ScreenHeight, Settings.WindowTitle);
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