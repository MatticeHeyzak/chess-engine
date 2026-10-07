using ChessEngine.Core;
using ChessEngine.Core.Main;
using Raylib_cs;

namespace ChessEngine.Ui.Renderer;

public class BoardRenderer(BoardLayout layout) : IRenderer
{
    private static readonly Color DarkSquareColor = new(125, 148, 93);
    private static readonly Color LightSquareColor = new(238, 238, 213);
    
    public void Draw()
    {
        for (var square = 0; square < Square.Count; square++)
        {
            var rect = layout.GetSquareRect(square);
            Raylib.DrawRectangleRec(rect, GetSquareColor(square));
        }
    }

    private static Color GetSquareColor(int square)
        => (Square.FileOf(square) + Square.RankOf(square)) % 2 == 0
            ? DarkSquareColor
            : LightSquareColor;
    
    public void Load() {}
    public void Unload() {}
}