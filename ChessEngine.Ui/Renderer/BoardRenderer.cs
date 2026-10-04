using Raylib_cs;

namespace ChessEngine.Ui.Renderer;

public class BoardRenderer : IRenderer
{
    private const int DistFromEdge = 50;
    private const int BoardSize = 8;
    private const int FullLength = Settings.ScreenHeight - DistFromEdge * 2;
    private const int SquareLength = FullLength / BoardSize;

    private static readonly Color DarkSquareColor = new(125, 148, 93);
    private static readonly Color LightSquareColor = new(238, 238, 213);
    
    public void Draw()
    {
        for (var row = 0; row < BoardSize; row++)
        {
            var y = row * SquareLength + DistFromEdge;
            for (var col = 0; col < BoardSize; col++)
            {
                var x = col * SquareLength + DistFromEdge;
                var color = GetSquareColor(row, col);
                Raylib.DrawRectangle(x, y, SquareLength, SquareLength, color);
            }
        }
    }

    private static Color GetSquareColor(int row, int col)
        => (row + col) % 2 == 0 ? LightSquareColor : DarkSquareColor;
}