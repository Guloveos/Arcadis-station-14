using System;
using System.Collections.Generic;
using Content.Shared;
using NUnit.Framework;
using Robust.Shared.Maths;
using Robust.UnitTesting;

namespace Content.Tests.Shared;

[TestFixture, TestOf(typeof(Span<Direction>)), Parallelizable(ParallelScope.Self)]
public sealed class DirectionRandomizerTest : RobustUnitTest
{
    [TestCase(new[]
    {
        Direction.East,
        Direction.NorthEast,
        Direction.West,
        Direction.NorthWest,
        Direction.South,
        Direction.SouthWest,
        Direction.North,
        Direction.SouthEast,
    })]
    [TestCase(new[]
    {
        Direction.East,
        Direction.West,
        Direction.South,
        Direction.North,
    })]
    [TestCase(new[]
    {
        Direction.East,
        Direction.West,
    })]
    public void TestRandomization(Direction[] x)
    {
        var set = new HashSet<Direction>(x);
        var randomizer = new Span<Direction>(x);
        randomizer.Shuffle();
        foreach (var direction in randomizer)
        {
            if (!set.Remove(direction))
            {
                // Asserts no double direction
                Assert.Fail("Post randomization the enumerator had repeated direction");
            }
        }
        // Because of above foreach this asserts
        // rand[1,2,3] - [1,2,3] == {}
        // i.e. randomized set minus original set is empty
        Assert.That(set, Is.Empty, "Each element must appear once ");
    }
}
