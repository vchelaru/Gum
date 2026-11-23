using Gum.Plugins.InternalPlugins.EditorTab;
using Shouldly;

namespace Gum.Presentation.Tests;

public class EditorTabIncrementalChangeTests
{
    // A scrub's intermediate ticks write the value into the state, so the full commit on release
    // sees "old == new" and skips the wireframe rebuild unless the variable is pushed incrementally.
    // OutlineThickness regenerates a font, which the intermediate ticks suppress, so the release
    // commit is the only thing that applies it.
    [Fact]
    public void PropertiesSupportingIncrementalChange_ShouldContainOutlineThickness()
    {
        EditorTabPluginBase.PropertiesSupportingIncrementalChange.ShouldContain("OutlineThickness");
    }

    // Toggling it in the Variables tab re-lays out the stack in place, like IgnoredByParentSize (#8410).
    [Fact]
    public void PropertiesSupportingIncrementalChange_ShouldContainIgnoredByParentLayout()
    {
        EditorTabPluginBase.PropertiesSupportingIncrementalChange.ShouldContain("IgnoredByParentLayout");
    }
}
