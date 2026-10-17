using GumTestSupport;
using Moq;
using Shouldly;
using Xunit.Sdk;
using Xunit.v3;

namespace MonoGameGum.IntegrationTests.TestOrdering;

// Needs no GraphicsDevice, so it is not in the SharedGame collection.
public class ShuffledTestCaseOrdererTests : IDisposable
{
    private readonly string? _originalSeed;
    private readonly List<ITestCase> _testCases;

    public ShuffledTestCaseOrdererTests()
    {
        _originalSeed = Environment.GetEnvironmentVariable(TestShuffle.SeedVariable);
        _testCases = new List<ITestCase>();
        for (int i = 0; i < 20; i++)
        {
            Mock<ITestCase> testCase = new Mock<ITestCase>();
            testCase.SetupGet(x => x.UniqueID).Returns(Guid.NewGuid().ToString());
            testCase.SetupGet(x => x.TestCaseDisplayName).Returns($"Test {i}");
            _testCases.Add(testCase.Object);
        }
    }

    public void Dispose()
    {
        Environment.SetEnvironmentVariable(TestShuffle.SeedVariable, _originalSeed);
    }

    [Fact]
    public void OrderTestCases_WithoutSeed_MatchesDefaultOrder()
    {
        Environment.SetEnvironmentVariable(TestShuffle.SeedVariable, null);

        List<ITestCase> ordered = new ShuffledTestCaseOrderer().OrderTestCases(_testCases).ToList();

        List<ITestCase> expected = DefaultTestCaseOrderer.Instance.OrderTestCases(_testCases).ToList();
        ordered.ShouldBe(expected);
    }

    [Fact]
    public void OrderTestCases_WithSeed_IsReproduciblePermutationOfDefaultOrder()
    {
        Environment.SetEnvironmentVariable(TestShuffle.SeedVariable, "12345");
        List<ITestCase> defaultOrder = DefaultTestCaseOrderer.Instance.OrderTestCases(_testCases).ToList();

        List<ITestCase> first = new ShuffledTestCaseOrderer().OrderTestCases(_testCases).ToList();
        List<ITestCase> second = new ShuffledTestCaseOrderer().OrderTestCases(_testCases.AsEnumerable().Reverse().ToList()).ToList();

        first.ShouldBe(second);
        first.ShouldBe(defaultOrder, ignoreOrder: true);
        first.ShouldNotBe(defaultOrder);
    }

    [Fact]
    public void OrderTestCases_NonIntegerSeed_Throws()
    {
        Environment.SetEnvironmentVariable(TestShuffle.SeedVariable, "abc");

        Should.Throw<InvalidOperationException>(() => new ShuffledTestCaseOrderer().OrderTestCases(_testCases).ToList());
    }
}
