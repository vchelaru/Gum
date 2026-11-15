using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Plugins.InternalPlugins.EditorTab.Services;
using Gum.ToolStates;
using Gum.Wireframe;
using GumRuntime;
using Moq;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using RenderingLibrary.Math.Geometry;
using Shouldly;
using System;
using System.Linq;
using Xunit;
using static Gum.Presentation.Tests.StateSelectionCanvasHarness;

namespace Gum.Presentation.Tests;

/// <summary>
/// Issue #7417: with editor outlines on, a component based on Container that has no instances of its
/// own (a StackPanel) gets the same dotted outline as a Container; components with instances stay
/// unoutlined. Uses the editor's real <see cref="EditorRenderableFactory"/>.
/// </summary>
public class ComponentOutlineTests : BaseTestClass
{
    private readonly Func<string, ISystemManagers, IRenderable?>? _previousFunc;
    private readonly EditorRenderableFactory _factory;

    public ComponentOutlineTests()
    {
        Mock<IProjectState> projectState = new Mock<IProjectState>();
        EditorRenderableFactory factory = _factory = new EditorRenderableFactory(projectState.Object);
        _previousFunc = ElementSaveExtensions.CustomCreateGraphicalComponentFunc;
        ElementSaveExtensions.CustomCreateGraphicalComponentFunc =
            (type, managers) => factory.CreateRenderableForType(type, managers);
        GraphicalUiElement.ShowLineRectangles = true;
    }

    public override void Dispose()
    {
        GraphicalUiElement.ShowLineRectangles = false;
        ElementSaveExtensions.CustomCreateGraphicalComponentFunc = _previousFunc;
        base.Dispose();
    }

    [Fact]
    public void ToGraphicalUiElement_ZeroInstanceComponentBasedOnContainer_HasDottedOutline()
    {
        ComponentSave stackPanel = new ComponentSave { Name = "StackPanel", BaseType = "Container" };
        stackPanel.States.Add(new StateSave { Name = "Default", ParentContainer = stackPanel });
        BuildProject(stackPanel);

        GraphicalUiElement root = stackPanel.ToGraphicalUiElement(systemManagers: null!, addToManagers: false);

        root.RenderableComponent.ShouldBeOfType<LineRectangle>().IsDotted.ShouldBeTrue();
    }

    [Fact]
    public void ToGraphicalUiElement_ComponentWithInstanceOfItsOwn_HasNoOutline()
    {
        ComponentSave button = new ComponentSave { Name = "Button", BaseType = "Container" };
        button.States.Add(new StateSave { Name = "Default", ParentContainer = button });
        button.Instances.Add(new InstanceSave { Name = "Background", BaseType = "Rectangle", ParentContainer = button });
        BuildProject(button);

        GraphicalUiElement root = button.ToGraphicalUiElement(systemManagers: null!, addToManagers: false);

        root.RenderableComponent.ShouldBeOfType<InvisibleRenderable>();
    }

    [Fact]
    public void ToGraphicalUiElement_ZeroInstanceComponentBasedOnComponentWithInstances_HasNoOutline()
    {
        ComponentSave button = new ComponentSave { Name = "Button", BaseType = "Container" };
        button.States.Add(new StateSave { Name = "Default", ParentContainer = button });
        button.Instances.Add(new InstanceSave { Name = "Background", BaseType = "Rectangle", ParentContainer = button });
        ComponentSave derived = new ComponentSave { Name = "FancyButton", BaseType = "Button" };
        derived.States.Add(new StateSave { Name = "Default", ParentContainer = derived });
        BuildProject(button, derived);

        GraphicalUiElement root = derived.ToGraphicalUiElement(systemManagers: null!, addToManagers: false);

        root.RenderableComponent.ShouldBeOfType<InvisibleRenderable>();
    }

    [Fact]
    public void ToGraphicalUiElement_ZeroInstanceComponentBasedOnZeroInstanceComponent_HasDottedOutline()
    {
        ComponentSave stackPanel = new ComponentSave { Name = "StackPanel", BaseType = "Container" };
        stackPanel.States.Add(new StateSave { Name = "Default", ParentContainer = stackPanel });
        ComponentSave derived = new ComponentSave { Name = "TallStackPanel", BaseType = "StackPanel" };
        derived.States.Add(new StateSave { Name = "Default", ParentContainer = derived });
        BuildProject(stackPanel, derived);

        GraphicalUiElement root = derived.ToGraphicalUiElement(systemManagers: null!, addToManagers: false);

        root.RenderableComponent.ShouldBeOfType<LineRectangle>().IsDotted.ShouldBeTrue();
    }

    [Fact]
    public void ToGraphicalUiElement_OutlinesOff_ZeroInstanceComponentStaysInvisible()
    {
        GraphicalUiElement.ShowLineRectangles = false;
        ComponentSave stackPanel = new ComponentSave { Name = "StackPanel", BaseType = "Container" };
        stackPanel.States.Add(new StateSave { Name = "Default", ParentContainer = stackPanel });
        BuildProject(stackPanel);

        GraphicalUiElement root = stackPanel.ToGraphicalUiElement(systemManagers: null!, addToManagers: false);

        root.RenderableComponent.ShouldBeOfType<InvisibleRenderable>();
    }

    [Fact]
    public void ToGraphicalUiElement_NoCustomFactory_ZeroInstanceComponentKeepsPlaceholder()
    {
        ElementSaveExtensions.CustomCreateGraphicalComponentFunc = null;
        ComponentSave stackPanel = new ComponentSave { Name = "StackPanel", BaseType = "Container" };
        stackPanel.States.Add(new StateSave { Name = "Default", ParentContainer = stackPanel });
        BuildProject(stackPanel);

        GraphicalUiElement root = stackPanel.ToGraphicalUiElement(systemManagers: null!, addToManagers: false);

        root.RenderableComponent.ShouldBeOfType<InvisibleRenderable>();
    }

    #region Outlines belong to the element being edited

    private ComponentSave BuildTextBoxWithInnerContainer()
    {
        ComponentSave textBox = new ComponentSave { Name = "TextBox", BaseType = "Container" };
        textBox.States.Add(new StateSave { Name = "Default", ParentContainer = textBox });
        textBox.Instances.Add(new InstanceSave { Name = "ClipContainer", BaseType = "Container", ParentContainer = textBox });
        return textBox;
    }

    private ScreenSave BuildScreenWith(params (string Name, string BaseType)[] instances)
    {
        ScreenSave screen = new ScreenSave { Name = "MainScreen" };
        screen.States.Add(new StateSave { Name = "Default", ParentContainer = screen });
        foreach ((string name, string baseType) in instances)
        {
            screen.Instances.Add(new InstanceSave { Name = name, BaseType = baseType, ParentContainer = screen });
        }
        return screen;
    }

    private GraphicalUiElement BuildShown(ElementSave element)
    {
        GraphicalUiElement root = element.ToGraphicalUiElement(systemManagers: null!, addToManagers: false);
        _factory.ShowOnlyOutlinesOwnedBy(root);
        return root;
    }

    private static GraphicalUiElement Find(GraphicalUiElement root, string path)
    {
        GraphicalUiElement current = root;
        foreach (string part in path.Split('.'))
        {
            current = current.ContainedElements.Single(item => item.Name == part);
        }
        return current;
    }

    [Fact]
    public void ShowOnlyOutlinesOwnedBy_ContainerInsideComponentInstanceOnScreen_IsHidden()
    {
        ComponentSave textBox = BuildTextBoxWithInnerContainer();
        ScreenSave screen = BuildScreenWith(("TextBoxInstance", "TextBox"), ("Plain", "Container"));
        BuildProject(textBox);
        ObjectFinder.Self.GumProjectSave!.Screens.Add(screen);

        GraphicalUiElement root = BuildShown(screen);

        Find(root, "TextBoxInstance.ClipContainer").RenderableComponent.ShouldBeOfType<LineRectangle>().LocalVisible.ShouldBeFalse();
        Find(root, "Plain").RenderableComponent.ShouldBeOfType<LineRectangle>().LocalVisible.ShouldBeTrue();
    }

    [Fact]
    public void ShowOnlyOutlinesOwnedBy_StackPanelInstanceOnScreen_IsShown()
    {
        ComponentSave stackPanel = new ComponentSave { Name = "StackPanel", BaseType = "Container" };
        stackPanel.States.Add(new StateSave { Name = "Default", ParentContainer = stackPanel });
        ScreenSave screen = BuildScreenWith(("Stack", "StackPanel"));
        BuildProject(stackPanel);
        ObjectFinder.Self.GumProjectSave!.Screens.Add(screen);

        GraphicalUiElement root = BuildShown(screen);

        Find(root, "Stack").RenderableComponent.ShouldBeOfType<LineRectangle>().LocalVisible.ShouldBeTrue();
    }

    [Fact]
    public void ShowOnlyOutlinesOwnedBy_EditingTheComponentItself_ShowsItsInnerContainer()
    {
        ComponentSave textBox = BuildTextBoxWithInnerContainer();
        BuildProject(textBox);

        GraphicalUiElement root = BuildShown(textBox);

        Find(root, "ClipContainer").RenderableComponent.ShouldBeOfType<LineRectangle>().LocalVisible.ShouldBeTrue();
    }

    [Fact]
    public void AreOutlinesHiddenForExport_Restore_KeepsOutlinesOfOtherElementsHidden()
    {
        ComponentSave textBox = BuildTextBoxWithInnerContainer();
        ScreenSave screen = BuildScreenWith(("TextBoxInstance", "TextBox"), ("Plain", "Container"));
        BuildProject(textBox);
        ObjectFinder.Self.GumProjectSave!.Screens.Add(screen);
        GraphicalUiElement root = BuildShown(screen);
        LineRectangle inner = Find(root, "TextBoxInstance.ClipContainer").RenderableComponent.ShouldBeOfType<LineRectangle>();
        LineRectangle plain = Find(root, "Plain").RenderableComponent.ShouldBeOfType<LineRectangle>();

        _factory.AreOutlinesHiddenForExport = true;
        plain.LocalVisible.ShouldBeFalse();
        _factory.AreOutlinesHiddenForExport = false;

        plain.LocalVisible.ShouldBeTrue();
        inner.LocalVisible.ShouldBeFalse();
    }

    #endregion
}
