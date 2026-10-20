using Gum.Tests.Shared;
using Shouldly;
using System.Collections.Generic;

namespace MonoGameGum.Shapes.Tests;

// Every shim in a legacy namespace records its earliest error and removal dates in its [Obsolete]
// message (and in a comment above the attribute). Covers MonoGameGum.dll and MonoGameGumShapes.dll;
// the Skia shims are covered in SkiaGum.Tests.
public class ShimRetentionTests
{
    [Fact]
    public void EveryLegacyNamespaceShim_RecordsEarliestRemovalDates()
    {
        List<string> violations = new List<string>();
        violations.AddRange(ShimRetention.FindViolations(typeof(Gum.GumService).Assembly));
        violations.AddRange(ShimRetention.FindViolations(typeof(Gum.Renderables.ShapeRenderer).Assembly));

        violations.ShouldBeEmpty();
    }
}
