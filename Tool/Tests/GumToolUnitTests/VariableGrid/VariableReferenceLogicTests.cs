using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Gum.Services;
using Gum.Services.Dialogs;
using Moq;
using Shouldly;
using System.Collections.Generic;

namespace GumToolUnitTests.VariableGrid;

public class VariableReferenceLogicTests : BaseTestClass
{
    private readonly Mock<IGuiCommands> _guiCommandsMock;
    private readonly VariableReferenceLogic _sut;

    public VariableReferenceLogicTests()
    {
        _guiCommandsMock = new Mock<IGuiCommands>();
        _sut = new VariableReferenceLogic(
            _guiCommandsMock.Object,
            new Mock<IWireframeCommands>().Object,
            new Mock<IDialogService>().Object,
            new Mock<IFileCommands>().Object);
    }

    #region GetAssignmentSyntax

    [Fact]
    public void GetAssignmentSyntax_EmptyString_ReturnsNull()
    {
        var result = _sut.GetAssignmentSyntax("");

        result.ShouldBeNull();
    }

    [Fact]
    public void GetAssignmentSyntax_NumericLiteralAssignment_ReturnsAssignment()
    {
        var result = _sut.GetAssignmentSyntax("Width=100");

        result.ShouldNotBeNull();
        result.Left.ToString().ShouldBe("Width");
    }

    [Fact]
    public void GetAssignmentSyntax_PlainIdentifier_ReturnsNull()
    {
        var result = _sut.GetAssignmentSyntax("hello");

        result.ShouldBeNull();
    }

    [Fact]
    public void GetAssignmentSyntax_SimpleVariableAssignment_ReturnsLeftAndRight()
    {
        var result = _sut.GetAssignmentSyntax("X=Y");

        result.ShouldNotBeNull();
        result.Left.ToString().ShouldBe("X");
        result.Right.ToString().ShouldBe("Y");
    }

    [Fact]
    public void GetAssignmentSyntax_SlashSyntaxRightSide_ReturnsAssignment()
    {
        // Gum uses slash syntax like "Components/Button.Width"; ConvertToCSharpSyntax converts it to valid C#
        var result = _sut.GetAssignmentSyntax("Width=Components/Button.Width");

        result.ShouldNotBeNull();
    }

    #endregion

    #region ReactIfChangedMemberIsVariableReference

    [Fact]
    public void ReactIfChangedMemberIsVariableReference_ColorAssignment_ExpandsToThreeEntries()
    {
        // "Background.Color" has no explicit left side, so AddImpliedLeftSide runs first,
        // converting it to "Color = Background.Color", then ExpandColorToRedGreenBlue splits it.
        StateSave stateSave = BuildStateWithVariableReferences("VariableReferences", "Background.Color");

        _sut.ReactIfChangedMemberIsVariableReference(
            parentElement: null, instance: null, stateSave, changedMember: "VariableReferences", oldValue: null);

        var varList = (List<string>)stateSave.GetVariableListSave("VariableReferences").ValueAsIList;
        varList.Count.ShouldBe(3);
        varList.ShouldContain("Red = Background.Red");
        varList.ShouldContain("Green = Background.Green");
        varList.ShouldContain("Blue = Background.Blue");
    }

    [Fact]
    public void ReactIfChangedMemberIsVariableReference_ImpliedLeftSide_InfersLeftFromRightSide()
    {
        // "Instance.Variable" with no equals sign should infer "Variable = Instance.Variable"
        StateSave stateSave = BuildStateWithVariableReferences("VariableReferences", "Background.Width");

        _sut.ReactIfChangedMemberIsVariableReference(
            parentElement: null, instance: null, stateSave, changedMember: "VariableReferences", oldValue: null);

        var varList = (List<string>)stateSave.GetVariableListSave("VariableReferences").ValueAsIList;
        varList[0].ShouldBe("Width = Background.Width");
    }

    [Fact]
    public void ReactIfChangedMemberIsVariableReference_ListChangedFromNull_RefreshesVariables()
    {
        StateSave stateSave = BuildStateWithVariableReferences("VariableReferences", "X=Y");

        _sut.ReactIfChangedMemberIsVariableReference(
            parentElement: null, instance: null, stateSave, changedMember: "VariableReferences", oldValue: null);

        _guiCommandsMock.Verify(x => x.RefreshVariables(It.IsAny<bool>()), Times.Once);
    }

    [Fact]
    public void ReactIfChangedMemberIsVariableReference_ListUnchanged_DoesNotRefreshVariables()
    {
        var oldValue = new List<string> { "X=Y" };
        StateSave stateSave = BuildStateWithVariableReferences("VariableReferences", "X=Y");

        _sut.ReactIfChangedMemberIsVariableReference(
            parentElement: null, instance: null, stateSave, changedMember: "VariableReferences", oldValue: oldValue);

        _guiCommandsMock.Verify(x => x.RefreshVariables(It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public void ReactIfChangedMemberIsVariableReference_NonVariableReferencesMember_DoesNotRefreshVariables()
    {
        StateSave stateSave = new StateSave();

        _sut.ReactIfChangedMemberIsVariableReference(
            parentElement: null, instance: null, stateSave, changedMember: "Width", oldValue: null);

        _guiCommandsMock.Verify(x => x.RefreshVariables(It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public void ReactIfChangedMemberIsVariableReference_QualifiedLeftSide_StripsLeftSideDotPrefix()
    {
        // A left side like "Instance.Width" should be stripped to just "Width"
        StateSave stateSave = BuildStateWithVariableReferences("VariableReferences", "Instance.Width=OtherInstance.Width");

        _sut.ReactIfChangedMemberIsVariableReference(
            parentElement: null, instance: null, stateSave, changedMember: "VariableReferences", oldValue: null);

        var varList = (List<string>)stateSave.GetVariableListSave("VariableReferences").ValueAsIList;
        varList[0].ShouldBe("Width=OtherInstance.Width");
    }

    [Fact]
    public void ReactIfChangedMemberIsVariableReference_WithInstance_QualifiesRightSideIdentifiers()
    {
        // When an instance is selected, bare right-side identifiers get prefixed with the instance name
        var instance = new InstanceSave { Name = "myInstance" };
        StateSave stateSave = BuildStateWithVariableReferences("myInstance.VariableReferences", "Width=SomeVar");

        _sut.ReactIfChangedMemberIsVariableReference(
            parentElement: null, instance, stateSave, changedMember: "VariableReferences", oldValue: null);

        var varList = (List<string>)stateSave.GetVariableListSave("myInstance.VariableReferences").ValueAsIList;
        varList[0].ShouldContain("myInstance.SomeVar");
    }

    #endregion

    #region DoVariableReferenceReaction

    [Fact]
    public void DoVariableReferenceReaction_InstanceVariableSet_PropagatesDeepReference()
    {
        // Scenario: ComponentA exposes Width and has a childInstance whose Width is driven by
        // "Width = Components/ComponentA.Width". When myCompA.Width is changed in a screen,
        // the reaction should propagate the new value to myCompA.childInstance.Width.
        GumProjectSave project = new GumProjectSave();
        ObjectFinder.Self.GumProjectSave = project;

        ComponentSave componentA = new ComponentSave { Name = "ComponentA" };
        StateSave componentADefaultState = new StateSave { Name = "Default", ParentContainer = componentA };
        componentADefaultState.Variables.Add(new VariableSave
        {
            Name = "Width",
            SetsValue = true,
            Value = 0f,
            Type = "float"
        });
        componentA.States.Add(componentADefaultState);

        InstanceSave childInstance = new InstanceSave
        {
            Name = "childInstance",
            BaseType = "Container",
            ParentContainer = componentA
        };
        componentA.Instances.Add(childInstance);

        VariableListSave<string> childVariableReferences = new VariableListSave<string>
        {
            Name = "childInstance.VariableReferences",
            Type = "string"
        };
        childVariableReferences.Value.Add("Width = Components/ComponentA.Width");
        componentADefaultState.VariableLists.Add(childVariableReferences);

        project.Components.Add(componentA);

        ScreenSave screen = new ScreenSave { Name = "TestScreen" };
        StateSave screenDefaultState = new StateSave { Name = "Default", ParentContainer = screen };
        screenDefaultState.Variables.Add(new VariableSave
        {
            Name = "myCompA.Width",
            SetsValue = true,
            Value = 100f,
            Type = "float"
        });
        screen.States.Add(screenDefaultState);

        InstanceSave myCompA = new InstanceSave
        {
            Name = "myCompA",
            BaseType = "ComponentA",
            ParentContainer = screen
        };
        screen.Instances.Add(myCompA);

        project.Screens.Add(screen);

        _sut.DoVariableReferenceReaction(
            parentElement: screen,
            leftSideInstance: myCompA,
            unqualifiedMember: "Width",
            stateSave: screenDefaultState,
            qualifiedName: "myCompA.Width",
            trySave: false);

        screen.DefaultState.GetValue("myCompA.childInstance.Width").ShouldBe(100f);
    }

    [Fact]
    public void DoVariableReferenceReaction_NotOperatorOnBoolean_InvertsValue()
    {
        // "Visible = !OtherInstance.Visible" should invert the boolean.
        // Currently fails because:
        //   1. EvaluatedSyntax doesn't handle PrefixUnaryExpressionSyntax -> validation
        //      comments out the line
        //   2. GetRightSideValue passes the raw "!OtherInstance.Visible" string to
        //      RecursiveVariableFinder which returns null
        GumProjectSave project = new GumProjectSave();
        ObjectFinder.Self.GumProjectSave = project;

        ScreenSave screen = new ScreenSave { Name = "TestScreen" };
        StateSave defaultState = new StateSave { Name = "Default", ParentContainer = screen };
        defaultState.Variables.Add(new VariableSave { Name = "Visible", SetsValue = true, Value = true, Type = "bool" });
        defaultState.Variables.Add(new VariableSave { Name = "OtherInstance.Visible", SetsValue = true, Value = true, Type = "bool" });
        screen.States.Add(defaultState);
        project.Screens.Add(screen);

        VariableListSave<string> varList = new VariableListSave<string> { Type = "string", Name = "VariableReferences" };
        varList.Value.Add("Visible = !OtherInstance.Visible");
        defaultState.VariableLists.Add(varList);

        _sut.DoVariableReferenceReaction(
            parentElement: screen,
            leftSideInstance: null,
            unqualifiedMember: "VariableReferences",
            stateSave: defaultState,
            qualifiedName: "VariableReferences",
            trySave: false);

        varList.Value[0].ShouldBe("Visible = !OtherInstance.Visible"); // not commented out
        defaultState.GetValue("Visible").ShouldBe(false);
    }

    #endregion

    #region Helpers

    private static StateSave BuildStateWithVariableReferences(string listName, string item)
    {
        var stateSave = new StateSave();
        var varList = new VariableListSave<string> { Type = "string", Name = listName };
        varList.Value.Add(item);
        stateSave.VariableLists.Add(varList);
        return stateSave;
    }

    #endregion
}
