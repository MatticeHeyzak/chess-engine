using ChessEngine.Core.Main;
using Raylib_cs;

namespace ChessEngine.Ui.Renderer;

public class BoardRenderer(BoardLayout layout) : IRenderer
{
    public void Draw()
    {
        Raylib.DrawRectangleRounded(
            new Rectangle(layout.OriginX - 6, layout.OriginY - 6, layout.BoardPixelSize + 12, layout.BoardPixelSize + 12),
            0.01f, 8, new Color(0, 0, 0, 90));

        for (var square = 0; square < Square.Count; square++)
            Raylib.DrawRectangleRec(layout.GetSquareRect(square), GetSquareColor(square));

        // Coordinates: files along the bottom row, ranks down the left column.
        var fontSize = Math.Max(10, layout.SquareSize / 5);
        var s = layout.SquareSize;
        for (var i = 0; i < Square.BoardSize; i++)
        {
            var file = layout.Flipped ? (char)('h' - i) : (char)('a' + i);
            var rank = layout.Flipped ? i + 1 : Square.BoardSize - i;
            var bottomDark = (Square.MaxIndex + i) % 2 == 1;
            var leftDark = i % 2 == 1;

            Raylib.DrawText(file.ToString(), layout.OriginX + (i + 1) * s - fontSize,
                layout.OriginY + Square.BoardSize * s - fontSize - 3, fontSize,
                bottomDark ? Theme.LightSquare : Theme.DarkSquare);
            Raylib.DrawText(rank.ToString(), layout.OriginX + 4, layout.OriginY + i * s + 3, fontSize,
                leftDark ? Theme.LightSquare : Theme.DarkSquare);
        }
    }

    // a1 (file 0 + rank 0) is a dark square, so an even file + rank sum is dark.
    private static Color GetSquareColor(int square)
        => (Square.FileOf(square) + Square.RankOf(square)) % 2 == 0 ? Theme.DarkSquare : Theme.LightSquare;

    public void Load() { }
    public void Unload() { }
}
