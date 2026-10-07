using ChessEngine.Core;

namespace ChessEngine.Ui.Renderer;

public class PieceRenderer(Board board, BoardLayout layout) : IRenderer
{
    private readonly PieceTextures _textures = new();
    
    public void Load()
    {
        var directory = Path.Combine(AppContext.BaseDirectory, "Assets");
        _textures.Load(directory);
    }
    
    public void Unload() => _textures.Unload();

    public void Draw()
    {
        for (var square = 0; square < Square.Count; square++)
        {
            var piece = board.PieceAt(square);
            if (piece == Piece.None)
                continue;

            _textures.Draw(piece, layout.GetSquareRect(square));
        }
    }
}