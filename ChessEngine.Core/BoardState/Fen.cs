using System.Text;
using ChessEngine.Core.Main;

namespace ChessEngine.Core.BoardState;

/// <summary>Reads and writes positions in Forsyth-Edwards Notation.</summary>
public static class Fen
{
    public const string StartPosition = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";

    // Index 0 is unused so that a piece type indexes the string directly.
    private const string WhitePieceChars = " PNBRQK";
    private const string BlackPieceChars = " pnbrqk";

    // Fields of a FEN record. The last two are optional.
    private const int PlacementField = 0;
    private const int SideToMoveField = 1;
    private const int CastlingField = 2;
    private const int EnPassantField = 3;
    private const int HalfmoveField = 4;
    private const int FullmoveField = 5;
    private const int RequiredFieldCount = 4;

    private const string NoEnPassant = "-";

    /// <summary>Builds a board from a FEN string; throws <see cref="FormatException"/> if it is malformed.</summary>
    public static Board Parse(string fen)
    {
        var parts = fen.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < RequiredFieldCount)
            throw new FormatException($"FEN needs at least {RequiredFieldCount} fields");

        var board = new Board();
        board.Clear();

        // Fields are validated in FEN order, so the first malformed one is the one reported.
        ParsePlacement(board, parts[PlacementField]);
        var sideToMove = ParseSideToMove(parts[SideToMoveField]);
        var castling = CastlingRights.Parse(parts[CastlingField]);
        var enPassant = ParseEnPassant(parts[EnPassantField]);
        var halfmove = parts.Length > HalfmoveField ? ParseNumber(parts[HalfmoveField], "halfmove clock") : 0;
        var fullmove = parts.Length > FullmoveField ? ParseNumber(parts[FullmoveField], "fullmove number") : 1;

        board.FinishSetup(sideToMove, castling, enPassant, halfmove, fullmove);

        if (Bitboard.PopCount(board.GetBitboard(Piece.WhiteKing)) != 1 ||
            Bitboard.PopCount(board.GetBitboard(Piece.BlackKing)) != 1)
            throw new FormatException("Each side must have exactly one king.");

        return board;
    }

    /// <summary>Writes the position as a FEN string.</summary>
    public static string Export(Board board)
    {
        var sb = new StringBuilder();

        for (var rank = Square.MaxIndex; rank >= 0; rank--)
        {
            AppendRank(sb, board, rank);

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

    /// <summary>Writes one rank, collapsing runs of empty squares into a digit.</summary>
    private static void AppendRank(StringBuilder sb, Board board, int rank)
    {
        var empty = 0;

        for (var file = 0; file < Square.BoardSize; file++)
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
    }

    private static void ParsePlacement(Board board, string placement)
    {
        var ranks = placement.Split('/');
        if (ranks.Length != Square.BoardSize)
            throw new FormatException($"Piece placement must have {Square.BoardSize} ranks.");

        // FEN lists rank 8 first.
        for (var i = 0; i < Square.BoardSize; i++)
            ParseRank(board, ranks[i], Square.MaxIndex - i);
    }

    private static void ParseRank(Board board, string text, int rank)
    {
        var file = 0;

        foreach (var c in text)
        {
            if (char.IsAsciiDigit(c))
            {
                file += c - '0';
                continue;
            }

            if (file > Square.MaxIndex)
                throw new FormatException($"Too many squares in rank {rank + 1}.");

            board.PlacePiece(CharToPiece(c), Square.Make(file, rank));
            file++;
        }

        if (file != Square.BoardSize)
            throw new FormatException($"Rank {rank + 1} does not describe exactly {Square.BoardSize} squares.");
    }

    private static int ParseSideToMove(string field) => field switch
    {
        "w" => Piece.White,
        "b" => Piece.Black,
        _ => throw new FormatException($"Invalid side to move: '{field}'.")
    };

    private static int ParseEnPassant(string field)
    {
        if (field == NoEnPassant)
            return Square.None;

        var square = Square.Parse(field);
        if (square == Square.None)
            throw new FormatException($"Invalid en passant square: '{field}'.");

        return square;
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

