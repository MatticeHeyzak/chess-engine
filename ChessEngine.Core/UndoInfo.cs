namespace ChessEngine.Core;

public readonly struct UndoInfo(int capturedPiece, int castlingRights, int enPassantSquare, int halfmoveClock, ulong hash)
{
    public int CapturedPiece { get; } = capturedPiece; // piece CODE (Piece.None if no piece captured)
    public int CastlingRights { get; } = castlingRights;
    public int EnPassantSquare { get; } = enPassantSquare;
    public int HalfmoveClock { get; } = halfmoveClock;
    public ulong Hash { get; } = hash;
}
