using System.Runtime.CompilerServices;
using ChessEngine.Core.Main;

namespace ChessEngine.Core.BoardState;

/// <summary>
/// Mutable game position: piece bitboards plus a mailbox for O(1) "what is on this square",
/// with incremental Zobrist hashing and an undo stack for make/unmake during search.
/// Make/unmake is the hottest code in the engine, so the helpers below are force-inlined.
/// </summary>
public sealed class Board
{
    /// <summary>Longest game (in plies) the undo stack can hold.</summary>
    private const int MaxGamePly = 2048;

    private readonly ulong[] _pieces = new ulong[Piece.CodeCount];
    private readonly ulong[] _colorOccupancy = new ulong[2];
    private ulong _occupancy;
    private readonly byte[] _mailbox = new byte[Square.Count];
    private readonly int[] _kingSquare = [Square.None, Square.None];

    private readonly UndoInfo[] _history = new UndoInfo[MaxGamePly];
    private int _ply;

    public int SideToMove { get; private set; } = Piece.White;
    public int Castling { get; private set; }
    public int EnPassantSquare { get; private set; } = Square.None;
    public int HalfmoveClock { get; private set; }
    public int FullmoveNumber { get; private set; } = 1;
    public ulong Hash { get; private set; }

    // Construction

    public static Board CreateStart() => Fen.Parse(Fen.StartPosition);

    /// <summary>
    /// Deep copy that keeps the undo history, so repetition detection still works on the copy
    /// (a FEN round trip would lose it). Used to hand a private board to a search thread.
    /// </summary>
    public Board Clone()
    {
        var copy = new Board();
        Array.Copy(_pieces, copy._pieces, _pieces.Length);
        Array.Copy(_colorOccupancy, copy._colorOccupancy, _colorOccupancy.Length);
        Array.Copy(_mailbox, copy._mailbox, _mailbox.Length);
        Array.Copy(_kingSquare, copy._kingSquare, _kingSquare.Length);
        Array.Copy(_history, copy._history, _ply); // only the live part
        copy._occupancy = _occupancy;
        copy._ply = _ply;
        copy.SideToMove = SideToMove;
        copy.Castling = Castling;
        copy.EnPassantSquare = EnPassantSquare;
        copy.HalfmoveClock = HalfmoveClock;
        copy.FullmoveNumber = FullmoveNumber;
        copy.Hash = Hash;
        return copy;
    }

    // Queries

    public int PieceAt(int square) => _mailbox[square];
    public ulong GetBitboard(int piece) => _pieces[piece];
    public ulong GetBitboard(int color, int type) => _pieces[Piece.MakePiece(color, type)];
    public ulong Occupancy => _occupancy;
    public ulong ColorOccupancy(int color) => _colorOccupancy[color];
    public int KingSquare(int color) => _kingSquare[color];

    /// <summary>True if <paramref name="color"/> has anything besides pawns and the king (null-move safety check).</summary>
    public bool HasNonPawnMaterial(int color)
        => (_colorOccupancy[color]
            & ~(_pieces[Piece.MakePiece(color, Piece.Pawn)] | _pieces[Piece.MakePiece(color, Piece.King)])) != 0;

    /// <summary>
    /// How many times the current position has occurred (counting itself).
    /// Only positions since the last irreversible move (the halfmove clock) can match, and only
    /// those with the same side to move, hence the step of two plies.
    /// </summary>
    public int CountRepetitions()
    {
        var count = 1;
        var oldest = Math.Max(0, _ply - HalfmoveClock);

        for (var i = _ply - 2; i >= oldest; i -= 2)
        {
            if (_history[i].Hash == Hash)
                count++;
        }

        return count;
    }

    // Making and unmaking moves

    /// <summary>Plays a legal move. The state needed to reverse it is pushed onto the undo stack.</summary>
    public void MakeMove(Move move)
    {
        var us = SideToMove;
        var movingPiece = _mailbox[move.From];

        _history[_ply] = new UndoInfo(CapturedPieceOf(move, us), Castling, EnPassantSquare, HalfmoveClock, Hash);

        RemoveCastlingAndEnPassantFromHash();
        ApplyMove(move, us);
        UpdateCastlingRights(move);
        UpdateEnPassantSquare(move, us);
        UpdateClocks(movingPiece, move, us);

        SideToMove = us ^ 1;
        Hash ^= Zobrist.SideToMove;

        _ply++;
    }

    /// <summary>Reverses the most recent <see cref="MakeMove"/>; <paramref name="move"/> must be that same move.</summary>
    public void UnmakeMove(Move move)
    {
        _ply--;
        var undo = _history[_ply];

        SideToMove ^= 1;
        var us = SideToMove;

        if (us == Piece.Black)
            FullmoveNumber--;

        Castling = undo.CastlingRights;
        EnPassantSquare = undo.EnPassantSquare;
        HalfmoveClock = undo.HalfmoveClock;

        if (move.IsCastle)
        {
            GetCastlingRookMove(move, out var rookFrom, out var rookTo);
            MovePiece(rookTo, rookFrom);
        }

        UndoMovement(move, us);
        RestoreCapturedPiece(move, us, undo.CapturedPiece);

        Hash = undo.Hash;
    }

    /// <summary>Passes the turn without moving (null-move pruning). Pair with <see cref="UnmakeNullMove"/>.</summary>
    public void MakeNullMove()
    {
        _history[_ply] = new UndoInfo(Piece.None, Castling, EnPassantSquare, HalfmoveClock, Hash);

        if (EnPassantSquare != Square.None)
        {
            Hash ^= Zobrist.EnPassantFile(Square.FileOf(EnPassantSquare));
            EnPassantSquare = Square.None;
        }

        HalfmoveClock++;
        SideToMove ^= 1;
        Hash ^= Zobrist.SideToMove;
        _ply++;
    }

    public void UnmakeNullMove()
    {
        _ply--;
        var undo = _history[_ply];
        SideToMove ^= 1;
        EnPassantSquare = undo.EnPassantSquare;
        HalfmoveClock = undo.HalfmoveClock;
        Hash = undo.Hash;
    }

    // MakeMove steps

    /// <summary>The piece code that the move removes from the board (<see cref="Piece.None"/> if it captures nothing).</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int CapturedPieceOf(Move move, int us)
        => move.IsEnPassant
            ? Piece.MakePiece(us ^ 1, Piece.Pawn)
            : _mailbox[move.To];

    /// <summary>Takes the old en passant and castling contributions out of the hash; they are re-added once updated.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void RemoveCastlingAndEnPassantFromHash()
    {
        if (EnPassantSquare != Square.None)
            Hash ^= Zobrist.EnPassantFile(Square.FileOf(EnPassantSquare));

        Hash ^= Zobrist.Castling(Castling);
    }

    /// <summary>Moves the pieces: capture first, then the mover (or the promoted piece), then the castling rook.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ApplyMove(Move move, int us)
    {
        var from = move.From;
        var to = move.To;

        if (move.IsEnPassant)
            RemovePiece(CapturedPawnSquare(us, to));
        else if (move.IsCapture)
            RemovePiece(to);

        if (move.IsPromotion)
        {
            RemovePiece(from);
            AddPiece(Piece.MakePiece(us, move.PromotionPieceType), to);
        }
        else
        {
            MovePiece(from, to);
        }

        if (move.IsCastle)
        {
            GetCastlingRookMove(move, out var rookFrom, out var rookTo);
            MovePiece(rookFrom, rookTo);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void UpdateCastlingRights(Move move)
    {
        Castling = CastlingRights.Update(Castling, move.From, move.To);
        Hash ^= Zobrist.Castling(Castling);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void UpdateEnPassantSquare(Move move, int us)
    {
        if (move.IsDoublePawnPush)
        {
            EnPassantSquare = EnPassantTargetSquare(us, move.From);
            Hash ^= Zobrist.EnPassantFile(Square.FileOf(EnPassantSquare));
        }
        else
        {
            EnPassantSquare = Square.None;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void UpdateClocks(int movingPiece, Move move, int us)
    {
        if (Piece.TypeOf(movingPiece) == Piece.Pawn || move.IsCapture)
            HalfmoveClock = 0;
        else
            HalfmoveClock++;

        if (us == Piece.Black)
            FullmoveNumber++;
    }

    // UnmakeMove steps

    /// <summary>Puts the mover back (demoting a promoted piece to a pawn).</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void UndoMovement(Move move, int us)
    {
        if (move.IsPromotion)
        {
            RemovePiece(move.To);
            AddPiece(Piece.MakePiece(us, Piece.Pawn), move.From);
        }
        else
        {
            MovePiece(move.To, move.From);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void RestoreCapturedPiece(Move move, int us, int capturedPiece)
    {
        if (move.IsEnPassant)
            AddPiece(capturedPiece, CapturedPawnSquare(us, move.To));
        else if (capturedPiece != Piece.None)
            AddPiece(capturedPiece, move.To);
    }

    // Geometry shared by make and unmake

    /// <summary>For a castling move (which records the king's squares) finds where the rook starts and ends.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void GetCastlingRookMove(Move move, out int rookFrom, out int rookTo)
    {
        var to = move.To;
        var kingSide = move.IsKingCastle;
        rookFrom = kingSide ? to + 1 : to - 2;
        rookTo = kingSide ? to - 1 : to + 1;
    }

    /// <summary>The captured pawn of an en passant capture sits behind the target square, from the capturer's view.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int CapturedPawnSquare(int us, int targetSquare)
        => us == Piece.White ? targetSquare - Square.BoardSize : targetSquare + Square.BoardSize;

    /// <summary>The square a double pawn push passes over, which becomes the en passant target.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int EnPassantTargetSquare(int us, int from)
        => us == Piece.White ? from + Square.BoardSize : from - Square.BoardSize;

    // Hash verification

    /// <summary>Recomputes the Zobrist hash from the position alone; used to verify the incremental hash.</summary>
    public ulong ComputeHashFromScratch()
    {
        ulong hash = 0;

        for (var sq = 0; sq < Square.Count; sq++)
        {
            var piece = _mailbox[sq];
            if (piece != Piece.None)
                hash ^= Zobrist.PieceSquare(piece, sq);
        }

        hash ^= Zobrist.Castling(Castling);

        if (EnPassantSquare != Square.None)
            hash ^= Zobrist.EnPassantFile(Square.FileOf(EnPassantSquare));

        if (SideToMove != Piece.Black)
            hash ^= Zobrist.SideToMove;

        return hash;
    }

    // Setup (used by Fen)

    internal void Clear()
    {
        Array.Clear(_pieces);
        Array.Clear(_colorOccupancy);
        Array.Clear(_mailbox);
        _occupancy = 0;
        _kingSquare[Piece.White] = Square.None;
        _kingSquare[Piece.Black] = Square.None;

        SideToMove = Piece.White;
        Castling = CastlingRights.None;
        EnPassantSquare = Square.None;
        HalfmoveClock = 0;
        FullmoveNumber = 1;
        Hash = 0;
        _ply = 0;
    }

    internal void PlacePiece(int piece, int square) => AddPiece(piece, square);

    internal void FinishSetup(int sideToMove, int castling, int enPassantSquare, int halfmoveClock, int fullmoveNumber)
    {
        SideToMove = sideToMove;
        Castling = castling;
        EnPassantSquare = enPassantSquare;
        HalfmoveClock = halfmoveClock;
        FullmoveNumber = fullmoveNumber;
        Hash = ComputeHashFromScratch();
    }

    // Piece actions: each keeps bitboards, occupancy, mailbox, king square and hash in sync.

    private void AddPiece(int piece, int square)
    {
        var bit = Bitboard.FromSquare(square);
        var color = Piece.ColorOf(piece);

        _pieces[piece] |= bit;
        _colorOccupancy[color] |= bit;
        _occupancy |= bit;
        _mailbox[square] = (byte)piece;
        Hash ^= Zobrist.PieceSquare(piece, square);

        if (Piece.TypeOf(piece) == Piece.King)
            _kingSquare[color] = square;
    }

    private void RemovePiece(int square)
    {
        var piece = _mailbox[square];
        var bit = Bitboard.FromSquare(square);

        _pieces[piece] &= ~bit;
        _colorOccupancy[Piece.ColorOf(piece)] &= ~bit;
        _occupancy &= ~bit;
        _mailbox[square] = Piece.None;
        Hash ^= Zobrist.PieceSquare(piece, square);
    }

    /// <summary>Moves a piece to an empty square using XOR masks (cheaper than a remove followed by an add).</summary>
    private void MovePiece(int from, int to)
    {
        var piece = _mailbox[from];
        var color = Piece.ColorOf(piece);
        var mask = Bitboard.FromSquare(from) | Bitboard.FromSquare(to);

        _pieces[piece] ^= mask;
        _colorOccupancy[color] ^= mask;
        _occupancy ^= mask;
        _mailbox[from] = Piece.None;
        _mailbox[to] = piece;
        Hash ^= Zobrist.PieceSquare(piece, from) ^ Zobrist.PieceSquare(piece, to);

        if (Piece.TypeOf(piece) == Piece.King)
            _kingSquare[color] = to;
    }
}

