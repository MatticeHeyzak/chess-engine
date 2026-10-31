namespace ChessEngine.Core.BoardState;

/// <summary>Everything <see cref="Board.MakeMove"/> cannot recompute from the move itself, saved so it can be undone.</summary>
public readonly struct UndoInfo(int capturedPiece, int castlingRights, int enPassantSquare, int halfmoveClock, ulong hash)
{
    /// <summary>Piece CODE (<see cref="Main.Piece.None"/> if no piece was captured).</summary>
    public int CapturedPiece { get; } = capturedPiece;

    public int CastlingRights { get; } = castlingRights;
    public int EnPassantSquare { get; } = enPassantSquare;
    public int HalfmoveClock { get; } = halfmoveClock;

    /// <summary>The position hash before the move; doubles as the history used for repetition detection.</summary>
    public ulong Hash { get; } = hash;
}

