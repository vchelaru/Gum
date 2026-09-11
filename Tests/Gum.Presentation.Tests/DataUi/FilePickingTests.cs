using System.Collections.Generic;
using System.IO;
using Gum.Services;
using Gum.Services.Dialogs;
using Moq;
using Shouldly;
using WpfDataUi.Controls;

namespace Gum.Presentation.Tests.DataUi;

public class FilePickingTests : IDisposable
{
    private readonly IDataUiFilePicker? _originalPicker;
    private readonly string _originalFolder;

    public FilePickingTests()
    {
        _originalPicker = FilePickingLogic.FilePicker;
        _originalFolder = FilePickingLogic.FolderRelativeTo;
    }

    public void Dispose()
    {
        FilePickingLogic.FilePicker = _originalPicker;
        FilePickingLogic.FolderRelativeTo = _originalFolder;
    }

    [Fact]
    public void DialogServiceFilePicker_PassesTheFilterAndReturnsTheFirstFile()
    {
        Mock<IDialogService> dialogService = new Mock<IDialogService>();
        dialogService
            .Setup(d => d.OpenFile(It.Is<OpenFileDialogOptions>(o => o.Filter == "Font|*.fnt")))
            .Returns(new List<string> { "/a/first.fnt", "/a/second.fnt" });
        DialogServiceFilePicker picker = new DialogServiceFilePicker(dialogService.Object, Mock.Of<IFileSystemRevealService>());

        picker.PickFile("Font|*.fnt").ShouldBe("/a/first.fnt");
    }

    [Fact]
    public void DialogServiceFilePicker_ReturnsNull_WhenCancelled()
    {
        Mock<IDialogService> dialogService = new Mock<IDialogService>();
        dialogService.Setup(d => d.OpenFile(It.IsAny<OpenFileDialogOptions>())).Returns((List<string>?)null);
        DialogServiceFilePicker picker = new DialogServiceFilePicker(dialogService.Object, Mock.Of<IFileSystemRevealService>());

        picker.PickFile("").ShouldBeNull();
    }

    [Fact]
    public void ShowInExplorer_ResolvesAgainstFolderRelativeTo_AndRevealsOnlyExistingFiles()
    {
        string folder = Path.Combine(Path.GetTempPath(), "GumFilePickingTests", Guid.NewGuid().ToString("N")) + "/";
        Directory.CreateDirectory(folder);
        File.WriteAllText(folder + "real.png", "");
        Mock<IDataUiFilePicker> picker = new Mock<IDataUiFilePicker>();
        FilePickingLogic.FilePicker = picker.Object;
        FilePickingLogic.FolderRelativeTo = folder;
        FilePickingLogic logic = new FilePickingLogic();

        logic.ShowInExplorer("real.png");
        logic.ShowInExplorer("missing.png");

        picker.Verify(p => p.RevealFile(It.Is<string>(path => path.EndsWith("real.png"))), Times.Once);
        picker.Verify(p => p.RevealFile(It.Is<string>(path => path.EndsWith("missing.png"))), Times.Never);
    }

    [Fact]
    public void ShowOpenDialog_UsesTheSharedPickerAndFilter()
    {
        Mock<IDataUiFilePicker> picker = new Mock<IDataUiFilePicker>();
        picker.Setup(p => p.PickFile("TrueType Font|*.ttf")).Returns("/fonts/a.ttf");
        FilePickingLogic.FilePicker = picker.Object;
        FilePickingLogic logic = new FilePickingLogic { Filter = "TrueType Font|*.ttf" };

        logic.ShowOpenDialog().ShouldBe("/fonts/a.ttf");
    }
}
