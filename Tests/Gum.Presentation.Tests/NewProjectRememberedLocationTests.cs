using System;
using System.IO;
using Gum.Dialogs;
using Gum.Managers;
using Gum.ProjectServices;
using Gum.ToolStates;
using Gum.Services.Dialogs;
using GumFormsPlugin.Services;
using GumFormsPlugin.ViewModels;
using Moq;
using Shouldly;

namespace Gum.Presentation.Tests;

/// <summary>
/// The New Project dialog starts in the parent of the last created project's folder (#7256), and
/// Browse opens there too.
/// </summary>
public class NewProjectRememberedLocationTests : IDisposable
{
    private static readonly string Documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

    private readonly string _root = Path.Combine(Path.GetTempPath(), "Gum7256_" + Guid.NewGuid().ToString("N"));
    private readonly Mock<INewProjectLocationSettings> _settings = new();
    private readonly Mock<IDialogService> _dialogService = new();

    public NewProjectRememberedLocationTests()
    {
        Directory.CreateDirectory(_root);
    }

    public void Dispose()
    {
        try { Directory.Delete(_root, recursive: true); } catch (IOException) { }
    }

    private NewProjectDialogViewModel Create()
    {
        Mock<IFormsFileService> forms = new();
        forms.Setup(x => x.DefaultThemeName).Returns("Standard");
        forms.Setup(x => x.GetAvailableThemes()).Returns(new System.Collections.Generic.List<string> { "Standard" });
        Mock<INewProjectService> service = new();
        return new NewProjectDialogViewModel(
            new ThemeSelectionViewModel(forms.Object, new Mock<IProjectState>().Object),
            service.Object, new Mock<IExistingCodeProjectDetector>().Object, _dialogService.Object, _settings.Object);
    }

    [Fact]
    public void Location_DefaultsToDocuments_WhenNothingWasRemembered()
    {
        Create().Location.ShouldBe(Documents);
    }

    [Fact]
    public void Location_DefaultsToTheRememberedFolder()
    {
        _settings.Setup(x => x.LastNewProjectParent).Returns(_root);

        NewProjectDialogViewModel viewModel = Create();

        viewModel.Location.ShouldBe(_root);
        viewModel.ProjectParentFolder.ShouldBe(_root);
    }

    [Fact]
    public void Location_FallsBackToDocuments_WhenTheRememberedFolderIsGone()
    {
        _settings.Setup(x => x.LastNewProjectParent).Returns(Path.Combine(_root, "deleted"));

        Create().Location.ShouldBe(Documents);
    }

    [Fact]
    public void Browse_StartsInTheRememberedFolder_WhenLocationIsNotAFolder()
    {
        _settings.Setup(x => x.LastNewProjectParent).Returns(_root);
        OpenFolderDialogOptions? captured = null;
        _dialogService.Setup(x => x.OpenFolder(It.IsAny<OpenFolderDialogOptions?>()))
            .Callback<OpenFolderDialogOptions?>(options => captured = options)
            .Returns((string?)null);
        NewProjectDialogViewModel viewModel = Create();
        viewModel.Location = Path.Combine(_root, "typed", "missing");

        viewModel.BrowseCommand.Execute(null);

        captured!.InitialDirectory.ShouldBe(_root);
    }

    [Fact]
    public void Browse_StartsInTheRememberedFolder_ByDefault()
    {
        _settings.Setup(x => x.LastNewProjectParent).Returns(_root);
        OpenFolderDialogOptions? captured = null;
        _dialogService.Setup(x => x.OpenFolder(It.IsAny<OpenFolderDialogOptions?>()))
            .Callback<OpenFolderDialogOptions?>(options => captured = options)
            .Returns((string?)null);

        Create().BrowseCommand.Execute(null);

        captured!.InitialDirectory.ShouldBe(_root);
    }
}
