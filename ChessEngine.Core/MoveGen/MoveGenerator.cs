using System.Runtime.CompilerServices;
using ChessEngine.Core.BoardState;
using ChessEngine.Core.Main;

namespace ChessEngine.Core.MoveGen;

/// <summary>
/// Generates strictly legal moves directly (no pseudo-legal generate-then-filter step):
/// it computes the squares the enemy attacks, the pieces pinned to the king and the check mask up front,
/// so every emitted move is already legal. That removes make/unmake legality probes from the search.
/// </summary>
public static class MoveGenerator
{
    // Files of the squares castling cares about, as offsets from the first square of the back rank.
    private const int QueenSideKnightFile = 1; // b
    private const int QueenSideKingTargetFile = 2; // c
    private const int QueenSideRookTargetFile = 3; // d
    private const int KingFile = 4; // e
    private const int KingSideRookTargetFile = 5; // f
    private const int KingSideKingTargetFile = 6; // g

    /// <summary>Knight, bishop, rook, queen: the four moves emitted for every promotion.</summary>
    private const int PromotionChoices = 4;

    /// <summary>Adds every legal move of the side to move to <paramref name="list"/>.</summary>
    public static void Generate(Board board, ref MoveList list)
    {
        if (board.SideToMove == Piece.White)
            Generate<White, Black>(board, ref list);
        else
            Generate<Black, White>(board, ref list);
    }

    /// <summary>Bitboard of every piece of <paramref name="byColor"/> that attacks <paramref name="square"/>, for a given occupancy.</summary>
    public static ulong AttackersTo(Board board, int square, int byColor, ulong occupancy)
    {
        var queens = board.GetBitboard(byColor, Piece.Queen);

        return (Attacks.Pawn(byColor ^ 1, square) & board.GetBitboard(byColor, Piece.Pawn))
               | (Attacks.Knight(square) & board.GetBitboard(byColor, Piece.Knight))
               | (Attacks.King(square) & board.GetBitboard(byColor, Piece.King))
               | (Attacks.Bishop(square, occupancy) & (board.GetBitboard(byColor, Piece.Bishop) | queens))
               | (Attacks.Rook(square, occupancy) & (board.GetBitboard(byColor, Piece.Rook) | queens));
    }

    public static bool IsSquareAttacked(Board board, int square, int byColor)
        => AttackersTo(board, square, byColor, board.Occupancy) != 0;

    public static bool IsInCheck(Board board)
        => IsSquareAttacked(board, board.KingSquare(board.SideToMove), board.SideToMove ^ 1);

    private static void Generate<TUs, TThem>(Board board, ref MoveList list)
        where TUs : struct, IColor
        where TThem : struct, IColor
    {
        var us = TUs.Us;
        var them = TThem.Us;
        var occupancy = board.Occupancy;
        var own = board.ColorOccupancy(us);
        var enemy = board.ColorOccupancy(them);
        var kingSquare = board.KingSquare(us);

        var checkers = AttackersTo(board, kingSquare, them, occupancy);

        // King moves. Our king is removed from the occupancy so sliders "see through" it,
        // which stops the king from stepping backwards along the line of a checking slider.
        var danger = ComputeDanger<TThem>(board, occupancy & ~Bitboard.FromSquare(kingSquare));
        AddMoves(ref list, kingSquare, Attacks.King(kingSquare) & ~own & ~danger, enemy);

        // Double check: only the king can move.
        if (Bitboard.HasMoreThanOneBit(checkers))
            return;

        var checkMask = ComputeCheckMask(kingSquare, checkers);
        var pinned = ComputePinned(board, them, kingSquare, occupancy, own);
        var targets = ~own & checkMask;

        AddKnightMoves(board, us, ref list, pinned, targets, enemy);

        // Queens are in both sets, so each is handled once as a diagonal and once as an orthogonal slider.
        var queens = board.GetBitboard(us, Piece.Queen);
        AddSliderMoves(ref list, board.GetBitboard(us, Piece.Bishop) | queens, occupancy, targets, enemy, pinned, kingSquare, diagonal: true);
        AddSliderMoves(ref list, board.GetBitboard(us, Piece.Rook) | queens, occupancy, targets, enemy, pinned, kingSquare, diagonal: false);

        AddPawnMoves<TUs>(board, ref list, enemy, checkMask, pinned, kingSquare);

        if (board.EnPassantSquare != Square.None)
            AddEnPassant<TUs, TThem>(board, ref list, occupancy, kingSquare);

        if (checkers == 0)
            AddCastling<TUs>(board, ref list, occupancy, danger);
    }

    // Danger map, check mask and pins

    /// <summary>Every square attacked by a piece of <typeparamref name="TThem"/>.</summary>
    private static ulong ComputeDanger<TThem>(Board board, ulong occupancy) where TThem : struct, IColor
    {
        var them = TThem.Us;
        var pawns = board.GetBitboard(them, Piece.Pawn);
        var danger = TThem.ShiftUpRight(pawns) | TThem.ShiftUpLeft(pawns) | Attacks.King(board.KingSquare(them));

        var knights = board.GetBitboard(them, Piece.Knight);
        while (knights != 0)
            danger |= Attacks.Knight(Bitboard.PopLeastSignificantBit(ref knights));

        var queens = board.GetBitboard(them, Piece.Queen);
        danger |= SliderAttacks(board.GetBitboard(them, Piece.Bishop) | queens, occupancy, diagonal: true);
        danger |= SliderAttacks(board.GetBitboard(them, Piece.Rook) | queens, occupancy, diagonal: false);

        return danger;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong SliderAttacks(ulong pieces, ulong occupancy, bool diagonal)
    {
        ulong attacks = 0;

        while (pieces != 0)
        {
            var square = Bitboard.PopLeastSignificantBit(ref pieces);
            attacks |= diagonal ? Attacks.Bishop(square, occupancy) : Attacks.Rook(square, occupancy);
        }

        return attacks;
    }

    /// <summary>
    /// Where a non-king move must land to resolve a single check: capture the checker or block its ray.
    /// With no check every square is allowed.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong ComputeCheckMask(int kingSquare, ulong checkers)
    {
        if (checkers == 0)
            return Bitboard.Full;

        return Attacks.Between(kingSquare, Bitboard.LeastSignificantBit(checkers)) | checkers;
    }

    /// <summary>
    /// A piece is pinned when it is the only piece between our king and an enemy slider
    /// that attacks along that line.
    /// </summary>
    private static ulong ComputePinned(Board board, int them, int kingSquare, ulong occupancy, ulong own)
    {
        var queens = board.GetBitboard(them, Piece.Queen);
        var snipers = (Attacks.Rook(kingSquare, 0) & (board.GetBitboard(them, Piece.Rook) | queens))
                      | (Attacks.Bishop(kingSquare, 0) & (board.GetBitboard(them, Piece.Bishop) | queens));

        ulong pinned = 0;

        while (snipers != 0)
        {
            var sniper = Bitboard.PopLeastSignificantBit(ref snipers);
            var between = Attacks.Between(kingSquare, sniper) & occupancy;

            if (between != 0 && !Bitboard.HasMoreThanOneBit(between))
                pinned |= between & own;
        }

        return pinned;
    }

    // Knights and sliders

    /// <summary>A pinned knight can never move, so pinned knights are skipped entirely.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void AddKnightMoves(Board board, int us, ref MoveList list, ulong pinned, ulong targets, ulong enemy)
    {
        var knights = board.GetBitboard(us, Piece.Knight) & ~pinned;

        while (knights != 0)
        {
            var from = Bitboard.PopLeastSignificantBit(ref knights);
            AddMoves(ref list, from, Attacks.Knight(from) & targets, enemy);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void AddSliderMoves(
        ref MoveList list, ulong pieces, ulong occupancy, ulong targets, ulong enemy,
        ulong pinned, int kingSquare, bool diagonal)
    {
        while (pieces != 0)
        {
            var from = Bitboard.PopLeastSignificantBit(ref pieces);
            var attacks = (diagonal ? Attacks.Bishop(from, occupancy) : Attacks.Rook(from, occupancy)) & targets;

            // A pinned slider may only move along the line through its king.
            if (Bitboard.IsSet(pinned, from))
                attacks &= Attacks.Line(kingSquare, from);

            AddMoves(ref list, from, attacks, enemy);
        }
    }

    // Pawns

    private static void AddPawnMoves<TUs>(
        Board board, ref MoveList list, ulong enemy, ulong checkMask, ulong pinned, int kingSquare)
        where TUs : struct, IColor
    {
        var pawns = board.GetBitboard(TUs.Us, Piece.Pawn);
        var empty = ~board.Occupancy;
        var promotionRank = TUs.PromotionRank;

        // The double push must start from the unmasked single push: the intermediate
        // square only needs to be empty, not to block a check.
        var single = TUs.ShiftUp(pawns) & empty;
        var doublePush = TUs.ShiftUp(single & TUs.ThirdRank) & empty & checkMask;
        single &= checkMask;

        AddPawnTargets(ref list, single & ~promotionRank, TUs.Up, MoveFlag.Quiet, pinned, kingSquare);
        AddPromotionTargets(ref list, single & promotionRank, TUs.Up, MoveFlag.PromoKnight, pinned, kingSquare);
        AddPawnTargets(ref list, doublePush, TUs.Up * 2, MoveFlag.DoublePawnPush, pinned, kingSquare);

        var captureTargets = enemy & checkMask;
        var right = TUs.ShiftUpRight(pawns) & captureTargets;
        var left = TUs.ShiftUpLeft(pawns) & captureTargets;

        AddPawnTargets(ref list, right & ~promotionRank, TUs.UpRight, MoveFlag.Capture, pinned, kingSquare);
        AddPawnTargets(ref list, left & ~promotionRank, TUs.UpLeft, MoveFlag.Capture, pinned, kingSquare);
        AddPromotionTargets(ref list, right & promotionRank, TUs.UpRight, MoveFlag.PromoKnightCapture, pinned, kingSquare);
        AddPromotionTargets(ref list, left & promotionRank, TUs.UpLeft, MoveFlag.PromoKnightCapture, pinned, kingSquare);
    }

    /// <param name="delta">Square-index distance from origin to target, so origin = target - delta.</param>
    private static void AddPawnTargets(
        ref MoveList list, ulong targets, int delta, int flag, ulong pinned, int kingSquare)
    {
        while (targets != 0)
        {
            var to = Bitboard.PopLeastSignificantBit(ref targets);
            var from = to - delta;

            if (IsPinnedOffLine(pinned, kingSquare, from, to))
                continue;

            list.Add(new Move(from, to, flag));
        }
    }

    /// <param name="knightFlag"><see cref="MoveFlag.PromoKnight"/> or <see cref="MoveFlag.PromoKnightCapture"/>.
    /// Adding 0..3 gives knight, bishop, rook, queen; the queen is emitted first.</param>
    private static void AddPromotionTargets(
        ref MoveList list, ulong targets, int delta, int knightFlag, ulong pinned, int kingSquare)
    {
        while (targets != 0)
        {
            var to = Bitboard.PopLeastSignificantBit(ref targets);
            var from = to - delta;

            if (IsPinnedOffLine(pinned, kingSquare, from, to))
                continue;

            for (var offset = PromotionChoices - 1; offset >= 0; offset--)
                list.Add(new Move(from, to, knightFlag + offset));
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsPinnedOffLine(ulong pinned, int kingSquare, int from, int to)
        => Bitboard.IsSet(pinned, from) && !Bitboard.IsSet(Attacks.Line(kingSquare, from), to);

    /// <summary>
    /// The captured pawn is not on the target square, so the check mask and pin lines don't
    /// apply. Instead the position after the capture is simulated, which also catches the
    /// horizontal pin where both pawns leave the same rank.
    /// </summary>
    private static void AddEnPassant<TUs, TThem>(Board board, ref MoveList list, ulong occupancy, int kingSquare)
        where TUs : struct, IColor
        where TThem : struct, IColor
    {
        var target = board.EnPassantSquare;
        var capturedBit = Bitboard.FromSquare(target - TUs.Up);
        var targetBit = Bitboard.FromSquare(target);

        var candidates = Attacks.Pawn(TThem.Us, target) & board.GetBitboard(TUs.Us, Piece.Pawn);

        while (candidates != 0)
        {
            var from = Bitboard.PopLeastSignificantBit(ref candidates);
            var occupancyAfter = (occupancy ^ Bitboard.FromSquare(from) ^ capturedBit) | targetBit;

            // The captured pawn still sits in the enemy bitboards, so mask it out of the attackers.
            if ((AttackersTo(board, kingSquare, TThem.Us, occupancyAfter) & ~capturedBit) == 0)
                list.Add(new Move(from, target, MoveFlag.EnPassant));
        }
    }

    // Castling

    /// <summary>
    /// Only called when not in check. The king may not pass through or land on an attacked square,
    /// and the squares between king and rook must be empty.
    /// </summary>
    private static void AddCastling<TUs>(Board board, ref MoveList list, ulong occupancy, ulong danger)
        where TUs : struct, IColor
    {
        var home = TUs.Home;
        var rights = board.Castling;

        if (CastlingRights.Has(rights, TUs.KingSideRight))
            AddKingSideCastling(ref list, home, occupancy, danger);

        if (CastlingRights.Has(rights, TUs.QueenSideRight))
            AddQueenSideCastling(ref list, home, occupancy, danger);
    }

    private static void AddKingSideCastling(ref MoveList list, int home, ulong occupancy, ulong danger)
    {
        var path = Bitboard.FromSquare(home + KingSideRookTargetFile) | Bitboard.FromSquare(home + KingSideKingTargetFile);

        if ((occupancy & path) == 0 && (danger & path) == 0)
            list.Add(new Move(home + KingFile, home + KingSideKingTargetFile, MoveFlag.KingCastle));
    }

    private static void AddQueenSideCastling(ref MoveList list, int home, ulong occupancy, ulong danger)
    {
        var safe = Bitboard.FromSquare(home + QueenSideKingTargetFile) | Bitboard.FromSquare(home + QueenSideRookTargetFile);
        var empty = safe | Bitboard.FromSquare(home + QueenSideKnightFile); // the b-file square may be attacked

        if ((occupancy & empty) == 0 && (danger & safe) == 0)
            list.Add(new Move(home + KingFile, home + QueenSideKingTargetFile, MoveFlag.QueenCastle));
    }

    // Shared emitter

    /// <summary>Emits captures before quiet moves, which already gives a rough move ordering for the search.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void AddMoves(ref MoveList list, int from, ulong targets, ulong enemy)
    {
        var captures = targets & enemy;
        var quiets = targets & ~enemy;

        while (captures != 0)
            list.Add(new Move(from, Bitboard.PopLeastSignificantBit(ref captures), MoveFlag.Capture));

        while (quiets != 0)
            list.Add(new Move(from, Bitboard.PopLeastSignificantBit(ref quiets), MoveFlag.Quiet));
    }
}

