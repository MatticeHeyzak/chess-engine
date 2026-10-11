using ChessEngine.Core.BoardState;
using ChessEngine.Core.Main;

namespace ChessEngine.Core.MoveGen;

public static class MoveGenerator
{
    public static void Generate(Board board, ref MoveList list)
    {
        if (board.SideToMove == Piece.White)
            AddPawnMoves<White>(board, ref list);
        else
            AddPawnMoves<Black>(board, ref list);
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

        var single = TColor.ShiftUp(pawns) & empty;
        var doublePush = TColor.ShiftUp(single & TColor.ThirdRank) & empty;
        
        AddPawnTargets(ref list, single & ~TColor.PromotionRank, TColor.Up, MoveFlag.Quiet);
        AddPawnTargets(ref list, doublePush, TColor.Up * 2, MoveFlag.DoublePawnPush);
    }

    private static void AddPawnCaptures<TColor>(Board board, ref MoveList list, ulong pawns)
        where TColor : struct, IColor
    {
        // Captures onto the last rank are promotions, so they are skipped for now
        var enemy = board.ColorOccupancy(TColor.Us ^ 1) & ~TColor.PromotionRank;    
        
        AddPawnTargets(ref list, TColor.ShiftUpRight(pawns) & enemy, TColor.UpRight, MoveFlag.Capture);
        AddPawnTargets(ref list, TColor.ShiftUpLeft(pawns) & enemy, TColor.UpLeft, MoveFlag.Capture);
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
}