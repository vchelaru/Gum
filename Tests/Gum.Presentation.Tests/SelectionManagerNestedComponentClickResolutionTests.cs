using Gum.Commands;
using Gum.DataTypes;
using Gum.Input;
using Gum.Managers;
using Gum.Plugins;
using Gum.Services.Dialogs;
using Gum.ToolStates;
using Gum.Undo;
using Gum.Wireframe;
using Moq;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using Shouldly;
using System.Collections.Generic;

namespace Gum.Presentation.Tests;

/// <summary>
/// Regression pin for a mistaken "fix" attempted on #4545: SelectionManager.GetElementOrInstanceForIpso
/// walks up from whatever visual got hit until it reaches something in AllIpsos - that's how it knows
/// it has reached a real top-level instance, per the method's own documented intent ("we only want to
/// select IPSOs that represent the current element or its InstanceSaves - not any children"). That walk-up
/// only works because AllIpsos contains ONLY each element's own top-level instances. A nested component
/// instance's internal children (e.g. a ScrollBar's Track/Thumb inside a ScrollViewer) must never be
/// members of AllIpsos, or the walk-up exits immediately at the deepest hit ipso and a click anywhere on
/// that internal visual resolves to the internal instance instead of the top-level owner - breaking the
/// "component instances are atomic until entered" WYSIWYG convention.
/// </summary>
public class SelectionManagerNestedComponentClickResolutionTests : BaseTestClass
{
    [Fact]
    public void GetRepresentationAt_ClickOnNestedComponentInternalVisual_ResolvesToTopLevelInstance()
    {
        ScreenSave screen = new ScreenSave { Name = "TestScreen" };
        InstanceSave outerInstance = new InstanceSave { Name = "OuterInstance", ParentContainer = screen };
        screen.Instances.Add(outerInstance);

        var screenGue = new GraphicalUiElement(new InvisibleRenderable()) { Name = "TestScreen", Tag = screen };

        // OuterInstance: a top-level Screen instance that is itself a component instance (e.g. a
        // ScrollViewer), owning its own internal instances.
        var outerGue = new GraphicalUiElement(new InvisibleRenderable())
        {
            Name = "OuterInstance",
            Tag = outerInstance,
            Width = 100,
            Height = 100
        };
        outerGue.Parent = screenGue;
        outerGue.ElementGueContainingThis = screenGue;

        // InnerInstance: owned by OuterInstance (mirrors a ScrollViewer's internal ScrollBar
        // instance), itself owning a Grandchild (mirrors that ScrollBar's own Thumb).
        var innerGue = new GraphicalUiElement(new InvisibleRenderable())
        {
            Name = "InnerInstance",
            Width = 20,
            Height = 100
        };
        innerGue.Parent = outerGue;
        innerGue.ElementGueContainingThis = outerGue;

        var grandchildGue = new GraphicalUiElement(new InvisibleRenderable())
        {
            Name = "Grandchild",
            Width = 20,
            Height = 30
        };
        grandchildGue.Parent = innerGue;
        grandchildGue.ElementGueContainingThis = innerGue;

        // AllIpsos, as WireframeObjectManager.AddChildrenRecursively actually builds it, contains
        // only the current element's own top-level instances - never the internals of a nested
        // component instance.
        var allIpsos = new List<GraphicalUiElement> { screenGue, outerGue };

        var mockWireframeManager = new Mock<IWireframeObjectManager>();
        mockWireframeManager.SetupGet(x => x.AllIpsos).Returns(allIpsos);
        mockWireframeManager.Setup(x => x.IsRepresentation(It.IsAny<IPositionedSizedObject>()))
            .Returns((IPositionedSizedObject ipso) => allIpsos.Contains(ipso));
        mockWireframeManager.Setup(x => x.GetSelectedRepresentations()).Returns(System.Array.Empty<GraphicalUiElement>());
        mockWireframeManager.Setup(x => x.GetRepresentation(outerInstance, It.IsAny<List<ElementWithState>>()))
            .Returns(outerGue);

        var mockPreciseHitTester = new Mock<IPreciseHitTester>();
        mockPreciseHitTester
            .Setup(x => x.HasCursorOver(It.IsAny<GraphicalUiElement>(), It.IsAny<float>(), It.IsAny<float>()))
            .Returns((GraphicalUiElement e, float x, float y) => e.HasCursorOver(x, y));

        var selectionManager = new SelectionManager(
            Mock.Of<ISelectedState>(),
            Mock.Of<IUndoManager>(),
            Mock.Of<IContextMenuState>(),
            Mock.Of<IDialogService>(),
            Mock.Of<IHotkeyManager>(),
            mockWireframeManager.Object,
            Mock.Of<IGuiCommands>(),
            Mock.Of<IWireframeEditorFactory>(),
            Mock.Of<INineSliceCoordinateRefresher>(),
            mockPreciseHitTester.Object);

        var elementStack = new List<ElementWithState> { new ElementWithState(screen) };

        // Click coordinates land inside Grandchild's bounds, which are nested entirely within
        // InnerInstance's bounds, which are nested entirely within OuterInstance's bounds.
        var result = selectionManager.GetRepresentationAt(5, 5, trySkipSelected: false, elementStack);

        result.ShouldBe(outerGue);
    }
}
