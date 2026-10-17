using System.Collections.Generic;
using System.Linq;
using Xunit.Sdk;
using Xunit.v3;

namespace GumTestSupport;

/// <summary>
/// The xunit v3 counterpart of the v2 <c>ShuffledTestCaseOrderer</c>: runs the test methods within
/// each class in an order shuffled by <see cref="TestShuffle.SeedVariable"/>, so a method that leaks
/// static state into a later method of the same class fails in some orders. Without the variable it
/// returns xUnit's default order unchanged. xunit v3 has no class orderer, so the order of the
/// classes comes from the test project's framework executor (<see cref="TestShuffle.OrderByShuffledKey{T}"/>).
/// </summary>
public class ShuffledTestCaseOrderer : ITestCaseOrderer
{
    /// <inheritdoc/>
    public IReadOnlyCollection<TTestCase> OrderTestCases<TTestCase>(IReadOnlyCollection<TTestCase> testCases)
        where TTestCase : notnull, ITestCase
    {
        IReadOnlyCollection<TTestCase> defaultOrder = DefaultTestCaseOrderer.Instance.OrderTestCases(testCases);

        if (!TestShuffle.TryReadSeed(out int seed))
        {
            return defaultOrder;
        }

        // Shuffling the default (sorted) order keeps a seed reproducible on any machine.
        List<TTestCase> shuffled = defaultOrder.ToList();
        TestShuffle.Shuffle(shuffled, seed);
        return shuffled;
    }
}
