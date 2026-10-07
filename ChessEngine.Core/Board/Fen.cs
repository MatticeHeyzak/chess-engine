using System.Text;
using ChessEngine.Core.Main;

namespace ChessEngine.Core.Board;

public static class Fen
{
    public const string StartPosition = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";

    private const string WhitePieceChars = " PNBRQK";
    private const string BlackPieceChars = " pnbrqk";

    public static Board Parse(string fen)
    {
        var parts = fen.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 4)
            throw new FormatException("FEN needs at least 4 fields");

        var board = new Board();
        board.Clear();
        
        ParsePlacement(board, parts[0]);

        var sideToMove = parts[1] switch
        {
            "w" => Piece.White,
            "b" => Piece.Black,
            _ => throw new FormatException($"Invalid side to move: '{parts[1]}'.")
        };

        var castling = CastlingRights.Parse(parts[2]);

        var enPassant = Square.None;
        if (parts[3] != "-")
        {
            enPassant = Square.Parse(parts[3]);
            if (enPassant == Square.None)
                throw new FormatException($"Invalid en passant square: '{parts[3]}'.");
        }
        
        var halfmove = parts.Length > 4 ? ParseNumber(parts[4], "halfmove clock") : 0;
        var fullmove = parts.Length > 5 ? ParseNumber(parts[5], "fullmove number") : 1;
        
        board.FinishSetup(sideToMove, castling, enPassant, halfmove, fullmove);
        
        if (Bitboard.PopCount(board.GetBitboard(Piece.WhiteKing)) != 1 ||
            Bitboard.PopCount(board.GetBitboard(Piece.BlackKing)) != 1)
            throw new FormatException("Each side must have exactly one king.");

        return board;
    }

    public static string Export(Board board)
    {
        var sb = new StringBuilder();

        for (var rank = 7; rank >= 0; rank--)
        {
            var empty = 0;

            for (var file = 0; file < 8; file++)
            {
                var piece = board.PieceAt(Square.Make(file, rank));

                if (piece == Piece.None)
                {
                    empty++;
                    continue;
                }

                if (empty > 0)
                {
                    sb.Append(empty);
                    empty = 0;
                }
                
                sb.Append(PieceToChar(piece));
            }

            if (empty > 0)
                sb.Append(empty);

            if (rank > 0)
                sb.Append('/');
        }
        
        sb.Append(' ').Append(board.SideToMove == Piece.White ? 'w' : 'b');
        sb.Append(' ').Append(CastlingRights.ToFen(board.Castling));
        sb.Append(' ').Append(Square.ToName(board.EnPassantSquare));
        sb.Append(' ').Append(board.HalfmoveClock);
        sb.Append(' ').Append(board.FullmoveNumber);

        return sb.ToString();
    }

    public static char PieceToChar(int piece)
    {
        var chars = Piece.ColorOf(piece) == Piece.White ? WhitePieceChars : BlackPieceChars;
        return chars[Piece.TypeOf(piece)];
    }

    private static void ParsePlacement(Board board, string placement)
    {
        var ranks = placement.Split('/');
        if (ranks.Length != 8)
            throw new FormatException("Piece placement must have 8 ranks.");

        for (var i = 0; i < 8; i++)
        {
            var rank = 7 - i;
            var file = 0;

            foreach (var c in ranks[i])
            {
                if (char.IsAsciiDigit(c))
                {
                    file += c - '0';
                    continue;
                }
                
                if (file > 7)
                    throw new FormatException($"Too many squares in rank {rank + 1}.");

                var piece = CharToPiece(c);
                board.PlacePiece(piece, Square.Make(file, rank));
                file++;
            }
        
            if (file != 8)
                throw new FormatException($"Rank {rank + 1} does not describe exactly 8 squares.");
        }
    }

    private static int CharToPiece(char c)
    {
        var index = WhitePieceChars.IndexOf(c);
        if (index > 0)
            return Piece.MakePiece(Piece.White, index);
        
        index = BlackPieceChars.IndexOf(c);
        if (index > 0)
            return Piece.MakePiece(Piece.Black, index);
        
        throw new FormatException($"Invalid piece character: '{c}'.");
    }

    private static int ParseNumber(string text, string fieldName)
    {
        if (!int.TryParse(text, out var value) || value < 0)
            throw new FormatException($"Invalid {fieldName}: '{text}'.");
        return value;
    }
}