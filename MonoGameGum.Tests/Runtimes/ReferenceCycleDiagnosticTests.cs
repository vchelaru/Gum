using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Expressions;
using Gum.Managers;
using Gum.Wireframe;
using GumRuntime;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace MonoGameGum.Tests.Runtimes;

/// <summary>
/// An element whose references read each other in a loop leaves one runtime diagnostic in diagnostic builds (#6885),
/// naming the element and the rows, and nothing in a build without FULL_DIAGNOSTICS. The rows are skipped either way (#6871).
/// </summary>
public class ReferenceCycleDiagnosticTests : BaseTestClass
{
    private readonly List<string> _messages = new();

    public ReferenceCycleDiagnosticTests()
    {
        StandardElementsManager.Self.Initialize();
        ElementSaveExtensions.CustomEvaluateExpression = null;
        ElementSaveExtensions.CustomEvaluateExpressionAllBranches = null;
        CustomSetPropertyOnRenderable.PropertyAssignmentError += Record;
    }

    private void Record(string message)
    {
        if (message.Contains("Circular variable reference"))
        {
            _messages.Add(message);
        }
    }

    public override void Dispose()
    {
        CustomSetPropertyOnRenderable.PropertyAssignmentError -= Record;
        ElementSaveExtensions.Reset();
        ElementSaveExtensions.CustomEvaluateExpression = null;
        ElementSaveExtensions.CustomEvaluateExpressionAllBranches = null;
        ObjectFinder.Self.GumProjectSave = null;
        base.Dispose();
    }

    private static GumProjectSave CreateProject()
    {
        GumProjectSave project = new GumProjectSave();
        StandardElementSave container = new StandardElementSave { Name = "Container" };
        container.States.Add(StandardElementsManager.Self.GetDefaultStateFor("Container")!.Clone());
        container.DefaultState!.ParentContainer = container;
        project.StandardElements.Add(container);
        ObjectFinder.Self.GumProjectSave = project;
        return project;
    }

    private static ComponentSave AddPanel(GumProjectSave project, string name, params string[] instanceNames) =>
        AddPanelBasedOn(project, name, "Container", instanceNames);

    private static ComponentSave AddPanelBasedOn(GumProjectSave project, string name, string baseType, params string[] instanceNames)
    {
        ComponentSave panel = new ComponentSave { Name = name, BaseType = baseType };
        panel.States.Add(new StateSave { Name = "Default", ParentContainer = panel });
        // A saved project repeats a base component's instances on the derived one, marked DefinedByBase.
        ComponentSave? baseComponent = project.Components.FirstOrDefault(item => item.Name == baseType);
        foreach (InstanceSave inherited in baseComponent?.Instances ?? new List<InstanceSave>())
        {
            panel.Instances.Add(new InstanceSave
            {
                Name = inherited.Name, BaseType = inherited.BaseType, DefinedByBase = true, ParentContainer = panel
            });
        }
        foreach (string instanceName in instanceNames)
        {
            panel.Instances.Add(new InstanceSave { Name = instanceName, BaseType = "Container", ParentContainer = panel });
            panel.DefaultState.Variables.Add(new VariableSave
            {
                Name = instanceName + ".Width", Type = "float", Value = 10f, SetsValue = true
            });
        }
        project.Components.Add(panel);
        return panel;
    }

    private static void AddRow(ComponentSave panel, string instanceName, string row)
    {
        string listName = instanceName + ".VariableReferences";
        VariableListSave<string>? list = panel.DefaultState.VariableLists
            .OfType<VariableListSave<string>>().FirstOrDefault(item => item.Name == listName);
        if (list == null)
        {
            list = new VariableListSave<string> { Type = "string", Name = listName };
            panel.DefaultState.VariableLists.Add(list);
        }
        list.Value.Add(row);
    }

    private static void AddCategorizedState(ComponentSave panel, string stateName, params (string Instance, string Row)[] rows)
    {
        StateSave state = new StateSave { Name = stateName, ParentContainer = panel };
        foreach ((string instance, string row) in rows)
        {
            VariableListSave<string> list = new VariableListSave<string> { Type = "string", Name = instance + ".VariableReferences" };
            list.Value.Add(row);
            state.VariableLists.Add(list);
        }
        StateSaveCategory category = new StateSaveCategory { Name = "Cat" };
        category.States.Add(state);
        panel.Categories.Add(category);
    }

    private static ScreenSave AddScreen(GumProjectSave project, params (string Instance, string Type)[] instances)
    {
        ScreenSave screen = new ScreenSave { Name = "TestScreen" };
        screen.States.Add(new StateSave { Name = "Default", ParentContainer = screen });
        foreach ((string instance, string type) in instances)
        {
            screen.Instances.Add(new InstanceSave { Name = instance, BaseType = type, ParentContainer = screen });
        }
        project.Screens.Add(screen);
        return screen;
    }

    private static ComponentSave AddTwoRowLoop(GumProjectSave project, string name)
    {
        ComponentSave panel = AddPanel(project, name, "A", "B");
        AddRow(panel, "A", "Width = B.Width + 1");
        AddRow(panel, "B", "Width = A.Width + 1");
        return panel;
    }

    private void UseExpressions(bool useExpressions)
    {
        if (useExpressions)
        {
            GumExpressionService.Initialize();
        }
    }

#if FULL_DIAGNOSTICS
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Create_RowsReadingEachOther_ReportTheElementAndTheRowsOnce(bool useExpressions)
    {
        UseExpressions(useExpressions);
        GumProjectSave project = CreateProject();
        AddTwoRowLoop(project, "Panel");
        ScreenSave screen = AddScreen(project, ("P", "Panel"));

        screen.ToGraphicalUiElement();

        _messages.Count.ShouldBe(1, string.Join(Environment.NewLine, _messages));
        _messages[0].ShouldContain("in Panel (Default):");
        _messages[0].ShouldContain("A.Width = B.Width + 1");
        _messages[0].ShouldContain("B.Width = A.Width + 1");
        _messages[0].ShouldContain("keep their stored values");
        _messages[0].ShouldContain("remove");
    }

    [Fact]
    public void Create_RepeatedCreationsAndApplies_ReportTheElementOnce()
    {
        GumProjectSave project = CreateProject();
        AddTwoRowLoop(project, "Panel");
        ScreenSave screen = AddScreen(project, ("P", "Panel"), ("Q", "Panel"));

        GraphicalUiElement root = screen.ToGraphicalUiElement();
        screen.ToGraphicalUiElement();
        root.RefreshStyles();
        project.ApplyAllVariableReferences();
        project.ApplyAllVariableReferences();
        root.GetGraphicalUiElementByName("P")!.ApplyState(project.Components[0].DefaultState);

        _messages.Count.ShouldBe(1, string.Join(Environment.NewLine, _messages));
    }

    [Fact]
    public void ApplyAllVariableReferences_RowsReadingEachOther_ReportOnceNoMatterHowManyTimesItRuns()
    {
        GumProjectSave project = CreateProject();
        AddTwoRowLoop(project, "Panel");

        project.ApplyAllVariableReferences();
        project.ApplyAllVariableReferences();
        project.ApplyAllVariableReferences();

        _messages.Count.ShouldBe(1, string.Join(Environment.NewLine, _messages));
        _messages[0].ShouldContain("in Panel (Default):");
    }

    [Fact]
    public void ApplyAllVariableReferences_ProjectReloadedWithTheSameLoop_ReportsAgainOnce()
    {
        GumProjectSave first = CreateProject();
        AddTwoRowLoop(first, "Panel");
        first.ApplyAllVariableReferences();

        GumProjectSave reloaded = CreateProject();
        AddTwoRowLoop(reloaded, "Panel");
        reloaded.ApplyAllVariableReferences();
        reloaded.ApplyAllVariableReferences();

        _messages.Count.ShouldBe(2, string.Join(Environment.NewLine, _messages));
    }

    [Fact]
    public void Create_TwoElementsWithLoops_ReportEachOnceByName()
    {
        GumProjectSave project = CreateProject();
        AddTwoRowLoop(project, "FirstPanel");
        AddTwoRowLoop(project, "SecondPanel");
        ScreenSave screen = AddScreen(project, ("P", "FirstPanel"), ("Q", "SecondPanel"), ("R", "FirstPanel"));

        screen.ToGraphicalUiElement();
        screen.ToGraphicalUiElement();

        _messages.Count.ShouldBe(2, string.Join(Environment.NewLine, _messages));
        _messages.Count(item => item.Contains("in FirstPanel (Default):")).ShouldBe(1);
        _messages.Count(item => item.Contains("in SecondPanel (Default):")).ShouldBe(1);
    }

    [Fact]
    public void Create_ALoopThroughAChain_ReportsEveryRowOfTheLoopInOneMessage()
    {
        GumProjectSave project = CreateProject();
        ComponentSave panel = AddPanel(project, "Panel", "A", "B", "C");
        AddRow(panel, "A", "Width = B.Width + 1");
        AddRow(panel, "B", "Width = C.Width + 1");
        AddRow(panel, "C", "Width = A.Width + 1");
        ScreenSave screen = AddScreen(project, ("P", "Panel"));

        screen.ToGraphicalUiElement();
        screen.ToGraphicalUiElement();

        _messages.Count.ShouldBe(1, string.Join(Environment.NewLine, _messages));
        _messages[0].ShouldContain("A.Width = B.Width + 1");
        _messages[0].ShouldContain("B.Width = C.Width + 1");
        _messages[0].ShouldContain("C.Width = A.Width + 1");
    }

    [Fact]
    public void Create_ALoopThroughTwoElements_ReportsOnceOnEachElementThatHoldsARow()
    {
        GumProjectSave project = CreateProject();
        ComponentSave first = AddPanel(project, "Panel", "A");
        ComponentSave second = AddPanel(project, "Other", "B");
        AddRow(first, "A", "Width = Components/Other.B.Width + 1");
        AddRow(second, "B", "Width = Components/Panel.A.Width + 1");
        ScreenSave screen = AddScreen(project, ("P", "Panel"), ("Q", "Other"));

        screen.ToGraphicalUiElement();
        screen.ToGraphicalUiElement();
        project.ApplyAllVariableReferences();

        _messages.Count.ShouldBe(2, string.Join(Environment.NewLine, _messages));
        _messages.Count(item => item.Contains("in Panel (Default):")).ShouldBe(1);
        _messages.Count(item => item.Contains("in Other (Default):")).ShouldBe(1);
    }

    [Fact]
    public void Create_RowsInACategorizedStateReadingEachOther_AreReported()
    {
        GumProjectSave project = CreateProject();
        ComponentSave panel = AddPanel(project, "Panel", "A", "B");
        AddCategorizedState(panel, "Alt", ("A", "Width = B.Width + 1"), ("B", "Width = A.Width + 1"));
        ScreenSave screen = AddScreen(project, ("P", "Panel"));

        screen.ToGraphicalUiElement();
        project.ApplyAllVariableReferences();
        project.ApplyAllVariableReferences();

        _messages.Count.ShouldBe(1, string.Join(Environment.NewLine, _messages));
        _messages[0].ShouldContain("in Panel (Alt):");
    }
    [Fact]
    public void Create_ALoopInTheDefaultState_NamesTheState()
    {
        GumProjectSave project = CreateProject();
        AddTwoRowLoop(project, "Panel");
        ScreenSave screen = AddScreen(project, ("P", "Panel"));

        screen.ToGraphicalUiElement();

        _messages.Count.ShouldBe(1, string.Join(Environment.NewLine, _messages));
        _messages[0].ShouldContain("in Panel (Default):");
    }

    [Fact]
    public void Create_ALoopInACategorizedState_NamesThatState()
    {
        GumProjectSave project = CreateProject();
        ComponentSave panel = AddPanel(project, "Panel", "A", "B");
        AddCategorizedState(panel, "Alt", ("A", "Width = B.Width + 1"), ("B", "Width = A.Width + 1"));
        ScreenSave screen = AddScreen(project, ("P", "Panel"));

        screen.ToGraphicalUiElement();
        project.ApplyAllVariableReferences();

        _messages.Count.ShouldBe(1, string.Join(Environment.NewLine, _messages));
        _messages[0].ShouldContain("in Panel (Alt):");
    }

    [Fact]
    public void Create_ALoopInABaseComponent_IsReportedOnceOnTheBase()
    {
        GumProjectSave project = CreateProject();
        AddTwoRowLoop(project, "BasePanel");
        AddPanelBasedOn(project, "FirstPanel", "BasePanel");
        AddPanelBasedOn(project, "SecondPanel", "BasePanel");
        ScreenSave screen = AddScreen(project, ("P", "FirstPanel"), ("Q", "SecondPanel"), ("R", "BasePanel"));

        screen.ToGraphicalUiElement();
        screen.ToGraphicalUiElement();
        project.ApplyAllVariableReferences();

        _messages.Count.ShouldBe(1, string.Join(Environment.NewLine, _messages));
        _messages[0].ShouldContain("in BasePanel (Default):");
        _messages[0].ShouldContain("A.Width = B.Width + 1");
    }

    [Fact]
    public void Create_OnlyDerivedElementsAreBuilt_ABaseLoopIsStillReportedOnTheBase()
    {
        GumProjectSave project = CreateProject();
        AddTwoRowLoop(project, "BasePanel");
        AddPanelBasedOn(project, "FirstPanel", "BasePanel");
        AddPanelBasedOn(project, "SecondPanel", "BasePanel");
        ScreenSave screen = AddScreen(project, ("P", "FirstPanel"), ("Q", "SecondPanel"));

        screen.ToGraphicalUiElement();

        _messages.Count.ShouldBe(1, string.Join(Environment.NewLine, _messages));
        _messages[0].ShouldContain("in BasePanel (Default):");
    }

    [Fact]
    public void Create_ALoopInABaseStandardElement_IsReportedOnceOnTheStandard()
    {
        GumProjectSave project = CreateProject();
        VariableListSave<string> list = new VariableListSave<string> { Type = "string", Name = "VariableReferences" };
        list.Value.Add("Width = Height + 1");
        list.Value.Add("Height = Width + 1");
        project.StandardElements[0].DefaultState.VariableLists.Add(list);
        AddPanelBasedOn(project, "FirstPanel", "Container");
        AddPanelBasedOn(project, "SecondPanel", "Container");
        ScreenSave screen = AddScreen(project, ("P", "FirstPanel"), ("Q", "SecondPanel"));

        screen.ToGraphicalUiElement();
        project.ApplyAllVariableReferences();

        _messages.Count.ShouldBe(1, string.Join(Environment.NewLine, _messages));
        _messages[0].ShouldContain("in Container (Default):");
    }

    [Fact]
    public void Create_ADerivedElementWithItsOwnLoop_ReportsItsLoopAndTheBaseLoopOnceEach()
    {
        GumProjectSave project = CreateProject();
        AddTwoRowLoop(project, "BasePanel");
        ComponentSave derived = AddPanelBasedOn(project, "DerivedPanel", "BasePanel", "C", "D");
        AddRow(derived, "C", "Width = D.Width + 1");
        AddRow(derived, "D", "Width = C.Width + 1");
        ScreenSave screen = AddScreen(project, ("P", "DerivedPanel"), ("Q", "BasePanel"));

        screen.ToGraphicalUiElement();
        screen.ToGraphicalUiElement();
        project.ApplyAllVariableReferences();

        _messages.Count.ShouldBe(2, string.Join(Environment.NewLine, _messages));
        string baseMessage = _messages.Single(item => item.Contains("in BasePanel (Default):"));
        baseMessage.ShouldContain("A.Width = B.Width + 1");
        baseMessage.ShouldNotContain("C.Width");
        string derivedMessage = _messages.Single(item => item.Contains("in DerivedPanel (Default):"));
        derivedMessage.ShouldContain("C.Width = D.Width + 1");
        derivedMessage.ShouldContain("D.Width = C.Width + 1");
        derivedMessage.ShouldNotContain("A.Width");
    }

    [Fact]
    public void Create_ADerivedRowClosesALoopWithABaseRow_IsReportedOnTheDerivedElement()
    {
        GumProjectSave project = CreateProject();
        ComponentSave basePanel = AddPanel(project, "BasePanel", "A", "B");
        AddRow(basePanel, "A", "Width = B.Width + 1");
        ComponentSave derived = AddPanelBasedOn(project, "DerivedPanel", "BasePanel");
        AddRow(derived, "B", "Width = A.Width + 1");
        ScreenSave screen = AddScreen(project, ("P", "DerivedPanel"), ("Q", "BasePanel"));

        screen.ToGraphicalUiElement();

        _messages.Count.ShouldBe(1, string.Join(Environment.NewLine, _messages));
        _messages[0].ShouldContain("in DerivedPanel (Default):");
        _messages[0].ShouldContain("A.Width = B.Width + 1");
        _messages[0].ShouldContain("B.Width = A.Width + 1");
    }

    [Fact]
    public void Create_LoopsInTwoStatesOfOneElement_ReportEachOnceNamingItsState()
    {
        GumProjectSave project = CreateProject();
        ComponentSave panel = AddTwoRowLoop(project, "Panel");
        AddCategorizedState(panel, "Alt", ("A", "Height = B.Height + 1"), ("B", "Height = A.Height + 1"));
        ScreenSave screen = AddScreen(project, ("P", "Panel"));

        screen.ToGraphicalUiElement();
        project.ApplyAllVariableReferences();
        project.ApplyAllVariableReferences();

        _messages.Count.ShouldBe(2, string.Join(Environment.NewLine, _messages));
        string defaultMessage = _messages.Single(item => item.Contains("in Panel (Default):"));
        defaultMessage.ShouldContain("A.Width = B.Width + 1");
        defaultMessage.ShouldNotContain("Height");
        string altMessage = _messages.Single(item => item.Contains("in Panel (Alt):"));
        altMessage.ShouldContain("A.Height = B.Height + 1");
        altMessage.ShouldNotContain("A.Width");
    }

    [Fact]
    public void Create_TwoSeparateLoopsInOneState_ReportEachLoopOnceOnItsOwn()
    {
        GumProjectSave project = CreateProject();
        ComponentSave panel = AddTwoRowLoop(project, "Panel");
        AddRow(panel, "A", "Height = B.Height + 1");
        AddRow(panel, "B", "Height = A.Height + 1");
        ScreenSave screen = AddScreen(project, ("P", "Panel"), ("Q", "Panel"));

        screen.ToGraphicalUiElement();
        screen.ToGraphicalUiElement();
        project.ApplyAllVariableReferences();

        _messages.Count.ShouldBe(2, string.Join(Environment.NewLine, _messages));
        string widthMessage = _messages.Single(item => item.Contains("A.Width = B.Width + 1"));
        widthMessage.ShouldContain("in Panel (Default):");
        widthMessage.ShouldNotContain("Height");
        string heightMessage = _messages.Single(item => item.Contains("A.Height = B.Height + 1"));
        heightMessage.ShouldContain("in Panel (Default):");
        heightMessage.ShouldNotContain("Width");
    }
#else
    [Fact]
    public void Create_RowsReadingEachOther_ReportNothingWithoutDiagnostics()
    {
        GumProjectSave project = CreateProject();
        AddTwoRowLoop(project, "Panel");
        ScreenSave screen = AddScreen(project, ("P", "Panel"));

        GraphicalUiElement root = screen.ToGraphicalUiElement();
        project.ApplyAllVariableReferences();

        _messages.ShouldBeEmpty();
        root.GetGraphicalUiElementByName("P")!.GetGraphicalUiElementByName("A")!.Width.ShouldBe(10f);
    }
#endif

    [Fact]
    public void Create_RowsInAChain_ReportNothing()
    {
        UseExpressions(true);
        GumProjectSave project = CreateProject();
        ComponentSave panel = AddPanel(project, "Panel", "A", "B", "C");
        AddRow(panel, "A", "Width = B.Width + 1");
        AddRow(panel, "B", "Width = C.Width + 1");
        ScreenSave screen = AddScreen(project, ("P", "Panel"));

        GraphicalUiElement root = screen.ToGraphicalUiElement();
        project.ApplyAllVariableReferences();

        _messages.ShouldBeEmpty();
        root.GetGraphicalUiElementByName("P")!.GetGraphicalUiElementByName("B")!.Width.ShouldBe(11f);
    }

    [Fact]
    public void Create_RowsReadingEachOther_StillKeepTheStoredValues()
    {
        GumProjectSave project = CreateProject();
        AddTwoRowLoop(project, "Panel");
        ScreenSave screen = AddScreen(project, ("P", "Panel"));

        GraphicalUiElement panel = screen.ToGraphicalUiElement().GetGraphicalUiElementByName("P")!;

        panel.GetGraphicalUiElementByName("A")!.Width.ShouldBe(10f);
        panel.GetGraphicalUiElementByName("B")!.Width.ShouldBe(10f);
    }
}
