using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
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
/// #7285: the line under Location that says where the project folder goes and whether it is made or reused, and the same
/// answer in the collapsed Folders header. The texts are literals, not the production constants.
/// </summary>
public class NewProjectFolderNoticeTests
{
    private static readonly string Parent = Path.Combine(Path.GetTempPath(), "GumFolderNotice");
    private static readonly string Sep = Path.DirectorySeparatorChar.ToString();

    private readonly Mock<INewProjectService> _service = new();
    private readonly Mock<IExistingCodeProjectDetector> _detector = new();

    public NewProjectFolderNoticeTests()
    {
        _service.Setup(x => x.IsSourceLinkedAvailable).Returns(true);
        _service.Setup(x => x.Validate(It.IsAny<NewProjectRequest>())).Returns(new NewProjectValidation
        {
            Resolved = new ResolvedNewProject { Name = "CodeProj3", CsprojPath = "g.csproj" },
        });
    }

    private NewProjectDialogViewModel Create(string location, string name = "CodeProj3", bool code = false)
    {
        Mock<IFormsFileService> forms = new();
        forms.Setup(x => x.DefaultThemeName).Returns("Standard");
        forms.Setup(x => x.GetAvailableThemes()).Returns(new List<string> { "Standard" });
        forms.Setup(x => x.GetThemeDirectory(It.IsAny<string>())).Returns("C:/nonexistent-theme/");
        return new NewProjectDialogViewModel(
            new ThemeSelectionViewModel(forms.Object, new Mock<IProjectState>().Object),
            _service.Object, _detector.Object, new Mock<IDialogService>().Object, new Mock<INewProjectLocationSettings>().Object)
        {
            ProjectName = name,
            Location = location,
            IncludeCodeProject = code,
            IncludeRecommendedPackages = false,
            IsIncludeDemoScreenGum = false,
            GenerateCodeChoice = false,
        };
    }

    private void FolderExists(string folder, string name, bool empty = false)
    {
        _detector.Setup(x => x.TargetExists(folder, name)).Returns(true);
        _detector.Setup(x => x.TargetExistsAndIsNotEmpty(folder, name)).Returns(!empty);
    }

    [Fact]
    public async Task ParentLocation_NewFolder_SaysItCreatesTheFullPath()
    {
        NewProjectDialogViewModel viewModel = Create(Parent);

        await viewModel.ChecksRefresh;

        viewModel.IsFolderNoticeVisible.ShouldBeTrue();
        viewModel.FolderNoticeHeading.ShouldBe("Creates a new folder");
        viewModel.FolderNoticePath.ShouldBe(Parent + Sep + "CodeProj3" + Sep);
        viewModel.FolderNoticeSuffix.ShouldBe("");
        viewModel.FoldersSummary.ShouldBe("Creates CodeProj3" + Sep + " in GumFolderNotice");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ParentLocation_ExistingFolder_EmptyOrNot_SaysItUsesIt(bool empty)
    {
        FolderExists(Parent, "CodeProj3", empty);
        NewProjectDialogViewModel viewModel = Create(Parent);

        await viewModel.ChecksRefresh;

        viewModel.IsFolderNoticeVisible.ShouldBeTrue();
        viewModel.FolderNoticeHeading.ShouldBe("Uses the existing folder");
        viewModel.FolderNoticePath.ShouldBe(Parent + Sep + "CodeProj3" + Sep);
        viewModel.FolderNoticeSuffix.ShouldBe("");
        viewModel.FoldersSummary.ShouldBe("Uses the existing folder");
    }

    [Fact]
    public async Task LocationIsProjectFolder_ThatExists_SaysItAlreadyExists()
    {
        string location = Path.Combine(Parent, "CodeProj3");
        FolderExists(Parent, "CodeProj3");
        NewProjectDialogViewModel viewModel = Create(location);

        await viewModel.ChecksRefresh;

        viewModel.FolderNoticeHeading.ShouldBe("Uses this folder as the project folder");
        viewModel.FolderNoticePath.ShouldBe(location + Sep);
        viewModel.FolderNoticeSuffix.ShouldBe("(it already exists)");
        viewModel.FoldersSummary.ShouldBe("Uses the existing folder");
    }

    [Fact]
    public async Task LocationIsProjectFolder_NotYetOnDisk_SaysItWillBeCreated()
    {
        string location = Path.Combine(Parent, "CodeProj3");
        NewProjectDialogViewModel viewModel = Create(location);

        await viewModel.ChecksRefresh;

        viewModel.FolderNoticeHeading.ShouldBe("Uses this folder as the project folder");
        viewModel.FolderNoticePath.ShouldBe(location + Sep);
        viewModel.FolderNoticeSuffix.ShouldBe("(it will be created)");
        viewModel.FoldersSummary.ShouldBe("Creates CodeProj3" + Sep + " in GumFolderNotice");
    }

    [Fact]
    public async Task ACodeProject_SaysTheSameThing()
    {
        NewProjectDialogViewModel viewModel = Create(Parent, code: true);

        await viewModel.ChecksRefresh;

        viewModel.FolderNoticeHeading.ShouldBe("Creates a new folder");
        viewModel.FolderNoticePath.ShouldBe(Parent + Sep + "CodeProj3" + Sep);
    }

    [Fact]
    public async Task ARootParent_SummaryNamesTheWholeRoot()
    {
        string root = Path.GetPathRoot(Parent)!;
        NewProjectDialogViewModel viewModel = Create(root);

        await viewModel.ChecksRefresh;

        viewModel.FoldersSummary.ShouldBe("Creates CodeProj3" + Sep + " in " + root);
    }

    [Fact]
    public async Task EmptyName_ShowsNothing()
    {
        NewProjectDialogViewModel viewModel = Create(Parent, name: "");

        await viewModel.ChecksRefresh;

        viewModel.IsFolderNoticeVisible.ShouldBeFalse();
        viewModel.FolderNoticePath.ShouldBe("");
        viewModel.FoldersSummary.ShouldBe("");
    }

    [Theory]
    [InlineData("")]
    [InlineData("relative/folder")]
    public async Task InvalidLocation_ShowsNothing(string location)
    {
        NewProjectDialogViewModel viewModel = Create(location);

        await viewModel.ChecksRefresh;

        viewModel.ValidationMessage.ShouldNotBeEmpty();
        viewModel.IsFolderNoticeVisible.ShouldBeFalse();
        viewModel.FoldersSummary.ShouldBe("");
    }

    [Fact]
    public async Task ExistingProjectRefusal_ShowsNoNotice()
    {
        FolderExists(Parent, "CodeProj3");
        _service.Setup(x => x.Validate(It.IsAny<NewProjectRequest>())).Returns(new NewProjectValidation
        {
            Error = new NewProjectError(NewProjectErrorKind.ProjectAlreadyExists, "Project already exists: x"),
        });
        NewProjectDialogViewModel viewModel = Create(Parent);

        await viewModel.ChecksRefresh;

        viewModel.IsRefused.ShouldBeTrue();
        viewModel.IsFolderNoticeVisible.ShouldBeFalse();
        viewModel.FoldersSummary.ShouldBe("");
    }

    [Fact]
    public async Task ChangingTheName_UpdatesThePathAndTheExistingCheck()
    {
        FolderExists(Parent, "Taken");
        NewProjectDialogViewModel viewModel = Create(Parent);
        await viewModel.ChecksRefresh;
        viewModel.FolderNoticeHeading.ShouldBe("Creates a new folder");

        viewModel.ProjectName = "Taken";
        await viewModel.ChecksRefresh;

        viewModel.FolderNoticeHeading.ShouldBe("Uses the existing folder");
        viewModel.FolderNoticePath.ShouldBe(Parent + Sep + "Taken" + Sep);
    }

    [Fact]
    public async Task UntilTheCheckForTheCurrentInputsIsBack_NothingIsShown_ThenTheLastInputsAnswer()
    {
        NewProjectDialogViewModel viewModel = Create(Parent);
        await viewModel.ChecksRefresh;
        viewModel.FolderNoticeHeading.ShouldBe("Creates a new folder");

        using ManualResetEventSlim gate = new ManualResetEventSlim(false);
        _detector.Setup(x => x.TargetExists(It.IsAny<string>(), It.IsAny<string>())).Returns((string folder, string name) =>
        {
            gate.Wait(TimeSpan.FromSeconds(20));
            return name == "Taken";
        });

        viewModel.ProjectName = "Taken";
        viewModel.IsFolderNoticeVisible.ShouldBeFalse("the old answer is not the answer for Taken");
        viewModel.FoldersSummary.ShouldBe("");
        viewModel.ProjectName = "Other";
        viewModel.IsFolderNoticeVisible.ShouldBeFalse();
        viewModel.FolderNoticePath.ShouldBe("");
        string elsewhere = Path.Combine(Path.GetTempPath(), "GumFolderNoticeElsewhere");
        viewModel.Location = elsewhere;
        viewModel.IsFolderNoticeVisible.ShouldBeFalse();

        gate.Set();
        await viewModel.ChecksRefresh;

        viewModel.IsFolderNoticeVisible.ShouldBeTrue();
        viewModel.FolderNoticeHeading.ShouldBe("Creates a new folder");
        viewModel.FolderNoticePath.ShouldBe(elsewhere + Sep + "Other" + Sep);

        viewModel.ProjectName = "Taken";
        await viewModel.ChecksRefresh;
        viewModel.FolderNoticeHeading.ShouldBe("Uses the existing folder");
        viewModel.FolderNoticePath.ShouldBe(elsewhere + Sep + "Taken" + Sep);
    }

    [Fact]
    public async Task AnUnreadableFolder_StillShowsTheDefaultNotice()
    {
        _detector.Setup(x => x.TargetExists(It.IsAny<string>(), It.IsAny<string>())).Throws(new IOException("boom"));
        NewProjectDialogViewModel viewModel = Create(Parent);

        await viewModel.ChecksRefresh;

        viewModel.FolderNoticeHeading.ShouldBe("Creates a new folder");
    }
}
