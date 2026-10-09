using GumTestSupport;
using Moq;
using Shouldly;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Gum.Presentation.Tests.TestOrdering;

public class ShuffledTestCaseOrdererTests : IDisposable
{
    readonly string? _originalSeed;
    readonly List<ITestCase> _testCases;
    readonly IMessageSink _messageSink;

    public ShuffledTestCaseOrdererTests()
    {
        _originalSeed = Environment.GetEnvironmentVariable(ShuffledTestCollectionOrderer.SeedVariable);
        _messageSink = new Mock<IMessageSink>().Object;
        _testCases = new List<ITestCase>();
        for (int i = 0; i < 20; i++)
        {
            Mock<ITestCase> testCase = new Mock<ITestCase>();
            testCase.SetupGet(x => x.UniqueID).Returns(Guid.NewGuid().ToString());
            testCase.SetupGet(x => x.DisplayName).Returns($"Test {i}");
            _testCases.Add(testCase.Object);
        }
    }

    public void Dispose()
    {
        Environment.SetEnvironmentVariable(ShuffledTestCollectionOrderer.SeedVariable, _originalSeed);
    }

    [Fact]
    public void OrderTestCases_WithoutSeed_MatchesDefaultOrder()
    {
        Environment.SetEnvironmentVariable(ShuffledTestCollectionOrderer.SeedVariable, null);

        List<ITestCase> ordered = new ShuffledTestCaseOrderer(_messageSink).OrderTestCases(_testCases).ToList();

        List<ITestCase> expected = new DefaultTestCaseOrderer(_messageSink).OrderTestCases(_testCases).ToList();
        ordered.ShouldBe(expected);
    }

    [Fact]
    public void OrderTestCases_WithSeed_IsReproduciblePermutationOfDefaultOrder()
    {
        Environment.SetEnvironmentVariable(ShuffledTestCollectionOrderer.SeedVariable, "12345");
        List<ITestCase> defaultOrder = new DefaultTestCaseOrderer(_messageSink).OrderTestCases(_testCases).ToList();

        List<ITestCase> first = new ShuffledTestCaseOrderer(_messageSink).OrderTestCases(_testCases).ToList();
        List<ITestCase> second = new ShuffledTestCaseOrderer(_messageSink).OrderTestCases(_testCases.AsEnumerable().Reverse()).ToList();

        first.ShouldBe(second);
        first.ShouldBe(defaultOrder, ignoreOrder: true);
        first.ShouldNotBe(defaultOrder);
    }

    [Fact]
    public void OrderTestCases_NonIntegerSeed_Throws()
    {
        Environment.SetEnvironmentVariable(ShuffledTestCollectionOrderer.SeedVariable, "abc");

        Should.Throw<InvalidOperationException>(() => new ShuffledTestCaseOrderer(_messageSink).OrderTestCases(_testCases).ToList());
    }
}
