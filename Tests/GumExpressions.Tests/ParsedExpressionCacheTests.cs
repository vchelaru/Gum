using Gum.Expressions;
using Shouldly;

namespace GumExpressions.Tests;

public class ParsedExpressionCacheTests
{
    [Fact]
    public void GetOrParse_SameText_ReturnsSameSyntaxInstance()
    {
        ParsedExpressionCache cache = new ParsedExpressionCache(maxEntries: 10);

        object first = cache.GetOrParse("Width + 10");
        object second = cache.GetOrParse("Width + 10");

        second.ShouldBeSameAs(first);
        cache.Count.ShouldBe(1);
    }

    [Fact]
    public void GetOrParse_EditedText_ParsesFresh()
    {
        ParsedExpressionCache cache = new ParsedExpressionCache(maxEntries: 10);

        object before = cache.GetOrParse("Width + 10");
        object after = cache.GetOrParse("Width + 20");

        after.ShouldNotBeSameAs(before);
        after.ToString().ShouldBe("Width + 20");
    }

    [Fact]
    public void GetOrParse_ExceedsMaxEntries_StaysBounded()
    {
        ParsedExpressionCache cache = new ParsedExpressionCache(maxEntries: 3);

        for (int i = 0; i < 10; i++)
        {
            cache.GetOrParse($"Width + {i}");
        }

        cache.Count.ShouldBeLessThanOrEqualTo(3);
        cache.GetOrParse("Width + 9").ToString().ShouldBe("Width + 9");
    }
}
