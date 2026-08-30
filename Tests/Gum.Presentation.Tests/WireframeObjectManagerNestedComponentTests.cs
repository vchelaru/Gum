using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Localization;
using Gum.Managers;
using Gum.Plugins;
using Gum.Services.Dialogs;
using Gum.Services.Fonts;
using Gum.ToolStates;
using Gum.Wireframe;
using GumRuntime;
using Moq;
using RenderingLibrary.Graphics;
using Shouldly;

namespace Gum.Presentation.Tests;

/// <summary>
/// Pins #4545: AllIpsos must include instances nested two or more component levels deep
/// (e.g. a ScrollBar's Thumb inside a ScrollViewer inside a Screen), not just an element's
/// direct top-level instances. SelectionManager.GetRepresentationAt hit-tests against
/// AllIpsos to decide whether the cursor is "over body"; anything silently excluded here
/// falls through to the rectangle/marquee selector instead of being treated as a click on
/// that instance.
/// </summary>
public class WireframeObjectManagerNestedComponentTests : BaseTestClass
{
    [Fact]
    public void RefreshAll_IncludesInstancesNestedTwoComponentLevelsDeep_InAllIpsos()
    {
        ScreenSave screen = new ScreenSave { Name = "TestScreen" };
        StateSave defaultState = new StateSave { Name = "Default", ParentContainer = screen };
        screen.States.Add(defaultState);

        InstanceSave scrollViewerInstance = new InstanceSave { Name = "ScrollViewerInstance", BaseType = "Container", ParentContainer = screen };
        screen.Instances.Add(scrollViewerInstance);

        StandardElementSave containerStandard = new StandardElementSave { Name = "Container" };
        containerStandard.States.Add(new StateSave { Name = "Default", ParentContainer = containerStandard });

        GumProjectSave project = new GumProjectSave();
        project.StandardElements.Add(containerStandard);
        project.Screens.Add(screen);
        ObjectFinder.Self.GumProjectSave = project;

        GraphicalUiElement? scrollViewerGueLive = null;
        GraphicalUiElement? scrollBarGueLive = null;
        GraphicalUiElement? thumbGueLive = null;

        Mock<IPluginManager> pluginManager = new Mock<IPluginManager>();
        pluginManager.Setup(x => x.CreateGraphicalUiElement(screen)).Returns(() =>
        {
            var root = new GraphicalUiElement(new InvisibleRenderable()) { Name = "TestScreen" };

            // ScrollViewerInstance: a top-level Screen instance that is itself a component
            // instance owning its own internal instances.
            var scrollViewerGue = new GraphicalUiElement(new InvisibleRenderable()) { Name = "ScrollViewerInstance", Tag = scrollViewerInstance };
            scrollViewerGue.Parent = root;
            scrollViewerGue.ElementGueContainingThis = root;

            // VerticalScrollBarInstance: owned by ScrollViewerInstance (mirrors the real
            // ScrollViewer component nesting a ScrollBar instance), itself owning a Thumb.
            var scrollBarGue = new GraphicalUiElement(new InvisibleRenderable()) { Name = "VerticalScrollBarInstance" };
            scrollBarGue.Parent = scrollViewerGue;
            scrollBarGue.ElementGueContainingThis = scrollViewerGue;

            var thumbGue = new GraphicalUiElement(new InvisibleRenderable()) { Name = "ThumbInstance" };
            thumbGue.Parent = scrollBarGue;
            thumbGue.ElementGueContainingThis = scrollBarGue;

            scrollViewerGueLive = scrollViewerGue;
            scrollBarGueLive = scrollBarGue;
            thumbGueLive = thumbGue;

            return root;
        });

        Mock<ISelectedState> selectedState = new Mock<ISelectedState>();
        selectedState.SetupGet(x => x.SelectedElements).Returns(new[] { screen });
        selectedState.SetupGet(x => x.SelectedElement).Returns(screen);
        selectedState.SetupGet(x => x.SelectedStateSave).Returns(defaultState);

        WireframeObjectManager wireframeObjectManager = new WireframeObjectManager(
            Mock.Of<IFontManager>(),
            selectedState.Object,
            Mock.Of<IDialogService>(),
            Mock.Of<IGuiCommands>(),
            new LocalizationService(),
            pluginManager.Object,
            Mock.Of<IProjectState>());

        wireframeObjectManager.RefreshAll(forceLayout: true);

        scrollViewerGueLive.ShouldNotBeNull();
        scrollBarGueLive.ShouldNotBeNull();
        thumbGueLive.ShouldNotBeNull();

        wireframeObjectManager.AllIpsos.ShouldContain(scrollViewerGueLive);
        // These are the two levels of component nesting that were previously dropped: the
        // ScrollBar instance owned by the ScrollViewer, and the Thumb instance it in turn owns.
        wireframeObjectManager.AllIpsos.ShouldContain(scrollBarGueLive);
        wireframeObjectManager.AllIpsos.ShouldContain(thumbGueLive);
    }
}
