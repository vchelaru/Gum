using Gum.Tests.Shared;
using Shouldly;
using Xunit;

namespace SkiaGum.Tests.GueDeriving;

// Every shim in a legacy namespace records its earliest error and removal dates in its [Obsolete]
// message (and in a comment above the attribute). Covers SkiaGum.dll; the MonoGameGum and shapes
// shims are covered in MonoGameGum.Shapes.Tests.
public class ShimRetentionTests
{
    [Fact]
    public void EverySkiaShim_RecordsEarliestRemovalDates()
    {
        ShimRetention.FindViolations(typeof(SkiaGum.CustomSetPropertyOnRenderable).Assembly).ShouldBeEmpty();
    }
}
