using ChessEngine.Core.Main;
using ChessEngine.Ui.Updates;
using Raylib_cs;

namespace ChessEngine.Ui.Renderer;

public class PieceRenderer(GameSession session, BoardLayout layout, BoardInteraction interaction) : IRenderer
{
    private const float DragScale = 1.1f;
    private readonly PieceTextures _textures = new();

    public void Load() => _textures.Load(Path.Combine(AppContext.BaseDirectory, "Assets"));
    public void Unload() => _textures.Unload();

    public void Draw()
    {
        var board = session.DisplayBoard;
        for (var square = 0; square < Square.Count; square++)
        {
            if (square == interaction.DragFrom)
                continue;

            var piece = board.PieceAt(square);
            if (piece == Piece.None)
                continue;

            _textures.Draw(piece, layout.GetSquareRect(square));
        }

        if (!interaction.Dragging)
            return;

        var mouse = Raylib.GetMousePosition();
        var size = layout.SquareSize * DragScale;

        var target = layout.ScreenToSquare((int)mouse.X, (int)mouse.Y);
        if (target != Square.None)
            Raylib.DrawRectangleLinesEx(layout.GetSquareRect(target), 3, new Color(255, 255, 255, 160));

        _textures.Draw(board.PieceAt(interaction.DragFrom),
            new Rectangle(mouse.X - size / 2, mouse.Y - size / 2, size, size));
    }
}
