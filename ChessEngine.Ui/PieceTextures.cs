using System.Numerics;
using ChessEngine.Core;
using ChessEngine.Core.Main;
using Raylib_cs;

namespace ChessEngine.Ui;

public sealed class PieceTextures
{
    private static readonly string[] TypeNames =
        ["", "pawn", "knight", "bishop", "rook", "queen", "king"];

    private static readonly string[] ColorNames = ["white", "black"];

    // Indexed by piece code. Unused slots (0, 7, 8, 15) stay default (Id == 0).
    private readonly Texture2D[] _textures = new Texture2D[Piece.CodeCount];
    private bool _loaded;

    public void Load(string directory)
    {
        for (var color = Piece.White; color <= Piece.Black; color++)
        {
            for (var type = Piece.Pawn; type <= Piece.King; type++)
            {
                var path = Path.Combine(directory, $"{ColorNames[color]}-{TypeNames[type]}.png");

                if (!File.Exists(path))
                    throw new FileNotFoundException($"Missing piece image: {path}");

                var texture = Raylib.LoadTexture(path);
                if (texture.Id == 0)
                    throw new InvalidOperationException($"Could not load piece image: {path}");

                Raylib.GenTextureMipmaps(ref texture);
                Raylib.SetTextureFilter(texture, TextureFilter.Trilinear);

                _textures[Piece.MakePiece(color, type)] = texture;
            }
        }

        _loaded = true;
    }

    public void Unload()
    {
        if (!_loaded)
            return;

        foreach (var texture in _textures)
        {
            if (texture.Id != 0)
                Raylib.UnloadTexture(texture);
        }

        _loaded = false;
    }

    public void Draw(int piece, Rectangle destination)
    {
        var texture = _textures[piece];
        var source = new Rectangle(0, 0, texture.Width, texture.Height);

        Raylib.DrawTexturePro(texture, source, destination, Vector2.Zero, 0f, Color.White);
    }
}