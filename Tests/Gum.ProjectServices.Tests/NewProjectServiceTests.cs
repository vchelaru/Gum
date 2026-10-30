using Shouldly;

namespace Gum.ProjectServices.Tests;

/// <summary>
/// The shared layer behind <c>gumcli new</c> and the New Project dialog (#7208).
/// </summary>
public class NewProjectServiceTests : BaseTestClass
{
    private readonly string _folder;
    private readonly string _checkout;
    private readonly FakeRestoreRunner _restoreRunner = new FakeRestoreRunner();

    public NewProjectServiceTests()
    {
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

    [Fact]
    public async Task Execute_WithAPlatformAndXmlFormat_WritesAGumxProject()
    {
        NewProjectService service = CreateService();

        NewProjectResult result = await service.ExecuteAsync(new NewProjectRequest
        {
            Path = Path.Combine(_folder, "XmlGame"),
            Platform = "monogame",
            Template = "empty",
            UseXmlFormat = true,
            Restore = false
        }, null, CancellationToken.None);

        result.Files.Success.ShouldBeTrue(result.Files.ErrorMessage);
        Path.GetExtension(result.Files.GumProjectPath).ShouldBe(".gumx");
        File.Exists(result.Files.GumProjectPath).ShouldBeTrue();
    }

    [Fact]
    public async Task Execute_ForEveryPlatformAndFormat_TheGeneratedCodeLoadsTheGumFileItWrote()
    {
        NewProjectService service = CreateService();

        foreach ((string name, HostPlatform _) in NewProjectPlatforms.MainNames)
        {
            foreach (bool xml in new[] { false, true })
            {
                string chosen = xml ? "GumProject.gumx" : "GumProject.gumj";
                string other = xml ? "GumProject.gumj" : "GumProject.gumx";
                string game = Path.Combine(_folder, $"{name}_{(xml ? "Xml" : "Json")}");

                NewProjectResult result = await service.ExecuteAsync(new NewProjectRequest
                {
                    Path = game, Platform = name, Template = "empty", UseXmlFormat = xml, Restore = false
                }, null, CancellationToken.None);

                result.Files.Success.ShouldBeTrue($"{name}: {result.Files.ErrorMessage}");
                Path.GetFileName(result.Files.GumProjectPath).ShouldBe(chosen, name);
                File.Exists(Path.Combine(game, "Content", "GumProject", other)).ShouldBeFalse(name);
                string code = string.Concat(Directory.GetFiles(game, "*.cs").Select(File.ReadAllText));
                code.ShouldContain(chosen, customMessage: name);
                code.ShouldNotContain(other, customMessage: name);
            }
        }
    }


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

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Validate_WithAPlatformAndAGumProjectOfEitherFormatAlreadyThere_IsRefused(bool existingIsXml)
    {
        NewProjectService service = CreateService();
        string game = Path.Combine(_folder, "Existing");
        await service.ExecuteAsync(new NewProjectRequest
        {
            Path = game, Platform = "raylib", Template = "empty", UseXmlFormat = existingIsXml, Restore = false
        }, null, CancellationToken.None);
        File.Delete(Path.Combine(game, "Existing.csproj"));

        NewProjectValidation validation = service.Validate(new NewProjectRequest
        {
            Path = game, Platform = "raylib", Template = "empty", UseXmlFormat = !existingIsXml
        });

        validation.Error.ShouldNotBeNull();
        validation.Error.Kind.ShouldBe(NewProjectErrorKind.ProjectAlreadyExists);
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

    [Theory]
    [InlineData(null, false)]
    [InlineData(null, true)]
    [InlineData("monogame", false)]
    [InlineData("monogame", true)]
    public void Validate_PathWithATrailingSeparator_ResolvesLikeThePathWithout(string? platform, bool altSeparator)
    {
        NewProjectService service = CreateService();
        string bare = Path.Combine(_folder, "Trailing");
        char separator = altSeparator ? Path.AltDirectorySeparatorChar : Path.DirectorySeparatorChar;

        NewProjectValidation expected = service.Validate(new NewProjectRequest { Path = bare, Platform = platform, Template = "empty" });
        NewProjectValidation actual = service.Validate(new NewProjectRequest { Path = bare + separator, Platform = platform, Template = "empty" });

        actual.IsValid.ShouldBeTrue(actual.Error?.Message);
        actual.Resolved!.Name.ShouldBe("Trailing");
        actual.Resolved.GumProjectPath.ShouldBe(expected.Resolved!.GumProjectPath);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("monogame")]
    public void Validate_PathWithSeveralTrailingSeparators_ResolvesLikeThePathWithout(string? platform)
    {
        NewProjectService service = CreateService();
        string bare = Path.Combine(_folder, "Trailing");
        string separators = new string(Path.DirectorySeparatorChar, 2) + Path.AltDirectorySeparatorChar;

        NewProjectValidation expected = service.Validate(new NewProjectRequest { Path = bare, Platform = platform, Template = "empty" });
        NewProjectValidation actual = service.Validate(new NewProjectRequest { Path = bare + separators, Platform = platform, Template = "empty" });

        actual.IsValid.ShouldBeTrue(actual.Error?.Message);
        actual.Resolved!.Name.ShouldBe("Trailing");
        actual.Resolved.RootDirectory.ShouldBe(expected.Resolved!.RootDirectory);
        actual.Resolved.GumProjectPath.ShouldBe(expected.Resolved.GumProjectPath);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("monogame")]
    public void Validate_PathEndingInDotDot_NamesTheProjectAfterTheFolderItResolvesTo(string? platform)
    {
        NewProjectService service = CreateService();

        NewProjectValidation validation = service.Validate(new NewProjectRequest
        {
            Path = Path.Combine(_folder, "Dotted", "Child", ".."),
            Platform = platform,
            Template = "empty"
        });

        validation.IsValid.ShouldBeTrue(validation.Error?.Message);
        validation.Resolved!.Name.ShouldBe("Dotted");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("monogame")]
    public void Validate_FileSystemRoot_IsRefusedAsInvalidName(string? platform)
    {
        NewProjectService service = CreateService();
        string root = Path.GetPathRoot(_folder)!;

        NewProjectValidation validation = service.Validate(new NewProjectRequest { Path = root, Platform = platform, Template = "empty" });

        validation.Error!.Kind.ShouldBe(NewProjectErrorKind.InvalidProjectName);
        validation.Error.Message.ShouldContain("no folder name");
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
    public async Task CreateFiles_WhenCreationFailsLate_KeepsPlannedFilesThatWereThereBefore()
    {
        NewProjectService service = CreateService();
        string game = Path.Combine(_folder, "Game");
        string gumFolder = Path.Combine(game, "Content", "GumProject");
        Directory.CreateDirectory(gumFolder);
        // Planned files that already exist, one in the game folder and one in the Gum project folder.
        File.WriteAllText(Path.Combine(game, "Program.cs"), "mine");
        File.WriteAllText(Path.Combine(gumFolder, "ExampleSpriteFrame.png"), "mine");
        // A folder where the code generation settings file goes fails the creation after the other files are written.
        Directory.CreateDirectory(Path.Combine(gumFolder, "ProjectCodeSettings.codsj"));
        ResolvedNewProject project = service.Validate(new NewProjectRequest
        {
            Path = game, Platform = "raylib", Template = "empty"
        }).Resolved!;

        NewProjectFilesResult result = await service.CreateFilesAsync(project, CancellationToken.None);

        result.Success.ShouldBeFalse();
        File.Exists(Path.Combine(game, "Program.cs")).ShouldBeTrue();
        File.Exists(Path.Combine(gumFolder, "ExampleSpriteFrame.png")).ShouldBeTrue();
        File.Exists(Path.Combine(game, "Game.csproj")).ShouldBeFalse();
        File.Exists(Path.Combine(gumFolder, "GumProject.gumj")).ShouldBeFalse();
    }

    [Fact]
    public async Task CreateFiles_WhenCreationFailsAfterTheSlnxIsWritten_RemovesTheSlnx()
    {
        NewProjectService service = CreateService();
        string game = Path.Combine(_folder, "Game");
        string gumFolder = Path.Combine(game, "Content", "GumProject");
        Directory.CreateDirectory(gumFolder);
        File.WriteAllText(Path.Combine(game, "keep.txt"), "mine");
        // A folder where the code generation settings file goes fails the creation after the host files are written.
        Directory.CreateDirectory(Path.Combine(gumFolder, "ProjectCodeSettings.codsj"));
        ResolvedNewProject project = service.Validate(new NewProjectRequest
        {
            Path = game, Platform = "raylib", Template = "empty"
        }).Resolved!;

        NewProjectFilesResult result = await service.CreateFilesAsync(project, CancellationToken.None);

        result.Success.ShouldBeFalse();
        File.Exists(Path.Combine(game, "Game.slnx")).ShouldBeFalse();
        File.Exists(Path.Combine(game, "Game.csproj")).ShouldBeFalse();
        File.ReadAllText(Path.Combine(game, "keep.txt")).ShouldBe("mine");
    }

    [Fact]
    public async Task CreateFiles_WhenASlnxAppearedAfterValidation_RefusesAndKeepsIt()
    {
        NewProjectService service = CreateService();
        string game = Path.Combine(_folder, "Game");
        ResolvedNewProject project = service.Validate(new NewProjectRequest
        {
            Path = game, Platform = "raylib", Template = "empty"
        }).Resolved!;
        Directory.CreateDirectory(game);
        string slnx = Path.Combine(game, "Game.slnx");
        File.WriteAllText(slnx, "mine");

        NewProjectFilesResult result = await service.CreateFilesAsync(project, CancellationToken.None);

        result.Success.ShouldBeFalse();
        File.ReadAllText(slnx).ShouldBe("mine");
        File.Exists(Path.Combine(game, "Game.csproj")).ShouldBeFalse();
    }

    [Fact]
    public void Validate_WithAPlatformAndASlnxAlreadyThere_IsRefusedAsAnExistingProject()
    {
        NewProjectService service = CreateService();
        string game = Path.Combine(_folder, "Game");
        Directory.CreateDirectory(game);
        File.WriteAllText(Path.Combine(game, "Game.slnx"), "mine");

        NewProjectValidation validation = service.Validate(new NewProjectRequest
        {
            Path = game, Platform = "raylib", Template = "empty"
        });

        validation.Error!.Kind.ShouldBe(NewProjectErrorKind.ProjectAlreadyExists);
        validation.Error.Message.ShouldContain("Game.slnx");
    }

    [Fact]
    public async Task Execute_WithoutAPlatform_WritesNoSolutionFile()
    {
        NewProjectService service = CreateService();
        string root = Path.Combine(_folder, "GumOnly");

        NewProjectResult result = await service.ExecuteAsync(
            new NewProjectRequest { Path = root, Template = "empty" }, null, CancellationToken.None);

        result.Files.Success.ShouldBeTrue(result.Files.ErrorMessage);
        Directory.GetFiles(root, "*.sln*", SearchOption.AllDirectories).ShouldBeEmpty();
    }

    [Fact]
    public async Task ExecuteAsync_WhenRestoreCannotStart_KeepsTheFilesAndSaysWhichStepStopped()
    {
        NewProjectService service = CreateService();
        _restoreRunner.NextResult = new RestoreResult { Status = RestoreStatus.ProcessNotFound, ErrorMessage = "no dotnet" };

        NewProjectResult result = await service.ExecuteAsync(
            new NewProjectRequest { Path = Path.Combine(_folder, "NoSdk"), Platform = "kni", Template = "empty" },
            null, CancellationToken.None);

        result.Files.Success.ShouldBeTrue();
        result.Restore!.Status.ShouldBe(RestoreStatus.ProcessNotFound);
        result.Restore.ErrorMessage.ShouldBe("no dotnet");
        File.Exists(result.Files.CsprojPath).ShouldBeTrue();
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

    #region Edge cases

    [Theory]
    [InlineData("FORMS", null, "MonoGame")]
    [InlineData("Empty", null, "KNISKIA")]
    [InlineData("forms", "STANDARD", "Silk.Net")]
    public void Validate_TemplatePlatformAndStandardThemeNames_IgnoreCase(string template, string? theme, string platform)
    {
        NewProjectService service = CreateService();

        NewProjectValidation validation = service.Validate(new NewProjectRequest
        {
            Path = Path.Combine(_folder, "Cased"),
            Template = template,
            Theme = theme,
            Platform = platform
        });

        validation.IsValid.ShouldBeTrue(validation.Error?.Message);
    }

    [Fact]
    public void Validate_EveryAcceptedPlatformName_ResolvesToItsPlatform()
    {
        NewProjectService service = CreateService();

        foreach ((string name, HostPlatform platform) in NewProjectPlatforms.All)
        {
            NewProjectValidation validation = service.Validate(new NewProjectRequest
            {
                Path = Path.Combine(_folder, "P" + name.Replace('.', '_')),
                Platform = name,
                Template = "empty"
            });

            validation.IsValid.ShouldBeTrue($"{name}: {validation.Error?.Message}");
            validation.Resolved!.Platform.ShouldBe(platform);
        }
    }

    [Fact]
    public async Task Execute_IntoAFolderWithOtherFiles_KeepsThem()
    {
        NewProjectService service = CreateService();
        string root = Path.Combine(_folder, "Existing");
        Directory.CreateDirectory(root);
        string other = Path.Combine(root, "notes.txt");
        File.WriteAllText(other, "keep me");

        NewProjectResult result = await service.ExecuteAsync(
            new NewProjectRequest { Path = root, Template = "empty" }, null, CancellationToken.None);

        result.Files.Success.ShouldBeTrue(result.Files.ErrorMessage);
        File.ReadAllText(other).ShouldBe("keep me");
        File.Exists(Path.Combine(root, "Existing.gumj")).ShouldBeTrue();
    }

    #endregion

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
