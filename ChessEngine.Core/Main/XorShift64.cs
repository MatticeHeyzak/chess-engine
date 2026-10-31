namespace ChessEngine.Core.Main;

/// <summary>
/// Small, fast, deterministic PRNG (xorshift64*). Determinism matters: Zobrist keys and magic
/// numbers are generated from fixed seeds, so every run and machine gets identical tables.
/// </summary>
public struct XorShift64(ulong seed)
{
    /// <summary>Replaces a zero seed, because an all-zero state would produce zeros forever.</summary>
    private const ulong DefaultSeed = 0x9E3779B97F4A7C15UL;

    private const int FirstShift = 12;
    private const int SecondShift = 25;
    private const int ThirdShift = 27;
    private const ulong Multiplier = 2685821657736338717UL;

    private ulong _state = seed == 0 ? DefaultSeed : seed;

    public ulong Next()
    {
        _state ^= _state >> FirstShift;
        _state ^= _state << SecondShift;
        _state ^= _state >> ThirdShift;
        return _state * Multiplier;
    }

    /// <summary>ANDs three outputs together: few set bits, which is what good magic-number candidates look like.</summary>
    public ulong NextSparse() => Next() & Next() & Next();
}

