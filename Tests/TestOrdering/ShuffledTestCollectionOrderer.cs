using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace GumTestSupport;

/// <summary>
/// Runs test collections (one per test class unless a class names a shared collection) in an order
/// shuffled by the seed in <see cref="SeedVariable"/>, so a class that leaks static state into a
/// later one fails in some orders (#5820). Without the variable it returns xUnit's default order
/// unchanged. The weekly shuffled-order workflow sets the seed; set it locally to replay a failure.
/// </summary>
public class ShuffledTestCollectionOrderer : ITestCollectionOrderer
{
    /// <summary>Environment variable holding the integer shuffle seed.</summary>
    public const string SeedVariable = "GUM_TEST_SHUFFLE_SEED";

    /// <summary>Optional environment variable naming a file that receives the shuffled order.</summary>
    public const string OrderFileVariable = "GUM_TEST_SHUFFLE_ORDER_FILE";

    readonly DefaultTestCollectionOrderer _defaultOrderer;

    /// <summary>Called by xUnit when it reads the assembly's <see cref="TestCollectionOrdererAttribute"/>.</summary>
    public ShuffledTestCollectionOrderer()
    {
        _defaultOrderer = new DefaultTestCollectionOrderer();
    }

    /// <inheritdoc/>
    public IEnumerable<ITestCollection> OrderTestCollections(IEnumerable<ITestCollection> testCollections)
    {
        IEnumerable<ITestCollection> defaultOrder = _defaultOrderer.OrderTestCollections(testCollections);

        string? seedText = Environment.GetEnvironmentVariable(SeedVariable);
        if (string.IsNullOrEmpty(seedText))
        {
            return defaultOrder;
        }
        if (!int.TryParse(seedText, out int seed))
        {
            throw new InvalidOperationException($"{SeedVariable} must be an integer, but was \"{seedText}\".");
        }

        // Shuffling the default (sorted) order keeps a seed reproducible on any machine.
        List<ITestCollection> shuffled = defaultOrder.ToList();
        Random random = new Random(seed);
        for (int i = shuffled.Count - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
        }

        string? orderFile = Environment.GetEnvironmentVariable(OrderFileVariable);
        if (!string.IsNullOrEmpty(orderFile))
        {
            File.WriteAllLines(orderFile, shuffled.Select(collection => collection.DisplayName));
        }

        return shuffled;
    }
}
