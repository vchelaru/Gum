using System.IO.Compression;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Gum.Avalonia.Dialogs.Views;
using Gum.Avalonia.Tests.Harness;
using Gum.Diagnostics;
using Gum.Services;
using Moq;
using Shouldly;

namespace Gum.Avalonia.Tests;

/// <summary>The Create diagnostics file dialog in the head's own window, driven with real clicks and typing.</summary>
public class CreateDiagnosticsViewTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "GumDiagView-" + Guid.NewGuid().ToString("N"));
    private readonly Mock<IFileSystemRevealService> _reveal = new();

    public CreateDiagnosticsViewTests()
    {
        Directory.CreateDirectory(_folder);
    }

    public void Dispose() => Directory.Delete(_folder, recursive: true);

    private CreateDiagnosticsViewModel CreateViewModel(string? gumx, string? csharpFolder)
    {
        Mock<IDiagnosticsEnvironment> environment = new();
        environment.SetupGet(x => x.SaveFolder).Returns(_folder);
        environment.SetupGet(x => x.UserName).Returns("sam");
        environment.SetupGet(x => x.HomeFolder).Returns(@"C:\Users\sam");
        environment.SetupGet(x => x.Today).Returns(new DateTime(2026, 11, 21));
        environment.Setup(x => x.GetSystemInfo()).Returns(new DiagnosticsSystemInfo("v", "os", "rt", "X64", "100%", "gpu"));
        Mock<IDiagnosticsContentSource> source = new();
        source.SetupGet(x => x.GumProjectFile).Returns(gumx);
        source.SetupGet(x => x.CSharpProjectFolder).Returns(csharpFolder);
        source.Setup(x => x.Collect()).Returns(new DiagnosticsContent(
            Array.Empty<string>(), "No errors.", "", Array.Empty<string>(), gumx, csharpFolder));
        return new CreateDiagnosticsViewModel(environment.Object, source.Object, _reveal.Object);
    }

    private static CheckBox Box(DialogWindowDriver driver, string name) => driver.Find<CheckBox>(box => box.Name == name);

    [AvaloniaFact]
    public void View_ShowsTheDesignedWordingAndFileName()
    {
        using DialogWindowDriver driver = new DialogWindowDriver(CreateViewModel(null, null));

        driver.Title.ShouldBe("Create diagnostics file");
        string text = driver.Text();
        text.ShouldContain("What went wrong? (optional)");
        text.ShouldContain("Gum version, system and graphics info");
        text.ShouldContain("Recent logs, errors, plugins and settings");
        text.ShouldContain("Your user name and folder paths are removed.");
        text.ShouldContain("GumDiagnostics-2026-11-21.zip");
        text.ShouldContain("The whole game project, without build output.");
        text.ShouldNotContain("MonoGame");
        text.ShouldNotContain("MB");
    }

    [AvaloniaFact]
    public void View_NoProjects_DisablesBothBoxesWithNotes()
    {
        using DialogWindowDriver driver = new DialogWindowDriver(CreateViewModel(null, null));

        Box(driver, CreateDiagnosticsView.GumProjectCheckBoxName).IsEnabled.ShouldBeFalse();
        Box(driver, CreateDiagnosticsView.CSharpProjectCheckBoxName).IsEnabled.ShouldBeFalse();
        driver.Text().ShouldContain(CreateDiagnosticsViewModel.NoGumProjectNote);
        driver.Text().ShouldContain(CreateDiagnosticsViewModel.NoCSharpProjectNote);
    }

    [AvaloniaFact]
    public void View_ProjectsFound_EnablesBothBoxesWithoutNotes()
    {
        using DialogWindowDriver driver = new DialogWindowDriver(CreateViewModel(@"C:\Game\Game.gumx", @"C:\Game"));

        Box(driver, CreateDiagnosticsView.GumProjectCheckBoxName).IsEnabled.ShouldBeTrue();
        Box(driver, CreateDiagnosticsView.CSharpProjectCheckBoxName).IsEnabled.ShouldBeTrue();
        driver.Text().ShouldNotContain(CreateDiagnosticsViewModel.NoGumProjectNote);
    }

    [AvaloniaFact]
    public void SaveZip_WithTypedDescription_WritesZipRevealsItAndClosesTheWindow()
    {
        using DialogWindowDriver driver = new DialogWindowDriver(CreateViewModel(null, null));
        driver.TypeInto(driver.Find<TextBox>(box => box.Name == CreateDiagnosticsView.DescriptionName), "Crash on drop");

        driver.ClickButton("Save zip");
        AsyncWorkWait.Until(() => driver.Result != null, TimeSpan.FromSeconds(10), "the zip to be saved",
            () => Dispatcher.UIThread.RunJobs(), Array.Empty<string>());

        driver.Result.ShouldBe(true);
        string zipPath = Path.Combine(_folder, "GumDiagnostics-2026-11-21.zip");
        using ZipArchive zip = ZipFile.OpenRead(zipPath);
        using StreamReader reader = new StreamReader(zip.GetEntry("description.txt")!.Open());
        reader.ReadToEnd().ShouldBe("Crash on drop");
        _reveal.Verify(x => x.RevealFile(zipPath), Times.Once);
    }

    [AvaloniaFact]
    public void SaveZip_WhenSavingFails_ShowsTheErrorAndKeepsTheDialogOpen()
    {
        CreateDiagnosticsViewModel viewModel = CreateViewModel(null, null);
        Directory.Delete(_folder, recursive: true);
        using DialogWindowDriver driver = new DialogWindowDriver(viewModel);

        driver.ClickButton("Save zip");
        AsyncWorkWait.Until(() => viewModel.HasError, TimeSpan.FromSeconds(10), "the failure",
            () => Dispatcher.UIThread.RunJobs(), Array.Empty<string>());

        driver.IsOpen.ShouldBeTrue();
        driver.Text().ShouldContain("Could not save the diagnostics file");
        Directory.CreateDirectory(_folder);
    }
}
