using ChessEngine.Core.BoardState;
using ChessEngine.Core.Main;
using ChessEngine.Core.MoveGen;
using Raylib_cs;

namespace ChessEngine.Ui.Updates;

public sealed class BoardInteraction(Board board, BoardLayout layout) : IUpdatable
{
    private readonly List<Move> _moves = [];

    public int Selected { get; private set; } = Square.None;
    public IReadOnlyList<Move> Moves => _moves;
    
    public void Update()
    {
        if (!Raylib.IsMouseButtonPressed(MouseButton.Left))
            return;
        
        var square = layout.ScreenToSquare(Raylib.GetMouseX(), Raylib.GetMouseY());
        if (square == Square.None)
        {
            Deselect();
            return;
        }

        // Clicking a target square of the selected piece plays that move.
        foreach (var move in _moves)
        {
            if (move.To != square)
                continue;
            
            board.MakeMove(move);
            Deselect();
            return;
        }

        var piece = board.PieceAt(square);
        if (piece != Piece.None && Piece.ColorOf(piece) == board.SideToMove)
            Select(square);
        else
            Deselect();
    }

    private void Select(int square)
    {
        Selected = square;
        _moves.Clear();

        var list = new MoveList();
        MoveGenerator.Generate(board, ref list);

        for (var i = 0; i < list.Count; i++)
        {
            if (list[i].From == square)
                _moves.Add(list[i]);
        }
    }
    
    private void Deselect()
    {
        Selected = Square.None;
        _moves.Clear();
    }
}