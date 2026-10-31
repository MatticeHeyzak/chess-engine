using ChessEngine.Core.Main;
using ChessEngine.Core.MoveGen;

namespace ChessEngine.Test;

/// <summary>Checks the magic-bitboard tables against the slow ray-walking reference.</summary>
public class AttackTests
{
    private const int RandomOccupancies = 2000;
    private const ulong Seed = 0xC0FFEE1234567UL;

    [Fact]
    public void MagicRookAttacksMatchReference()
    {
        var rng = new XorShift64(Seed);

        for (var i = 0; i < RandomOccupancies; i++)
        {
            var occupancy = rng.NextSparse() | rng.NextSparse();

            for (var square = 0; square < Square.Count; square++)
                Assert.Equal(Attacks.SlowRook(square, occupancy), Attacks.Rook(square, occupancy));
        }
    }

    [Fact]
    public void MagicBishopAttacksMatchReference()
    {
        var rng = new XorShift64(Seed);

        for (var i = 0; i < RandomOccupancies; i++)
        {
            var occupancy = rng.NextSparse() | rng.NextSparse();

            for (var square = 0; square < Square.Count; square++)
                Assert.Equal(Attacks.SlowBishop(square, occupancy), Attacks.Bishop(square, occupancy));
        }
    }
}

