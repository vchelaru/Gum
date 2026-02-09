using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Logic;
using Gum.Managers;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Gum.PropertyGridHelpers;
using Gum.ToolCommands;
using Gum.ToolStates;
using Gum.ViewModels;
using Moq;
using Moq.AutoMock;
using Shouldly;

namespace GumToolUnitTests.ViewModels;

public class RightClickViewModelTests
{
    private readonly AutoMocker _mocker;
    private readonly Mock<ISelectedState> _selectedState;
    private readonly IReorderLogic _reorderLogic;
    private readonly Mock<ObjectFinder> _objectFinder;
    private readonly Mock<IElementCommands> _elementCommands;
    private readonly Mock<INameVerifier> _nameVerifier;
    private readonly Mock<ISetVariableLogic> _setVariableLogic;
    private readonly RightClickViewModel _sut;

    public RightClickViewModelTests()
    {
        _mocker = new AutoMocker();
        _reorderLogic = _mocker.CreateInstance<IReorderLogic>();
        _selectedState = new Mock<ISelectedState>();
        _objectFinder = new Mock<ObjectFinder>();
        _elementCommands = new Mock<IElementCommands>();
        _nameVerifier = new Mock<INameVerifier>();
        _setVariableLogic = new Mock<ISetVariableLogic>();

        _sut = new RightClickViewModel(
            _selectedState.Object,
            _reorderLogic,
            _objectFinder.Object,
            _elementCommands.Object,
            _nameVerifier.Object,
            _setVariableLogic.Object);
    }

    [Fact]
    public void GetMenuItems_ShouldReturnEmpty_WhenNoInstanceSelected()
    {
        _selectedState.Setup(x => x.SelectedInstance).Returns((InstanceSave?)null);

        var result = _sut.GetMenuItems();

        result.ShouldBeEmpty();
    }

    [Fact]
    public void GetMenuItems_ShouldReturnItems_WhenInstanceIsSelected()
    {
        var element = CreateElementWithInstances("InstanceA");
        var instance = element.Instances[0];

        _selectedState.Setup(x => x.SelectedInstance).Returns(instance);
        _selectedState.Setup(x => x.SelectedElement).Returns(element);

        var result = _sut.GetMenuItems();

        result[0].Text.ShouldBe("Bring to Front");
        result[1].Text.ShouldBe("Move Forward");
        result[2].Text.ShouldBe("Move In Front Of");
        result[3].Text.ShouldBe("Move Backward");
        result[4].Text.ShouldBe("Send to Back");
        result[5].IsSeparator.ShouldBeTrue();
        result[6].Text.ShouldStartWith("Add child object to");
    }

    [Fact]
    public void GetMenuItems_ShouldPopulateMoveInFrontOfChildren_WithSiblingInstances()
    {
        var element = CreateElementWithInstances("InstanceA", "InstanceB", "InstanceC");
        var selectedInstance = element.Instances[0];

        _selectedState.Setup(x => x.SelectedInstance).Returns(selectedInstance);
        _selectedState.Setup(x => x.SelectedElement).Returns(element);

        var result = _sut.GetMenuItems();

        var moveInFrontOf = result[2];
        moveInFrontOf.Text.ShouldBe("Move In Front Of");
        moveInFrontOf.Children.Count.ShouldBe(2);
        moveInFrontOf.Children[0].Text.ShouldBe("InstanceB");
        moveInFrontOf.Children[1].Text.ShouldBe("InstanceC");
    }

    [Fact]
    public void GetMenuItems_ShouldExcludeNonSiblings_FromMoveInFrontOfChildren()
    {
        var element = CreateElementWithInstances("InstanceA", "InstanceB", "ChildOfB");

        // Make ChildOfB a child of InstanceB
        element.States[0].Variables.Add(new VariableSave
        {
            Name = "ChildOfB.Parent",
            Value = "InstanceB"
        });

        var selectedInstance = element.Instances[0]; // InstanceA (root level)

        _selectedState.Setup(x => x.SelectedInstance).Returns(selectedInstance);
        _selectedState.Setup(x => x.SelectedElement).Returns(element);

        var result = _sut.GetMenuItems();

        var moveInFrontOf = result[2];
        // ChildOfB should be excluded because it has a different parent (InstanceB) than InstanceA (null)
        moveInFrontOf.Children.Count.ShouldBe(1);
        moveInFrontOf.Children[0].Text.ShouldBe("InstanceB");
    }

    [Fact]
    public void GetMenuItems_ShouldIncludeOnlySiblingsWithSameParent_InMoveInFrontOf()
    {
        var element = CreateElementWithInstances("Parent1", "ChildA", "ChildB", "OtherRoot");

        // Make ChildA and ChildB children of Parent1
        element.States[0].Variables.Add(new VariableSave
        {
            Name = "ChildA.Parent",
            Value = "Parent1"
        });
        element.States[0].Variables.Add(new VariableSave
        {
            Name = "ChildB.Parent",
            Value = "Parent1"
        });

        var selectedInstance = element.Instances[1]; // ChildA

        _selectedState.Setup(x => x.SelectedInstance).Returns(selectedInstance);
        _selectedState.Setup(x => x.SelectedElement).Returns(element);

        var result = _sut.GetMenuItems();

        var moveInFrontOf = result[2];
        // Only ChildB shares the same parent (Parent1), Parent1 and OtherRoot are root-level
        moveInFrontOf.Children.Count.ShouldBe(1);
        moveInFrontOf.Children[0].Text.ShouldBe("ChildB");
    }

    [Fact]
    public void GetMenuItems_ShouldHaveActions_ForAllTopLevelItems()
    {
        var element = CreateElementWithInstances("InstanceA");
        var instance = element.Instances[0];

        _selectedState.Setup(x => x.SelectedInstance).Returns(instance);
        _selectedState.Setup(x => x.SelectedElement).Returns(element);

        var result = _sut.GetMenuItems();

        result[0].Action.ShouldNotBeNull(); // Bring to Front
        result[1].Action.ShouldNotBeNull(); // Move Forward
        result[2].Action.ShouldBeNull();    // Move In Front Of (parent-only)
        result[3].Action.ShouldNotBeNull(); // Move Backward
        result[4].Action.ShouldNotBeNull(); // Send to Back
    }

    [Fact]
    public void GetEffectiveParentNameFor_ShouldReturnNull_WhenNoParentVariable()
    {
        var element = CreateElementWithInstances("InstanceA");
        var instance = element.Instances[0];

        var result = RightClickViewModel.GetEffectiveParentNameFor(instance, element);

        result.ShouldBeNull();
    }

    [Fact]
    public void GetEffectiveParentNameFor_ShouldReturnNull_WhenParentInstanceDoesNotExist()
    {
        var element = CreateElementWithInstances("InstanceA");
        element.States[0].Variables.Add(new VariableSave
        {
            Name = "InstanceA.Parent",
            Value = "NonExistentParent"
        });

        var instance = element.Instances[0];

        var result = RightClickViewModel.GetEffectiveParentNameFor(instance, element);

        result.ShouldBeNull();
    }

    [Fact]
    public void GetEffectiveParentNameFor_ShouldReturnParentName_WhenParentInstanceExists()
    {
        var element = CreateElementWithInstances("InstanceA", "ParentInstance");
        element.States[0].Variables.Add(new VariableSave
        {
            Name = "InstanceA.Parent",
            Value = "ParentInstance"
        });

        var instance = element.Instances[0];

        var result = RightClickViewModel.GetEffectiveParentNameFor(instance, element);

        result.ShouldBe("ParentInstance");
    }

    private static ComponentSave CreateElementWithInstances(params string[] instanceNames)
    {
        var element = new ComponentSave();
        element.States.Add(new StateSave());

        foreach (var name in instanceNames)
        {
            var instance = new InstanceSave { Name = name };
            instance.ParentContainer = element;
            element.Instances.Add(instance);
        }

        return element;
    }
}
