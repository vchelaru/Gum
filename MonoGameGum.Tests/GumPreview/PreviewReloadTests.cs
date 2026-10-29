#pragma warning disable CS0618
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Gum;
using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Managers;
using Gum.Plugins.InternalPlugins.EditorTab.Services;
using Gum.Wireframe;
using GumPreview;
using GumRuntime;
using MonoGameGum.Tests.Hot;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.GumPreview;

/// <summary>
/// Preview after a hot reload: the policy decides from the manager's real result, the presenter
/// rebuilds, and the tree has to end up equal to what a fresh launch of the saved files would show.
/// </summary>
public class PreviewReloadTests : BaseTestClass
{
    private static readonly SweepSpec Spec = new SweepSpec();

    private readonly SweepWorld _world = new SweepWorld();
    private readonly PreviewReloadPolicy _policy = new PreviewReloadPolicy();
    private PreviewSelectionMessage _selection = new PreviewSelectionMessage("MainScreen");
    private int _rebuilds;
    private bool _isElementMissing;

    public override void Dispose()
    {
        _world.Dispose();
        ElementSaveExtensions.Reset();
        ObjectFinder.Self.GumProjectSave = null;
        base.Dispose();
    }

    private void StartPreview(SweepSpec spec)
    {
        _world.Boot(spec);
        _world.Manager.ReloadFinished += result => _policy.OnReloadFinished(result, _isElementMissing);
        ShowSelection();
        _isElementMissing.ShouldBeFalse();
    }

    private void ShowSelection() => _isElementMissing = !PreviewElementPresenter.Show(_selection);

    // One game frame: the rebuild a result asked for runs first, then the manager's Update.
    private void Frame()
    {
        if (_policy.TryTakePendingReload())
        {
            _rebuilds++;
            ShowSelection();
        }
        _world.Manager.Update(GumService.Default.Root.Children.ToList());
    }

    private void Save(params string[] changedPaths)
    {
        foreach (string path in changedPaths)
        {
            _world.Notify(path);
        }
        _world.Advance(300);
        Frame();
        Frame();
    }

    private void RenameMainScreen(string newName)
    {
        GumProjectSave project = GumProjectSave.Load(_world.Gumx);
        project.Screens.Single().Name = newName;
        project.ScreenReferences.Single().Name = newName;
        project.Save(_world.Gumx, saveElements: true);
    }

    private void DeleteMainScreen()
    {
        GumProjectSave project = GumProjectSave.Load(_world.Gumx);
        project.ScreenReferences.Clear();
        project.Save(_world.Gumx, saveElements: false);
    }

    private static string Describe()
    {
        StringBuilder text = new StringBuilder();
        foreach (GraphicalUiElement root in GumService.Default.Root.Children)
        {
            Describe(root, 0, text);
        }
        return text.ToString();
    }

    private static void Describe(GraphicalUiElement element, int depth, StringBuilder text)
    {
        text.Append(new string(' ', depth * 2)).Append(element.Name).Append(" <").Append(element.ElementSave?.Name).Append("> x=")
            .Append(element.X);
        if (element is ColoredRectangleRuntime rectangle)
        {
            text.Append(" red=").Append(rectangle.Red);
        }
        text.AppendLine();
        foreach (GraphicalUiElement child in element.ContainedElements.Concat(element.Children).Distinct()
                     .Where(child => child.Tag is InstanceSave))
        {
            Describe(child, depth + 1, text);
        }
    }

    // What a launch now, from the files on disk, would show for the selection.
    private string DescribeAFreshLaunch()
    {
        GumProjectSave fresh = GumProjectSave.Load(_world.Gumx, out GumLoadResult _)!;
        fresh.Initialize();
        ObjectFinder.Self.GumProjectSave = fresh;
        PreviewElementPresenter.Show(_selection);
        return Describe();
    }

    [Fact]
    public void RenamingTheShownScreen_LeavesPreviewAsAFreshLaunchWould()
    {
        StartPreview(Spec);
        Describe().ShouldContain("MainScreen");

        RenameMainScreen("HomeScreen");
        Save(_world.Gumx);

        _rebuilds.ShouldBe(1);
        string afterReload = Describe();
        afterReload.ShouldBeEmpty();
        afterReload.ShouldBe(DescribeAFreshLaunch());
    }

    [Fact]
    public void DeletingTheShownScreen_LeavesPreviewAsAFreshLaunchWould()
    {
        StartPreview(Spec);

        DeleteMainScreen();
        Save(_world.Gumx);

        _rebuilds.ShouldBe(1);
        string afterReload = Describe();
        afterReload.ShouldBeEmpty();
        afterReload.ShouldBe(DescribeAFreshLaunch());
    }

    [Fact]
    public void AfterTheShownScreenWasRenamed_SelectingTheNewNameShowsIt()
    {
        StartPreview(Spec);
        RenameMainScreen("HomeScreen");
        Save(_world.Gumx);

        _selection = new PreviewSelectionMessage("HomeScreen");
        ShowSelection();
        _isElementMissing.ShouldBeFalse();

        Describe().ShouldContain("<HomeScreen>");
        Describe().ShouldBe(DescribeAFreshLaunch());
    }

    [Fact]
    public void ACleanEdit_KeepsTheTreeAndDoesNotRebuild()
    {
        StartPreview(Spec);
        GraphicalUiElement shown = GumService.Default.Root.Children.Single();

        _world.SaveAll(Spec with { BoxX = 9 });
        Save(_world.Gumx, _world.ScreenFile);

        _rebuilds.ShouldBe(0);
        GumService.Default.Root.Children.Single().ShouldBeSameAs(shown);
        Describe().ShouldBe(DescribeAFreshLaunch());
    }

    [Fact]
    public void DeletingAComponentTheScreenUses_MatchesARestart_SoPreviewDoesNotRebuild()
    {
        StartPreview(Spec);
        GraphicalUiElement shown = GumService.Default.Root.Children.Single();

        GumProjectSave project = GumProjectSave.Load(_world.Gumx);
        project.ComponentReferences.RemoveAll(reference => reference.Name.EndsWith("Widget"));
        project.Save(_world.Gumx, saveElements: false);
        Save(_world.Gumx);

        _rebuilds.ShouldBe(0);
        GumService.Default.Root.Children.Single().ShouldBeSameAs(shown);
        Describe().ShouldBe(DescribeAFreshLaunch());
    }

    [Fact]
    public void AMissingFileThatStaysMissing_RebuildsOncePerProblem_NotOnEverySave()
    {
        StartPreview(Spec with { WithGadget = true });
        File.Delete(_world.GadgetFile);

        Save(_world.Gumx);
        _rebuilds.ShouldBe(1);
        string afterFirst = Describe();

        _world.SaveAll(Spec with { WithGadget = true, BoxX = 12 });
        File.Delete(_world.GadgetFile);
        Save(_world.Gumx, _world.ScreenFile);
        Save(_world.Gumx, _world.ScreenFile);

        _rebuilds.ShouldBe(1);
        Describe().ShouldBe(DescribeAFreshLaunch());
        afterFirst.ShouldNotBeEmpty();
    }

    [Fact]
    public void SelectingANameBeforeTheSaveAddsIt_ShowsItOnceTheSaveLands()
    {
        StartPreview(Spec);
        _selection = new PreviewSelectionMessage("HomeScreen");
        ShowSelection();
        _isElementMissing.ShouldBeTrue();
        Describe().ShouldBeEmpty();

        RenameMainScreen("HomeScreen");
        Save(_world.Gumx);

        _rebuilds.ShouldBe(1);
        _isElementMissing.ShouldBeFalse();
        Describe().ShouldContain("<HomeScreen>");
        Describe().ShouldBe(DescribeAFreshLaunch());
    }

    [Fact]
    public void AnElementThatStaysMissing_RebuildsOncePerReload_NotWithoutOne()
    {
        StartPreview(Spec);
        _selection = new PreviewSelectionMessage("HomeScreen");
        ShowSelection();

        _world.SaveAll(Spec with { BoxX = 9 });
        Save(_world.Gumx, _world.ScreenFile);
        _rebuilds.ShouldBe(1);
        Frame();
        Frame();

        _rebuilds.ShouldBe(1);
        _isElementMissing.ShouldBeTrue();
        Describe().ShouldBeEmpty();
    }
}
