using ChessEngine.Ui.Renderer;
using Raylib_cs;

namespace ChessEngine.Ui;

public class ChessApplication(IEnumerable<IRenderer> renderers)
{
    private readonly List<IRenderer> _renderers = renderers.ToList();
    
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