using Raylib_cs;

namespace ChessEngine.Ui;

internal static class Program
{
    [STAThread]
    public static void Main()
    {
        Raylib.InitWindow(Settings.ScreenWidth, Settings.ScreenHeight, Settings.WindowTitle);
        
        Raylib.SetTargetFPS(Settings.Fps);

        while (!Raylib.WindowShouldClose())
        {
            Update();
            Draw();
        }
        
        Raylib.EndDrawing();
    }

    private static void Update()
    {
        
    }

    private static void Draw()
    {
        Raylib.BeginDrawing();
        Raylib.ClearBackground(Color.Black);
        Raylib.EndDrawing();
    }
}
