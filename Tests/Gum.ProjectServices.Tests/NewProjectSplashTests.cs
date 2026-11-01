#pragma warning disable xUnit1031 // The helpers create projects synchronously; nothing here can deadlock (no synchronization context).
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Gum.ProjectServices.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp;
using Shouldly;

namespace Gum.ProjectServices.Tests;

/// <summary>
/// Splash-damage sweep of New Project (#7328): the service across the option matrix, every theme on every platform,
/// the generated host's references, paths with spaces, and what is left on disk when a step stops.
/// </summary>
public class NewProjectSplashTests : IDisposable
{
    private readonly string _folder;
    private readonly string _checkout;
    private readonly List<string> _calls = new();
    private readonly SplashCodegen _codegen;

    public NewProjectSplashTests()
    {
        // The space is deliberate: the user's real projects live under "Vic Personal".
        _folder = Path.Combine(Path.GetTempPath(), "Gum Splash 7328 " + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_folder);
        _checkout = GumSourceLocator.Find(AppContext.BaseDirectory)
            ?? throw new InvalidOperationException("These tests run from a Gum checkout.");
        _codegen = new SplashCodegen(_calls);
    }

    public void Dispose()
    {
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, recursive: true);
        }
    }

    private string ThemesRoot => Path.Combine(_checkout, "Tools", "Gum.ProjectServices", "Templates", "FormsThemes");

    private static string[] ThemeNames(string themesRoot) =>
        Directory.GetDirectories(themesRoot).Select(Path.GetFileName).Select(name => name!).OrderBy(name => name).ToArray();

    private NewProjectService CreateService(IRestoreRunner? restore = null, IPackageAddRunner? add = null) =>
        new NewProjectService(
            new FormsThemeLocator(new[] { ThemesRoot }),
            restore ?? new SplashRestore(_calls),
            add ?? new SplashAdd(_calls),
            new[] { _checkout },
            _codegen);

    private NewProjectRequest Request(
        string? platform, string name = "Game", string template = "forms", string? theme = null, bool sourceLinked = false,
        bool restore = false, bool packages = false, bool generate = false, bool xml = false) =>
        new NewProjectRequest
        {
            Path = Path.Combine(_folder, name),
            Platform = platform,
            Template = template,
            Theme = theme,
            SourceLinked = sourceLinked,
            Restore = restore,
            IncludeRecommendedPackages = packages,
            GenerateCode = generate,
            UseXmlFormat = xml,
        };

    private Task<NewProjectResult> Run(NewProjectRequest request, CancellationToken token = default) =>
        CreateService().ExecuteAsync(request, null, token);

    private string Game(string name = "Game") => Path.Combine(_folder, name);

    private static IEnumerable<(string Name, HostPlatform Platform)> Platforms => NewProjectPlatforms.MainNames;

    public static IEnumerable<object[]> PlatformNames() => NewProjectPlatforms.MainNames.Select(entry => new object[] { entry.Name });

    #region Names and codegen (#7338, #7336)

    [Theory]
    [InlineData("CON")]
    [InlineData("prn")]
    [InlineData("Aux")]
    [InlineData("NUL")]
    [InlineData("COM1")]
    [InlineData("com9")]
    [InlineData("LPT1")]
    [InlineData("lpt9")]
    [InlineData("CON.txt")]
    [InlineData("Nul.Game")]
    [InlineData("Game.")]
    [InlineData("Game ")]
    public async Task Execute_ANameNoOperatingSystemShouldBeGiven_IsRefusedOnEveryOsAndWritesNothing(string name)
    {
        foreach (string? platform in new string?[] { null, "monogame" })
        {
            NewProjectValidation validation = CreateService().Validate(Request(platform, name));
            NewProjectResult result = await Run(Request(platform, name));

            validation.Resolved.ShouldBeNull($"platform={platform}");
            validation.Error!.Kind.ShouldBe(NewProjectErrorKind.InvalidProjectName, $"platform={platform}");
            result.Files.Success.ShouldBeFalse($"platform={platform}");
            result.Files.ErrorMessage.ShouldBe(validation.Error.Message, $"platform={platform}");
            Directory.Exists(Game(name)).ShouldBeFalse($"platform={platform}");
        }
    }

    [Theory]
    [InlineData("COM10")]
    [InlineData("Console")]
    [InlineData("Game")]
    public void Validate_ANameThatOnlyLooksReserved_IsAccepted(string name)
    {
        CreateService().Validate(Request("monogame", name)).Resolved.ShouldNotBeNull();
        CreateService().Validate(Request(null, name)).Resolved.ShouldNotBeNull();
    }

    [Fact]
    public void Validate_ANameTooLongForTheFiles_IsRefusedWithTheMessageTheDialogShows()
    {
        NewProjectValidation validation = CreateService().Validate(Request(null, new string('a', 251)));

        validation.Resolved.ShouldBeNull();
        validation.Error!.Kind.ShouldBe(NewProjectErrorKind.InvalidProjectName);
    }

    [Fact]
    public async Task Execute_GenerateCodeWithoutRestore_RunsNoCodegen()
    {
        NewProjectResult result = await Run(Request("monogame", restore: false, generate: true));

        result.Files.Success.ShouldBeTrue(result.Files.ErrorMessage);
        result.Codegen.ShouldBeNull();
        result.CodegenSkipped.ShouldBeTrue("The caller must be told the code was not generated.");
        _calls.ShouldNotContain(call => call.StartsWith("codegen"), "The dialog says code generation needs the restore, so it is skipped.");
    }

    [Fact]
    public async Task Execute_GenerateCodeWithRestore_RunsCodegenAndReportsNothingSkipped()
    {
        NewProjectResult result = await Run(Request("monogame", restore: true, generate: true));

        result.Codegen.ShouldNotBeNull();
        result.CodegenSkipped.ShouldBeFalse();
    }

    #endregion

    #region Option matrix

    [Fact]
    public void Validate_EveryOptionCombination_ResolvesOrIsRefusedForTheDocumentedReason()
    {
        NewProjectService service = CreateService();
        int checkedCount = 0;

        foreach (string? platform in new string?[] { null }.Concat(Platforms.Select(entry => entry.Name)))
        foreach (string template in new[] { "forms", "empty" })
        foreach (bool xml in new[] { false, true })
        foreach (bool sourceLinked in new[] { false, true })
        foreach (bool packages in new[] { false, true })
        foreach (bool generate in new[] { false, true })
        foreach (bool restore in new[] { false, true })
        {
            // Source-linked together with packages is the subject of #7328; the next two are covered by their own tests.
            if (sourceLinked && packages)
            {
                continue;
            }
            if (platform == "kniskia" && !sourceLinked)
            {
                continue;
            }

            string label = $"platform={platform} template={template} xml={xml} linked={sourceLinked} packages={packages} generate={generate} restore={restore}";
            NewProjectValidation validation = service.Validate(Request(platform, template: template, xml: xml,
                sourceLinked: sourceLinked, packages: packages, generate: generate, restore: restore));

            NewProjectErrorKind? expected =
                platform == null && sourceLinked ? NewProjectErrorKind.SourceLinkedNeedsPlatform
                : platform == null && packages ? NewProjectErrorKind.RecommendedPackagesNeedPlatform
                : platform == null && generate ? NewProjectErrorKind.GenerateCodeNeedsPlatform
                : null;

            ((NewProjectErrorKind?)validation.Error?.Kind).ShouldBe(expected, label + " " + validation.Error?.Message);
            (validation.Resolved != null).ShouldBe(expected == null, label);
            checkedCount++;
        }

        checkedCount.ShouldBeGreaterThan(100);
    }

    [Fact]
    public void Validate_GenerateCodeWithTheEmptyTemplate_IsAcceptedAndGeneratesNothing()
    {
        NewProjectValidation validation = CreateService().Validate(Request("monogame", template: "empty", generate: true));

        validation.Resolved.ShouldNotBeNull(validation.Error?.Message);
        Run(Request("monogame", template: "empty", generate: true, restore: true)).GetAwaiter().GetResult();
        _calls.ShouldNotContain(call => call.StartsWith("codegen"));
    }

    [Fact]
    public async Task Execute_ThemeStandard_WritesTheSameFilesAsNoTheme()
    {
        NewProjectResult plain = await Run(Request("monogame", "Plain"));
        NewProjectResult standard = await Run(Request("monogame", "Std", theme: "Standard"));

        plain.Files.Success.ShouldBeTrue(plain.Files.ErrorMessage);
        standard.Files.Success.ShouldBeTrue(standard.Files.ErrorMessage);
        RelativeFiles(Game("Std")).Select(file => file.Replace("Std", "Plain")).Order().ShouldBe(RelativeFiles(Game("Plain")).Order());
    }

    [Fact]
    public async Task Execute_ThemeNameInAnotherCase_WritesTheThemesFonts()
    {
        NewProjectResult result = await Run(Request("raylib", theme: "bUbBlEgUm"));

        result.Files.Success.ShouldBeTrue(result.Files.ErrorMessage);
        string cache = Path.Combine(Game(), "Content", "GumProject", "FontCache");
        Directory.GetFiles(cache).Select(Path.GetFileName).Order()
            .ShouldBe(Directory.GetFiles(Path.Combine(ThemesRoot, "Bubblegum", "FontCache")).Select(Path.GetFileName).Order());
    }

    #endregion

    #region Themes on every platform

    [Fact]
    public async Task Execute_EveryTheme_OnARotatingPlatform_LoadsWithTheThemesFontsAndTheCodeProjectAroundIt()
    {
        string[] themes = ThemeNames(ThemesRoot);
        themes.Length.ShouldBeGreaterThanOrEqualTo(8);

        // A theme's files do not depend on the platform, so each theme takes the next platform in turn: every theme and
        // every platform is covered without the cost of the cross product (each creation copies the theme's fonts).
        string[] allThemes = themes.Append("Standard").ToArray();
        for (int i = 0; i < allThemes.Length; i++)
        {
            string theme = allThemes[i];
            string name = Platforms.ElementAt(i % Platforms.Count()).Name;
            string folderName = $"{theme}_{name}";
            string label = $"theme={theme} platform={name}";
            NewProjectResult result = await Run(Request(name, folderName, theme: theme, sourceLinked: name == "kniskia"));

            result.Files.Success.ShouldBeTrue(label + " " + result.Files.ErrorMessage);
            string gumFolder = Path.Combine(Game(folderName), "Content", "GumProject");
            File.Exists(Path.Combine(gumFolder, "GumProject.gumj")).ShouldBeTrue(label);
            Gum.DataTypes.GumProjectSave project = Gum.DataTypes.GumProjectSave.Load(Path.Combine(gumFolder, "GumProject.gumj"));
            project.Screens.Count.ShouldBe(1, label);
            project.Components.Count.ShouldBeGreaterThan(10, label);

            if (theme != "Standard")
            {
                string[] expectedFonts = Directory.GetFiles(Path.Combine(ThemesRoot, theme, "FontCache")).Select(Path.GetFileName).Order().ToArray()!;
                string[] actualFonts = Directory.GetFiles(Path.Combine(gumFolder, "FontCache")).Select(Path.GetFileName).Order().ToArray()!;
                actualFonts.ShouldBe(expectedFonts, label);
                Directory.GetFiles(Path.Combine(gumFolder, "Fonts")).Length.ShouldBe(
                    Directory.GetFiles(Path.Combine(ThemesRoot, theme, "Fonts")).Length, label);
            }

            string csproj = File.ReadAllText(Path.Combine(Game(folderName), folderName + ".csproj"));
            csproj.ShouldContain(@"Content\**\*.*", Case.Sensitive, label);
            csproj.ShouldContain("CopyToOutputDirectory", Case.Sensitive, label);
            Directory.Delete(Game(folderName), recursive: true);
        }
    }

    [Theory]
    [MemberData(nameof(PlatformNames))]
    public async Task Execute_EveryPlatform_ProducesAWellFormedCsprojWithOneGumReference(string platform)
    {
        NewProjectResult result = await Run(Request(platform, sourceLinked: platform == "kniskia"));

        result.Files.Success.ShouldBeTrue(result.Files.ErrorMessage);
        XDocument document = XDocument.Load(result.Files.CsprojPath!);
        string[] gumReferences = document.Descendants()
            .Where(element => element.Name.LocalName is "PackageReference" or "ProjectReference")
            .Select(element => (string?)element.Attribute("Include") ?? "")
            .Where(include => include.StartsWith("Gum.", StringComparison.OrdinalIgnoreCase) || include.Contains("Gum", StringComparison.Ordinal))
            .ToArray();
        gumReferences.Length.ShouldBe(1, string.Join(" | ", gumReferences));
        document.Descendants("TargetFramework").Single().Value.ShouldStartWith("net");
        result.Files.CsprojPath!.ShouldStartWith(Game());
    }

    #endregion

    #region Source-linked paths

    [Theory]
    [MemberData(nameof(PlatformNames))]
    public async Task Execute_SourceLinkedFromAFolderWithSpaces_ReferencesARuntimeProjectThatResolvesFromTheCsproj(string platform)
    {
        NewProjectResult result = await Run(Request(platform, "My Spaced Game".Replace(" ", "_"), sourceLinked: true));

        result.Files.Success.ShouldBeTrue(result.Files.ErrorMessage);
        XDocument document = XDocument.Load(result.Files.CsprojPath!);
        string include = document.Descendants("ProjectReference").Single().Attribute("Include")!.Value;
        string resolved = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(result.Files.CsprojPath!)!, include.Replace('\\', Path.DirectorySeparatorChar)));
        File.Exists(resolved).ShouldBeTrue(resolved);
        resolved.ShouldStartWith(_checkout.TrimEnd(Path.DirectorySeparatorChar));
    }

    [Fact]
    public async Task Execute_SourceLinkedFromADeeplyNestedFolder_StillResolvesTheRuntimeProject()
    {
        string nested = Path.Combine(_folder, "a b", "c", "d e", "f", "g");
        NewProjectResult result = await CreateService().ExecuteAsync(new NewProjectRequest
        {
            Path = Path.Combine(nested, "Deep"), Platform = "monogame", SourceLinked = true, Restore = false, Template = "empty"
        }, null, CancellationToken.None);

        result.Files.Success.ShouldBeTrue(result.Files.ErrorMessage);
        string include = XDocument.Load(result.Files.CsprojPath!).Descendants("ProjectReference").Single().Attribute("Include")!.Value;
        include.ShouldContain("MonoGameGum.csproj");
        File.Exists(Path.GetFullPath(Path.Combine(Path.GetDirectoryName(result.Files.CsprojPath!)!, include.Replace('\\', Path.DirectorySeparatorChar)))).ShouldBeTrue();
    }

    [Fact]
    public async Task Execute_SourceLinked_NamesTheGumProjectFileTheHostLoads_ForBothFormats()
    {
        foreach (bool xml in new[] { false, true })
        {
            string name = xml ? "LinkedX" : "LinkedJ";
            NewProjectResult result = await Run(Request("monogame", name, sourceLinked: true, xml: xml));

            result.Files.Success.ShouldBeTrue(result.Files.ErrorMessage);
            string host = File.ReadAllText(Path.Combine(Game(name), "Game1.cs"));
            host.ShouldContain(xml ? "GumProject.gumx" : "GumProject.gumj");
            host.ShouldNotContain(xml ? "GumProject.gumj" : "GumProject.gumx");
        }
    }

    #endregion

    #region Host and recommended packages

    [Theory]
    [MemberData(nameof(PlatformNames))]
    public async Task Execute_WithRecommendedPackages_InitializesExactlyThePackagesTheCsprojWasGiven(string platformName)
    {
        HostPlatform platform = NewProjectPlatforms.MainNames.First(entry => entry.Name == platformName).Platform;
        if (platform == HostPlatform.KniSkia)
        {
            return; // Always source-linked, which refuses packages (#7328), and it has none to add anyway.
        }

        NewProjectResult result = await Run(Request(platformName, packages: true));

        result.Files.Success.ShouldBeTrue(result.Files.ErrorMessage);
        string host = File.ReadAllText(Path.Combine(Game(), NewProjectLayout.WritesGame1(platform) ? "Game1.cs" : "Program.cs"));
        IReadOnlyList<string> added = RecommendedPackages.For(platform);
        _calls.Where(call => call.StartsWith("add ")).Select(call => call.Substring(4)).ShouldBe(added);

        host.Contains("GumExpressionService.Initialize()").ShouldBe(added.Contains("Gum.Expressions"));
        host.Contains("KernSmith.Gum.").ShouldBe(added.Any(package => package.StartsWith("KernSmith.")));
    }

    [Fact]
    public async Task Execute_WhenRestoreFailsAfterThePackagesWereAdded_TheHostStillInitializesThem()
    {
        NewProjectService service = CreateService(restore: new FailingRestore());

        NewProjectResult result = await service.ExecuteAsync(
            Request("monogame", packages: true, restore: true), null, CancellationToken.None);

        result.Restore!.Status.ShouldBe(RestoreStatus.Failed);
        File.ReadAllText(Path.Combine(Game(), "Game1.cs")).ShouldContain("GumExpressionService.Initialize()");
        File.Exists(Path.Combine(Game(), "Game.csproj")).ShouldBeTrue();
    }

    [Fact]
    public async Task Execute_WhenCodegenFailsAfterThePackagesWereAdded_KeepsThePackageInitAndTheRuntimeLoadedScreen()
    {
        _codegen.Fail = true;

        NewProjectResult result = await Run(Request("monogame", packages: true, restore: true, generate: true));

        result.Codegen!.Success.ShouldBeFalse();
        string host = File.ReadAllText(Path.Combine(Game(), "Game1.cs"));
        host.ShouldContain("GumExpressionService.Initialize()");
        host.ShouldContain("ObjectFinder.Self.GetElementSave(\"DemoScreenGum\")");
        host.ShouldNotContain("new DemoScreenGum()");
    }

    [Fact]
    public async Task Execute_WhenCodegenThrows_ReportsAFailedCodegenInsteadOfThrowing_AndKeepsTheFiles()
    {
        _codegen.Throw = true;

        NewProjectResult result = await Run(Request("raylib", restore: true, generate: true));

        result.Codegen!.Success.ShouldBeFalse();
        result.Codegen.OutputTail.ShouldContain("boom");
        File.Exists(Path.Combine(Game(), "Game.csproj")).ShouldBeTrue();
    }

    [Fact]
    public async Task Execute_GenerateCode_RunsTheCodegenOnTheGumProjectItWrote_AfterTheRestore()
    {
        NewProjectResult result = await Run(Request("kni", restore: true, generate: true, xml: true));

        result.Codegen!.Success.ShouldBeTrue();
        _calls.Select(call => call.Split(' ')[0]).ToArray().ShouldBe(new[] { "restore", "codegen" });
        _calls.Last().ShouldBe("codegen GumProject.gumx");
        File.ReadAllText(Path.Combine(Game(), "Game1.cs")).ShouldContain("new DemoScreenGum()");
    }

    [Fact]
    public async Task Execute_Canceled_WhileRestoring_KeepsTheFilesAndSkipsCodegen()
    {
        using CancellationTokenSource source = new CancellationTokenSource();
        NewProjectService service = CreateService(restore: new CancelingRestore(source));

        NewProjectResult result = await service.ExecuteAsync(Request("monogame", restore: true, generate: true), null, source.Token);

        result.Restore!.Status.ShouldBe(RestoreStatus.Canceled);
        result.Codegen.ShouldBeNull();
        File.Exists(Path.Combine(Game(), "Game.csproj")).ShouldBeTrue();
        File.Exists(Path.Combine(Game(), "Content", "GumProject", "GumProject.gumj")).ShouldBeTrue();
    }

    [Fact]
    public async Task Execute_Canceled_WhileAddingPackages_KeepsTheFilesAndSkipsTheRestore()
    {
        using CancellationTokenSource source = new CancellationTokenSource();
        NewProjectService service = CreateService(add: new CancelingAdd(source));

        NewProjectResult result = await service.ExecuteAsync(Request("monogame", packages: true, restore: true), null, source.Token);

        result.PackageAdd!.Status.ShouldBe(RestoreStatus.Canceled);
        result.Restore.ShouldBeNull();
        _calls.ShouldNotContain(call => call.StartsWith("restore"));
        File.Exists(Path.Combine(Game(), "Game.csproj")).ShouldBeTrue();
    }

    [Fact]
    public async Task Execute_CanceledBeforeAnythingRuns_LeavesTheTargetFolderUntouched()
    {
        using CancellationTokenSource source = new CancellationTokenSource();
        source.Cancel();

        NewProjectResult result = await Run(Request("monogame", restore: true), source.Token);

        result.Files.Success.ShouldBeFalse();
        Directory.Exists(Game()).ShouldBeFalse();
    }

    #endregion

    #region Names

    [Theory]
    [InlineData("Gum-Game")]
    [InlineData("a--b")]
    [InlineData("X.1")]
    [InlineData("namespace")]
    [InlineData("default")]
    [InlineData("_")]
    [InlineData("My.Game.Name")]
    public async Task Execute_AcceptedPlatformNames_GenerateAValidNamespace(string name)
    {
        // The namespace comes from the name alone, so every platform's host is checked with a name in turn.
        foreach ((string platformName, HostPlatform platform) in Platforms.Where((_, index) => index == name.Sum(character => character) % Platforms.Count()))
        {
            // The folder name is the project name, so use a sub-folder with exactly the name.
            string path = Path.Combine(_folder, platformName, name);
            NewProjectResult result = await CreateService().ExecuteAsync(new NewProjectRequest
            {
                Path = path, Platform = platformName, Restore = false, Template = "empty", SourceLinked = platformName == "kniskia"
            }, null, CancellationToken.None);

            result.Files.Success.ShouldBeTrue($"{platformName} {name}: {result.Files.ErrorMessage}");
            string host = File.ReadAllText(Path.Combine(path, NewProjectLayout.WritesGame1(platform) ? "Game1.cs" : "Program.cs"));
            MatchCollection namespaces = Regex.Matches(host, @"^namespace\s+(\S+?)\s*;", RegexOptions.Multiline);
            foreach (Match ns in namespaces)
            {
                foreach (string segment in ns.Groups[1].Value.Split('.'))
                {
                    SyntaxFacts.IsValidIdentifier(segment.TrimStart('@')).ShouldBeTrue($"{platformName} {name} -> {ns.Groups[1].Value}");
                }
            }
        }
    }

    [Theory]
    [InlineData("Program")]
    [InlineData("MonoGameGum")]
    [InlineData("GumCommon")]
    public void Validate_PlatformNamesThatClashWithTheRuntime_AreRefusedBeforeAnythingIsWritten(string name)
    {
        NewProjectValidation validation = CreateService().Validate(Request("monogame", name));

        validation.Error.ShouldNotBeNull();
        validation.Error!.Kind.ShouldBe(NewProjectErrorKind.InvalidProjectName);
        Directory.Exists(Game(name)).ShouldBeFalse();
    }

    [Theory]
    [InlineData("My Game")]
    [InlineData("1Game")]
    [InlineData("Jeué")]
    [InlineData("Game!")]
    [InlineData("Game$")]
    public void Validate_PlatformNamesThatAreNotProjectNames_AreRefusedWithAMessageNamingTheName(string name)
    {
        NewProjectValidation validation = CreateService().Validate(Request("raylib", name));

        validation.Error.ShouldNotBeNull();
        validation.Error!.Message.ShouldContain(name);
    }

    #endregion

    #region Folders

    [Fact]
    public async Task Execute_ExistingEmptyTargetFolder_IsUsedAndKept_WhenCreationFails()
    {
        Directory.CreateDirectory(Game("Empty"));
        // A directory named like the csproj makes the write fail after the folder already existed.
        Directory.CreateDirectory(Path.Combine(Game("Empty"), "Empty.csproj"));

        NewProjectResult result = await Run(Request("monogame", "Empty"));

        result.Files.Success.ShouldBeFalse();
        Directory.Exists(Game("Empty")).ShouldBeTrue();
    }

    [Fact]
    public async Task Execute_GumOnlyIntoAnExistingEmptyFolder_CreatesTheProjectInIt()
    {
        Directory.CreateDirectory(Game("Existing"));

        NewProjectResult result = await Run(Request(null, "Existing"));

        result.Files.Success.ShouldBeTrue(result.Files.ErrorMessage);
        File.Exists(Path.Combine(Game("Existing"), "Existing.gumj")).ShouldBeTrue();
    }

    [Fact]
    public async Task Execute_GumOnly_WritesNoCodeProjectFiles()
    {
        NewProjectResult result = await Run(Request(null, "OnlyGum", generate: false));

        result.Files.Success.ShouldBeTrue(result.Files.ErrorMessage);
        RelativeFiles(Game("OnlyGum")).ShouldNotContain(file => file.EndsWith(".csproj") || file.EndsWith(".cs") || file.EndsWith(".slnx"));
        result.Files.CsprojPath.ShouldBeNull();
        _calls.ShouldBeEmpty();
    }

    #endregion

    private static string[] RelativeFiles(string root) =>
        Directory.GetFiles(root, "*", SearchOption.AllDirectories).Select(file => Path.GetRelativePath(root, file)).Order().ToArray();

    #region Fakes

    private class SplashRestore : IRestoreRunner
    {
        private readonly List<string> _calls;

        public SplashRestore(List<string> calls) => _calls = calls;

        public Task<RestoreResult> RestoreAsync(string csprojPath, Action<string>? onOutputLine, CancellationToken cancellationToken)
        {
            _calls.Add("restore " + Path.GetFileName(csprojPath));
            return Task.FromResult(new RestoreResult { Status = RestoreStatus.Succeeded, ExitCode = 0 });
        }
    }

    private class FailingRestore : IRestoreRunner
    {
        public Task<RestoreResult> RestoreAsync(string csprojPath, Action<string>? onOutputLine, CancellationToken cancellationToken) =>
            Task.FromResult(new RestoreResult { Status = RestoreStatus.Failed, ExitCode = 1, OutputTail = "offline" });
    }

    private class CancelingRestore : IRestoreRunner
    {
        private readonly CancellationTokenSource _source;

        public CancelingRestore(CancellationTokenSource source) => _source = source;

        public Task<RestoreResult> RestoreAsync(string csprojPath, Action<string>? onOutputLine, CancellationToken cancellationToken)
        {
            _source.Cancel();
            return Task.FromResult(new RestoreResult { Status = RestoreStatus.Canceled });
        }
    }

    private class SplashAdd : IPackageAddRunner
    {
        private readonly List<string> _calls;

        public SplashAdd(List<string> calls) => _calls = calls;

        public Task<RestoreResult> AddAsync(string csprojPath, string packageId, Action<string>? onOutputLine, CancellationToken cancellationToken)
        {
            _calls.Add("add " + packageId);
            return Task.FromResult(new RestoreResult { Status = RestoreStatus.Succeeded, ExitCode = 0 });
        }
    }

    private class CancelingAdd : IPackageAddRunner
    {
        private readonly CancellationTokenSource _source;

        public CancelingAdd(CancellationTokenSource source) => _source = source;

        public Task<RestoreResult> AddAsync(string csprojPath, string packageId, Action<string>? onOutputLine, CancellationToken cancellationToken)
        {
            _source.Cancel();
            return Task.FromResult(new RestoreResult { Status = RestoreStatus.Canceled });
        }
    }

    private class SplashCodegen : IHeadlessCodegenRunner
    {
        private readonly List<string> _calls;

        public SplashCodegen(List<string> calls) => _calls = calls;

        public bool Fail { get; set; }

        public bool Throw { get; set; }

        public int Run(string projectPath, IReadOnlyList<string>? elementNames, bool prune, TextWriter output, TextWriter error)
        {
            _calls.Add("codegen " + Path.GetFileName(projectPath));
            if (Throw)
            {
                throw new InvalidOperationException("boom");
            }
            if (Fail)
            {
                error.WriteLine("boom");
                return 1;
            }
            return 0;
        }
    }

    #endregion
}
