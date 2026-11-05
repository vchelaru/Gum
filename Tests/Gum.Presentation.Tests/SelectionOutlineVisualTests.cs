using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Input;
using Gum.Managers;
using Gum.Plugins;
using Gum.Plugins.InternalPlugins.EditorTab.Services;
using Gum.Services;
using Gum.ToolCommands;
using Gum.ToolStates;
using Gum.Undo;
using Gum.Commands;
using Gum.PropertyGridHelpers;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Gum.Wireframe;
using Gum.Wireframe.Editors;
using Moq;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using RenderingLibrary.Math.Geometry;
using Shouldly;
using System.Collections.Generic;
using System.Linq;

namespace Gum.Presentation.Tests;

/// <summary>
/// Pins that <see cref="StandardWireframeEditor"/> registers the selection outlines (solid for a
/// selected instance whose component hides its size, dashed for a locked one) and that they follow
/// the component when it changes while the instance stays selected (#7638).
/// </summary>
public class SelectionOutlineVisualTests : BaseTestClass
{
    private readonly SystemManagers? _previousDefault;
    private readonly Layer _layer = new Layer();
    private readonly Mock<ISelectedState> _selectedState = new Mock<ISelectedState>();
    private readonly StandardWireframeEditor _editor;

    public SelectionOutlineVisualTests()
    {
        _previousDefault = SystemManagers.Default;
        SystemManagers.Default = new SystemManagers
        {
            Renderer = new Renderer(),
            ShapeManager = new ShapeManager(),
            TextManager = new TextManager()
        };
        ObjectFinder.Self.GumProjectSave = new GumProjectSave();
        _selectedState.Setup(x => x.SelectedStateSave).Returns(new StateSave());

        _editor = new StandardWireframeEditor(
            _layer,
            System.Drawing.Color.White,
            System.Drawing.Color.White,
            Mock.Of<IHotkeyManager>(),
            Mock.Of<ISelectionManager>(),
            _selectedState.Object,
            Mock.Of<IElementCommands>(),
            Mock.Of<IGuiCommands>(),
            Mock.Of<IFileCommands>(),
            Mock.Of<ISetVariableLogic>(),
            Mock.Of<IUndoManager>(),
            Mock.Of<IVariableInCategoryPropagationLogic>(),
            Mock.Of<IWireframeObjectManager>(),
            Mock.Of<IUiSettingsService>(),
            new Camera(),
            Mock.Of<IGumCursorState>(),
            Mock.Of<IToolFontService>(),
            Mock.Of<IPluginManager>(),
            new CanvasDisplayScale());
    }

    public override void Dispose()
    {
        _editor.Destroy();
        SystemManagers.Default = _previousDefault;
        base.Dispose();
    }

    private ComponentSave AddComponent(string name, params string[] hidden)
    {
        ComponentSave component = new ComponentSave { Name = name };
        component.States.Add(new StateSave { Name = "Default" });
        component.VariablesHiddenFromInstances.AddRange(hidden);
        ObjectFinder.Self.GumProjectSave!.Components.Add(component);
        return component;
    }

    private GraphicalUiElement Select(ElementSave container, string baseType, bool locked = false)
    {
        InstanceSave instance = new InstanceSave { Name = "inst", BaseType = baseType, Locked = locked, ParentContainer = container };
        container.Instances.Add(instance);
        _selectedState.Setup(x => x.SelectedInstance).Returns(instance);
        GraphicalUiElement gue = new GraphicalUiElement(new InvisibleRenderable()) { Tag = instance, Width = 100, Height = 60 };
        gue.UpdateLayout();
        _editor.UpdateToSelection(new List<GraphicalUiElement> { gue });
        return gue;
    }

    // Full-bounds outlines only: the resize handles are small solid LineRectangles on the same layer.
    private List<LineRectangle> VisibleOutlines(bool dotted) =>
        _layer.Renderables.OfType<LineRectangle>().Where(r => r.Visible && r.IsDotted == dotted && r.Width > 50).ToList();

    private ScreenSave NewScreen()
    {
        ScreenSave screen = new ScreenSave { Name = "Screen" };
        screen.States.Add(new StateSave { Name = "Default" });
        return screen;
    }

    [Fact]
    public void UpdateToSelection_ShouldShowSolidOutline_WhenBothSizesAreHidden()
    {
        AddComponent("Hidden", "Width", "Height");

        Select(NewScreen(), "Hidden");

        VisibleOutlines(dotted: false).Count.ShouldBe(1);
        VisibleOutlines(dotted: true).ShouldBeEmpty();
    }

    [Fact]
    public void UpdateToSelection_ShouldShowSolidOutline_WhenInstanceIsNestedInAComponent()
    {
        AddComponent("Hidden", "Width");
        ComponentSave outer = AddComponent("Outer");

        Select(outer, "Hidden");

        VisibleOutlines(dotted: false).Count.ShouldBe(1);
    }

    [Fact]
    public void UpdateToSelection_ShouldNotShowSolidOutline_WhenSizeIsNotHidden()
    {
        AddComponent("Plain");

        Select(NewScreen(), "Plain");

        VisibleOutlines(dotted: false).ShouldBeEmpty();
    }

    [Fact]
    public void UpdateToSelection_ShouldShowOnlyDashedOutline_WhenInstanceIsLocked()
    {
        AddComponent("Hidden", "Width", "Height");

        Select(NewScreen(), "Hidden", locked: true);

        VisibleOutlines(dotted: true).Count.ShouldBe(1);
        VisibleOutlines(dotted: false).ShouldBeEmpty();
    }

    [Fact]
    public void UpdateVisuals_ShouldShowOutline_WhenSizeBecomesHiddenWhileSelected()
    {
        ComponentSave component = AddComponent("C");
        GraphicalUiElement gue = Select(NewScreen(), "C");
        VisibleOutlines(dotted: false).ShouldBeEmpty();

        component.VariablesHiddenFromInstances.Add("Width");
        _editor.UpdateVisuals(new List<GraphicalUiElement> { gue });

        VisibleOutlines(dotted: false).Count.ShouldBe(1);
    }

    [Fact]
    public void UpdateVisuals_ShouldHideOutline_WhenSizeBecomesUnhiddenWhileSelected()
    {
        ComponentSave component = AddComponent("C", "Width", "Height");
        GraphicalUiElement gue = Select(NewScreen(), "C");
        VisibleOutlines(dotted: false).Count.ShouldBe(1);

        component.VariablesHiddenFromInstances.Clear();
        _editor.UpdateVisuals(new List<GraphicalUiElement> { gue });

        VisibleOutlines(dotted: false).ShouldBeEmpty();
    }
}
