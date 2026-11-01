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

    public ComponentOutlineTests()
    {
        Mock<IProjectState> projectState = new Mock<IProjectState>();
        EditorRenderableFactory factory = new EditorRenderableFactory(projectState.Object);
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
}
