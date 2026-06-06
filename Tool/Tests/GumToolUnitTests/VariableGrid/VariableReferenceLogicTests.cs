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
            new Mock<IFileCommands>().Object,
            new CompositeMemberRegistry());
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
        // converting it to "Color = Background.Color", then the composite expansion splits it.
        StateSave stateSave = BuildStateWithVariableReferences("VariableReferences", "Background.Color");

        _sut.ReactIfChangedMemberIsVariableReference(
            instance: null, stateSave, changedMember: "VariableReferences", oldValue: null);

        var varList = (List<string>)stateSave.GetVariableListSave("VariableReferences").ValueAsIList;
        varList.Count.ShouldBe(3);
        varList.ShouldContain("Red = Background.Red");
        varList.ShouldContain("Green = Background.Green");
        varList.ShouldContain("Blue = Background.Blue");
    }

    [Fact]
    public void ReactIfChangedMemberIsVariableReference_FillColorAssignment_ExpandsToFillChannels()
    {
        // Affixed colors expand to their affixed channels, the inverse of how CompositeMemberLogic
        // collapses FillRed/FillGreen/FillBlue into a single "FillColor" swatch.
        StateSave stateSave = BuildStateWithVariableReferences("VariableReferences", "Background.FillColor");

        _sut.ReactIfChangedMemberIsVariableReference(
            instance: null, stateSave, changedMember: "VariableReferences", oldValue: null);

        var varList = (List<string>)stateSave.GetVariableListSave("VariableReferences").ValueAsIList;
        varList.Count.ShouldBe(3);
        varList.ShouldContain("FillRed = Background.FillRed");
        varList.ShouldContain("FillGreen = Background.FillGreen");
        varList.ShouldContain("FillBlue = Background.FillBlue");
    }

    [Fact]
    public void ReactIfChangedMemberIsVariableReference_StrokeColorAssignment_ExpandsToStrokeChannels()
    {
        StateSave stateSave = BuildStateWithVariableReferences("VariableReferences", "Background.StrokeColor");

        _sut.ReactIfChangedMemberIsVariableReference(
            instance: null, stateSave, changedMember: "VariableReferences", oldValue: null);

        var varList = (List<string>)stateSave.GetVariableListSave("VariableReferences").ValueAsIList;
        varList.Count.ShouldBe(3);
        varList.ShouldContain("StrokeRed = Background.StrokeRed");
        varList.ShouldContain("StrokeGreen = Background.StrokeGreen");
        varList.ShouldContain("StrokeBlue = Background.StrokeBlue");
    }

    [Fact]
    public void ReactIfChangedMemberIsVariableReference_SuffixedColorAssignment_ExpandsToSuffixedChannels()
    {
        // Gradient channels carry a numeric suffix (e.g. Color2 -> Red2/Green2/Blue2).
        StateSave stateSave = BuildStateWithVariableReferences("VariableReferences", "Background.Color2");

        _sut.ReactIfChangedMemberIsVariableReference(
            instance: null, stateSave, changedMember: "VariableReferences", oldValue: null);

        var varList = (List<string>)stateSave.GetVariableListSave("VariableReferences").ValueAsIList;
        varList.Count.ShouldBe(3);
        varList.ShouldContain("Red2 = Background.Red2");
        varList.ShouldContain("Green2 = Background.Green2");
        varList.ShouldContain("Blue2 = Background.Blue2");
    }

    [Fact]
    public void ReactIfChangedMemberIsVariableReference_ImpliedLeftSide_InfersLeftFromRightSide()
    {
        // "Instance.Variable" with no equals sign should infer "Variable = Instance.Variable"
        StateSave stateSave = BuildStateWithVariableReferences("VariableReferences", "Background.Width");

        _sut.ReactIfChangedMemberIsVariableReference(
            instance: null, stateSave, changedMember: "VariableReferences", oldValue: null);

        var varList = (List<string>)stateSave.GetVariableListSave("VariableReferences").ValueAsIList;
        varList[0].ShouldBe("Width = Background.Width");
    }

    [Fact]
    public void ReactIfChangedMemberIsVariableReference_ListChangedFromNull_RefreshesVariables()
    {
        StateSave stateSave = BuildStateWithVariableReferences("VariableReferences", "X=Y");

        _sut.ReactIfChangedMemberIsVariableReference(
            instance: null, stateSave, changedMember: "VariableReferences", oldValue: null);

        _guiCommandsMock.Verify(x => x.RefreshVariables(It.IsAny<bool>()), Times.Once);
    }

    [Fact]
    public void ReactIfChangedMemberIsVariableReference_ListUnchanged_DoesNotRefreshVariables()
    {
        var oldValue = new List<string> { "X=Y" };
        StateSave stateSave = BuildStateWithVariableReferences("VariableReferences", "X=Y");

        _sut.ReactIfChangedMemberIsVariableReference(
            instance: null, stateSave, changedMember: "VariableReferences", oldValue: oldValue);

        _guiCommandsMock.Verify(x => x.RefreshVariables(It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public void ReactIfChangedMemberIsVariableReference_NonVariableReferencesMember_DoesNotRefreshVariables()
    {
        StateSave stateSave = new StateSave();

        _sut.ReactIfChangedMemberIsVariableReference(
            instance: null, stateSave, changedMember: "Width", oldValue: null);

        _guiCommandsMock.Verify(x => x.RefreshVariables(It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public void ReactIfChangedMemberIsVariableReference_QualifiedLeftSide_StripsLeftSideDotPrefix()
    {
        // A left side like "Instance.Width" should be stripped to just "Width"
        StateSave stateSave = BuildStateWithVariableReferences("VariableReferences", "Instance.Width=OtherInstance.Width");

        _sut.ReactIfChangedMemberIsVariableReference(
            instance: null, stateSave, changedMember: "VariableReferences", oldValue: null);

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
            instance, stateSave, changedMember: "VariableReferences", oldValue: null);

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

    #region CategoryStateLeftSide

    [Fact]
    public void DoVariableReferenceReaction_CategoryStateLiteralAssignment_AcceptsLine()
    {
        // "ButtonCategoryState = \"Disabled\"" is a synthetic category-state LHS that the
        // runtime routes through GraphicalUiElement.SetProperty. Validation should accept
        // it when "Disabled" exists in ButtonCategory.
        ComponentSave button = BuildButtonComponentWithCategory("ButtonCategory", "Enabled", "Disabled");
        ScreenSave screen = BuildScreenWithVariableReference(
            line: "ButtonCategoryState = \"Disabled\"",
            out StateSave defaultState,
            out VariableListSave<string> varList);

        GumProjectSave project = new GumProjectSave();
        project.Components.Add(button);
        project.Screens.Add(screen);
        ObjectFinder.Self.GumProjectSave = project;

        ScreenInheritsFromButton(screen, button);

        _sut.DoVariableReferenceReaction(
            parentElement: screen,
            leftSideInstance: null,
            unqualifiedMember: "VariableReferences",
            stateSave: defaultState,
            qualifiedName: "VariableReferences",
            trySave: false);

        varList.Value[0].ShouldBe("ButtonCategoryState = \"Disabled\"");
    }

    [Fact]
    public void DoVariableReferenceReaction_CategoryStateNonexistentLiteral_CommentsLine()
    {
        // Literal RHS that does not name a real state in the category should be rejected.
        ComponentSave button = BuildButtonComponentWithCategory("ButtonCategory", "Enabled", "Disabled");
        ScreenSave screen = BuildScreenWithVariableReference(
            line: "ButtonCategoryState = \"NonexistentState\"",
            out StateSave defaultState,
            out VariableListSave<string> varList);

        GumProjectSave project = new GumProjectSave();
        project.Components.Add(button);
        project.Screens.Add(screen);
        ObjectFinder.Self.GumProjectSave = project;

        ScreenInheritsFromButton(screen, button);

        _sut.DoVariableReferenceReaction(
            parentElement: screen,
            leftSideInstance: null,
            unqualifiedMember: "VariableReferences",
            stateSave: defaultState,
            qualifiedName: "VariableReferences",
            trySave: false);

        varList.Value[0].ShouldStartWith("//");
    }

    [Fact]
    public void DoVariableReferenceReaction_CategoryStateNonexistentCategory_CommentsLine()
    {
        // No <CategoryName> matching "MissingCategory" exists anywhere in the chain.
        ComponentSave button = BuildButtonComponentWithCategory("ButtonCategory", "Enabled");
        ScreenSave screen = BuildScreenWithVariableReference(
            line: "MissingCategoryState = \"Foo\"",
            out StateSave defaultState,
            out VariableListSave<string> varList);

        GumProjectSave project = new GumProjectSave();
        project.Components.Add(button);
        project.Screens.Add(screen);
        ObjectFinder.Self.GumProjectSave = project;

        ScreenInheritsFromButton(screen, button);

        _sut.DoVariableReferenceReaction(
            parentElement: screen,
            leftSideInstance: null,
            unqualifiedMember: "VariableReferences",
            stateSave: defaultState,
            qualifiedName: "VariableReferences",
            trySave: false);

        varList.Value[0].ShouldStartWith("//");
    }

    [Fact]
    public void DoVariableReferenceReaction_CategoryStateTernaryRhs_AcceptsLineLeniently()
    {
        // Non-literal RHS: a ternary that resolves to a string should be accepted even
        // though we cannot statically verify the resulting state name.
        ComponentSave button = BuildButtonComponentWithCategory("ButtonCategory", "Enabled", "Disabled");
        ScreenSave screen = BuildScreenWithVariableReference(
            line: "ButtonCategoryState = IsEnabled ? \"Enabled\" : \"Disabled\"",
            out StateSave defaultState,
            out VariableListSave<string> varList);

        defaultState.Variables.Add(new VariableSave
        {
            Name = "IsEnabled",
            Value = true,
            Type = "bool",
            SetsValue = true
        });

        GumProjectSave project = new GumProjectSave();
        project.Components.Add(button);
        project.Screens.Add(screen);
        ObjectFinder.Self.GumProjectSave = project;

        ScreenInheritsFromButton(screen, button);

        _sut.DoVariableReferenceReaction(
            parentElement: screen,
            leftSideInstance: null,
            unqualifiedMember: "VariableReferences",
            stateSave: defaultState,
            qualifiedName: "VariableReferences",
            trySave: false);

        varList.Value[0].ShouldBe("ButtonCategoryState = IsEnabled ? \"Enabled\" : \"Disabled\"");
    }

    [Fact]
    public void DoVariableReferenceReaction_InheritedCategoryState_AcceptsLine()
    {
        // The matching category lives on the base component, not directly on the
        // current element; FindCategoryForStateLeftSide must walk the inheritance chain.
        ComponentSave baseButton = BuildButtonComponentWithCategory("ButtonCategory", "Enabled", "Disabled");
        baseButton.Name = "BaseButton";

        ComponentSave derivedButton = new ComponentSave { Name = "DerivedButton", BaseType = "BaseButton" };
        StateSave derivedDefault = new StateSave { Name = "Default", ParentContainer = derivedButton };
        derivedButton.States.Add(derivedDefault);

        VariableListSave<string> varList = new VariableListSave<string> { Type = "string", Name = "VariableReferences" };
        varList.Value.Add("ButtonCategoryState = \"Disabled\"");
        derivedDefault.VariableLists.Add(varList);

        GumProjectSave project = new GumProjectSave();
        project.Components.Add(baseButton);
        project.Components.Add(derivedButton);
        ObjectFinder.Self.GumProjectSave = project;

        _sut.DoVariableReferenceReaction(
            parentElement: derivedButton,
            leftSideInstance: null,
            unqualifiedMember: "VariableReferences",
            stateSave: derivedDefault,
            qualifiedName: "VariableReferences",
            trySave: false);

        varList.Value[0].ShouldBe("ButtonCategoryState = \"Disabled\"");
    }

    #endregion

    #region Helpers

    private static ComponentSave BuildButtonComponentWithCategory(string categoryName, params string[] stateNames)
    {
        ComponentSave button = new ComponentSave { Name = "Button" };
        StateSave defaultState = new StateSave { Name = "Default", ParentContainer = button };
        button.States.Add(defaultState);

        StateSaveCategory category = new StateSaveCategory { Name = categoryName };
        foreach (string stateName in stateNames)
        {
            category.States.Add(new StateSave { Name = stateName, ParentContainer = button });
        }
        button.Categories.Add(category);

        return button;
    }

    private static ScreenSave BuildScreenWithVariableReference(string line, out StateSave defaultState, out VariableListSave<string> varList)
    {
        ScreenSave screen = new ScreenSave { Name = "TestScreen" };
        defaultState = new StateSave { Name = "Default", ParentContainer = screen };
        screen.States.Add(defaultState);

        varList = new VariableListSave<string> { Type = "string", Name = "VariableReferences" };
        varList.Value.Add(line);
        defaultState.VariableLists.Add(varList);

        return screen;
    }

    private static void ScreenInheritsFromButton(ScreenSave screen, ComponentSave button)
    {
        // Make the screen carry the category by base inheritance via instances:
        // simplest setup is to set BaseType so FindCategoryForStateLeftSide walks
        // GetBaseElements. Screens cannot set BaseType to a Component normally, so we
        // attach the category directly on the screen element when no inheritance applies.
        screen.Categories.Add(new StateSaveCategory
        {
            Name = button.Categories[0].Name,
            States = new List<StateSave>(button.Categories[0].States)
        });
    }

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
