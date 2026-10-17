using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace GumTestSupport;

/// <summary>
/// The xunit-version-neutral half of the shuffled-order support: the seed variable, the shuffle and
/// the order file. The xunit v2 orderers (Tests/TestOrdering) and the xunit v3 ones
/// (Tests/TestOrdering/Xunit3) both build on it, so a seed replays the same way in either.
/// </summary>
public static class TestShuffle
{
    /// <summary>Environment variable holding the integer shuffle seed.</summary>
    public const string SeedVariable = "GUM_TEST_SHUFFLE_SEED";

    /// <summary>Optional environment variable naming a file that receives the shuffled order.</summary>
    public const string OrderFileVariable = "GUM_TEST_SHUFFLE_ORDER_FILE";

    /// <summary>Reads <see cref="SeedVariable"/>; false when it is unset, throws when it isn't an integer.</summary>
    public static bool TryReadSeed(out int seed)
    {
        seed = 0;
        string? seedText = Environment.GetEnvironmentVariable(SeedVariable);
        if (string.IsNullOrEmpty(seedText))
        {
            return false;
        }
        if (!int.TryParse(seedText, out seed))
        {
            throw new InvalidOperationException($"{SeedVariable} must be an integer, but was \"{seedText}\".");
        }
        return true;
    }

    /// <summary>Fisher-Yates shuffle of <paramref name="items"/> in place, driven by <paramref name="seed"/>.</summary>
    public static void Shuffle<T>(List<T> items, int seed)
    {
        Random random = new Random(seed);
        for (int i = items.Count - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            (items[i], items[j]) = (items[j], items[i]);
        }
    }

    /// <summary>Writes <paramref name="lines"/> to the <see cref="OrderFileVariable"/> file, if one is named.</summary>
    public static void WriteOrderFile(IEnumerable<string> lines)
    {
        string? orderFile = Environment.GetEnvironmentVariable(OrderFileVariable);
        if (!string.IsNullOrEmpty(orderFile))
        {
            File.WriteAllLines(orderFile, lines);
        }
    }

    /// <summary>
    /// Reorders <paramref name="items"/> so the items sharing a <paramref name="key"/> (a test class)
    /// stay together and the groups appear in an order shuffled by <paramref name="seed"/>. Items keep
    /// their relative order within a group. The groups are shuffled from their sorted order, so a seed
    /// reproduces on any machine. <paramref name="order"/> receives the shuffled keys.
    /// </summary>
    public static List<T> OrderByShuffledKey<T>(IEnumerable<T> items, Func<T, string> key, int seed, out List<string> order)
    {
        List<T> itemList = items.ToList();
        order = itemList.Select(key).Distinct().OrderBy(groupKey => groupKey, StringComparer.Ordinal).ToList();
        Shuffle(order, seed);

        Dictionary<string, int> rankByKey = new Dictionary<string, int>();
        for (int i = 0; i < order.Count; i++)
        {
            rankByKey[order[i]] = i;
        }

        // OrderBy is stable, so each group keeps its incoming order.
        return itemList.OrderBy(item => rankByKey[key(item)]).ToList();
    }
}
