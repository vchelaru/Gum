using System.IO.Compression;
using Gum.Diagnostics;
using Gum.Services;
using Moq;
using Shouldly;

namespace Gum.Presentation.Tests;

public class CreateDiagnosticsViewModelTests : IDisposable
{
    private readonly string _saveFolder;
    private readonly Mock<IDiagnosticsEnvironment> _environment = new();
    private readonly Mock<IDiagnosticsContentSource> _source = new();
    private readonly Mock<IFileSystemRevealService> _reveal = new();

    public CreateDiagnosticsViewModelTests()
    {
        _saveFolder = Path.Combine(Path.GetTempPath(), "GumDiagVm-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_saveFolder);
        _environment.SetupGet(x => x.SaveFolder).Returns(_saveFolder);
        _environment.SetupGet(x => x.UserName).Returns("sam");
        _environment.SetupGet(x => x.HomeFolder).Returns(@"C:\Users\sam");
        _environment.SetupGet(x => x.Today).Returns(new DateTime(2026, 11, 21));
        _environment.Setup(x => x.GetSystemInfo()).Returns(new DiagnosticsSystemInfo("v", "os", "rt", "X64", "100%", "gpu"));
        _source.SetupGet(x => x.GumProjectFile).Returns((string?)null);
        _source.SetupGet(x => x.CSharpProjectFolder).Returns((string?)null);
        _source.Setup(x => x.Collect()).Returns(new DiagnosticsContent(
            Array.Empty<string>(), "No errors.", "", Array.Empty<string>(), null, null));
    }

    public void Dispose() => Directory.Delete(_saveFolder, recursive: true);

    private CreateDiagnosticsViewModel Create() => new CreateDiagnosticsViewModel(_environment.Object, _source.Object, _reveal.Object);

    [Fact]
    public void Constructor_SetsTitleButtonsAndFileName()
    {
        CreateDiagnosticsViewModel viewModel = Create();

        viewModel.Title.ShouldBe("Create diagnostics file");
        viewModel.AffirmativeText.ShouldBe("Save zip");
        viewModel.NegativeText.ShouldBe("Cancel");
        viewModel.FileName.ShouldBe("GumDiagnostics-2026-11-21.zip");
    }

    [Fact]
    public void Constructor_FileNameTaken_ShowsNumberedName()
    {
        File.WriteAllText(Path.Combine(_saveFolder, "GumDiagnostics-2026-11-21.zip"), "old");

        Create().FileName.ShouldBe("GumDiagnostics-2026-11-21-2.zip");
    }

    [Fact]
    public void Constructor_NoProjectLoaded_DisablesGumProjectWithNote()
    {
        CreateDiagnosticsViewModel viewModel = Create();

        viewModel.CanIncludeGumProject.ShouldBeFalse();
        viewModel.GumProjectNote.ShouldBe(CreateDiagnosticsViewModel.NoGumProjectNote);
    }

    [Fact]
    public void Constructor_CSharpProjectNotFound_DisablesBoxWithNote()
    {
        CreateDiagnosticsViewModel viewModel = Create();

        viewModel.CanIncludeCSharpProject.ShouldBeFalse();
        viewModel.CSharpProjectNote.ShouldBe(CreateDiagnosticsViewModel.NoCSharpProjectNote);
    }

    [Fact]
    public void Constructor_ProjectsFound_EnablesBothBoxesWithoutNotes()
    {
        _source.SetupGet(x => x.GumProjectFile).Returns(@"C:\Game\Game.gumx");
        _source.SetupGet(x => x.CSharpProjectFolder).Returns(@"C:\Game");

        CreateDiagnosticsViewModel viewModel = Create();

        viewModel.CanIncludeGumProject.ShouldBeTrue();
        viewModel.CanIncludeCSharpProject.ShouldBeTrue();
        viewModel.GumProjectNote.ShouldBeNull();
        viewModel.CSharpProjectNote.ShouldBeNull();
    }

    [Fact]
    public async Task SaveAsync_WritesZipRevealsItAndCloses()
    {
        CreateDiagnosticsViewModel viewModel = Create();
        bool? closed = null;
        viewModel.RequestClose += (_, result) => closed = result;
        viewModel.Description = "it broke";

        await viewModel.SaveAsync();

        string expected = Path.Combine(_saveFolder, "GumDiagnostics-2026-11-21.zip");
        File.Exists(expected).ShouldBeTrue();
        using (ZipArchive zip = ZipFile.OpenRead(expected))
        {
            zip.Entries.Select(entry => entry.FullName).ShouldContain("description.txt");
            zip.Entries.Select(entry => entry.FullName).ShouldContain("system-info.txt");
        }
        _reveal.Verify(x => x.RevealFile(expected), Times.Once);
        closed.ShouldBe(true);
        viewModel.IsSaving.ShouldBeFalse();
        viewModel.HasError.ShouldBeFalse();
    }

    [Fact]
    public async Task SaveAsync_SecondSave_NeverOverwritesTheFirst()
    {
        string first = Path.Combine(_saveFolder, "GumDiagnostics-2026-11-21.zip");
        File.WriteAllText(first, "old");
        CreateDiagnosticsViewModel viewModel = Create();

        await viewModel.SaveAsync();

        File.ReadAllText(first).ShouldBe("old");
        File.Exists(Path.Combine(_saveFolder, "GumDiagnostics-2026-11-21-2.zip")).ShouldBeTrue();
    }

    [Fact]
    public async Task SaveAsync_DisabledBoxChecked_IsIgnored()
    {
        CreateDiagnosticsViewModel viewModel = Create();
        viewModel.IncludeGumProject = true;
        viewModel.IncludeCSharpProject = true;

        await viewModel.SaveAsync();

        using ZipArchive zip = ZipFile.OpenRead(Path.Combine(_saveFolder, "GumDiagnostics-2026-11-21.zip"));
        zip.Entries.ShouldNotContain(entry => entry.FullName.Contains("project/"));
    }

    [Fact]
    public async Task SaveAsync_WriteFails_ShowsErrorAndStaysOpen()
    {
        _environment.SetupGet(x => x.SaveFolder).Returns(Path.Combine(_saveFolder, "missing", "folder"));
        CreateDiagnosticsViewModel viewModel = Create();
        bool closed = false;
        viewModel.RequestClose += (_, _) => closed = true;

        await viewModel.SaveAsync();

        viewModel.HasError.ShouldBeTrue();
        viewModel.ErrorMessage!.ShouldStartWith("Could not save the diagnostics file");
        closed.ShouldBeFalse();
        viewModel.IsSaving.ShouldBeFalse();
        _reveal.Verify(x => x.RevealFile(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task SaveAsync_AfterFailure_ClearsTheErrorOnRetry()
    {
        string folder = Path.Combine(_saveFolder, "later");
        _environment.SetupGet(x => x.SaveFolder).Returns(folder);
        CreateDiagnosticsViewModel viewModel = Create();
        await viewModel.SaveAsync();
        viewModel.HasError.ShouldBeTrue();

        Directory.CreateDirectory(folder);
        await viewModel.SaveAsync();

        viewModel.HasError.ShouldBeFalse();
    }
}
