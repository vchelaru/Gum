using System;
using System.Collections.Generic;
using System.IO;
using Gum.ProjectServices;
using Gum.Services.Dialogs;
using HtmlToGumPlugin;
using Moq;
using Shouldly;

namespace Gum.Presentation.Tests.HtmlToGum;

/// <summary>The Import HTML options dialog's checks and clean-up, shared by both heads' views.</summary>
public class ImportHtmlOptionsViewModelTests
{
    private readonly Mock<IDialogService> _dialogService = new();

    [Fact]
    public void Import_RefusesAnEmptySource_AndStaysOpen()
    {
        ImportHtmlOptionsViewModel dialog = Create(new ImportOptions { HtmlPath = "" });
        bool closed = false;
        dialog.RequestClose += (_, _) => closed = true;

        dialog.OnAffirmative();

        _dialogService.Verify(service => service.ShowMessage("Enter a local HTML file path or a URL.", "Import HTML", null), Times.Once);
        closed.ShouldBeFalse();
        dialog.Result.ShouldBeNull();
    }

    [Fact]
    public void Import_RefusesAMissingLocalFile_AndStaysOpen()
    {
        string missing = Path.Combine(Path.GetTempPath(), "GumHtmlImport-" + Guid.NewGuid().ToString("N") + ".html");
        ImportHtmlOptionsViewModel dialog = Create(new ImportOptions { HtmlPath = missing, IsUrl = false });

        dialog.OnAffirmative();

        _dialogService.Verify(service => service.ShowMessage("File not found:\n" + missing, "Import HTML", null), Times.Once);
        dialog.Result.ShouldBeNull();
    }

    [Fact]
    public void Import_CleansUpTheChoices_ForAnExistingFile()
    {
        string file = Path.Combine(Path.GetTempPath(), "GumHtmlImport-" + Guid.NewGuid().ToString("N") + ".html");
        File.WriteAllText(file, "<html></html>");
        try
        {
            ImportHtmlOptionsViewModel dialog = Create(new ImportOptions { HtmlPath = file, Width = 800, Height = 600, ScreenName = "Fallback" });
            dialog.ScreenName = "9 my screen ";
            dialog.Selector = "  ";
            dialog.Width = 0;
            dialog.DestinationSubfolder = " Imported ";
            bool closed = false;
            dialog.RequestClose += (_, affirmed) => closed = affirmed;

            dialog.OnAffirmative();

            closed.ShouldBeTrue();
            ImportOptions result = dialog.Result.ShouldNotBeNull();
            result.HtmlPath.ShouldBe(file);
            result.ScreenName.ShouldBe("S_9_my_screen");
            result.Selector.ShouldBe("body");
            result.Width.ShouldBe(800);
            result.Height.ShouldBe(600);
            result.DestinationSubfolder.ShouldBe("Imported");
        }
        finally
        {
            File.Delete(file);
        }
    }

    [Fact]
    public void Import_NormalizesAUrl()
    {
        ImportHtmlOptionsViewModel dialog = Create(new ImportOptions { HtmlPath = "example.com/page", IsUrl = true });

        dialog.OnAffirmative();

        dialog.Result.ShouldNotBeNull().HtmlPath.ShouldBe(HtmlImportNaming.NormalizeUrl("example.com/page"));
        dialog.HtmlPath.ShouldBe(HtmlImportNaming.NormalizeUrl("example.com/page"));
    }

    [Fact]
    public void Browse_TakesThePickedFile_AndNamesTheScreenAfterIt_WhenTheNameIsTheDefault()
    {
        _dialogService.Setup(service => service.OpenFile(It.IsAny<OpenFileDialogOptions>()))
            .Returns(new List<string> { Path.Combine("C:", "pages", "landing page.html") });
        ImportHtmlOptionsViewModel dialog = Create(new ImportOptions { ScreenName = MainHtmlToGumPlugin.DefaultScreenName });

        dialog.BrowseCommand.CanExecute(null).ShouldBeTrue();
        dialog.BrowseCommand.Execute(null);

        dialog.HtmlPath.ShouldBe(Path.Combine("C:", "pages", "landing page.html"));
        dialog.ScreenName.ShouldBe("landing_page");
        dialog.IsUrl = true;
        dialog.BrowseCommand.CanExecute(null).ShouldBeFalse();
    }

    private ImportHtmlOptionsViewModel Create(ImportOptions defaults) => new ImportHtmlOptionsViewModel(_dialogService.Object, defaults);
}
