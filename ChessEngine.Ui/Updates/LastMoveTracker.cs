using ChessEngine.Core.Main;

namespace ChessEngine.Ui.Updates;

/// <summary>
/// Remembers the most recent move played on the board so the UI can highlight it.
/// </summary>
public sealed class LastMoveTracker
{
    public Move LastMove { get; private set; } = Move.Null;

    public bool HasMove => !LastMove.IsNull;

    public void Record(Move move) => LastMove = move;
}
