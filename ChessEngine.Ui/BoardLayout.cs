using ChessEngine.Core;
using ChessEngine.Core.Main;
using Raylib_cs;

namespace ChessEngine.Ui;

public sealed class BoardLayout(int originX, int originY, int squareSize)
{
    public int OriginX { get; } = originX;
    public int OriginY { get; } = originY;
    public int SquareSize { get; } = squareSize;
    public int BoardPixelSize => SquareSize * 8;
    
    // false: white at bottom (a1 bottom-left). true: black at the bottom.
    public bool Flipped { get; set; }

    // Screen row/col of a square. Row 0 is at the top of the screen
    private (int Row, int Col) ToRowCol(int square)
    {
        var file = Square.FileOf(square);
        var rank = Square.RankOf(square);

        return Flipped
            ? (rank, 7 - file)
            : (7 - rank, file);
    }

    public Rectangle GetSquareRect(int square)
    {
        var (row, col) = ToRowCol(square);
        return new Rectangle(
            OriginX + col * SquareSize,
            OriginY + row * SquareSize,
            SquareSize,
            SquareSize);
    }

    public int ScreenToSquare(int x, int y)
    {
        if (x < OriginX || y < OriginY)
            return Square.None;
        
        var col = (x - OriginX) / SquareSize;
        var row = (y - OriginY) / SquareSize;

        if (col > 7 || row > 7)
            return Square.None;

        return Flipped
            ? Square.Make(7 - col, row)
            : Square.Make(col, 7 - row);
    }
}