using Gum.GueDeriving;
using Gum.Wireframe;
using MonoGameGum.TestsCommon;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Performance;

/// <summary>
/// <see cref="TextRuntime.GetTagAtIndex"/> (#7062) is meant to be called every frame while a link
/// is hovered, so it allocates nothing beyond the returned struct.
/// </summary>
public class TextTagQueryAllocationTests : BaseTestClass
{
    [Fact]
    public void GetTagAtIndex_InSteadyState_AllocatesNothing()
    {
        TextRuntime textRuntime = new();
        textRuntime.Text = "Click [Link=shop]here[/Link] or [Link=bag]there[/Link] now";

        int hits = 0;
        AllocationResult result = AllocationMeasurer.MeasureMinimum(
            () =>
            {
                if (textRuntime.GetTagAtIndex("Link", 7) != null)
                {
                    hits++;
                }
                if (textRuntime.GetTagAtIndex("link", 0) == null)
                {
                    hits++;
                }
            },
            attempts: 3,
            warmupIterations: 50,
            measuredIterations: 200);

        // Liveness: proves the query found the link (and missed the plain text) on every iteration.
        hits.ShouldBeGreaterThanOrEqualTo(2 * 200 * 3);
        result.TotalBytes.ShouldBe(0);
    }
}
