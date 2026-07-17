using Gum.Commands;
using Gum.DataTypes;
using Gum.Dialogs;
using Gum.Logic;
using Gum.Managers;
using Gum.ToolStates;
using Moq;
using Shouldly;
using System;
using System.IO;
using ToolsUtilities;

namespace Gum.Presentation.Tests;

/// <summary>
/// Characterization (pinning) tests for RenameFolderDialogViewModel, relocated out of Gum.csproj
/// into the headless Gum.Presentation assembly (ADR-0005, #3754). FolderNode used to be cast to the
/// concrete, WinForms-coupled TreeNodeWrapper to reach the underlying TreeNode.Text setter; ITreeNode
/// gained a Text setter instead, so folder nodes are mocked directly via ITreeNode here rather than
/// real WinForms TreeNode/TreeNodeWrapper instances.
/// </summary>
public class RenameFolderDialogViewModelTests : BaseTestClass
{
    private readonly Mock<INameVerifier> _nameVerifier;
    private readonly Mock<IRenameLogic> _renameLogic;
    private readonly Mock<IGuiCommands> _guiCommands;
    private readonly Mock<IFileCommands> _fileCommands;
    private readonly Mock<IFileLocations> _fileLocations;
    private readonly Mock<IProjectState> _projectState;
    private RenameFolderDialogViewModel _sut;

    public RenameFolderDialogViewModelTests()
    {
        _nameVerifier = new Mock<INameVerifier>();
        _renameLogic = new Mock<IRenameLogic>();
        _guiCommands = new Mock<IGuiCommands>();
        _fileCommands = new Mock<IFileCommands>();
        _fileLocations = new Mock<IFileLocations>();
        _projectState = new Mock<IProjectState>();

        _sut = new RenameFolderDialogViewModel(
            _nameVerifier.Object,
            _renameLogic.Object,
            _guiCommands.Object,
            _fileLocations.Object,
            _fileCommands.Object,
            _projectState.Object);
    }

    [Fact]
    public void FolderNode_WhenSet_ShouldPopulateValueWithCurrentFolderName()
    {
        Mock<ITreeNode> treeNode = new();
        treeNode.Setup(x => x.Text).Returns("MyFolder");

        _sut.FolderNode = treeNode.Object;

        _sut.Value.ShouldBe("MyFolder");
    }

    [Fact]
    public void FolderNode_WhenSetToNull_ShouldClearValue()
    {
        Mock<ITreeNode> treeNode = new();
        treeNode.Setup(x => x.Text).Returns("MyFolder");
        _sut.FolderNode = treeNode.Object;

        _sut.FolderNode = null;

        _sut.Value.ShouldBeNull();
    }

    // Pins the case-only-rename bug reported by a user whose project moved from Windows to
    // Linux: Windows' Directory.Exists is case-insensitive, so renaming a folder to a name that
    // differs only by case is misdiagnosed as "already exists" and the rename never happens -
    // even though the on-disk folder name is never actually corrected to match what the
    // .gumx / element names reference, which then breaks the project on a case-sensitive
    // filesystem (Linux/macOS). Uses a real temp directory so Directory.Exists is genuinely
    // exercised rather than trivially returning false for a mocked path.
    [Fact]
    public void OnAffirmative_WhenNewFolderNameDiffersOnlyByCase_ShouldRenameFolderOnDisk()
    {
        string tempRoot = Path.Combine(Path.GetTempPath(), "GumRenameCaseTest_" + Guid.NewGuid());
        string screensFolder = Path.Combine(tempRoot, "Screens");
        string oldFolder = Path.Combine(screensFolder, "GameMenuScreens");
        Directory.CreateDirectory(oldFolder);

        try
        {
            GumProjectSave project = new() { FullFileName = Path.Combine(tempRoot, "Project.gumx") };

            Mock<ITreeNode> screensRoot = new();
            screensRoot.Setup(x => x.Text).Returns("Screens");
            screensRoot.Setup(x => x.Parent).Returns((ITreeNode?)null);

            Mock<ITreeNode> folderNode = new();
            folderNode.Setup(x => x.Text).Returns("GameMenuScreens");
            folderNode.Setup(x => x.Parent).Returns(screensRoot.Object);
            folderNode.Setup(x => x.GetFullFilePath()).Returns(new FilePath(oldFolder + "\\"));

            string? whyNotValid = null;
            _nameVerifier
                .Setup(x => x.IsFolderNameValid(It.IsAny<string?>(), out whyNotValid))
                .Returns(true);
            _projectState.Setup(x => x.GumProjectSave).Returns(project);
            _fileLocations.Setup(x => x.ScreensFolder).Returns(screensFolder + "\\");

            _sut.FolderNode = folderNode.Object;
            _sut.Value = "gamemenuscreens";

            _sut.OnAffirmative();

            _sut.Error.ShouldBeNull();
            _fileCommands.Verify(
                x => x.MoveDirectory(
                    It.Is<string>(s => s.Contains("GameMenuScreens")),
                    It.Is<string>(s => s.Contains("gamemenuscreens"))),
                Times.Once);
        }
        finally
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }

    // Pins the FolderNode.Text update that keeps the tree label in sync immediately after rename,
    // without going through a full RefreshElementTreeView. This used to require an "as TreeNodeWrapper"
    // cast to reach the underlying WinForms TreeNode - now it's a plain ITreeNode.Text set, so it's
    // mockable directly.
    [Fact]
    public void OnAffirmative_SetsFolderNodeTextToNewValue()
    {
        string screensFolder = "c:\\Project\\Screens\\";
        string oldFolderPath = screensFolder + "OldFolder\\";

        GumProjectSave project = new() { FullFileName = "c:\\Project\\Project.gumx" };

        Mock<ITreeNode> screensRoot = new();
        screensRoot.Setup(x => x.Text).Returns("Screens");
        screensRoot.Setup(x => x.Parent).Returns((ITreeNode?)null);

        Mock<ITreeNode> folderNode = new();
        folderNode.Setup(x => x.Text).Returns("OldFolder");
        folderNode.Setup(x => x.Parent).Returns(screensRoot.Object);
        folderNode.Setup(x => x.GetFullFilePath()).Returns(new FilePath(oldFolderPath));

        string? whyNotValid = null;
        _nameVerifier
            .Setup(x => x.IsFolderNameValid(It.IsAny<string?>(), out whyNotValid))
            .Returns(true);
        _projectState.Setup(x => x.GumProjectSave).Returns(project);
        _fileLocations.Setup(x => x.ScreensFolder).Returns(screensFolder);

        _sut.FolderNode = folderNode.Object;
        _sut.Value = "NewFolder";

        _sut.OnAffirmative();

        _sut.Error.ShouldBeNull();
        folderNode.VerifySet(x => x.Text = "NewFolder", Times.Once);
    }
}
