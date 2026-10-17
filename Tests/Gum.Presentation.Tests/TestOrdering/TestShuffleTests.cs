using GumTestSupport;
using Shouldly;

namespace Gum.Presentation.Tests.TestOrdering;

public class TestShuffleTests
{
    static List<string> BuildTestCases()
    {
        List<string> testCases = new List<string>();
        for (int classIndex = 0; classIndex < 12; classIndex++)
        {
            for (int methodIndex = 0; methodIndex < 3; methodIndex++)
            {
                testCases.Add($"Class{classIndex:00}.Method{methodIndex}");
            }
        }
        return testCases;
    }

    static string ClassOf(string testCase) => testCase.Substring(0, testCase.IndexOf('.'));

    [Fact]
    public void OrderByShuffledKey_KeepsEachClassTogetherAndInItsOwnOrder()
    {
        List<string> testCases = BuildTestCases();

        List<string> ordered = TestShuffle.OrderByShuffledKey(testCases, ClassOf, 12345, out List<string> order);

        ordered.ShouldBe(testCases, ignoreOrder: true);
        ordered.Select(ClassOf).Distinct().ToList().ShouldBe(order);
        foreach (string className in order)
        {
            ordered.Where(testCase => ClassOf(testCase) == className)
                .ShouldBe(testCases.Where(testCase => ClassOf(testCase) == className));
        }
    }

    [Fact]
    public void OrderByShuffledKey_SameSeed_ReproducesOrderWhateverTheInputOrder()
    {
        List<string> testCases = BuildTestCases();

        List<string> first = TestShuffle.OrderByShuffledKey(testCases, ClassOf, 12345, out List<string> firstOrder);
        TestShuffle.OrderByShuffledKey(Enumerable.Reverse(testCases), ClassOf, 12345, out List<string> secondOrder);

        secondOrder.ShouldBe(firstOrder);
        first.Select(ClassOf).Distinct().ShouldBe(firstOrder);
    }

    [Fact]
    public void OrderByShuffledKey_DifferentSeeds_GiveDifferentClassOrders()
    {
        List<string> testCases = BuildTestCases();

        TestShuffle.OrderByShuffledKey(testCases, ClassOf, 1, out List<string> orderOne);
        TestShuffle.OrderByShuffledKey(testCases, ClassOf, 2, out List<string> orderTwo);

        orderOne.ShouldNotBe(orderTwo);
    }
}
