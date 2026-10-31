namespace ChessEngine.Core.MoveGen;

/// <summary>
/// Perfect-hash parameters for one square: <c>(occupancy &amp; Mask) * Number &gt;&gt; Shift</c> plus
/// <c>Offset</c> indexes the shared attack table (see <see cref="Attacks"/>).
/// </summary>
internal struct Magic
{
    public ulong Mask;
    public ulong Number;
    public int Shift;
    public int Offset;
}

