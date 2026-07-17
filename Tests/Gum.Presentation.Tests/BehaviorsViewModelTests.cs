using System.Linq;
using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.Managers;
using Gum.Plugins.Behaviors;
using Gum.ToolStates;
using Moq;
using Shouldly;

namespace Gum.Presentation.Tests;

/// <summary>
/// Relocated out of Gum.csproj into the headless Gum.Presentation assembly (ADR-0005 Phase 3,
/// #3754), unblocked by narrowing IProjectManager off the whole, WinForms-entangled
/// GeneralSettingsFile and converting AddedListVisibility/EditListVisibility to a single bool
/// (IsEditing) the view converts via a WPF converter (code-style.md).
/// </summary>
public class BehaviorsViewModelTests
{
    private readonly Mock<ISelectedState> _selectedState;
    private readonly Mock<IProjectManager> _projectManager;
    private readonly BehaviorsViewModel _viewModel;
    private readonly GumProjectSave _project;

    public BehaviorsViewModelTests()
    {
        _selectedState = new Mock<ISelectedState>();
        _project = new GumProjectSave();
        _projectManager = new Mock<IProjectManager>();
        _projectManager.SetupGet(x => x.GumProjectSave).Returns(_project);

        _viewModel = new BehaviorsViewModel(_selectedState.Object, _projectManager.Object);
    }

    [Fact]
    public void IsEditing_DefaultsToFalse()
    {
        _viewModel.IsEditing.ShouldBeFalse();
    }

    [Fact]
    public void UpdateTo_ShouldFlagOrphan_OnAddedBehaviors_SoReadOnlyViewShowsMissingState()
    {
        _project.Behaviors.Add(new BehaviorSave { Name = "ProjectBehavior" });

        ComponentSave component = new ComponentSave { Name = "Component1" };
        component.Behaviors.Add(new ElementBehaviorReference { BehaviorName = "ProjectBehavior" });
        component.Behaviors.Add(new ElementBehaviorReference { BehaviorName = "OrphanBehavior" });

        _viewModel.UpdateTo(component);

        _viewModel.AddedBehaviors.Count.ShouldBe(2);
        _viewModel.AddedBehaviors.Single(x => x.Name == "OrphanBehavior").IsOrphaned.ShouldBeTrue();
        _viewModel.AddedBehaviors.Single(x => x.Name == "ProjectBehavior").IsOrphaned.ShouldBeFalse();
    }

    [Fact]
    public void UpdateTo_ShouldFlagOrphan_WhenBehaviorMissingFromProject()
    {
        _project.Behaviors.Add(new BehaviorSave { Name = "ProjectBehavior" });

        ComponentSave component = new ComponentSave { Name = "Component1" };
        component.Behaviors.Add(new ElementBehaviorReference { BehaviorName = "OrphanBehavior" });

        _viewModel.UpdateTo(component);

        _viewModel.AllBehaviors.Single(x => x.Name == "OrphanBehavior").IsOrphaned.ShouldBeTrue();
        _viewModel.AllBehaviors.Single(x => x.Name == "ProjectBehavior").IsOrphaned.ShouldBeFalse();
    }

    [Fact]
    public void UpdateTo_ShouldIncludeOrphanBehaviorReferenced_ByComponentButMissingFromProject()
    {
        ComponentSave component = new ComponentSave { Name = "Component1" };
        component.Behaviors.Add(new ElementBehaviorReference { BehaviorName = "MissingBehavior" });

        _viewModel.UpdateTo(component);

        CheckListBehaviorItem? orphan = _viewModel.AllBehaviors
            .FirstOrDefault(item => item.Name == "MissingBehavior");

        orphan.ShouldNotBeNull();
        orphan!.IsChecked.ShouldBeTrue();
    }

    [Fact]
    public void UpdateTo_ShouldListBehaviorOnce_WhenPresentInBothProjectAndComponent()
    {
        _project.Behaviors.Add(new BehaviorSave { Name = "SharedBehavior" });

        ComponentSave component = new ComponentSave { Name = "Component1" };
        component.Behaviors.Add(new ElementBehaviorReference { BehaviorName = "SharedBehavior" });

        _viewModel.UpdateTo(component);

        _viewModel.AllBehaviors.Count(x => x.Name == "SharedBehavior").ShouldBe(1);
        _viewModel.AllBehaviors.Single(x => x.Name == "SharedBehavior").IsChecked.ShouldBeTrue();
    }

    [Fact]
    public void UpdateTo_ShouldListProjectBehavior_AsUncheckedWhenComponentDoesNotReferenceIt()
    {
        _project.Behaviors.Add(new BehaviorSave { Name = "ProjectBehavior" });

        ComponentSave component = new ComponentSave { Name = "Component1" };

        _viewModel.UpdateTo(component);

        CheckListBehaviorItem? item = _viewModel.AllBehaviors
            .FirstOrDefault(x => x.Name == "ProjectBehavior");

        item.ShouldNotBeNull();
        item!.IsChecked.ShouldBeFalse();
    }
}
