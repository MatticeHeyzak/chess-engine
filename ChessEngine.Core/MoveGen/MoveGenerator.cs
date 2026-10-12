using ChessEngine.Core.BoardState;
using ChessEngine.Core.Main;

namespace ChessEngine.Core.MoveGen;

public static class MoveGenerator
{
    public static void Generate(Board board, ref MoveList list)
    {
        var us = board.SideToMove;
        var own = board.ColorOccupancy(us);
        var enemy = board.ColorOccupancy(us ^ 1);
        
        if (us == Piece.White)
            AddPawnMoves<White>(board, ref list);
        else
            AddPawnMoves<Black>(board, ref list);

        AddKnightMoves(board, ref list, us, own, enemy);
        AddKingMoves(board, ref list, us, own, enemy);
    }

    private static void AddKnightMoves(Board board, ref MoveList list, int us, ulong own, ulong enemy)
    {
        var knights = board.GetBitboard(us, Piece.Knight);

        while (knights != 0)
        {
            var from = Bitboard.PopLeastSignificantBit(ref knights);
            AddMoves(ref list, from, Attacks.Knight(from) & ~own, enemy);
        }
    }

    private static void AddKingMoves(Board board, ref MoveList list, int us, ulong own, ulong enemy)
    {
        var from = board.KingSquare(us);
        AddMoves(ref list, from, Attacks.King(from) & ~own, enemy);
    }

    // Turns a target bitboard into moves.
    private static void AddMoves(ref MoveList list, int from, ulong targets, ulong enemy)
    {
        var captures = targets & enemy;
        var quiets = targets & ~enemy;
        
        while (captures != 0)
            list.Add(new Move(from, Bitboard.PopLeastSignificantBit(ref captures), MoveFlag.Capture));
        
        while (quiets != 0)
            list.Add(new Move(from, Bitboard.PopLeastSignificantBit(ref quiets)));
    }

    private static void AddPawnMoves<TColor>(Board board, ref MoveList list) where TColor : struct, IColor
    {
        var pawns = board.GetBitboard(TColor.Us, Piece.Pawn);
        
        AddPawnPushes<TColor>(board, ref list, pawns);
        AddPawnCaptures<TColor>(board, ref list, pawns);
    }

    private static void AddPawnPushes<TColor>(Board board, ref MoveList list, ulong pawns) where TColor : struct, IColor
    {
        var empty = ~board.Occupancy;
        var promotionRank = ~TColor.PromotionRank;

        var single = TColor.ShiftUp(pawns) & empty;
        var doublePush = TColor.ShiftUp(single & TColor.ThirdRank) & empty;
        
        AddPawnTargets(ref list, single & ~TColor.PromotionRank, TColor.Up, MoveFlag.Quiet);
        AddPromotionTargets(ref list, single & promotionRank, TColor.Us, MoveFlag.PromoKnight);
        AddPawnTargets(ref list, doublePush, TColor.Up * 2, MoveFlag.DoublePawnPush);
    }

    private static void AddPawnCaptures<TColor>(Board board, ref MoveList list, ulong pawns)
        where TColor : struct, IColor
    {
        var enemy = board.ColorOccupancy(TColor.Us ^ 1);
        var promotionRank = TColor.PromotionRank;

        var right = TColor.ShiftUpRight(pawns) & enemy;
        var left = TColor.ShiftUpLeft(pawns) & enemy;
        
        AddPawnTargets(ref list, right & ~promotionRank, TColor.UpRight, MoveFlag.Capture);
        AddPawnTargets(ref list, left & ~promotionRank, TColor.UpLeft, MoveFlag.Capture);
        AddPromotionTargets(ref list, right & promotionRank, TColor.UpRight, MoveFlag.PromoKnightCapture);
        AddPromotionTargets(ref list, left & promotionRank, TColor.UpLeft, MoveFlag.PromoKnightCapture);
    }
    
    // `delta` is the square-index distance from origin to target, so origin = target - delta
    private static void AddPawnTargets(ref MoveList list, ulong targets, int delta, int flag)
    {
        while (targets != 0)
        {
            var to = Bitboard.PopLeastSignificantBit(ref targets);
            list.Add(new Move(to - delta, to, flag));
        }
    }

    private static void AddPromotionTargets(ref MoveList list, ulong targets, int delta, int knightFlag)
    {
        while (targets != 0)
        {
            var to = Bitboard.PopLeastSignificantBit(ref targets);
            var from = to - delta;
            
            for (var offset = 3; offset >= 0; offset--)
                list.Add(new Move(from, to, knightFlag + offset));
        }
    }
}