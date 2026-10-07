using ChessEngine.Core.Main;

namespace ChessEngine.Core.Board;

public class Board
{
    private const int MaxPly = 2048;

    private readonly ulong[] _pieces = new ulong[Piece.CodeCount];
    private readonly ulong[] _colorOccupancy = new ulong[2];
    private ulong _occupancy;
    private readonly byte[] _mailbox = new byte[Square.Count];
    private readonly int[] _kingSquare = [Square.None, Square.None];
    
    private readonly UndoInfo[] _history = new UndoInfo[MaxPly];
    private int _ply;

    public int SideToMove { get; private set; } = Piece.White;
    public int Castling { get; private set; }
    public int EnPassantSquare { get; private set; } = Square.None;
    public int HalfmoveClock { get; private set; }
    public int FullmoveNumber { get; private set; } = 1;
    public ulong Hash { get; private set; }
    public int Ply => _ply;
    
    // Construction
    public static Board CreateStart() => Fen.Parse(Fen.StartPosition);
    
    // Queries
    public int PieceAt(int square) => _mailbox[square];
    public ulong GetBitboard(int piece) => _pieces[piece];
    public ulong GetBitboard(int color, int type) => _pieces[Piece.MakePiece(color, type)];
    public ulong Occupancy => _occupancy;
    public ulong ColorOccupancy(int color) => _colorOccupancy[color];
    public int KingSquare(int color) => _kingSquare[color];

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

    public void MakeMove(Move move)
    {
        var us = SideToMove;
        var them = us ^ 1;
        var from = move.From;
        var to = move.To;
        var movingPiece = _mailbox[from];
        
        var captured = move.IsEnPassant
            ? Piece.MakePiece(them, Piece.Pawn)
            : _mailbox[to];
        
        _history[_ply] = new UndoInfo(captured, Castling, EnPassantSquare, HalfmoveClock, Hash);
        
        // Remove old en passant and castling contributions from the hash.
        if (EnPassantSquare != Square.None)
            Hash ^= Zobrist.EnPassantFile(Square.FileOf(EnPassantSquare));
        Hash ^= Zobrist.Castling(Castling);
        
        // Captures
        if (move.IsEnPassant)
            RemovePiece(us == Piece.White ? to - 8 : to + 8);
        else if (move.IsCapture)
            RemovePiece(to);
        
        // Move the piece (or replace the pawn on promotion)
        if (move.IsPromotion)
        {
            RemovePiece(from);
            AddPiece(Piece.MakePiece(us, move.PromotionPieceType), to);
        }
        else
        {
            MovePiece(from, to);
        }
        
        // Castling: move the rook as well
        if (move.IsCastle)
        {
            var kingSide = move.IsKingCastle;
            var rookFrom = kingSide ? to + 1 : to - 2;
            var rookTo = kingSide ? to - 1 : to + 1;
            MovePiece(rookFrom, rookTo);
        }
        
        // Castling rights
        Castling = CastlingRights.Update(Castling, from, to);
        Hash ^= Zobrist.Castling(Castling);
        
        // En passent square
        if (move.IsDoublePawnPush)
        {
            EnPassantSquare = us == Piece.White ? from + 8 : from - 8;
            Hash ^= Zobrist.EnPassantFile(Square.FileOf(EnPassantSquare));
        }
        else
        {
            EnPassantSquare = Square.None;
        }
        
        // Clocks
        if (Piece.TypeOf(movingPiece) == Piece.Pawn || move.IsCapture)
            HalfmoveClock = 0;
        else
            HalfmoveClock++;

        if (us == Piece.Black)
            FullmoveNumber++;

        SideToMove = them;
        Hash ^= Zobrist.SideToMove;

        _ply++;
    }

    public void UnmakeMove(Move move)
    {
        _ply--;
        var undo = _history[_ply];

        SideToMove ^= 1;
        var us = SideToMove;
        var from = move.From;
        var to = move.To;

        if (us == Piece.Black)
            FullmoveNumber--;

        Castling = undo.CastlingRights;
        EnPassantSquare = undo.EnPassantSquare;
        HalfmoveClock = undo.HalfmoveClock;

        if (move.IsCastle)
        {
            var kingSide = move.IsKingCastle;
            var rookFrom = kingSide ? to + 1 : to - 2;
            var rookTo = kingSide ? to - 1 : to + 1;
            MovePiece(rookTo, rookFrom);
        }

        if (move.IsPromotion)
        {
            RemovePiece(to);
            AddPiece(Piece.MakePiece(us, Piece.Pawn), from);
        }
        else
        {
            MovePiece(to, from);
        }
        
        if (move.IsEnPassant)
            AddPiece(undo.CapturedPiece, us == Piece.White ? to - 8 : to + 8);
        else if (undo.CapturedPiece != Piece.None)
            AddPiece(undo.CapturedPiece, to);

        Hash = undo.Hash;
    }
    
    
    // Hash verification

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
        
        if (EnPassantSquare != Piece.None)
            hash ^= Zobrist.EnPassantFile(Square.FileOf(EnPassantSquare));

        if (SideToMove != Piece.Black)
            hash ^= Zobrist.SideToMove;

        return hash;
    }
    
    // Setup (used by fen)

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
        EnPassantSquare = Piece.None;
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
    
    // Piece actions

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