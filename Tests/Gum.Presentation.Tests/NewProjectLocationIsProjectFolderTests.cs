using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
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
/// #7281: a Location whose last folder is the project's own name is the project folder (no nested <c>Name</c> folder), and
/// the remembered default is its parent. Everything here runs the real view model, the real service and the real detector on
/// a temp folder, so the displayed path, the checks and the files on disk are compared against each other.
/// </summary>
public class NewProjectLocationIsProjectFolderTests : IDisposable
{
    private const string Name = "MyProject1";

    private readonly string _root = Path.Combine(Path.GetTempPath(), "Gum7281_" + Guid.NewGuid().ToString("N"));
    private readonly string _parent;
    private readonly Mock<INewProjectLocationSettings> _settings = new();
    private readonly Mock<IDialogService> _dialogService = new();

    public NewProjectLocationIsProjectFolderTests()
    {
        _parent = Path.Combine(_root, "GumProjects");
        Directory.CreateDirectory(_parent);
    }

    public void Dispose()
    {
        try { Directory.Delete(_root, recursive: true); } catch (IOException) { }
    }

    private string ProjectFolder => Path.Combine(_parent, Name);

    private NewProjectDialogViewModel Create(string location, bool code = false, bool chained = false, string name = Name)
    {
        Mock<IFormsFileService> forms = new();
        forms.Setup(x => x.DefaultThemeName).Returns("Standard");
        forms.Setup(x => x.GetAvailableThemes()).Returns(new List<string> { "Standard" });
        Mock<IFormsThemeLocator> themes = new();
        themes.Setup(x => x.GetAvailableThemes()).Returns(Array.Empty<string>());
        NewProjectService service = new NewProjectService(themes.Object, new Mock<IRestoreRunner>().Object, Array.Empty<string>());
        return new NewProjectDialogViewModel(
            new ThemeSelectionViewModel(forms.Object, new Mock<IProjectState>().Object),
            service, new ExistingCodeProjectDetector(), _dialogService.Object, _settings.Object)
        {
            ProjectName = name,
            Location = location,
            IncludeCodeProject = code,
            PlatformName = "monogame",
            SourceLinkedChoice = false,
            RunRestore = false,
            IncludeRecommendedPackages = chained,
            GenerateCodeChoice = false,
            IsIncludeDemoScreenGum = false,
        };
    }

    // Normalizes the separators the way the dialog displays them.
    private static string Shown(string path) => NewProjectDisplayPath.Normalize(path);

    [Fact]
    public void GumOnly_LocationNamedLikeTheProject_IsTheProjectFolder_AndItsParentIsRemembered()
    {
        NewProjectDialogViewModel viewModel = Create(ProjectFolder);

        viewModel.GumProjectPath.ShouldBe(Shown(Path.Combine(ProjectFolder, Name + ".gumj")));
        viewModel.CommandLine.ShouldBe($"gumcli new \"{Shown(Path.Combine(ProjectFolder, Name + ".gumj"))}\"");
        viewModel.ProjectParentFolder.ShouldBe(Shown(_parent));
    }

    [Fact]
    public void GumOnly_LocationNotNamedLikeTheProject_IsUnchanged()
    {
        NewProjectDialogViewModel viewModel = Create(_parent);

        viewModel.GumProjectPath.ShouldBe(Shown(Path.Combine(_parent, Name, Name + ".gumj")));
        viewModel.ProjectParentFolder.ShouldBe(Shown(_parent));
    }

    [Fact]
    public void GumOnly_ALocationWhoseNameOnlyContainsTheProjectName_IsNotAMatch()
    {
        string location = Path.Combine(_parent, Name + "Old");

        NewProjectDialogViewModel viewModel = Create(location);

        viewModel.GumProjectPath.ShouldBe(Shown(Path.Combine(location, Name, Name + ".gumj")));
        viewModel.ProjectParentFolder.ShouldBe(Shown(location));
    }

    [Fact]
    public void GumOnly_ATrailingSeparatorOnLocation_StillMatches()
    {
        NewProjectDialogViewModel viewModel = Create(ProjectFolder + Path.DirectorySeparatorChar);

        viewModel.GumProjectPath.ShouldBe(Shown(Path.Combine(ProjectFolder, Name + ".gumj")));
        viewModel.ProjectParentFolder.ShouldBe(Shown(_parent));
    }

    [Fact]
    public void GumOnly_ADifferentCase_MatchesOnWindowsAndMacButNotOnLinux()
    {
        string location = Path.Combine(_parent, Name.ToUpperInvariant());

        NewProjectDialogViewModel viewModel = Create(location);

        if (OperatingSystem.IsLinux())
        {
            viewModel.GumProjectPath.ShouldBe(Shown(Path.Combine(location, Name, Name + ".gumj")));
            viewModel.ProjectParentFolder.ShouldBe(Shown(location));
        }
        else
        {
            viewModel.GumProjectPath.ShouldBe(Shown(Path.Combine(location, Name + ".gumj")));
            viewModel.ProjectParentFolder.ShouldBe(Shown(_parent));
        }
    }

    [Fact]
    public void CodeProject_LocationNamedLikeTheProject_IsTheGameFolder()
    {
        NewProjectDialogViewModel viewModel = Create(ProjectFolder, code: true);

        viewModel.CommandLine.ShouldBe($"gumcli new \"{Shown(ProjectFolder)}\" --platform monogame --no-restore");
        viewModel.GumProjectPath.ShouldBe(Shown(Path.Combine(ProjectFolder, "Content", "GumProject", "GumProject.gumj")));
        viewModel.ProjectParentFolder.ShouldBe(Shown(_parent));
    }

    [Fact]
    public void CodeProject_TheChainedCommandNamesTheCsprojInTheProjectFolder()
    {
        NewProjectDialogViewModel viewModel = Create(ProjectFolder, code: true, chained: true);
        viewModel.RunRestore = true;

        viewModel.CommandLine.ShouldContain($"dotnet add \"{Shown(Path.Combine(ProjectFolder, Name + ".csproj"))}\" package");
        viewModel.CommandLine.ShouldContain($"dotnet restore \"{Shown(Path.Combine(ProjectFolder, Name + ".csproj"))}\"");
        viewModel.CommandLine.ShouldNotContain(Path.Combine(ProjectFolder, Name) + Path.DirectorySeparatorChar);
    }

    [Fact]
    public void CodeProject_TheCodegenStepNamesTheGumProjectInTheProjectFolder()
    {
        NewProjectDialogViewModel viewModel = Create(ProjectFolder, code: true);
        viewModel.RunRestore = true;
        viewModel.GenerateCodeChoice = true;

        viewModel.CommandLine.ShouldContain(
            $"gumcli codegen \"{Shown(Path.Combine(ProjectFolder, "Content", "GumProject", "GumProject.gumj"))}\"");
        viewModel.CommandLine.ShouldNotContain(Path.Combine(ProjectFolder, Name) + Path.DirectorySeparatorChar);
    }

    [Fact]
    public void FoldersTree_RootsAtTheProjectFolder_WhetherOrNotLocationIsIt()
    {
        Create(ProjectFolder).FolderTree.Single().Name.ShouldBe(NewProjectDisplayPath.AsFolder(ProjectFolder));
        Create(ProjectFolder, code: true).FolderTree.Single().Name.ShouldBe(NewProjectDisplayPath.AsFolder(ProjectFolder));
        Create(_parent).FolderTree.Single().Name.ShouldBe(NewProjectDisplayPath.AsFolder(ProjectFolder));
    }

    [Fact]
    public async Task Refusal_LooksAtTheProjectInLocation()
    {
        Directory.CreateDirectory(ProjectFolder);
        File.WriteAllText(Path.Combine(ProjectFolder, Name + ".gumj"), "{}");
        NewProjectDialogViewModel viewModel = Create(ProjectFolder);

        await viewModel.ChecksRefresh;

        viewModel.IsRefused.ShouldBeTrue();
        viewModel.RefusalText.ShouldContain(Shown(Path.Combine(ProjectFolder, Name + ".gumj")));
    }

    [Fact]
    public async Task NotEmptyBanner_LooksAtLocationItself()
    {
        Directory.CreateDirectory(ProjectFolder);
        File.WriteAllText(Path.Combine(ProjectFolder, "notes.txt"), "x");
        NewProjectDialogViewModel viewModel = Create(ProjectFolder);

        await viewModel.ChecksRefresh;

        viewModel.IsTargetNotEmptyWarningVisible.ShouldBeTrue();
        viewModel.TargetNotEmptyText.ShouldContain(Shown(ProjectFolder));
        viewModel.TargetNotEmptyText.ShouldNotContain(Shown(Path.Combine(ProjectFolder, Name)));
    }

    [Fact]
    public async Task NotEmptyBanner_IsHidden_ForAnEmptyLocationFolder()
    {
        Directory.CreateDirectory(ProjectFolder);
        NewProjectDialogViewModel viewModel = Create(ProjectFolder);

        await viewModel.ChecksRefresh;

        viewModel.IsTargetNotEmptyWarningVisible.ShouldBeFalse();
        viewModel.IsRefused.ShouldBeFalse();
    }

    [Fact]
    public async Task ExistingCodeProjectBanner_ReadsFromTheFolderAboveTheProjectFolder()
    {
        File.WriteAllText(Path.Combine(_parent, "Other.csproj"), "<Project />");
        NewProjectDialogViewModel viewModel = Create(ProjectFolder, code: true);

        await viewModel.ChecksRefresh;

        viewModel.IsExistingCodeProjectWarningVisible.ShouldBeTrue();
        viewModel.ExistingCodeProjectText.ShouldBe($"Found Other.csproj in {Shown(_parent)}.");
        viewModel.ExistingCodeProjectTip.ShouldContain(Shown(ProjectFolder));
    }

    [Fact]
    public async Task Creation_CodeProject_WritesTheProjectInLocationItself()
    {
        Directory.CreateDirectory(ProjectFolder);
        NewProjectDialogViewModel viewModel = Create(ProjectFolder, code: true);

        await viewModel.RunCreationAsync();

        viewModel.ErrorMessage.ShouldBe(string.Empty);
        File.Exists(Path.Combine(ProjectFolder, Name + ".csproj")).ShouldBeTrue();
        File.Exists(Path.Combine(ProjectFolder, "Content", "GumProject", "GumProject.gumj")).ShouldBeTrue();
        Directory.Exists(Path.Combine(ProjectFolder, Name)).ShouldBeFalse();
        viewModel.CreatedGumProjectPath.ShouldBe(Path.Combine(ProjectFolder, "Content", "GumProject", "GumProject.gumj"));
    }

    [Fact]
    public void Browse_StartsInTheParent_WhenLocationIsTheProjectFolder()
    {
        Directory.CreateDirectory(ProjectFolder);
        OpenFolderDialogOptions? captured = null;
        _dialogService.Setup(x => x.OpenFolder(It.IsAny<OpenFolderDialogOptions?>()))
            .Callback<OpenFolderDialogOptions?>(options => captured = options)
            .Returns((string?)null);
        NewProjectDialogViewModel viewModel = Create(ProjectFolder);

        viewModel.BrowseCommand.Execute(null);

        captured!.InitialDirectory.ShouldBe(Shown(_parent));
    }

    [Fact]
    public void Browse_StartsInLocation_WhenItIsNotTheProjectFolder()
    {
        OpenFolderDialogOptions? captured = null;
        _dialogService.Setup(x => x.OpenFolder(It.IsAny<OpenFolderDialogOptions?>()))
            .Callback<OpenFolderDialogOptions?>(options => captured = options)
            .Returns((string?)null);
        NewProjectDialogViewModel viewModel = Create(_parent);

        viewModel.BrowseCommand.Execute(null);

        captured!.InitialDirectory.ShouldBe(_parent);
    }

    [Fact]
    public void ProjectParentFolder_ForAUncShareRootNamedLikeTheProject_IsTheShareItself()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        string share = new string(Path.DirectorySeparatorChar, 2) + "server" + Path.DirectorySeparatorChar + Name;

        NewProjectDialogViewModel viewModel = Create(share);

        viewModel.ProjectParentFolder.ShouldBe(share);
        viewModel.GumProjectPath.ShouldBe(Path.Combine(share, Name, Name + ".gumj"));
    }
}
