using System;
using System.Linq;
using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Plugins;
using Gum.Plugins.Behaviors;
using Gum.ToolCommands;
using Gum.ToolStates;
using Gum.Undo;
using Moq;
using Shouldly;

namespace Gum.Presentation.Tests;

/// <summary>
/// Relocated out of the WPF-hosted MainBehaviorsPlugin into the headless Gum.Presentation
/// assembly (ADR-0005 Phase 3, #3928) so this business logic is unit testable. The tab dependency
/// is narrowed to <see cref="ITabVisibility"/> since the concrete PluginTab is WPF-typed.
/// </summary>
public class BehaviorsLogicTests
{
    private readonly Mock<ISelectedState> _selectedState = new();
    private readonly Mock<IElementCommands> _elementCommands = new();
    private readonly Mock<IUndoManager> _undoManager = new();
    private readonly Mock<IPluginManager> _pluginManager = new();
    private readonly Mock<IGuiCommands> _guiCommands = new();
    private readonly Mock<IFileCommands> _fileCommands = new();
    private readonly Mock<IProjectManager> _projectManager = new();
    private readonly Mock<ITabVisibility> _tab = new();
    private readonly BehaviorsViewModel _viewModel;
    private readonly BehaviorsLogic _logic;

    public BehaviorsLogicTests()
    {
        _undoManager.Setup(x => x.RequestLock()).Returns(new UndoLock(() => { }));
        _projectManager.SetupGet(x => x.GumProjectSave).Returns(new GumProjectSave());
        _viewModel = new BehaviorsViewModel(_selectedState.Object, _projectManager.Object);

        _logic = new BehaviorsLogic(
            _selectedState.Object, _elementCommands.Object, _undoManager.Object, _pluginManager.Object,
            _guiCommands.Object, _fileCommands.Object, _viewModel, _tab.Object);
    }

    [Fact]
    public void HandleApplyBehaviorChanges_NoSelectedComponent_DoesNothing()
    {
        _selectedState.Setup(x => x.SelectedComponent).Returns((ComponentSave?)null);

        _logic.HandleApplyBehaviorChanges(null, EventArgs.Empty);

        _elementCommands.Verify(x => x.AddBehaviorTo(It.IsAny<string>(), It.IsAny<ComponentSave>(), It.IsAny<bool>()), Times.Never);
        _guiCommands.Verify(x => x.RefreshStateTreeView(), Times.Never);
    }

    [Fact]
    public void HandleApplyBehaviorChanges_NoCheckedOrExistingBehaviors_DoesNotRefreshOrSave()
    {
        ComponentSave component = new() { Name = "Component1" };
        _selectedState.Setup(x => x.SelectedComponent).Returns(component);
        _viewModel.UpdateTo(component);

        _logic.HandleApplyBehaviorChanges(null, EventArgs.Empty);

        _guiCommands.Verify(x => x.RefreshStateTreeView(), Times.Never);
        _fileCommands.Verify(x => x.TryAutoSaveElement(It.IsAny<ElementSave>()), Times.Never);
        _pluginManager.Verify(x => x.BehaviorReferencesChanged(It.IsAny<ElementSave>()), Times.Never);
    }

    [Fact]
    public void HandleApplyBehaviorChanges_AddedBehavior_AddsToComponentAndRefreshesAndSaves()
    {
        GumProjectSave project = new();
        project.Behaviors.Add(new BehaviorSave { Name = "Flyable" });
        _projectManager.SetupGet(x => x.GumProjectSave).Returns(project);

        ComponentSave component = new() { Name = "Component1" };
        _selectedState.Setup(x => x.SelectedComponent).Returns(component);
        _viewModel.UpdateTo(component);
        _viewModel.AllBehaviors.Single(x => x.Name == "Flyable").IsChecked = true;

        _logic.HandleApplyBehaviorChanges(null, EventArgs.Empty);

        _elementCommands.Verify(x => x.AddBehaviorTo("Flyable", component, false), Times.Once);
        _guiCommands.Verify(x => x.RefreshStateTreeView(), Times.Once);
        _fileCommands.Verify(x => x.TryAutoSaveElement(component), Times.Once);
        _pluginManager.Verify(x => x.BehaviorReferencesChanged(component), Times.Once);
    }

    [Fact]
    public void HandleApplyBehaviorChanges_WhilePluginManagerReentersSynchronously_SuppressesReentrantTabUpdate()
    {
        // PluginManager.BehaviorReferencesChanged, in the real tool, fans out synchronously to every
        // subscribed plugin - including this plugin's own HandleBehaviorReferencesChanged. This pins
        // the _isApplyingChanges guard that keeps that reentrant call from touching the tab while the
        // apply is still in flight.
        GumProjectSave project = new();
        project.Behaviors.Add(new BehaviorSave { Name = "Flyable" });
        _projectManager.SetupGet(x => x.GumProjectSave).Returns(project);

        ComponentSave component = new() { Name = "Component1" };
        _selectedState.Setup(x => x.SelectedComponent).Returns(component);
        _selectedState.Setup(x => x.SelectedElement).Returns(component);
        _selectedState.Setup(x => x.SelectedInstance).Returns((InstanceSave?)null);
        _viewModel.UpdateTo(component);
        _viewModel.AllBehaviors.Single(x => x.Name == "Flyable").IsChecked = true;

        _pluginManager.Setup(x => x.BehaviorReferencesChanged(component))
            .Callback(() => _logic.HandleBehaviorReferencesChanged(component));

        _logic.HandleApplyBehaviorChanges(null, EventArgs.Empty);

        _tab.Verify(x => x.Show(), Times.Never);
    }

    [Fact]
    public void HandleElementSelected_ComponentSelectedWithNoInstance_ShowsTab()
    {
        ComponentSave component = new() { Name = "Component1" };
        _selectedState.Setup(x => x.SelectedComponent).Returns(component);
        _selectedState.Setup(x => x.SelectedInstance).Returns((InstanceSave?)null);

        _logic.HandleElementSelected(component);

        _tab.Verify(x => x.Show(), Times.Once);
        _tab.Verify(x => x.Hide(), Times.Never);
    }

    [Fact]
    public void HandleInstanceSelected_InstanceSelected_HidesTab()
    {
        ComponentSave component = new() { Name = "Component1" };
        InstanceSave instance = new() { Name = "Instance1" };
        _selectedState.Setup(x => x.SelectedComponent).Returns(component);
        _selectedState.Setup(x => x.SelectedInstance).Returns(instance);

        _logic.HandleInstanceSelected(component, instance);

        _tab.Verify(x => x.Hide(), Times.Once);
        _tab.Verify(x => x.Show(), Times.Never);
    }

    [Fact]
    public void HandleStateAdd_ElementImplementingBehaviorMissingState_AddsStateToThatElement()
    {
        GumProjectSave project = new();
        BehaviorSave behavior = new() { Name = "Flyable" };
        StateSaveCategory behaviorCategory = new() { Name = "FlyState" };
        StateSave newState = new() { Name = "Flying" };
        behaviorCategory.States.Add(newState);
        behavior.Categories.Add(behaviorCategory);
        project.Behaviors.Add(behavior);

        ComponentSave elementMissingState = new() { Name = "Bird" };
        elementMissingState.Behaviors.Add(new ElementBehaviorReference { BehaviorName = "Flyable" });
        StateSaveCategory elementCategory = new() { Name = "FlyState" };
        elementMissingState.Categories.Add(elementCategory);
        project.Screens.Clear();
        project.Components.Add(elementMissingState);

        ObjectFinder.Self.GumProjectSave = project;
        try
        {
            _logic.HandleStateAdd(newState);

            _elementCommands.Verify(
                x => x.AddState(elementMissingState, elementCategory, "Flying"), Times.Once);
        }
        finally
        {
            ObjectFinder.Self.GumProjectSave = null;
        }
    }
}
