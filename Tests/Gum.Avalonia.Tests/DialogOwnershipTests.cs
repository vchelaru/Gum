using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Gum.Avalonia.Dialogs;
using Gum.Avalonia.Tests.Harness;
using Gum.Services.Dialogs;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Gum.Avalonia.Tests;

/// <summary>Which window owns (is disabled by) a dialog or file picker the service opens (#5538).</summary>
public class DialogOwnershipTests
{
    [AvaloniaFact]
    public void DialogOpenedFromADialog_IsOwnedByThatDialog_AndOwnershipReturnsToItOnceTheInnerCloses()
    {
        // Avalonia's ShowDialog disables only the owner, so a prompt owned by the main window left
        // the dialog that raised it clickable.
        AvaloniaDialogService service = TestAppBuilder.Services.GetRequiredService<AvaloniaDialogService>();
        DialogWindow? outer = null;
        Window? firstInnerOwner = null;
        Window? secondInnerOwner = null;
        int openAfterFirstInnerClosed = -1;

        Dispatcher.UIThread.Post(() =>
        {
            outer = service.OpenDialogs.Single();
            firstInnerOwner = ShowAndCloseInner(service);
            openAfterFirstInnerClosed = service.OpenDialogs.Count;
            secondInnerOwner = ShowAndCloseInner(service);
            outer.Close();
        });
        service.ShowMessage("Outer");

        outer.ShouldNotBeNull();
        firstInnerOwner.ShouldBeSameAs(outer);
        openAfterFirstInnerClosed.ShouldBe(1);
        secondInnerOwner.ShouldBeSameAs(outer);
        service.OpenDialogs.ShouldBeEmpty();
    }

    [AvaloniaFact]
    public void FilePickers_WithNoDialogOpen_OpenOverTheMainWindow_WithTheCallersOptions()
    {
        Window mainWindow = new Window();
        RecordingFilePickers pickers = new RecordingFilePickers();
        AvaloniaDialogService service = CreateService(pickers, mainWindow);
        string folder = Path.GetTempPath();
        string missingFolder = Path.Combine(folder, Guid.NewGuid().ToString("N"));
        string pickedFile = Path.Combine(folder, "Picked.png");
        pickers.Answer = new List<string> { pickedFile };

        List<string>? opened = service.OpenFile(new OpenFileDialogOptions
        {
            Title = "Pick a texture",
            Filter = "PNG Files (*.png)|*.png",
            Multiselect = true,
            InitialDirectory = folder,
        });
        string? saved = service.SaveFile(new SaveFileDialogOptions { Title = "Save as", FileName = "Out.png", Filter = "PNG Files (*.png)|*.png", InitialDirectory = folder });
        string? chosenFolder = service.OpenFolder(new OpenFolderDialogOptions { Title = "Pick a folder", InitialDirectory = missingFolder });

        pickers.Owners.ShouldBe(new TopLevel[] { mainWindow, mainWindow, mainWindow });
        opened.ShouldBe(new[] { pickedFile });
        saved.ShouldBe(pickedFile);
        chosenFolder.ShouldBe(pickedFile);
        OpenFilePickerRequest openRequest = pickers.LastOpenFile.ShouldNotBeNull();
        openRequest.Title.ShouldBe("Pick a texture");
        openRequest.AllowMultiple.ShouldBeTrue();
        openRequest.Filter.Single().Patterns.ShouldBe(new[] { "*.png" });
        openRequest.StartDirectory.ShouldBe(folder);
        SaveFilePickerRequest saveRequest = pickers.LastSaveFile.ShouldNotBeNull();
        saveRequest.Title.ShouldBe("Save as");
        saveRequest.FileName.ShouldBe("Out.png");
        saveRequest.Filter.Single().Name.ShouldBe("PNG Files (*.png)");
        saveRequest.StartDirectory.ShouldBe(folder);
        OpenFolderPickerRequest folderRequest = pickers.LastOpenFolder.ShouldNotBeNull();
        folderRequest.Title.ShouldBe("Pick a folder");
        folderRequest.StartDirectory.ShouldBeNull("a start folder that does not exist is dropped");
    }

    [AvaloniaFact]
    public void FilePickers_Cancelled_AnswerNull()
    {
        RecordingFilePickers pickers = new RecordingFilePickers();
        AvaloniaDialogService service = CreateService(pickers, new Window());

        service.OpenFile().ShouldBeNull();
        service.SaveFile().ShouldBeNull();
        service.OpenFolder().ShouldBeNull();
    }

    [AvaloniaFact]
    public void FilePickers_OpenedFromADialog_OpenOverThatDialog()
    {
        // A Browse button in a dialog: the picker must be modal over the dialog, not the main window.
        RecordingFilePickers pickers = new RecordingFilePickers();
        AvaloniaDialogService service = CreateService(pickers, new Window());
        DialogWindow? dialog = null;

        Dispatcher.UIThread.Post(() =>
        {
            dialog = service.OpenDialogs.Single();
            service.OpenFile();
            service.SaveFile();
            service.OpenFolder();
            dialog.Close();
        });
        service.ShowMessage("Has a Browse button");

        dialog.ShouldNotBeNull();
        pickers.Owners.ShouldBe(new TopLevel[] { dialog, dialog, dialog });
    }

    private static AvaloniaDialogService CreateService(RecordingFilePickers pickers, Window mainWindow) =>
        new AvaloniaDialogService(
            TestAppBuilder.Services,
            TestAppBuilder.Services.GetRequiredService<DialogViewRegistry>(),
            pickers,
            () => mainWindow);

    private static Window? ShowAndCloseInner(AvaloniaDialogService service)
    {
        Window? owner = null;
        Dispatcher.UIThread.Post(() =>
        {
            DialogWindow inner = service.OpenDialogs[^1];
            owner = inner.Owner as Window;
            inner.Close();
        });
        service.ShowMessage("Inner");
        return owner;
    }
}
