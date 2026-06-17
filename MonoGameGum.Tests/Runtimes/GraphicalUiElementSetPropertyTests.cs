using Gum.Managers;
using Gum.Wireframe;
using RenderingLibrary.Graphics;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Runtimes;

/// <summary>
/// Covers the data-driven (.gumx -> ApplyState -> SetProperty) coercions for GraphicalUiElement's
/// own enum properties handled by the hardcoded TrySetValueOnThis switch (e.g. ChildrenLayout,
/// XOrigin). These must tolerate a string name or an underlying int the same way the renderable
/// reflection path does (see SetPropertyThroughReflectionTests) - a string name produced by a
/// variable reference or hand-written file must not crash the editor under FULL_DIAGNOSTICS.
/// </summary>
public class GraphicalUiElementSetPropertyTests : BaseTestClass
{
    [Fact]
    public void SetProperty_EnumToChildrenLayout_AssignsValue()
    {
        // The already-typed path must keep working unchanged.
        GraphicalUiElement gue = new GraphicalUiElement();
        gue.SetProperty("ChildrenLayout", ChildrenLayout.LeftToRightStack);
        gue.ChildrenLayout.ShouldBe(ChildrenLayout.LeftToRightStack);
    }

    [Fact]
    public void SetProperty_IntToChildrenLayout_AssignsEnumValue()
    {
        GraphicalUiElement gue = new GraphicalUiElement();
        Should.NotThrow(() => gue.SetProperty("ChildrenLayout", 2));
        gue.ChildrenLayout.ShouldBe(ChildrenLayout.LeftToRightStack);
    }

    [Fact]
    public void SetProperty_StringToChildrenLayout_IsCaseInsensitive()
    {
        GraphicalUiElement gue = new GraphicalUiElement();
        Should.NotThrow(() => gue.SetProperty("ChildrenLayout", "lefttorightstack"));
        gue.ChildrenLayout.ShouldBe(ChildrenLayout.LeftToRightStack);
    }

    [Fact]
    public void SetProperty_StringToChildrenLayout_ParsesEnumName()
    {
        // Repro of the editor crash: a variable reference materializes ChildrenLayout as the
        // string "LeftToRightStack"; the typed setter must parse it rather than throw.
        GraphicalUiElement gue = new GraphicalUiElement();
        Should.NotThrow(() => gue.SetProperty("ChildrenLayout", "LeftToRightStack"));
        gue.ChildrenLayout.ShouldBe(ChildrenLayout.LeftToRightStack);
    }

    [Fact]
    public void SetProperty_StringToXOrigin_ParsesEnumName()
    {
        GraphicalUiElement gue = new GraphicalUiElement();
        Should.NotThrow(() => gue.SetProperty("XOrigin", "Center"));
        gue.XOrigin.ShouldBe(HorizontalAlignment.Center);
    }
}
