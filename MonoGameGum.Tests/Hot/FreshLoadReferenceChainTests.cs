using Gum;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;
using GumRuntime;
using Shouldly;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace MonoGameGum.Tests.Hot;

/// <summary>
/// #6881: a game started on files edited by hand (only the last member of a reference chain changed, so the
/// members in between still hold their saved copies) shows the same value a hot reload of that edit shows.
/// </summary>
public class FreshLoadReferenceChainTests : BaseTestClass
{
    public override void Dispose()
    {
        ElementSaveExtensions.Reset();
        ElementSaveExtensions.CustomEvaluateExpression = null;
        ElementSaveExtensions.CustomEvaluateExpressionAllBranches = null;
        ObjectFinder.Self.GumProjectSave = null;
        base.Dispose();
    }

    public static IEnumerable<object[]> Matrix() =>
        HotReloadReferenceChainTests.StaleCells().Select(c => new object[] { c });

    private static int FreshLoadRed(ChainCell cell)
    {
        HotReloadReferenceChainTests.UseExpressions(cell.Expressions);
        GumProjectSave edited = HotReloadReferenceChainTests.Build(cell, second: true);
        // What GumService does after it loads a project from files.
        edited.ApplyAllVariableReferencesBeforeFirstBuild();
        GraphicalUiElement screen = HotReloadFormsProjects.Load(edited);
        HotReloadReferenceChainTests.EnterState(screen, cell);
        return HotReloadFormsProjects.Red(HotReloadReferenceChainTests.ReaderVisual(screen, cell.Reader));
    }

    private static int ReloadedRed(ChainCell cell)
    {
        HotReloadReferenceChainTests.UseExpressions(cell.Expressions);
        GraphicalUiElement screen = HotReloadFormsProjects.Load(HotReloadReferenceChainTests.Build(cell, second: false));
        HotReloadReferenceChainTests.EnterState(screen, cell);
        HotReloadFormsProjects.Reload(screen, HotReloadReferenceChainTests.Build(cell, second: true));
        return HotReloadFormsProjects.Red(HotReloadReferenceChainTests.ReaderVisual(screen, cell.Reader));
    }

    [Theory]
    [MemberData(nameof(Matrix))]
    public void FreshLoad_OnlyAMemberOfTheChainEdited_ReaderShowsTheEndOfTheChain(ChainCell cell)
    {
        FreshLoadRed(cell).ShouldBe(HotReloadReferenceChainTests.ExpectedAfterEdit(cell));
    }

    [Theory]
    [MemberData(nameof(Matrix))]
    public void FreshLoad_OnlyAMemberOfTheChainEdited_ShowsWhatAReloadOfTheSameEditShows(ChainCell cell)
    {
        int reloaded = ReloadedRed(cell);
        ObjectFinder.Self.GumProjectSave = null;
        FreshLoadRed(cell).ShouldBe(reloaded);
    }

    private static readonly ChainCell Plain = new ChainCell(3, ChainPlacement.AcrossComponents, ChainEdit.TailOnly);

    [Fact]
    public void Build_ProjectNotLoadedByAService_KeepsTheSavedValues()
    {
        // Characterization: a project built in code, or loaded by the tool, is not changed by a build.
        HotReloadReferenceChainTests.UseExpressions(false);
        GraphicalUiElement screen = HotReloadFormsProjects.Load(HotReloadReferenceChainTests.Build(Plain, second: true));

        HotReloadFormsProjects.Red(HotReloadReferenceChainTests.ReaderVisual(screen, Plain.Reader))
            .ShouldBe(HotReloadReferenceChainTests.Old);
    }

    [Fact]
    public void Build_SecondBuildAfterTheFirst_DoesNotApplyTheReferencesAgain()
    {
        HotReloadReferenceChainTests.UseExpressions(false);
        GumProjectSave edited = HotReloadReferenceChainTests.Build(Plain, second: true);
        edited.ApplyAllVariableReferencesBeforeFirstBuild();
        GraphicalUiElement first = HotReloadFormsProjects.Load(edited);
        HotReloadFormsProjects.Red(HotReloadReferenceChainTests.ReaderVisual(first, Plain.Reader))
            .ShouldBe(HotReloadReferenceChainTests.New, "sanity: first build");

        // The game changes the tail's stored value after the first build. The rows are still there, so a second
        // application would carry 200 to the reader; the first one already settled the chain.
        const int ChangedLater = 200;
        ComponentSave tail = edited.Components.Single(c => c.Name == "Chain3");
        tail.DefaultState!.Variables.Single(v => v.Name == "Swatch.Red").Value = ChangedLater;

        GraphicalUiElement second = HotReloadFormsProjects.Load(edited);

        HotReloadFormsProjects.Red(HotReloadReferenceChainTests.ReaderVisual(second, Plain.Reader))
            .ShouldBe(HotReloadReferenceChainTests.New);
    }

    [Fact]
    public void Load_BeforeAnyBuild_LeavesTheSavedValuesForGumExpressionsToBeInstalled()
    {
        // The game calls GumExpressionService.Initialize() after GumService.Initialize(), so the rows wait for
        // the first build, when the evaluator is there.
        GumProjectSave edited = HotReloadReferenceChainTests.Build(Plain, second: true);

        edited.ApplyAllVariableReferencesBeforeFirstBuild();

        ComponentSave button = edited.Components.Single(c => c.Name == "Controls/ButtonStandard");
        button.DefaultState!.Variables.Single(v => v.Name == "Background.Red").Value.ShouldBe(HotReloadReferenceChainTests.Old);
    }
}
