using ChessEngine.Core.Main;
using Raylib_cs;

namespace ChessEngine.Ui;

/// <summary>Maps between board squares and screen pixels, optionally with the board flipped (black at the bottom).</summary>
public sealed class BoardLayout(int originX, int originY, int squareSize)
{
    public int OriginX { get; } = originX;
    public int OriginY { get; } = originY;
    public int SquareSize { get; } = squareSize;
    public int BoardPixelSize => SquareSize * Square.BoardSize;

    /// <summary>False: white at the bottom (a1 bottom-left). True: black at the bottom.</summary>
    public bool Flipped { get; set; }

    public Rectangle GetSquareRect(int square)
    {
        var (row, col) = ToRowCol(square);
        return new Rectangle(
            OriginX + col * SquareSize,
            OriginY + row * SquareSize,
            SquareSize,
            SquareSize);
    }

    /// <summary>The square under a screen position, or <see cref="Square.None"/> if it is outside the board.</summary>
    public int ScreenToSquare(int x, int y)
    {
        if (x < OriginX || y < OriginY)
            return Square.None;

        var col = (x - OriginX) / SquareSize;
        var row = (y - OriginY) / SquareSize;

        if (col > Square.MaxIndex || row > Square.MaxIndex)
            return Square.None;

        return Flipped
            ? Square.Make(Square.MaxIndex - col, row)
            : Square.Make(col, Square.MaxIndex - row);
    }

    /// <summary>Screen row/col of a square. Row 0 is at the top of the screen.</summary>
    private (int Row, int Col) ToRowCol(int square)
    {
        var file = Square.FileOf(square);
        var rank = Square.RankOf(square);

        return Flipped
            ? (rank, Square.MaxIndex - file)
            : (Square.MaxIndex - rank, file);
    }
}

