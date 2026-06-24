using System.Collections.Generic;
using Gum.Plugins.FileWatchPlugin;
using Shouldly;

namespace Gum.Presentation.Tests;

/// <summary>
/// Characterization (pinning) tests for the first ViewModel relocated into the headless
/// Gum.Presentation assembly (ADR-0005). The point is not the FileWatch feature itself but
/// the boundary: this test project references ONLY Gum.Presentation (no WPF/WinForms), so a
/// green run proves a relocated tool ViewModel — and the GumCommon ViewModel base it relies
/// on for Get/Set/NotifyPropertyChanged — constructs and behaves with no UI framework present.
/// </summary>
public class FileWatchViewModelTests
{
    [Fact]
    public void PrintFileChangesToOutput_RaisesPropertyChanged_WhenSet()
    {
        // This bool is the property MainFileWatchPlugin keys off of, so its change
        // notification carries real behavioral significance worth pinning.
        FileWatchViewModel viewModel = new();
        List<string?> changedProperties = new();
        viewModel.PropertyChanged += (_, e) => changedProperties.Add(e.PropertyName);

        viewModel.PrintFileChangesToOutput = true;

        viewModel.PrintFileChangesToOutput.ShouldBeTrue();
        changedProperties.ShouldContain(nameof(FileWatchViewModel.PrintFileChangesToOutput));
    }

    [Fact]
    public void WatchFolderInformation_RoundTripsValueAndRaisesPropertyChanged_WhenSet()
    {
        FileWatchViewModel viewModel = new();
        const string expectedFolder = @"Watching C:\Project\Gum";
        List<string?> changedProperties = new();
        viewModel.PropertyChanged += (_, e) => changedProperties.Add(e.PropertyName);

        viewModel.WatchFolderInformation = expectedFolder;

        viewModel.WatchFolderInformation.ShouldBe(expectedFolder);
        changedProperties.ShouldContain(nameof(FileWatchViewModel.WatchFolderInformation));
    }
}
