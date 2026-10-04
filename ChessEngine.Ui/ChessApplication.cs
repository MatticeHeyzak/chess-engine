using ChessEngine.Ui.Renderer;
using Raylib_cs;

namespace ChessEngine.Ui;

public class ChessApplication(IEnumerable<IRenderer> renderers)
{
    public void Run()
    {
        Raylib.InitWindow(Settings.ScreenWidth, Settings.ScreenHeight, Settings.WindowTitle);
        
        Raylib.SetTargetFPS(Settings.Fps);

        while (!Raylib.WindowShouldClose())
        {
            Update();
            Draw();
        }
        
        Raylib.CloseWindow();
    }

    private void Update()
    {
        
    }

    private void Draw()
    {
        Raylib.BeginDrawing();
        Raylib.ClearBackground(Color.DarkGray);
        
        foreach (var renderer in renderers)
            renderer.Draw();
        
        Raylib.EndDrawing();
    }
}