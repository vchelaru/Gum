using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.Managers;
using Gum.Plugins.Behaviors;
using Gum.ToolStates;
using Moq;
using Moq.AutoMock;
using Shouldly;
using System.Linq;

namespace GumToolUnitTests.ViewModels;

public class BehaviorsViewModelTests : BaseTestClass
{
    private readonly AutoMocker _mocker;
    private readonly Mock<IProjectManager> _projectManager;
    private readonly BehaviorsViewModel _viewModel;
    private readonly GumProjectSave _project;

    public BehaviorsViewModelTests()
    {
        _mocker = new AutoMocker();

        _project = new GumProjectSave();
        _projectManager = _mocker.GetMock<IProjectManager>();
        _projectManager.SetupGet(x => x.GumProjectSave).Returns(_project);

        _viewModel = _mocker.CreateInstance<BehaviorsViewModel>();
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
    public void CheckListBehaviorItem_ShouldAppendMissingSuffix_WhenOrphaned()
    {
        CheckListBehaviorItem item = new CheckListBehaviorItem
        {
            Name = "Foo",
            IsOrphaned = true,
        };

        item.DisplayText.ShouldBe("Foo (missing)");
    }

    [Fact]
    public void CheckListBehaviorItem_ShouldShowPlainName_WhenNotOrphaned()
    {
        CheckListBehaviorItem item = new CheckListBehaviorItem
        {
            Name = "Foo",
            IsOrphaned = false,
        };

        item.DisplayText.ShouldBe("Foo");
    }
}
