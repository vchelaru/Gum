using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Gum.Dialogs;
using Gum.Managers;
using Gum.ProjectServices;
using Gum.Services.Dialogs;
using Gum.ToolStates;
using GumFormsPlugin.Services;
using GumFormsPlugin.ViewModels;
using Moq;
using Shouldly;

namespace Gum.Presentation.Tests;

/// <summary>
/// The "Gum packages" group of the New Project dialog (#7356): Stable (default), Preview and Local Gum source, what each does to
/// the request, the command line and the notes, and that no note names a specific package.
/// </summary>
public class NewProjectPackageChannelDialogTests
{
    private static readonly string Folder = Path.Combine(Path.GetTempPath(), "GumNewProjectChannelDialogTests");

    private readonly Mock<INewProjectService> _service = new();
    private readonly Mock<IFormsFileService> _formsFileService = new();
    private readonly Mock<IProjectState> _projectState = new();

    public NewProjectPackageChannelDialogTests()
    {
        _formsFileService.Setup(x => x.DefaultThemeName).Returns("Standard");
        _formsFileService.Setup(x => x.GetAvailableThemes()).Returns(new List<string> { "Standard" });
        _service.Setup(x => x.IsSourceLinkedAvailable).Returns(true);
    }

    private NewProjectDialogViewModel Create(Action<NewProjectDialogViewModel>? configure = null)
    {
        NewProjectDialogViewModel viewModel = new NewProjectDialogViewModel(
            new ThemeSelectionViewModel(_formsFileService.Object, _projectState.Object),
            _service.Object, new Mock<IExistingCodeProjectDetector>().Object, new Mock<IDialogService>().Object,
            new Mock<INewProjectLocationSettings>().Object)
        {
            ProjectName = "MyGame",
            Location = Folder,
            IncludeCodeProject = true,
        };
        configure?.Invoke(viewModel);
        return viewModel;
    }

    #region Choice

    [Fact]
    public void Default_IsStable_WithNoWarning()
    {
        NewProjectDialogViewModel viewModel = Create();

        viewModel.PackageChannel.ShouldBe(NewProjectPackageChannel.Stable);
        viewModel.IsStableChannelChecked.ShouldBeTrue();
        viewModel.IsPreviewChannelChecked.ShouldBeFalse();
        viewModel.IsLocalSourceChannelChecked.ShouldBeFalse();
        viewModel.IsPreviewWarningVisible.ShouldBeFalse();
        viewModel.CommandLine.ShouldNotContain("--preview");
        viewModel.CommandLine.ShouldNotContain("--source-linked");
    }

    [Fact]
    public void PickingPreview_ChecksOnlyPreview_ShowsTheWarning_AndAddsThePreviewFlag()
    {
        NewProjectDialogViewModel viewModel = Create(vm => vm.IsPreviewChannelChecked = true);

        viewModel.PackageChannel.ShouldBe(NewProjectPackageChannel.Preview);
        viewModel.IsStableChannelChecked.ShouldBeFalse();
        viewModel.IsPreviewChannelChecked.ShouldBeTrue();
        viewModel.IsLocalSourceChannelChecked.ShouldBeFalse();
        viewModel.IsPreviewWarningVisible.ShouldBeTrue();
        viewModel.CommandLine.ShouldContain(" --preview");
        viewModel.CommandLine.ShouldNotContain("--source-linked");
    }

    [Fact]
    public void PickingLocalSource_ChecksOnlyLocalSource_HidesTheWarning_AndAddsTheSourceLinkedFlag()
    {
        NewProjectDialogViewModel viewModel = Create(vm => vm.IsLocalSourceChannelChecked = true);

        viewModel.PackageChannel.ShouldBe(NewProjectPackageChannel.LocalSource);
        viewModel.IsLocalSourceChannelChecked.ShouldBeTrue();
        viewModel.IsPreviewChannelChecked.ShouldBeFalse();
        viewModel.IsPreviewWarningVisible.ShouldBeFalse();
        viewModel.CommandLine.ShouldContain("--source-linked");
        viewModel.CommandLine.ShouldNotContain("--preview");
    }

    [Fact]
    public void UncheckingARadio_ChangesNothing_BecauseTheGroupChecksAnother()
    {
        NewProjectDialogViewModel viewModel = Create(vm => vm.IsPreviewChannelChecked = true);

        viewModel.IsPreviewChannelChecked = false;

        viewModel.PackageChannel.ShouldBe(NewProjectPackageChannel.Preview);
    }

    [Fact]
    public void WarningAndFlag_NeedACodeProject()
    {
        NewProjectDialogViewModel viewModel = Create(vm => vm.IsPreviewChannelChecked = true);

        viewModel.IncludeCodeProject = false;

        viewModel.IsPreviewWarningVisible.ShouldBeFalse();
        viewModel.CommandLine.ShouldNotContain("--preview");
    }

    [Fact]
    public void LocalSource_WhenTheToolWasNotBuiltFromSource_IsDisabled_AndFallsBackToStable()
    {
        _service.Setup(x => x.IsSourceLinkedAvailable).Returns(false);
        NewProjectDialogViewModel viewModel = Create(vm => vm.IsLocalSourceChannelChecked = true);

        viewModel.IsSourceLinkedEnabled.ShouldBeFalse();
        viewModel.PackageChannel.ShouldBe(NewProjectPackageChannel.Stable);
        viewModel.IsStableChannelChecked.ShouldBeTrue();
        viewModel.IsPreviewChannelEnabled.ShouldBeTrue();
        viewModel.CommandLine.ShouldNotContain("--source-linked");
    }

    [Fact]
    public void KniSkia_ForcesLocalSource_AndDisablesTheOtherTwo_ThenTheChoiceComesBack()
    {
        NewProjectDialogViewModel viewModel = Create(vm => vm.IsPreviewChannelChecked = true);

        viewModel.PlatformName = "kniskia";

        viewModel.PackageChannel.ShouldBe(NewProjectPackageChannel.LocalSource);
        viewModel.IsLocalSourceChannelChecked.ShouldBeTrue();
        viewModel.IsPreviewChannelChecked.ShouldBeFalse();
        viewModel.IsPreviewChannelEnabled.ShouldBeFalse();
        viewModel.IsStableChannelEnabled.ShouldBeFalse();
        viewModel.IsPreviewWarningVisible.ShouldBeFalse();

        viewModel.PlatformName = "monogame";

        viewModel.PackageChannel.ShouldBe(NewProjectPackageChannel.Preview);
        viewModel.IsPreviewChannelEnabled.ShouldBeTrue();
    }

    [Fact]
    public void ChoosingAChannel_RaisesTheRadiosAndTheWarning()
    {
        NewProjectDialogViewModel viewModel = Create();
        List<string?> raised = new List<string?>();
        viewModel.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        viewModel.IsPreviewChannelChecked = true;

        raised.ShouldContain(nameof(NewProjectDialogViewModel.IsStableChannelChecked));
        raised.ShouldContain(nameof(NewProjectDialogViewModel.IsPreviewChannelChecked));
        raised.ShouldContain(nameof(NewProjectDialogViewModel.IsPreviewWarningVisible));
    }

    #endregion

    #region Request and command line

    [Fact]
    public void Preview_CommandLine_AddsPreviewToNewAndPrereleaseToEveryAdd()
    {
        NewProjectDialogViewModel viewModel = Create(vm => vm.IsPreviewChannelChecked = true);

        viewModel.CommandLine.ShouldContain("--platform monogame --preview --no-restore");
        foreach (string package in RecommendedPackages.For(HostPlatform.MonoGame, NewProjectPackageChannel.Preview))
        {
            viewModel.CommandLine.ShouldContain($" package {package} --no-restore --prerelease");
        }
    }

    [Fact]
    public void Stable_CommandLine_AddsNoPrereleaseFlag()
    {
        NewProjectDialogViewModel viewModel = Create();

        viewModel.CommandLine.ShouldContain(" package ");
        viewModel.CommandLine.ShouldNotContain("--prerelease");
    }

    [Fact]
    public void PreviewAddsThePreviewPackages_AndLocalSourceLinksProjectsInstead()
    {
        string stable = Create().CommandLine;
        string preview = Create(vm => vm.IsPreviewChannelChecked = true).CommandLine;

        stable.ShouldNotContain("Gum.Shapes.MonoGame");
        preview.ShouldContain("Gum.Shapes.MonoGame");
    }

    #endregion

    #region Notes name no package

    private static IEnumerable<string> AllKnownPackageNames() =>
        NewProjectPlatforms.MainNames.Select(entry => entry.Platform).SelectMany(platform =>
                RecommendedPackages.For(platform)
                    .Concat(RecommendedPackages.For(platform, NewProjectPackageChannel.Preview))
                    .Concat(RecommendedPackages.ForSourceLinked(platform)))
            .Concat(new[] { "Gum.Shapes", "ShadowDusk", "KernSmith", "Gum.Expressions" })
            .Distinct();

    [Fact]
    public void TheAddOnNotes_NameNoSpecificPackage()
    {
        foreach (string note in new[]
                 {
                     NewProjectDialogText.RecommendedPackagesNote, NewProjectDialogText.RecommendedPackagesDisabledNote,
                     NewProjectDialogText.UseLocalSourceNote, NewProjectDialogText.PreviewWarning, NewProjectDialogText.PackageChannelTip,
                     NewProjectDialogText.CommandLineDescription, NewProjectDialogText.DemoHostCommandNote, NewProjectDialogText.ChainedCommandNote,
                 })
        {
            foreach (string package in AllKnownPackageNames())
            {
                note.ShouldNotContain(package, Case.Insensitive);
            }
        }
    }

    [Fact]
    public void TheRecommendedPackagesNote_IsTheSameForEveryChannelAndPlatform_AndNamesNoPackage()
    {
        HashSet<string> notes = new HashSet<string>();
        foreach (string platform in NewProjectPlatforms.MainNames.Select(entry => entry.Name).Where(name => name != "kniskia"))
        foreach (NewProjectPackageChannel channel in Enum.GetValues<NewProjectPackageChannel>())
        {
            NewProjectDialogViewModel viewModel = Create(vm =>
            {
                vm.PlatformName = platform;
                vm.PackageChannelChoice = channel;
            });

            notes.Add(viewModel.RecommendedPackagesNote);
            viewModel.CommandLineNote.ShouldNotBeNull();
            foreach (string package in AllKnownPackageNames())
            {
                viewModel.RecommendedPackagesNote.ShouldNotContain(package, Case.Insensitive, $"{platform} {channel}");
                viewModel.CommandLineNote.ShouldNotContain(package, Case.Insensitive, $"{platform} {channel}");
            }
        }

        notes.ShouldBe(new[] { NewProjectDialogText.RecommendedPackagesNote });
    }

    #endregion
}
