using GumTestSupport;
using Moq;
using Shouldly;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Gum.Presentation.Tests.TestOrdering;

public class ShuffledTestCollectionOrdererTests : IDisposable
{
    readonly string? _originalSeed;
    readonly string? _originalOrderFile;
    readonly List<ITestCollection> _collections;

    public ShuffledTestCollectionOrdererTests()
    {
        _originalSeed = Environment.GetEnvironmentVariable(ShuffledTestCollectionOrderer.SeedVariable);
        _originalOrderFile = Environment.GetEnvironmentVariable(ShuffledTestCollectionOrderer.OrderFileVariable);
        // Keeps these tests from overwriting the order file of a shuffled run they are part of.
        Environment.SetEnvironmentVariable(ShuffledTestCollectionOrderer.OrderFileVariable, null);
        _collections = new List<ITestCollection>();
        for (int i = 0; i < 20; i++)
        {
            Mock<ITestCollection> collection = new Mock<ITestCollection>();
            collection.SetupGet(x => x.UniqueID).Returns(Guid.NewGuid());
            collection.SetupGet(x => x.DisplayName).Returns($"Collection {i}");
            _collections.Add(collection.Object);
        }
    }

    public void Dispose()
    {
        Environment.SetEnvironmentVariable(ShuffledTestCollectionOrderer.SeedVariable, _originalSeed);
        Environment.SetEnvironmentVariable(ShuffledTestCollectionOrderer.OrderFileVariable, _originalOrderFile);
    }

    [Fact]
    public void OrderTestCollections_WithoutSeed_MatchesDefaultOrder()
    {
        Environment.SetEnvironmentVariable(ShuffledTestCollectionOrderer.SeedVariable, null);

        List<ITestCollection> ordered = new ShuffledTestCollectionOrderer().OrderTestCollections(_collections).ToList();

        List<ITestCollection> expected = new DefaultTestCollectionOrderer().OrderTestCollections(_collections).ToList();
        ordered.ShouldBe(expected);
    }

    [Fact]
    public void OrderTestCollections_WithSeed_IsReproduciblePermutationOfDefaultOrder()
    {
        Environment.SetEnvironmentVariable(ShuffledTestCollectionOrderer.SeedVariable, "12345");
        List<ITestCollection> defaultOrder = new DefaultTestCollectionOrderer().OrderTestCollections(_collections).ToList();

        List<ITestCollection> first = new ShuffledTestCollectionOrderer().OrderTestCollections(_collections).ToList();
        List<ITestCollection> second = new ShuffledTestCollectionOrderer().OrderTestCollections(_collections.AsEnumerable().Reverse()).ToList();

        first.ShouldBe(second);
        first.ShouldBe(defaultOrder, ignoreOrder: true);
        first.ShouldNotBe(defaultOrder);
    }

    [Fact]
    public void OrderTestCollections_NonIntegerSeed_Throws()
    {
        Environment.SetEnvironmentVariable(ShuffledTestCollectionOrderer.SeedVariable, "abc");

        Should.Throw<InvalidOperationException>(() => new ShuffledTestCollectionOrderer().OrderTestCollections(_collections).ToList());
    }
}
