using System.Collections.Generic;
using Gum.DataTypes;
using Gum.Plugins.PropertiesWindowPlugin;
using Shouldly;

namespace Gum.Presentation.Tests;

/// <summary>
/// Characterization (pinning) tests for ProjectPropertiesViewModel, relocated out of Gum.csproj
/// into the headless Gum.Presentation assembly (ADR-0005, #3754). SetFrom/ApplyToModelObjects
/// were narrowed from taking a whole GeneralSettingsFile (tool-assembly-coupled) to just the one
/// bool field the VM actually used.
/// </summary>
public class ProjectPropertiesViewModelTests
{
    [Fact]
    public void ApplyToModelObjects_WritesBackToGumProjectSave()
    {
        ProjectPropertiesViewModel viewModel = new();
        GumProjectSave gumProject = new();
        viewModel.SetFrom(autoSave: false, gumProject);

        viewModel.ShowCanvasOutline = true;

        viewModel.ApplyToModelObjects();

        gumProject.ShowCanvasOutline.ShouldBeTrue();
    }

    [Fact]
    public void LocalizationFiles_ShouldReturnSameListInstance_WhenReadRepeatedly()
    {
        // Regression: getter used to `?? new List<string>()` which returned a fresh,
        // orphaned list on every read when no value had been set yet. Callers that
        // mutated the returned list silently lost their changes.
        ProjectPropertiesViewModel viewModel = new();

        List<string> first = viewModel.LocalizationFiles;
        first.Add("foo.resx");
        List<string> second = viewModel.LocalizationFiles;

        second.ShouldBeSameAs(first);
        second.Count.ShouldBe(1);
    }

    [Fact]
    public void SetFrom_AssignsAutoSaveFromParameter()
    {
        ProjectPropertiesViewModel viewModel = new();
        GumProjectSave gumProject = new();

        viewModel.SetFrom(autoSave: true, gumProject);

        viewModel.AutoSave.ShouldBeTrue();
    }
}
