using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Expressions;
using Gum.Managers;
using Gum.StateAnimation.Runtime;
using Gum.Wireframe;
using GumRuntime;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace MonoGameGum.Tests.Runtimes;

/// <summary>
/// Every input of a same-component reference is live: whatever was set last, by code, a state, an
/// animation or a parent at creation, is what the row reads, and rows cascade like spreadsheet cells
/// (#6411, ADR 0022).
/// </summary>
public class LiveInputReferenceTests : BaseTestClass
{
    public LiveInputReferenceTests()
    {
        StandardElementsManager.Self.Initialize();
        GumExpressionService.Initialize();
    }

    public override void Dispose()
    {
        ElementSaveExtensions.CustomEvaluateExpression = null;
        ElementSaveExtensions.CustomEvaluateExpressionAllBranches = null;
        ObjectFinder.Self.GumProjectSave = null;
        base.Dispose();
    }

    #region Two inputs

    [Fact]
    public void TwoInputs_OtherSetFirstThenTrigger_RowReadsTheOther()
    {
        GraphicalUiElement gue = Create("Y = Progress + Offset").ToGraphicalUiElement();

        gue.SetProperty("Offset", 5f);
        gue.SetProperty("Progress", 2f);

        gue.Y.ShouldBe(7f);
    }

    [Fact]
    public void TwoInputs_TypedOtherSetFirst_RowReadsTheOther()
    {
        GraphicalUiElement gue = Create("Y = Progress + X").ToGraphicalUiElement();

        gue.X = 5;
        gue.SetProperty("Progress", 2f);

        gue.Y.ShouldBe(7f);
    }

    [Fact]
    public void TwoInputs_OtherIsAnInstanceVariable_RowReadsTheInstance()
    {
        ComponentSave element = Create("Y = Progress + Child.Width");
        AddInstance(element, "Child");
        GraphicalUiElement gue = element.ToGraphicalUiElement();

        gue.GetGraphicalUiElementByName("Child")!.Width = 30;
        gue.SetProperty("Progress", 2f);

        gue.Y.ShouldBe(32f);
    }

    [Fact]
    public void TwoInputs_OtherIsABool_RowReadsTheOther()
    {
        GraphicalUiElement gue = Create("Y = Progress * (Visible ? 1 : 10)").ToGraphicalUiElement();

        gue.Visible = false;
        gue.SetProperty("Progress", 2f);

        gue.Y.ShouldBe(20f);
    }

    [Fact]
    public void TwoInputs_OtherIsNullableAndSet_RowReadsTheOther()
    {
        // MinWidth is authored null; code sets it, then the trigger moves.
        GraphicalUiElement gue = Create("Y = Progress + MinWidth").ToGraphicalUiElement();

        gue.MinWidth = 40;
        gue.SetProperty("Progress", 2f);

        gue.Y.ShouldBe(42f);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TwoInputs_NullableSetBackToNull_RowDoesNotReadTheStaleValue(bool throughSetProperty)
    {
        GraphicalUiElement gue = Create("Y = Progress + MinWidth").ToGraphicalUiElement();
        gue.MinWidth = 40;
        gue.SetProperty("Progress", 2f);
        gue.Y.ShouldBe(42f);

        if (throughSetProperty)
        {
            gue.SetProperty("MinWidth", null);
        }
        else
        {
            gue.MinWidth = null;
        }
        gue.SetProperty("Progress", 3f);

        gue.Y.ShouldNotBe(43f);
    }

    [Fact]
    public void TwoInputs_RowWritesANullable_RowReadsTheOther()
    {
        GraphicalUiElement gue = Create("MaxWidth = Progress + Offset").ToGraphicalUiElement();

        gue.SetProperty("Offset", 5f);
        gue.SetProperty("Progress", 2f);

        gue.MaxWidth.ShouldBe(7f);
    }

    [Fact]
    public void TwoInputs_OtherSetByAState_RowReadsTheOther()
    {
        GraphicalUiElement gue = Create("Y = Progress + Offset").ToGraphicalUiElement();
        StateSave offsetState = new StateSave();
        offsetState.Variables.Add(new VariableSave { Name = "Offset", Type = "float", Value = 5f, SetsValue = true });
        StateSave progressState = new StateSave();
        progressState.Variables.Add(new VariableSave { Name = "Progress", Type = "float", Value = 2f, SetsValue = true });

        gue.ApplyState(offsetState);
        gue.ApplyState(progressState);

        gue.Y.ShouldBe(7f);
    }

    [Fact]
    public void TwoInputs_OtherSetByAnAnimation_RowReadsTheOther()
    {
        ComponentSave element = Create("Y = Progress + Offset");
        AnimationRuntime animation = CreateAnimation(element, "Offset", 0f, 10f);
        GraphicalUiElement gue = element.ToGraphicalUiElement();

        animation.ApplyAtTimeTo(1.0, gue);
        gue.SetProperty("Progress", 2f);

        gue.Y.ShouldBe(12f);
    }

    [Fact]
    public void TwoInputs_OtherSetByAParentAtCreation_RowReadsTheOther()
    {
        ComponentSave wave = Create("Y = Progress + Offset");
        ScreenSave screen = new ScreenSave { Name = "Main" };
        screen.States.Add(new StateSave { Name = "Default", ParentContainer = screen });
        screen.Instances.Add(new InstanceSave { Name = "Wave1", BaseType = "Wave", ParentContainer = screen });
        screen.DefaultState!.Variables.Add(new VariableSave { Name = "Wave1.Offset", Type = "float", Value = 5f, SetsValue = true });
        ObjectFinder.Self.GumProjectSave!.Screens.Add(screen);

        GraphicalUiElement root = screen.ToGraphicalUiElement();
        GraphicalUiElement wave1 = root.GetGraphicalUiElementByName("Wave1")!;
        wave1.SetProperty("Progress", 2f);

        wave1.Y.ShouldBe(7f);
    }

    [Fact]
    public void TwoInputs_DerivedComponent_RowReadsTheOther()
    {
        Create("Y = Progress + Offset");
        GraphicalUiElement gue = CreateDerived().ToGraphicalUiElement();

        gue.SetProperty("Offset", 5f);
        gue.SetProperty("Progress", 2f);

        gue.Y.ShouldBe(7f);
    }

    [Fact]
    public void TwoInputs_InputAuthoredAndNeverSet_RowReadsTheAuthoredValue()
    {
        ComponentSave element = Create("Y = Progress + Offset");
        element.DefaultState!.Variables.First(item => item.Name == "Offset").Value = 9f;
        GraphicalUiElement gue = element.ToGraphicalUiElement();

        gue.SetProperty("Progress", 2f);

        gue.Y.ShouldBe(11f);
    }

    #endregion

    #region Chains

    public static IEnumerable<object[]> ChainOrders()
    {
        string[] rows = { "A = B + C", "D = A * 2", "Width = D + F", "Height = Width + 1" };
        yield return new object[] { string.Join("|", rows) };
        yield return new object[] { string.Join("|", rows.Reverse()) };
        yield return new object[] { string.Join("|", new[] { rows[2], rows[0], rows[3], rows[1] }) };
    }

    [Theory]
    [MemberData(nameof(ChainOrders))]
    public void Chain_AnyInputSetInAnyOrder_EveryRowFollowsInDependencyOrder(string rows)
    {
        GraphicalUiElement gue = Create(rows.Split('|')).ToGraphicalUiElement();

        gue.SetProperty("F", 100f);
        gue.SetProperty("C", 2f);
        gue.SetProperty("B", 3f);

        // A = 5, D = 10, Width = 110, Height = 111
        gue.Width.ShouldBe(110f);
        gue.Height.ShouldBe(111f);

        gue.SetProperty("F", 1f);
        gue.Width.ShouldBe(11f);
        gue.Height.ShouldBe(12f);

        gue.SetProperty("C", 7f);
        // A = 10, D = 20
        gue.Width.ShouldBe(21f);
        gue.Height.ShouldBe(22f);
    }

    [Fact]
    public void Chain_InputSetByAState_CascadesWithTheOthersLive()
    {
        GraphicalUiElement gue = Create("A = B + C", "D = A * 2", "Width = D + F").ToGraphicalUiElement();
        gue.SetProperty("C", 2f);
        gue.SetProperty("F", 100f);
        StateSave state = new StateSave();
        state.Variables.Add(new VariableSave { Name = "B", Type = "float", Value = 3f, SetsValue = true });

        gue.ApplyState(state);

        gue.Width.ShouldBe(110f);
    }

    [Fact]
    public void Chain_InputSetByAnAnimation_CascadesWithTheOthersLive()
    {
        ComponentSave element = Create("A = B + C", "D = A * 2", "Width = D + F");
        AnimationRuntime animation = CreateAnimation(element, "B", 0f, 10f);
        GraphicalUiElement gue = element.ToGraphicalUiElement();
        gue.SetProperty("C", 2f);
        gue.SetProperty("F", 100f);

        animation.ApplyAtTimeTo(0.5, gue);

        // B = 5, A = 7, D = 14
        gue.Width.ShouldBe(114f);
    }

    [Fact]
    public void Chain_InputSetByAParentAtCreation_CascadesWithTheOthersLive()
    {
        ComponentSave wave = Create("A = B + C", "D = A * 2", "Width = D + F");
        ScreenSave screen = new ScreenSave { Name = "Main" };
        screen.States.Add(new StateSave { Name = "Default", ParentContainer = screen });
        screen.Instances.Add(new InstanceSave { Name = "Wave1", BaseType = "Wave", ParentContainer = screen });
        screen.DefaultState!.Variables.Add(new VariableSave { Name = "Wave1.F", Type = "float", Value = 100f, SetsValue = true });
        ObjectFinder.Self.GumProjectSave!.Screens.Add(screen);

        GraphicalUiElement wave1 = screen.ToGraphicalUiElement().GetGraphicalUiElementByName("Wave1")!;
        wave1.SetProperty("C", 2f);
        wave1.SetProperty("B", 3f);

        wave1.Width.ShouldBe(110f);
    }

    [Fact]
    public void Chain_DerivedComponent_ReevaluatesTheInheritedChain()
    {
        Create("A = B + C", "D = A * 2", "Width = D + F");
        GraphicalUiElement gue = CreateDerived().ToGraphicalUiElement();

        gue.SetProperty("F", 100f);
        gue.SetProperty("C", 2f);
        gue.SetProperty("B", 3f);

        gue.Width.ShouldBe(110f);
    }

    [Fact]
    public void Diamond_BothBranchesReadTheSameInput_EndsConsistentAndEvaluatesEachRowOnce()
    {
        ComponentSave element = Create("A = B + 1", "A2 = B * 2", "Y = A + A2");
        GraphicalUiElement gue = element.ToGraphicalUiElement();
        gue.SetProperty("A", 0f);
        int evaluations = 0;
        CountEvaluations(() => evaluations++);

        gue.SetProperty("B", 3f);

        gue.Y.ShouldBe(10f);
        evaluations.ShouldBe(3);
    }

    [Fact]
    public void Chain_FourRows_OneChangeEvaluatesEachRowOnce()
    {
        ComponentSave element = Create("A = B + C", "D = A * 2", "Width = D + F", "Height = Width + 1");
        GraphicalUiElement gue = element.ToGraphicalUiElement();
        int evaluations = 0;
        CountEvaluations(() => evaluations++);

        gue.SetProperty("B", 3f);

        evaluations.ShouldBe(4);
    }

    [Fact]
    public void Clone_CustomVariableChain_EvaluatesFromItsOwnValuesAndDoesNotLeakToOrFromTheSource()
    {
        GraphicalUiElement source = Create("A = B + C", "D = A * 2", "Width = D + F").ToGraphicalUiElement();
        source.SetProperty("C", 2f);
        source.SetProperty("F", 100f);

        GraphicalUiElement clone = source.Clone();

        // The clone starts with the source's values...
        clone.SetProperty("B", 3f);
        clone.Width.ShouldBe(110f);

        // ...then owns them: a change on the clone is not read by the source, nor the reverse.
        clone.SetProperty("C", 10f);
        source.SetProperty("B", 1f);
        source.Width.ShouldBe(106f);
        clone.SetProperty("B", 3f);
        clone.Width.ShouldBe(126f);
        source.SetProperty("F", 0f);
        clone.SetProperty("B", 4f);
        clone.Width.ShouldBe(128f);
    }

    #endregion

    #region Override and cycles

    [Fact]
    public void Override_RowResultSetInCode_DownstreamRowsReadItAndTheNextUpstreamChangeWins()
    {
        GraphicalUiElement gue = Create("A = B + C", "Width = A + F").ToGraphicalUiElement();
        gue.SetProperty("F", 100f);
        gue.SetProperty("B", 1f);
        gue.SetProperty("C", 1f);
        gue.Width.ShouldBe(102f);

        // Last set wins: the code value of A is what Width reads...
        gue.SetProperty("A", 50f);
        gue.Width.ShouldBe(150f);

        // ...until an input of A's own row changes after it, which re-asserts the row.
        gue.SetProperty("B", 4f);
        gue.Width.ShouldBe(105f);
    }

    [Fact]
    public void Override_TypedRowResultSetInCode_DownstreamRowsReadIt()
    {
        GraphicalUiElement gue = Create("Y = Progress * 2", "Width = Y + 1").ToGraphicalUiElement();

        gue.Y = 100;
        gue.Width.ShouldBe(101f);

        gue.SetProperty("Progress", 3f);
        gue.Y.ShouldBe(6f);
        gue.Width.ShouldBe(7f);
    }

    [Fact]
    public void Cycle_RowsThatReadEachOther_TerminateAndOtherRowsStillUseLiveInputs()
    {
        GraphicalUiElement gue = Create("A = B + 1", "B = A + 1", "Y = Progress + Offset").ToGraphicalUiElement();

        Should.NotThrow(() =>
        {
            gue.SetProperty("A", 3f);
            gue.SetProperty("Offset", 5f);
            gue.SetProperty("Progress", 2f);
        });

        gue.Y.ShouldBe(7f);
    }

    #endregion

    #region Nullable in a chain

    [Fact]
    public void NullableChain_InputsSetInAnyOrder_ReadsEveryInputLive()
    {
        GraphicalUiElement gue = Create("MinWidth = Progress + Offset", "Y = MinWidth + Other").ToGraphicalUiElement();

        gue.SetProperty("Other", 1f);
        gue.SetProperty("Offset", 5f);
        gue.SetProperty("Progress", 2f);

        gue.MinWidth.ShouldBe(7f);
        gue.Y.ShouldBe(8f);
    }

    #endregion

    private static void CountEvaluations(Action onEvaluate)
    {
        Func<StateSave, string, string, GraphicalUiElement, object> inner = ElementSaveExtensions.CustomEvaluateExpression!;
        ElementSaveExtensions.CustomEvaluateExpression = (state, right, leftType, live) =>
        {
            onEvaluate();
            return inner(state, right, leftType!, live!);
        };
    }

    private static void AddInstance(ComponentSave element, string name)
    {
        element.Instances.Add(new InstanceSave { Name = name, BaseType = "Container", ParentContainer = element });
    }

    private static ComponentSave CreateDerived()
    {
        ComponentSave derived = new ComponentSave { Name = "Derived", BaseType = "Wave" };
        derived.States.Add(new StateSave { Name = "Default", ParentContainer = derived });
        ObjectFinder.Self.GumProjectSave!.Components.Add(derived);
        return derived;
    }

    // A component named Wave with custom float variables and the given rows. Must be called first.
    private static ComponentSave Create(params string[] rows)
    {
        ComponentSave element = new ComponentSave { Name = "Wave", BaseType = "Container" };
        StateSave state = new StateSave { Name = "Default", ParentContainer = element };
        element.States.Add(state);
        foreach (string name in new[] { "Progress", "Offset", "Other", "A", "A2", "B", "C", "D", "F", "X", "Y" })
        {
            state.Variables.Add(new VariableSave { Name = name, Type = "float", Value = 0f, SetsValue = true });
        }

        VariableListSave<string> list = new VariableListSave<string> { Type = "string", Name = "VariableReferences" };
        list.Value.AddRange(rows);
        state.VariableLists.Add(list);

        GumProjectSave project = new GumProjectSave();
        project.Components.Add(element);
        StandardElementSave container = new StandardElementSave { Name = "Container" };
        container.States.Add(StandardElementsManager.Self.GetDefaultStateFor("Container")!);
        container.DefaultState!.ParentContainer = container;
        project.StandardElements.Add(container);
        ObjectFinder.Self.GumProjectSave = project;
        return element;
    }

    // The variable moves from start to end over one second.
    private static AnimationRuntime CreateAnimation(ComponentSave element, string variable, float start, float end)
    {
        StateSaveCategory category = new StateSaveCategory { Name = "Cat" };
        foreach ((string name, float value) in new[] { ("Start", start), ("End", end) })
        {
            StateSave state = new StateSave { Name = name, ParentContainer = element };
            state.Variables.Add(new VariableSave { Name = variable, Type = "float", Value = value, SetsValue = true });
            category.States.Add(state);
        }
        element.Categories.Add(category);

        AnimationRuntime animation = new AnimationRuntime();
        animation.Keyframes.Add(new KeyframeRuntime
        {
            StateName = "Cat/Start",
            Time = 0,
            InterpolationType = FlatRedBall.Glue.StateInterpolation.InterpolationType.Linear
        });
        animation.Keyframes.Add(new KeyframeRuntime
        {
            StateName = "Cat/End",
            Time = 1,
            InterpolationType = FlatRedBall.Glue.StateInterpolation.InterpolationType.Linear
        });
        animation.RefreshCumulativeStates(element);
        return animation;
    }
}
