using System.Collections.Generic;
using System.Linq;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace GumTestSupport;

/// <summary>
/// Runs the test methods within each class in an order shuffled by
/// <see cref="ShuffledTestCollectionOrderer.SeedVariable"/>, so a method that leaks static state into
/// a later method of the same class fails in some orders (#5840). Without the variable it returns
/// xUnit's default order unchanged. <see cref="ShuffledTestCollectionOrderer"/> shuffles the classes.
/// </summary>
public class ShuffledTestCaseOrderer : ITestCaseOrderer
{
    readonly DefaultTestCaseOrderer _defaultOrderer;

    /// <summary>Called by xUnit when it reads the assembly's <see cref="TestCaseOrdererAttribute"/>.</summary>
    public ShuffledTestCaseOrderer(IMessageSink diagnosticMessageSink)
    {
        _defaultOrderer = new DefaultTestCaseOrderer(diagnosticMessageSink);
    }

    /// <inheritdoc/>
    public IEnumerable<TTestCase> OrderTestCases<TTestCase>(IEnumerable<TTestCase> testCases) where TTestCase : ITestCase
    {
        IEnumerable<TTestCase> defaultOrder = _defaultOrderer.OrderTestCases(testCases);

        if (!ShuffledTestCollectionOrderer.TryReadSeed(out int seed))
        {
            return defaultOrder;
        }

        // Shuffling the default (sorted) order keeps a seed reproducible on any machine.
        List<TTestCase> shuffled = defaultOrder.ToList();
        ShuffledTestCollectionOrderer.Shuffle(shuffled, seed);
        return shuffled;
    }
}
