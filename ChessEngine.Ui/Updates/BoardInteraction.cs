using System.Numerics;
using ChessEngine.Core.BoardState;
using ChessEngine.Core.Main;
using ChessEngine.Core.MoveGen;
using Raylib_cs;

namespace ChessEngine.Ui.Updates;

public sealed class BoardInteraction(Board board, BoardLayout layout, LastMoveTracker lastMove, int playerColor) : IUpdatable
{
    public static readonly int[] PromotionChoices = [Piece.Queen, Piece.Rook, Piece.Bishop, Piece.Knight];

    private readonly List<Move> _moves = [];

    public int Selected { get; private set; } = Square.None;
    public int DragFrom { get; private set; } = Square.None;
    public bool Dragging => DragFrom != Square.None;
    public int PromotionSquare { get; private set; } = Square.None;
    public bool PromotionPending => PromotionSquare != Square.None;
    public IReadOnlyList<Move> Moves => _moves;

    private bool CanInteract => board.SideToMove == playerColor;

    public Rectangle PromotionRect(int index)
    {
        var rect = layout.GetSquareRect(PromotionSquare);
        var expandDown = rect.Y < layout.OriginY + layout.BoardPixelSize / 2f;
        var offset = (expandDown ? index : -index) * rect.Height;
        return new Rectangle(rect.X, rect.Y + offset, rect.Width, rect.Height);
    }

    public void Update()
    {
        if (!CanInteract)
        {
            if (Selected != Square.None || Dragging || PromotionPending)
                Deselect();
            return;
        }

        var mouse = Raylib.GetMousePosition();
        if (Raylib.IsMouseButtonPressed(MouseButton.Left))
            OnPress(mouse);
        else if (Raylib.IsMouseButtonReleased(MouseButton.Left))
            OnRelease(mouse);
    }

    private void OnPress(Vector2 mouse)
    {
        if (PromotionPending)
        {
            HandlePromotionClick(mouse);
            return;
        }

        var square = layout.ScreenToSquare((int)mouse.X, (int)mouse.Y);
        if (square == Square.None)
        {
            Deselect();
            return;
        }

        if (Selected != Square.None && TryMoveTo(square))
            return;

        var piece = board.PieceAt(square);
        if (piece != Piece.None && Piece.ColorOf(piece) == board.SideToMove)
        {
            Select(square);
            DragFrom = square;
        }
        else Deselect();
    }

    private void OnRelease(Vector2 mouse)
    {
        if (!Dragging) return;
        var from = DragFrom;
        DragFrom = Square.None;

        var square = layout.ScreenToSquare((int)mouse.X, (int)mouse.Y);
        if (square == Square.None || square == from) return;
        TryMoveTo(square);
    }

    private bool TryMoveTo(int square)
    {
        foreach (var move in _moves)
        {
            if (move.To != square) continue;

            if (move.IsPromotion) PromotionSquare = square;
            else Play(move);
            return true;
        }
        return false;
    }

    private void HandlePromotionClick(Vector2 mouse)
    {
        for (var i = 0; i < PromotionChoices.Length; i++)
        {
            if (!Raylib.CheckCollisionPointRec(mouse, PromotionRect(i)))
                continue;

            foreach (var move in _moves)
            {
                if (move.To == PromotionSquare && move.IsPromotion && move.PromotionPieceType == PromotionChoices[i])
                {
                    Play(move);
                    return;
                }
            }
        }
        PromotionSquare = Square.None;
    }

    private void Select(int square)
    {
        Selected = square;
        _moves.Clear();

        var list = new MoveList();
        MoveGenerator.Generate(board, ref list);
        for (var i = 0; i < list.Count; i++)
            if (list[i].From == square)
                _moves.Add(list[i]);
    }

    private void Play(Move move)
    {
        board.MakeMove(move);
        lastMove.Record(move);
        Deselect();
    }

    private void Deselect()
    {
        Selected = Square.None;
        DragFrom = Square.None;
        PromotionSquare = Square.None;
        _moves.Clear();
    }
}
