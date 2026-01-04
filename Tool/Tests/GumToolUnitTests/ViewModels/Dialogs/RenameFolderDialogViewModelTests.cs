using Gum.Dialogs;
using Gum.Managers;
using Moq;
using Moq.AutoMock;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GumToolUnitTests.ViewModels.Dialogs;

public class RenameFolderDialogViewModelTests
{
    private readonly AutoMocker _mocker;


    RenameFolderDialogViewModel _sut;

    public RenameFolderDialogViewModelTests()
    {
        _mocker = new();

        _sut = _mocker.CreateInstance<RenameFolderDialogViewModel>();
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
