using ChessEngine.Core.Main;

namespace ChessEngine.Core.MoveGen;

/// <summary>
/// Finds magic numbers by random trial at start-up. For every square, every subset of the relevant
/// blockers must map to a table slot that is either unused or already holds the same attack set
/// (a harmless collision). Slides are computed the slow way once here, so lookups are O(1) afterwards.
/// </summary>
internal sealed class MagicGenerator((int Df, int Dr)[] directions, XorShift64 rng)
{
    /// <summary>A mask has at most 12 relevant bits (a rook in a corner), hence at most 2^12 blocker subsets.</summary>
    private const int MaxSubsets = 1 << 12;

    private const int BitsInKey = 64;

    // Good magics spread the mask bits into the top byte; candidates that don't are rejected cheaply.
    private const int TopByteShift = 56;
    private const int MinTopByteBits = 6;

    private readonly ulong[] _occupancies = new ulong[MaxSubsets];
    private readonly ulong[] _references = new ulong[MaxSubsets];
    private readonly ulong[] _used = new ulong[MaxSubsets];
    private readonly int[] _epoch = new int[MaxSubsets]; // avoids clearing _used between attempts

    private XorShift64 _rng = rng;
    private int _attempt;
    private int _offset;

    /// <summary>Fills <paramref name="magics"/> and the shared <paramref name="table"/> for all 64 squares.</summary>
    public void Generate(Magic[] magics, ulong[] table)
    {
        for (var square = 0; square < Square.Count; square++)
            magics[square] = BuildSquare(square, table);
    }

    private Magic BuildSquare(int square, ulong[] table)
    {
        var mask = RelevantMask(square);
        var bits = Bitboard.PopCount(mask);
        var shift = BitsInKey - bits;

        var count = EnumerateSubsets(square, mask);
        var number = FindMagicNumber(mask, shift, count);

        var magic = new Magic { Mask = mask, Number = number, Shift = shift, Offset = _offset };

        for (var i = 0; i < count; i++)
            table[_offset + IndexOf(_occupancies[i], number, shift)] = _references[i];

        _offset += 1 << bits;
        return magic;
    }

    /// <summary>The squares whose occupancy can change the attack set; edge squares never do, so they are excluded.</summary>
    private ulong RelevantMask(int square)
    {
        var edges = ((Bitboard.Rank1 | Bitboard.Rank8) & ~Bitboard.Ranks[Square.RankOf(square)])
                    | ((Bitboard.FileA | Bitboard.FileH) & ~Bitboard.Files[Square.FileOf(square)]);

        return Attacks.Slide(square, 0, directions) & ~edges;
    }

    /// <summary>Records every subset of the mask with its true attack set (carry-rippler trick); returns how many.</summary>
    private int EnumerateSubsets(int square, ulong mask)
    {
        var count = 0;
        ulong subset = 0;

        do
        {
            _occupancies[count] = subset;
            _references[count] = Attacks.Slide(square, subset, directions);
            count++;
            subset = (subset - mask) & mask;
        } while (subset != 0);

        return count;
    }

    private ulong FindMagicNumber(ulong mask, int shift, int count)
    {
        while (true)
        {
            var candidate = _rng.NextSparse();

            if (Bitboard.PopCount((mask * candidate) >> TopByteShift) < MinTopByteBits)
                continue;

            _attempt++;

            if (MapsWithoutHarmfulCollisions(candidate, shift, count))
                return candidate;
        }
    }

    private bool MapsWithoutHarmfulCollisions(ulong magic, int shift, int count)
    {
        for (var i = 0; i < count; i++)
        {
            var index = IndexOf(_occupancies[i], magic, shift);

            if (_epoch[index] != _attempt)
            {
                _epoch[index] = _attempt;
                _used[index] = _references[i];
            }
            else if (_used[index] != _references[i])
            {
                return false;
            }
        }

        return true;
    }

    private static int IndexOf(ulong occupancy, ulong magic, int shift)
        => (int)((occupancy * magic) >> shift);
}

