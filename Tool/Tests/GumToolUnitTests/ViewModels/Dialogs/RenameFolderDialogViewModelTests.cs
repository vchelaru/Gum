using Gum.Commands;
using Gum.DataTypes;
using Gum.Dialogs;
using Gum.Managers;
using Gum.Services;
using Gum.ToolStates;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Moq.AutoMock;
using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ToolsUtilities;
using Xunit;

namespace GumToolUnitTests.ViewModels.Dialogs;

public class RenameFolderDialogViewModelTests : IDisposable
{
    private readonly AutoMocker _mocker;


    RenameFolderDialogViewModel _sut;

    public RenameFolderDialogViewModelTests()
    {
        _mocker = new();

        _sut = _mocker.CreateInstance<RenameFolderDialogViewModel>();
    }

    public void Dispose()
    {
        ObjectFinder.Self.GumProjectSave = null;
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
    // filesystem (Linux/macOS).
    [Fact]
    public void OnAffirmative_WhenNewFolderNameDiffersOnlyByCase_ShouldRenameFolderOnDisk()
    {
        string tempRoot = Path.Combine(Path.GetTempPath(), "GumRenameCaseTest_" + Guid.NewGuid());
        string screensFolder = Path.Combine(tempRoot, "Screens");
        string oldFolder = Path.Combine(screensFolder, "GameMenuScreens");
        Directory.CreateDirectory(oldFolder);

        try
        {
            var project = new GumProjectSave { FullFileName = Path.Combine(tempRoot, "Project.gumx") };
            ObjectFinder.Self.GumProjectSave = project;

            var projectManagerMock = new Mock<IProjectManager>();
            projectManagerMock.SetupGet(x => x.GumProjectSave).Returns(project);

            var services = new ServiceCollection();
            services.AddSingleton(projectManagerMock.Object);
            services.AddSingleton(new Mock<Gum.Services.Dialogs.IDialogService>().Object);
            Locator.Register(services.BuildServiceProvider());

            var screensRootNode = new TreeNode("Screens");
            var folderNode = new TreeNode("GameMenuScreens");
            screensRootNode.Nodes.Add(folderNode);
            var wrappedFolderNode = new TreeNodeWrapper(folderNode);

            string? whyNotValid = null;
            _mocker.GetMock<INameVerifier>()
                .Setup(x => x.IsFolderNameValid(It.IsAny<string?>(), out whyNotValid))
                .Returns(true);

            _mocker.GetMock<IProjectState>()
                .Setup(x => x.GumProjectSave).Returns(project);

            _mocker.Use(new FileLocations());

            _sut = _mocker.CreateInstance<RenameFolderDialogViewModel>();
            _sut.FolderNode = wrappedFolderNode;
            _sut.Value = "gamemenuscreens";

            _sut.OnAffirmative();

            _sut.Error.ShouldBeNull();
            _mocker.GetMock<IFileCommands>().Verify(
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

    // This requires more mocking - made some progress but more is needed to get this to work:
    //[Fact]
    //public void OnAffirmative_ShouldRenameComponents()
    //{
    //    Mock<ITreeNode> treeNode = new Mock<ITreeNode>();

    //    treeNode.Setup(x => x.IsComponentsFolderTreeNode()).Returns(true);
    //    treeNode.Setup(x => x.GetFullFilePath()).Returns("c:\\Project\\OldFolderPath\\");

    //    _sut.FolderNode = treeNode.Object;
    //    _sut.Value = "NewFolderName";

    //    _sut.OnAffirmative();
    //}
}
