using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.ProjectServices;
using Shouldly;

namespace Gum.ProjectServices.Tests;

public class DiffStandardsServiceTests : IDisposable
{
    private readonly GumProjectSave _reference;

    public DiffStandardsServiceTests()
    {
        // Initialize the StandardElementsManager singleton the same way ProjectLoader
        // would, then build a fresh reference project from its programmatic defaults.
        StandardElementsManager.Self.Initialize();
        StandardElementsManager.Self.RegisterExtendedDefaultStates();

        _reference = new GumProjectSave();
        StandardElementsManager.Self.PopulateProjectWithDefaultStandards(_reference);
    }

    [Fact]
    public void Diff_ProjectThatMirrorsTheReference_ShouldReportNoDrift()
    {
        // A project built the same way as the reference (the tool's File → New flow)
        // must by definition produce no drift.
        GumProjectSave project = new GumProjectSave();
        StandardElementsManager.Self.PopulateProjectWithDefaultStandards(project);

        DiffStandardsResult result = DiffStandardsService.DiffProjects(project, _reference);

        result.HasDrift.ShouldBeFalse();
        result.Differences.ShouldBeEmpty();
        result.MissingFromProject.ShouldBeEmpty();
        result.ProjectOnlyStandards.ShouldBeEmpty();
    }

    [Fact]
    public void Diff_ChangedVariableValue_ShouldReportChangedDiff()
    {
        GumProjectSave project = new GumProjectSave();
        StandardElementsManager.Self.PopulateProjectWithDefaultStandards(project);

        StandardElementSave text = project.StandardElements.Single(s => s.Name == "Text");
        VariableSave font = text.DefaultState.Variables.Single(v => v.Name == "Font");
        object? originalFont = font.Value;
        font.Value = "ComicSans";

        DiffStandardsResult result = DiffStandardsService.DiffProjects(project, _reference);

        result.HasDrift.ShouldBeTrue();
        StandardVariableDiff diff = result.Differences.ShouldHaveSingleItem();
        diff.StandardName.ShouldBe("Text");
        diff.VariableName.ShouldBe("Font");
        diff.Kind.ShouldBe(StandardVariableDiffKind.Changed);
        diff.DefaultValue.ShouldBe(originalFont?.ToString());
        diff.ProjectValue.ShouldBe("ComicSans");
    }

    [Fact]
    public void Diff_VariableAddedInProject_ShouldReportAddedDiff()
    {
        GumProjectSave project = new GumProjectSave();
        StandardElementsManager.Self.PopulateProjectWithDefaultStandards(project);

        StandardElementSave text = project.StandardElements.Single(s => s.Name == "Text");
        text.DefaultState.Variables.Add(new VariableSave
        {
            Name = "NotInDefault",
            Type = "string",
            Value = "x",
            SetsValue = true
        });

        DiffStandardsResult result = DiffStandardsService.DiffProjects(project, _reference);

        StandardVariableDiff diff = result.Differences.ShouldHaveSingleItem();
        diff.Kind.ShouldBe(StandardVariableDiffKind.AddedInProject);
        diff.VariableName.ShouldBe("NotInDefault");
        diff.DefaultValue.ShouldBe("(absent)");
        diff.ProjectValue.ShouldBe("x");
    }

    [Fact]
    public void Diff_VariableRemovedFromProject_ShouldReportRemovedDiff()
    {
        GumProjectSave project = new GumProjectSave();
        StandardElementsManager.Self.PopulateProjectWithDefaultStandards(project);

        StandardElementSave text = project.StandardElements.Single(s => s.Name == "Text");
        VariableSave removed = text.DefaultState.Variables.Single(v => v.Name == "Font");
        text.DefaultState.Variables.Remove(removed);

        DiffStandardsResult result = DiffStandardsService.DiffProjects(project, _reference);

        StandardVariableDiff diff = result.Differences.ShouldHaveSingleItem();
        diff.Kind.ShouldBe(StandardVariableDiffKind.RemovedFromProject);
        diff.VariableName.ShouldBe("Font");
        diff.ProjectValue.ShouldBe("(absent)");
        diff.DefaultValue.ShouldBe(removed.Value?.ToString());
    }

    [Fact]
    public void Diff_StandardMissingFromProject_ShouldReportMissing()
    {
        GumProjectSave project = new GumProjectSave();
        StandardElementsManager.Self.PopulateProjectWithDefaultStandards(project);
        project.StandardElements.RemoveAll(s => s.Name == "Text");

        DiffStandardsResult result = DiffStandardsService.DiffProjects(project, _reference);

        result.MissingFromProject.ShouldBe(new[] { "Text" });
        result.HasDrift.ShouldBeTrue();
    }

    [Fact]
    public void Diff_ProjectOnlyStandard_ShouldBeListedButNotDiffed()
    {
        GumProjectSave project = new GumProjectSave();
        StandardElementsManager.Self.PopulateProjectWithDefaultStandards(project);

        StandardElementSave extra = new StandardElementSave { Name = "RoundedRectangle" };
        StateSave state = new StateSave { Name = "Default" };
        state.ParentContainer = extra;
        state.Variables.Add(new VariableSave { Name = "CornerRadius", Type = "float", Value = 5f });
        extra.States.Add(state);
        project.StandardElements.Add(extra);

        DiffStandardsResult result = DiffStandardsService.DiffProjects(project, _reference);

        result.ProjectOnlyStandards.ShouldBe(new[] { "RoundedRectangle" });
        result.Differences.ShouldBeEmpty();
        result.HasDrift.ShouldBeFalse();
    }

    [Fact]
    public void Diff_NewCategoryInProject_ShouldReportItsStateVariablesAsAdded()
    {
        // A theme that adds a state category like "Forms" to a Standard has effectively
        // polluted the universal base. Every variable in those category states should
        // surface as AddedInProject drift.
        GumProjectSave project = new GumProjectSave();
        StandardElementsManager.Self.PopulateProjectWithDefaultStandards(project);

        StandardElementSave text = project.StandardElements.Single(s => s.Name == "Text");
        StateSaveCategory category = new StateSaveCategory { Name = "Forms" };
        StateSave selected = new StateSave { Name = "Selected" };
        selected.ParentContainer = text;
        selected.Variables.Add(new VariableSave
        {
            Name = "Color",
            Type = "string",
            Value = "Blue",
            SetsValue = true
        });
        category.States.Add(selected);
        text.Categories.Add(category);

        DiffStandardsResult result = DiffStandardsService.DiffProjects(project, _reference);

        result.HasDrift.ShouldBeTrue();
        StandardVariableDiff diff = result.Differences.ShouldHaveSingleItem();
        diff.StateName.ShouldBe("Selected");
        diff.Kind.ShouldBe(StandardVariableDiffKind.AddedInProject);
    }

    public void Dispose()
    {
        ObjectFinder.Self.GumProjectSave = null;
    }
}
