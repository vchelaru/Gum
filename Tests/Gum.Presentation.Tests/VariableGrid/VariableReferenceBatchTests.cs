using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Expressions;
using Gum.Managers;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Gum.Services;
using Gum.Services.Dialogs;
using Gum.Wireframe;
using GumRuntime;
using Moq;
using Shouldly;

namespace Gum.Presentation.Tests.VariableGrid;

/// <summary>
/// A multi-select edit sets one variable per selected instance. Re-applying every reference of the state is
/// state-wide work, so it runs once for the batch rather than once per instance (#7509).
/// </summary>
public class VariableReferenceBatchTests : BaseTestClass
{
    private const int Count = 5;

    private readonly VariableReferenceLogic _sut;
    private readonly Mock<IFileCommands> _fileCommands = new();
    private readonly List<(string Name, object? Value)> _notifications = new();

    public VariableReferenceBatchTests()
    {
        GumExpressionService.Initialize();
        _sut = new VariableReferenceLogic(
            Mock.Of<IGuiCommands>(),
            Mock.Of<IWireframeCommands>(),
            Mock.Of<IDialogService>(),
            _fileCommands.Object,
            Mock.Of<ICompositeMemberRegistry>(),
            Mock.Of<IDispatcher>(),
            Mock.Of<IWireframeObjectManager>());
        ElementSaveExtensions.VariableChangedThroughReference = (_, _, name, _, _) => _notifications.Add((name, null));
    }

    public override void Dispose()
    {
        ElementSaveExtensions.VariableChangedThroughReference = null;
        ElementSaveExtensions.CustomEvaluateExpression = null;
        base.Dispose();
    }

    /// <summary>A screen whose Total reads the sum of V0..V4, so every edit changes Total until the last one lands.</summary>
    private ScreenSave BuildScreen(out StateSave state, string name = "BatchScreen", bool newProject = true)
    {
        if (newProject)
        {
            ObjectFinder.Self.GumProjectSave = new GumProjectSave();
        }
        ScreenSave screen = new ScreenSave { Name = name };
        state = new StateSave { Name = "Default", ParentContainer = screen };
        screen.States.Add(state);
        for (int i = 0; i < Count; i++)
        {
            state.Variables.Add(new VariableSave { Name = $"V{i}", Value = 0f, Type = "float", SetsValue = true });
        }
        state.Variables.Add(new VariableSave { Name = "Total", Value = 0f, Type = "float", SetsValue = true });
        VariableListSave<string> references = new VariableListSave<string> { Type = "string", Name = "VariableReferences" };
        references.Value.Add("Total = " + string.Join(" + ", Enumerable.Range(0, Count).Select(i => $"V{i}")));
        state.VariableLists.Add(references);
        ObjectFinder.Self.GumProjectSave.Screens.Add(screen);
        return screen;
    }

    private void SetAndReact(ScreenSave screen, StateSave state, int index, float value)
    {
        state.SetValue($"V{index}", value);
        _sut.DoVariableReferenceReaction(screen, null, $"V{index}", state, $"V{index}", trySave: true);
    }

    [Fact]
    public void DoVariableReferenceReaction_WithoutBatch_ReappliesTheStateAfterEveryEdit()
    {
        ScreenSave screen = BuildScreen(out StateSave state);

        for (int i = 0; i < Count; i++)
        {
            SetAndReact(screen, state, i, 10f);
        }

        state.GetValue("Total").ShouldBe(50f);
        _notifications.Count(item => item.Name == "Total").ShouldBe(Count);
    }

    [Fact]
    public void BeginReferenceBatch_AppliesTheStateOnceAndGivesTheSameValues()
    {
        ScreenSave screen = BuildScreen(out StateSave state);

        using (_sut.BeginReferenceBatch())
        {
            for (int i = 0; i < Count; i++)
            {
                SetAndReact(screen, state, i, 10f);
            }

            // Nothing state-wide has run yet.
            state.GetValue("Total").ShouldBe(0f);
        }

        state.GetValue("Total").ShouldBe(50f);
        _notifications.Count(item => item.Name == "Total").ShouldBe(1);
    }

    [Fact]
    public void BeginReferenceBatch_SavesTheElementOnceWhenAVariableReferencesRowWasEdited()
    {
        ScreenSave screen = BuildScreen(out StateSave state);

        using (_sut.BeginReferenceBatch())
        {
            for (int i = 0; i < Count; i++)
            {
                _sut.DoVariableReferenceReaction(screen, null, "VariableReferences", state, "VariableReferences", trySave: true);
            }
        }

        _fileCommands.Verify(x => x.TryAutoSaveElement(screen), Times.Once);
    }

    [Fact]
    public void BeginReferenceBatch_SpanningTwoElements_AppliesEachElementOnce()
    {
        ScreenSave first = BuildScreen(out StateSave firstState, "FirstScreen");
        ScreenSave second = BuildScreen(out StateSave secondState, "SecondScreen", newProject: false);

        using (_sut.BeginReferenceBatch())
        {
            for (int i = 0; i < Count; i++)
            {
                SetAndReact(first, firstState, i, 10f);
                SetAndReact(second, secondState, i, 20f);
            }
        }

        firstState.GetValue("Total").ShouldBe(50f);
        secondState.GetValue("Total").ShouldBe(100f);
        _notifications.Count(item => item.Name == "Total").ShouldBe(2);
    }

    [Fact]
    public void BeginReferenceBatch_Nested_RunsTheStateWideWorkWhenTheOutermostScopeEnds()
    {
        ScreenSave screen = BuildScreen(out StateSave state);

        using (_sut.BeginReferenceBatch())
        {
            using (_sut.BeginReferenceBatch())
            {
                SetAndReact(screen, state, 0, 10f);
            }

            state.GetValue("Total").ShouldBe(0f);
        }

        state.GetValue("Total").ShouldBe(10f);
    }
}
