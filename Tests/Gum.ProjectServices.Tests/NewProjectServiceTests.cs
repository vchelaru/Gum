using System.Diagnostics;
using Shouldly;
using Xunit.Abstractions;

namespace Gum.ProjectServices.Tests;

/// <summary>
/// The shared layer behind <c>gumcli new</c> and the New Project dialog (#7208).
/// </summary>
public class NewProjectServiceTests : BaseTestClass
{
    private readonly ITestOutputHelper _output;
    private readonly string _folder;
    private readonly string _checkout;
    private readonly FakeRestoreRunner _restoreRunner = new FakeRestoreRunner();

    public NewProjectServiceTests(ITestOutputHelper output)
    {
        _output = output;
        _folder = Path.Combine(Path.GetTempPath(), "GumNewProjectService_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_folder);
        _checkout = GumSourceLocator.Find(AppContext.BaseDirectory)
            ?? throw new InvalidOperationException("These tests run from a Gum checkout.");
    }

    public override void Dispose()
    {
        base.Dispose();
        Directory.Delete(_folder, recursive: true);
    }

    private NewProjectService CreateService(params string[] gumSourceSearchDirectories)
    {
        FormsThemeLocator locator = new FormsThemeLocator(new[]
        {
            Path.Combine(_checkout, "Tools", "Gum.ProjectServices", "Templates", "FormsThemes")
        });
        return new NewProjectService(locator, _restoreRunner, gumSourceSearchDirectories.Length == 0
            ? new[] { _checkout }
            : gumSourceSearchDirectories);
    }

    public static IEnumerable<object?[]> AllCombinations()
    {
        foreach (string? platform in new string?[] { null, "monogame", "kni", "kniskia", "raylib", "stride", "silknet" })
        {
            yield return new object?[] { platform, "forms", null };
            yield return new object?[] { platform, "empty", null };
            yield return new object?[] { platform, "forms", "Meadow" };
        }
    }

    #region Plan

    [Theory]
    [MemberData(nameof(AllCombinations))]
    public async Task GetPlan_ForEveryPlatformTemplateAndTheme_EqualsTheFilesExecuteWrites(
        string? platform, string template, string? theme)
    {
        NewProjectService service = CreateService();
        NewProjectRequest request = new NewProjectRequest
        {
            Path = Path.Combine(_folder, "Planned"),
            Platform = platform,
            Template = template,
            Theme = theme,
            Restore = false
        };

        NewProjectPlan? plan = service.GetPlan(request);
        NewProjectResult result = await service.ExecuteAsync(request, null, CancellationToken.None);

        plan.ShouldNotBeNull();
        result.Files.Success.ShouldBeTrue(result.Files.ErrorMessage);
        string root = Path.Combine(_folder, "Planned");
        plan.RootDirectory.ShouldBe(root);

        List<string> actualFiles = Directory.GetFiles(root, "*", SearchOption.AllDirectories)
            .Select(path => Path.GetRelativePath(root, path).Replace('\\', '/')).OrderBy(path => path, StringComparer.Ordinal).ToList();
        List<string> actualDirectories = Directory.GetDirectories(root, "*", SearchOption.AllDirectories)
            .Select(path => Path.GetRelativePath(root, path).Replace('\\', '/')).OrderBy(path => path, StringComparer.Ordinal).ToList();

        plan.Files.ToList().ShouldBe(actualFiles);
        plan.Directories.ToList().ShouldBe(actualDirectories);
    }

    [Fact]
    public void GetPlan_WithXmlFormat_ListsTheGumxProject()
    {
        NewProjectService service = CreateService();

        NewProjectPlan? plan = service.GetPlan(new NewProjectRequest
        {
            Path = Path.Combine(_folder, "Xml"),
            UseXmlFormat = true,
            Template = "empty"
        });

        plan.ShouldNotBeNull();
        plan.Files.ShouldContain("Xml.gumx");
    }

    [Fact]
    public void GetPlan_WithPlatform_NamesTheCodeProjectAfterTheFolder()
    {
        NewProjectService service = CreateService();

        NewProjectPlan? plan = service.GetPlan(new NewProjectRequest
        {
            Path = Path.Combine(_folder, "Space Free"),
            Platform = "monogame"
        });

        plan.ShouldNotBeNull();
        plan.Files.ShouldContain("Space Free.csproj");
        plan.Files.ShouldContain("Content/GumProject/GumProject.gumj");
    }

    [Fact]
    public void GetPlan_DoesNotCreateTheTargetFolder()
    {
        NewProjectService service = CreateService();
        string target = Path.Combine(_folder, "NotCreated");

        service.GetPlan(new NewProjectRequest { Path = target, Platform = "monogame" });

        Directory.Exists(target).ShouldBeFalse();
        Directory.GetFileSystemEntries(_folder).ShouldBeEmpty();
    }

    [Fact]
    public void GetPlan_WithAnInvalidOption_ReturnsNull()
    {
        NewProjectService service = CreateService();

        service.GetPlan(new NewProjectRequest { Platform = "fna" }).ShouldBeNull();
        service.GetPlan(new NewProjectRequest { Theme = "NoSuchTheme" }).ShouldBeNull();
    }

    [Fact]
    public void GetPlan_ForFormsWithPlatform_IsFastOnceCachedAndReportsTheFirstCost()
    {
        NewProjectService service = CreateService();
        NewProjectRequest request = new NewProjectRequest { Path = Path.Combine(_folder, "Fast"), Platform = "monogame" };

        Stopwatch first = Stopwatch.StartNew();
        service.GetPlan(request);
        first.Stop();

        Stopwatch cached = Stopwatch.StartNew();
        service.GetPlan(new NewProjectRequest { Path = Path.Combine(_folder, "OtherName"), Platform = "monogame" });
        cached.Stop();

        _output.WriteLine($"first plan: {first.ElapsedMilliseconds} ms, cached plan: {cached.ElapsedMilliseconds} ms");
        cached.ElapsedMilliseconds.ShouldBeLessThan(300);
    }

    #endregion

    #region Validate

    [Fact]
    public void Validate_NameWithoutExtension_ResolvesToFolderWithGumjInside()
    {
        NewProjectService service = CreateService();
        string path = Path.Combine(_folder, "MyGame");

        NewProjectValidation validation = service.Validate(new NewProjectRequest { Path = path });

        validation.IsValid.ShouldBeTrue();
        validation.Resolved!.GumProjectPath.ShouldBe(Path.Combine(path, "MyGame.gumj"));
        validation.Resolved.RootDirectory.ShouldBe(path);
    }

    [Fact]
    public void Validate_PathWithGumxExtension_ResolvesToThatFile()
    {
        NewProjectService service = CreateService();
        string path = Path.Combine(_folder, "Exact.gumx");

        NewProjectValidation validation = service.Validate(new NewProjectRequest { Path = path });

        validation.Resolved!.GumProjectPath.ShouldBe(path);
        validation.Resolved.RootDirectory.ShouldBe(_folder);
    }

    [Fact]
    public void Validate_PlatformWithPath_ResolvesGameFolderAndGumProjectInsideContent()
    {
        NewProjectService service = CreateService();
        string path = Path.Combine(_folder, "Game");

        NewProjectValidation validation = service.Validate(new NewProjectRequest { Path = path, Platform = "KNI" });

        validation.Resolved!.Platform.ShouldBe(HostPlatform.Kni);
        validation.Resolved.CsprojPath.ShouldBe(Path.Combine(path, "Game.csproj"));
        validation.Resolved.GumProjectPath.ShouldBe(Path.Combine(path, "Content", "GumProject", "GumProject.gumj"));
    }

    [Theory]
    [InlineData("forms", null, "fna", false, NewProjectErrorKind.FnaNotSupported)]
    [InlineData("forms", null, "amiga", false, NewProjectErrorKind.UnknownPlatform)]
    [InlineData("forms", null, null, true, NewProjectErrorKind.SourceLinkedNeedsPlatform)]
    [InlineData("bogus", null, null, false, NewProjectErrorKind.UnknownTemplate)]
    [InlineData("empty", "Meadow", null, false, NewProjectErrorKind.ThemeNeedsFormsTemplate)]
    [InlineData("forms", "NoSuchTheme", null, false, NewProjectErrorKind.UnknownTheme)]
    public void Validate_BadOptions_ReturnsTheMatchingError(
        string template, string? theme, string? platform, bool sourceLinked, NewProjectErrorKind expected)
    {
        NewProjectService service = CreateService();

        NewProjectValidation validation = service.Validate(new NewProjectRequest
        {
            Path = Path.Combine(_folder, "G"),
            Template = template,
            Theme = theme,
            Platform = platform,
            SourceLinked = sourceLinked
        });

        validation.Error.ShouldNotBeNull();
        validation.Error.Kind.ShouldBe(expected);
        validation.Resolved.ShouldBeNull();
    }

    [Fact]
    public void Validate_UnknownTheme_ListsStandardAndTheBundledThemes()
    {
        NewProjectService service = CreateService();

        NewProjectValidation validation = service.Validate(new NewProjectRequest { Theme = "Nope" });

        validation.Error!.Message.ShouldStartWith("Unknown theme 'Nope'. Available themes: Standard, ");
        validation.Error.Message.ShouldContain("Meadow");
    }

    [Fact]
    public void Validate_PlatformWithProjectFilePath_IsRefused()
    {
        NewProjectService service = CreateService();

        NewProjectValidation validation = service.Validate(new NewProjectRequest
        {
            Path = Path.Combine(_folder, "G.gumj"),
            Platform = "monogame"
        });

        validation.Error!.Kind.ShouldBe(NewProjectErrorKind.PlatformPathIsProjectFile);
    }

    [Fact]
    public void Validate_PlatformFolderNamedLikeAGumRuntime_IsRefusedAsInvalidName()
    {
        NewProjectService service = CreateService();

        NewProjectValidation validation = service.Validate(new NewProjectRequest
        {
            Path = Path.Combine(_folder, "MonoGameGum"),
            Platform = "monogame"
        });

        validation.Error!.Kind.ShouldBe(NewProjectErrorKind.InvalidProjectName);
    }

    [Fact]
    public async Task Validate_ProjectAlreadyThere_IsRefusedForBothLayouts()
    {
        NewProjectService service = CreateService();
        NewProjectRequest gumOnly = new NewProjectRequest { Path = Path.Combine(_folder, "Gum1"), Template = "empty" };
        NewProjectRequest withPlatform = new NewProjectRequest
        {
            Path = Path.Combine(_folder, "Game1"), Template = "empty", Platform = "raylib", Restore = false
        };
        await service.ExecuteAsync(gumOnly, null, CancellationToken.None);
        await service.ExecuteAsync(withPlatform, null, CancellationToken.None);

        service.Validate(gumOnly).Error!.Kind.ShouldBe(NewProjectErrorKind.ProjectAlreadyExists);
        service.Validate(withPlatform).Error!.Kind.ShouldBe(NewProjectErrorKind.ProjectAlreadyExists);
    }

    [Fact]
    public void Validate_SourceLinkedWithoutACheckout_IsRefused()
    {
        NewProjectService service = CreateService(_folder);

        NewProjectValidation validation = service.Validate(new NewProjectRequest
        {
            Path = Path.Combine(_folder, "G"),
            Platform = "monogame",
            SourceLinked = true
        });

        validation.Error!.Kind.ShouldBe(NewProjectErrorKind.GumSourceNotFound);
        validation.Error.Message.ShouldBe(
            "--source-linked needs a Gum checkout: run a gumcli built from one, or run this from inside one.");
    }

    [Fact]
    public void Validate_SourceLinkedWithACheckout_ResolvesTheCheckout()
    {
        NewProjectService service = CreateService();

        NewProjectValidation validation = service.Validate(new NewProjectRequest
        {
            Path = Path.Combine(_folder, "G"),
            Platform = "monogame",
            SourceLinked = true
        });

        validation.Resolved!.GumSourceDirectory.ShouldBe(_checkout);
    }

    [Fact]
    public void IsSourceLinkedAvailable_ReflectsWhetherACheckoutIsFound()
    {
        CreateService().IsSourceLinkedAvailable.ShouldBeTrue();
        CreateService(_folder).IsSourceLinkedAvailable.ShouldBeFalse();
        CreateService(_folder).FindGumSourceDirectory().ShouldBeNull();
    }

    #endregion

    #region CreateFiles

    [Fact]
    public async Task CreateFiles_WhenCreationFailsInANewFolder_RemovesTheFoldersItCreated()
    {
        NewProjectService service = CreateService();
        string parent = Path.Combine(_folder, "NewParent");
        ResolvedNewProject project = BrokenThemeProject(Path.Combine(parent, "Game"));

        NewProjectFilesResult result = await service.CreateFilesAsync(project, CancellationToken.None);

        result.Success.ShouldBeFalse();
        result.ErrorMessage.ShouldContain("Theme project not found");
        Directory.Exists(parent).ShouldBeFalse();
    }

    [Fact]
    public async Task CreateFiles_WhenCreationFailsInAnExistingFolder_KeepsWhatWasThere()
    {
        NewProjectService service = CreateService();
        string game = Path.Combine(_folder, "Game");
        Directory.CreateDirectory(game);
        File.WriteAllText(Path.Combine(game, "keep.txt"), "mine");
        // A file where the Content folder must go makes the creation fail after the host files are written.
        File.WriteAllText(Path.Combine(game, "Content"), "in the way");
        ResolvedNewProject project = service.Validate(new NewProjectRequest
        {
            Path = game, Platform = "raylib", Template = "empty"
        }).Resolved!;

        NewProjectFilesResult result = await service.CreateFilesAsync(project, CancellationToken.None);

        result.Success.ShouldBeFalse();
        File.ReadAllText(Path.Combine(game, "keep.txt")).ShouldBe("mine");
        File.ReadAllText(Path.Combine(game, "Content")).ShouldBe("in the way");
        File.Exists(Path.Combine(game, "Game.csproj")).ShouldBeFalse();
        File.Exists(Path.Combine(game, "Program.cs")).ShouldBeFalse();
    }

    [Fact]
    public async Task CreateFiles_WhenAlreadyCanceled_WritesNothing()
    {
        NewProjectService service = CreateService();
        string target = Path.Combine(_folder, "Canceled");
        ResolvedNewProject project = service.Validate(new NewProjectRequest { Path = target }).Resolved!;
        using CancellationTokenSource cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        NewProjectFilesResult result = await service.CreateFilesAsync(project, cancellation.Token);

        result.Canceled.ShouldBeTrue();
        Directory.Exists(target).ShouldBeFalse();
    }

    #endregion

    #region Execute

    [Fact]
    public async Task ExecuteAsync_WithPlatform_CreatesFilesThenRestoresTheCodeProject()
    {
        NewProjectService service = CreateService();
        RecordingProgress progress = new RecordingProgress();

        NewProjectResult result = await service.ExecuteAsync(
            new NewProjectRequest { Path = Path.Combine(_folder, "Run"), Platform = "monogame", Template = "empty" },
            progress, CancellationToken.None);

        result.Files.Success.ShouldBeTrue();
        result.Restore!.Status.ShouldBe(RestoreStatus.Succeeded);
        _restoreRunner.Restored.ShouldBe(new[] { Path.Combine(_folder, "Run", "Run.csproj") });
        progress.Steps.ShouldBe(new[] { NewProjectStep.CreatingFiles, NewProjectStep.Restoring });
    }

    [Fact]
    public async Task ExecuteAsync_WhenRestoreFails_KeepsTheFilesAndRestoreCanBeRetriedAlone()
    {
        NewProjectService service = CreateService();
        _restoreRunner.NextResult = new RestoreResult { Status = RestoreStatus.Failed, ExitCode = 1, OutputTail = "NU1301 offline" };

        NewProjectResult result = await service.ExecuteAsync(
            new NewProjectRequest { Path = Path.Combine(_folder, "Offline"), Platform = "monogame", Template = "empty" },
            null, CancellationToken.None);

        result.Files.Success.ShouldBeTrue();
        result.Restore!.Status.ShouldBe(RestoreStatus.Failed);
        result.Restore.OutputTail.ShouldBe("NU1301 offline");
        File.Exists(result.Files.CsprojPath).ShouldBeTrue();

        _restoreRunner.NextResult = new RestoreResult { Status = RestoreStatus.Succeeded, ExitCode = 0 };
        RestoreResult retry = await service.RestoreAsync(result.Files.CsprojPath!, null, CancellationToken.None);

        retry.Status.ShouldBe(RestoreStatus.Succeeded);
        _restoreRunner.Restored.Count.ShouldBe(2);
    }

    [Fact]
    public async Task ExecuteAsync_WithoutPlatformOrWithRestoreOff_DoesNotRestore()
    {
        NewProjectService service = CreateService();

        NewProjectResult gumOnly = await service.ExecuteAsync(
            new NewProjectRequest { Path = Path.Combine(_folder, "A"), Template = "empty" }, null, CancellationToken.None);
        NewProjectResult noRestore = await service.ExecuteAsync(
            new NewProjectRequest { Path = Path.Combine(_folder, "B"), Template = "empty", Platform = "kni", Restore = false },
            null, CancellationToken.None);

        gumOnly.Restore.ShouldBeNull();
        noRestore.Restore.ShouldBeNull();
        _restoreRunner.Restored.ShouldBeEmpty();
    }

    [Fact]
    public async Task ExecuteAsync_WithAnInvalidRequest_ReportsTheMessageAndWritesNothing()
    {
        NewProjectService service = CreateService();

        NewProjectResult result = await service.ExecuteAsync(
            new NewProjectRequest { Path = Path.Combine(_folder, "X"), Template = "bogus" }, null, CancellationToken.None);

        result.Files.Success.ShouldBeFalse();
        result.Files.ErrorMessage.ShouldBe("Unknown template 'bogus'. Valid values are: forms, empty.");
        Directory.GetFileSystemEntries(_folder).ShouldBeEmpty();
    }

    #endregion

    private ResolvedNewProject BrokenThemeProject(string game)
    {
        string brokenTheme = Path.Combine(_folder, "BrokenTheme");
        Directory.CreateDirectory(brokenTheme);
        (string csproj, string gumProject) = (Path.Combine(game, "Game.csproj"), Path.Combine(game, "Content", "GumProject", "GumProject.gumj"));
        return new ResolvedNewProject
        {
            Platform = HostPlatform.MonoGame,
            IncludeFormsTemplate = true,
            ThemeDirectory = brokenTheme,
            RootDirectory = game,
            GumProjectPath = gumProject,
            CsprojPath = csproj,
            Name = "Game"
        };
    }

    private class FakeRestoreRunner : IRestoreRunner
    {
        public List<string> Restored { get; } = new List<string>();

        public RestoreResult NextResult { get; set; } = new RestoreResult { Status = RestoreStatus.Succeeded, ExitCode = 0 };

        public Task<RestoreResult> RestoreAsync(string csprojPath, Action<string>? onOutputLine, CancellationToken cancellationToken)
        {
            Restored.Add(csprojPath);
            return Task.FromResult(NextResult);
        }
    }

    private class RecordingProgress : IProgress<NewProjectProgress>
    {
        public List<NewProjectStep> Steps { get; } = new List<NewProjectStep>();

        public void Report(NewProjectProgress value) => Steps.Add(value.Step);
    }
}
