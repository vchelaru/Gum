using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Plugins.InternalPlugins.EditorTab.Views;
using GumRuntime;
using Shouldly;
using Xunit;

namespace GumToolUnitTests.Wireframe;

/// <summary>
/// Pins the Gum tool's own type-instantiation registration (WireframeControl.InitializeDefaultTypeInstantiation),
/// which is separate from - and must be kept in sync with - the runtime's own registration
/// (RenderingLibrary.SystemManagers.RegisterComponentRuntimeInstantiations). A standard type
/// missing here falls back to a plain GraphicalUiElement wrapping a raw renderable at design time;
/// most CustomSetPropertyOnRenderable dispatch branches cast to the typed Runtime and silently
/// no-op if that fails, so the symptom is a control that shows the correct value in the Variables
/// grid but never actually renders it (confirmed for NineSlice: Red/Green/Blue stuck at the
/// renderable's default white, reported against a real Standard-theme project).
/// </summary>
public class WireframeControlDefaultTypeInstantiationTests
{
    public WireframeControlDefaultTypeInstantiationTests()
    {
        ElementSaveExtensions.Reset();
    }

    [Theory]
    // Circle/Polygon are deliberately excluded - their constructors reach RenderingLibrary.Graphics.Renderer.Self,
    // which needs a real graphics context (SystemManagers.Default initialized), unavailable in this
    // headless test process. They were already correctly registered before this fix; this test's
    // purpose is pinning the one that wasn't (NineSlice), not full coverage.
    [InlineData("ColoredRectangle", typeof(ColoredRectangleRuntime))]
    [InlineData("NineSlice", typeof(NineSliceRuntime))]
    [InlineData("Sprite", typeof(SpriteRuntime))]
    public void InitializeDefaultTypeInstantiation_ShouldRegisterEveryStandardType_SoInstancesConstructAsTheTypedRuntime(
        string standardElementName, System.Type expectedRuntimeType)
    {
        WireframeControl.InitializeDefaultTypeInstantiation();

        StandardElementSave elementSave = new() { Name = standardElementName };
        var gue = ElementSaveExtensions.CreateGueForElement(elementSave);

        gue.ShouldBeOfType(expectedRuntimeType);
    }

    /// <summary>
    /// Container is the one standard type the tool must NOT register (issue #4386). Every registered
    /// runtime installs its own renderable in its constructor, which makes SetGraphicalUiElement skip
    /// CreateGraphicalComponent - and CreateGraphicalComponent is the only path that consults
    /// GraphicalUiElement.ShowLineRectangles and hands back the dotted LineRectangle the editor draws
    /// for a Container when Show Outlines is checked. Registering ContainerRuntime silently swapped
    /// that outline for an InvisibleRenderable, making containers invisible in the editor.
    /// </summary>
    [Fact]
    public void InitializeDefaultTypeInstantiation_ShouldLeaveContainerUnregistered_SoShowOutlinesCanBackItWithADottedLineRectangle()
    {
        WireframeControl.InitializeDefaultTypeInstantiation();

        StandardElementSave containerElement = new() { Name = "Container" };
        var gue = ElementSaveExtensions.CreateGueForElement(containerElement);

        gue.RenderableComponent.ShouldBeNull();
    }
}
