using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Shouldly;

namespace Gum.ProjectServices.Tests;

/// <summary>
/// Unusual inputs and states for the shared creation layer behind <c>gumcli new</c> (#7208).
/// </summary>
public class NewProjectEdgeCaseTests : BaseTestClass
{
    private readonly string _folder;
    private readonly string _checkout;

    public NewProjectEdgeCaseTests()
    {
        _folder = Path.Combine(Path.GetTempPath(), "GumNewProjectEdge_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_folder);
        _checkout = GumSourceLocator.Find(AppContext.BaseDirectory)
            ?? throw new InvalidOperationException("These tests run from a Gum checkout.");
    }

    public override void Dispose()
    {
        base.Dispose();
        MakeWritable(_folder);
        Directory.Delete(_folder, recursive: true);
    }

    private static void MakeWritable(string folder)
    {
        if (!OperatingSystem.IsWindows())
        {
            foreach (string directory in Directory.GetDirectories(folder, "*", SearchOption.AllDirectories))
            {
                File.SetUnixFileMode(directory, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
            }

            File.SetUnixFileMode(folder, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        }
    }

    private NewProjectService CreateService(params string[] gumSourceSearchDirectories)
    {
        FormsThemeLocator locator = new FormsThemeLocator(new[]
        {
            Path.Combine(_checkout, "Tools", "Gum.ProjectServices", "Templates", "FormsThemes")
        });
        return new NewProjectService(locator, new NoRestoreRunner(), gumSourceSearchDirectories.Length == 0
            ? new[] { _checkout }
            : gumSourceSearchDirectories);
    }

    private static NewProjectRequest Request(string path, string? platform = null) =>
        new NewProjectRequest
        {
            Path = path, Platform = platform, Template = "empty", Restore = false,
            // KNI with Skia is not on NuGet, so it is always source-linked.
            SourceLinked = platform == "kniskia"
        };

    private static List<string> RelativeFiles(string root) =>
        Directory.GetFiles(root, "*", SearchOption.AllDirectories)
            .Select(path => Path.GetRelativePath(root, path).Replace('\\', '/'))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToList();

    #region Names

    public static IEnumerable<object[]> HostPlatformsAndRiskyNames()
    {
        // Names the generated code uses as a simple type name, names that clash with its namespaces or with
        // the Program class and the game and args variables the top-level statements create, contextual keywords,
        // a dotted name, and a safe control.
        string[] names =
        {
            "Game", "Button", "Color", "Game1", "Gum", "Microsoft", "System", "Vector2", "Texture2D",
            "GumService", "GameTime", "GraphicsDeviceManager", "DateTime", "SkiaRenderer", "SKColors",
            "SkiaRenderTarget2D", "My.Game", "MyGame", "game", "args", "button", "Content", "Window", "GraphicsDevice",
            "var", "global", "record", "file", "dynamic", "async"
        };
        foreach (string platform in new[] { "monogame", "kni", "kniskia" })
        {
            foreach (string name in names)
            {
                yield return new object[] { platform, name };
            }
        }
    }

    [Theory]
    [MemberData(nameof(HostPlatformsAndRiskyNames))]
    public async Task Execute_XnaHostWithAProjectNamedLikeATypeItUses_GeneratesCodeThatCompiles(string platform, string name)
    {
        NewProjectService service = CreateService();
        NewProjectResult result = await service.ExecuteAsync(Request(Path.Combine(_folder, name), platform), null, CancellationToken.None);
        result.Files.Success.ShouldBeTrue(result.Files.ErrorMessage);

        CSharpCompilation compilation = CSharpCompilation.Create(
            "Host",
            new[]
            {
                CSharpSyntaxTree.ParseText(XnaStubs),
                CSharpSyntaxTree.ParseText(File.ReadAllText(Path.Combine(_folder, name, "Game1.cs"))),
                CSharpSyntaxTree.ParseText(File.ReadAllText(Path.Combine(_folder, name, "Program.cs")))
            },
            ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator)
                .Select(path => MetadataReference.CreateFromFile(path)),
            new CSharpCompilationOptions(OutputKind.ConsoleApplication, nullableContextOptions: NullableContextOptions.Disable));
        List<string> errors = compilation.GetDiagnostics()
            .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .Select(diagnostic => diagnostic.ToString())
            .ToList();

        errors.ShouldBeEmpty(string.Join(Environment.NewLine, errors));
    }

    // Small stand-ins for the Xna/KNI, Gum and Skia types the Game1/Program templates use.
    private const string XnaStubs = @"
namespace Microsoft.Xna.Framework
{
    public class GameTime { }
    public struct Color { public static Color CornflowerBlue; }
    public class GameWindow { public bool AllowUserResizing { get; set; } }
    public class ContentManager { public string RootDirectory { get; set; } }
    public class PresentationParameters { public int BackBufferWidth { get; set; } public int BackBufferHeight { get; set; } }
    public class GraphicsDevice
    {
        public PresentationParameters PresentationParameters { get; } = new PresentationParameters();
        public void Clear(Color color) { }
        public void SetRenderTarget(object target) { }
    }
    public class GraphicsDeviceManager { public GraphicsDeviceManager(Game game) { } }
    public class Game : System.IDisposable
    {
        public ContentManager Content { get; } = new ContentManager();
        public GameWindow Window { get; } = new GameWindow();
        public bool IsMouseVisible { get; set; }
        public GraphicsDevice GraphicsDevice { get; } = new GraphicsDevice();
        protected virtual void Initialize() { }
        protected virtual void Update(GameTime gameTime) { }
        protected virtual void Draw(GameTime gameTime) { }
        protected virtual void UnloadContent() { }
        public void Run() { }
        public void Dispose() { }
    }
}
namespace SkiaSharp
{
    public struct SKColor { }
    public static class SKColors { public static SKColor Transparent; }
    public class SKCanvas { public void Clear(SKColor color) { } }
}
namespace SkiaGameRendering
{
    public class SkiaTexture { public int Width { get; set; } public int Height { get; set; } }
    public class SkiaRenderTarget2D : System.IDisposable
    {
        public SkiaRenderTarget2D(Microsoft.Xna.Framework.GraphicsDevice device, int width, int height) { }
        public SkiaTexture Texture { get; } = new SkiaTexture();
        public SkiaSharp.SKCanvas Canvas { get; } = new SkiaSharp.SKCanvas();
        public void Begin() { }
        public void End() { }
        public void Dispose() { }
    }
    public static class SkiaRenderer
    {
        public static bool IsInitialized { get; }
        public static bool IsReady { get; }
        public static void Initialize(Microsoft.Xna.Framework.GraphicsDevice device) { }
        public static void Dispose() { }
    }
}
namespace Gum
{
    public class GumService
    {
        public static GumService Default { get; } = new GumService();
        public void Initialize(Microsoft.Xna.Framework.Game game, string project) { }
        public void Update(Microsoft.Xna.Framework.GameTime gameTime) { }
        public void Draw() { }
        public void Draw(SkiaSharp.SKCanvas canvas) { }
        public void HandleResize(int width, int height) { }
    }
}
namespace Gum.Forms.Controls
{
    public class Button
    {
        public string Text { get; set; }
        public event System.EventHandler Click;
        public void AddToRoot() { }
    }
}";

    [Theory]
    [InlineData("monogame")]
    [InlineData("kni")]
    [InlineData("kniskia")]
    [InlineData("raylib")]
    [InlineData("stride")]
    [InlineData("silknet")]
    public async Task Validate_PlatformNameEqualToAPackageTheHostReferences_IsRefused(string platform)
    {
        NewProjectService service = CreateService();
        NewProjectResult sample = await service.ExecuteAsync(Request(Path.Combine(_folder, "Sample"), platform), null, CancellationToken.None);
        sample.Files.Success.ShouldBeTrue(sample.Files.ErrorMessage);
        List<string> packages = XDocument.Load(sample.Files.CsprojPath!)
            .Descendants("PackageReference").Select(element => element.Attribute("Include")!.Value).ToList();
        packages.ShouldNotBeEmpty();

        // A project named like a package it references fails to restore with NU1108 (a cycle).
        foreach (string package in packages)
        {
            NewProjectValidation validation = service.Validate(Request(Path.Combine(_folder, package), platform));

            validation.IsValid.ShouldBeFalse(package);
            validation.Error!.Kind.ShouldBe(NewProjectErrorKind.InvalidProjectName);
            validation.Error.Message.ShouldContain(package);
        }
    }

    [Theory]
    [InlineData("monogame")]
    [InlineData("kni")]
    [InlineData("kniskia")]
    [InlineData("raylib")]
    [InlineData("stride")]
    [InlineData("silknet")]
    public void Validate_PlatformNameProgram_IsRefusedWithTheReason(string platform)
    {
        NewProjectService service = CreateService();

        NewProjectValidation validation = service.Validate(Request(Path.Combine(_folder, "Program"), platform));

        validation.Error!.Kind.ShouldBe(NewProjectErrorKind.InvalidProjectName);
        validation.Error.Message.ShouldContain("namespace");
    }

    [Fact]
    public void Validate_PlatformNameProgramInAnotherCase_IsAccepted()
    {
        NewProjectService service = CreateService();

        NewProjectValidation validation = service.Validate(Request(Path.Combine(_folder, "program"), "raylib"));

        validation.IsValid.ShouldBeTrue(validation.Error?.Message);
    }

    [Fact]
    public void Validate_PlatformNameEqualToAPackageInDifferentCase_IsRefused()
    {
        NewProjectService service = CreateService();

        NewProjectValidation validation = service.Validate(Request(Path.Combine(_folder, "gum.RAYLIB"), "raylib"));

        validation.Error!.Kind.ShouldBe(NewProjectErrorKind.InvalidProjectName);
    }

    [Fact]
    public void Validate_PlatformNameEqualToAPackageOnlyAnotherPlatformReferences_IsAccepted()
    {
        NewProjectService service = CreateService();

        NewProjectValidation validation = service.Validate(Request(Path.Combine(_folder, "Gum.MonoGame"), "raylib"));

        validation.IsValid.ShouldBeTrue(validation.Error?.Message);
    }

    [Theory]
    [InlineData("My Game")]
    [InlineData("My.Game.v2")]
    [InlineData("Jeu é")]
    [InlineData("日本語")]
    public async Task Execute_GumOnlyNameWithSpacesDotsOrUnicode_CreatesTheProject(string name)
    {
        NewProjectService service = CreateService();
        NewProjectRequest request = Request(Path.Combine(_folder, name));

        NewProjectResult result = await service.ExecuteAsync(request, null, CancellationToken.None);

        result.Files.Success.ShouldBeTrue(result.Files.ErrorMessage);
        File.Exists(Path.Combine(_folder, name, name + ".gumj")).ShouldBeTrue();
    }

    [Theory]
    [InlineData("My Game")]
    [InlineData("Jeu_é")]
    [InlineData("1Game")]
    public void Validate_PlatformNameThatIsNotAnIdentifierLikeName_IsRefusedAsInvalidName(string name)
    {
        NewProjectService service = CreateService();

        NewProjectValidation validation = service.Validate(Request(Path.Combine(_folder, name), "raylib"));

        validation.Error!.Kind.ShouldBe(NewProjectErrorKind.InvalidProjectName);
        validation.Error.Message.ShouldContain(name);
    }

    [Theory]
    [InlineData("My-Game")]
    [InlineData("My.Game")]
    [InlineData("class")]
    [InlineData("_under")]
    public async Task Execute_PlatformNameNeedingEscaping_GeneratesAValidNamespace(string name)
    {
        NewProjectService service = CreateService();

        NewProjectResult result = await service.ExecuteAsync(Request(Path.Combine(_folder, name), "monogame"), null, CancellationToken.None);

        result.Files.Success.ShouldBeTrue(result.Files.ErrorMessage);
        string game1 = File.ReadAllText(Path.Combine(_folder, name, "Game1.cs"));
        Match match = Regex.Match(game1, @"^namespace\s+(\S+?);", RegexOptions.Multiline);
        match.Success.ShouldBeTrue();
        SyntaxFacts.IsValidIdentifier(match.Groups[1].Value.TrimStart('@')).ShouldBeTrue(match.Groups[1].Value);
    }

    [Fact]
    public async Task Execute_UppercaseProjectExtension_CreatesAJsonProjectAtThatPath()
    {
        NewProjectService service = CreateService();
        string path = Path.Combine(_folder, "Upper", "Game.GUMJ");

        NewProjectResult result = await service.ExecuteAsync(Request(path), null, CancellationToken.None);

        result.Files.Success.ShouldBeTrue(result.Files.ErrorMessage);
        File.ReadAllText(path).TrimStart().ShouldStartWith("{");
    }

    #endregion

    #region Existing files and folders

    [Theory]
    [InlineData(null)]
    [InlineData("raylib")]
    public async Task Execute_TargetFolderIsAFile_FailsWithAMessageAndKeepsTheFile(string? platform)
    {
        NewProjectService service = CreateService();
        string taken = Path.Combine(_folder, "Taken");
        File.WriteAllText(taken, "mine");

        NewProjectResult result = await service.ExecuteAsync(Request(taken, platform), null, CancellationToken.None);

        result.Files.Success.ShouldBeFalse();
        result.Files.ErrorMessage.ShouldNotBeNullOrWhiteSpace();
        File.ReadAllText(taken).ShouldBe("mine");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("raylib")]
    public async Task Execute_ParentOfTargetIsAFile_FailsWithAMessageAndKeepsTheFile(string? platform)
    {
        NewProjectService service = CreateService();
        string parent = Path.Combine(_folder, "ParentFile");
        File.WriteAllText(parent, "mine");

        NewProjectResult result = await service.ExecuteAsync(Request(Path.Combine(parent, "Game"), platform), null, CancellationToken.None);

        result.Files.Success.ShouldBeFalse();
        result.Files.ErrorMessage.ShouldNotBeNullOrWhiteSpace();
        File.ReadAllText(parent).ShouldBe("mine");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("raylib")]
    public async Task Execute_ReadOnlyParent_FailsWithAMessageAndLeavesNothing(string? platform)
    {
        if (OperatingSystem.IsWindows() || Environment.UserName == "root")
        {
            return;
        }

        NewProjectService service = CreateService();
        string parent = Path.Combine(_folder, "ReadOnly");
        Directory.CreateDirectory(parent);
        File.SetUnixFileMode(parent, UnixFileMode.UserRead | UnixFileMode.UserExecute);

        NewProjectResult result = await service.ExecuteAsync(Request(Path.Combine(parent, "Game"), platform), null, CancellationToken.None);

        result.Files.Success.ShouldBeFalse();
        result.Files.ErrorMessage.ShouldNotBeNullOrWhiteSpace();
        Directory.GetFileSystemEntries(parent).ShouldBeEmpty();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("raylib")]
    public async Task Execute_NameLongerThanTheFileSystemAllows_FailsWithAMessageAndLeavesNothing(string? platform)
    {
        NewProjectService service = CreateService();

        NewProjectResult result = await service.ExecuteAsync(Request(Path.Combine(_folder, new string('a', 300)), platform), null, CancellationToken.None);

        result.Files.Success.ShouldBeFalse();
        result.Files.ErrorMessage.ShouldNotBeNullOrWhiteSpace();
        Directory.GetFileSystemEntries(_folder).ShouldBeEmpty();
    }

    [Fact]
    public async Task Execute_DeeplyNestedNewFolders_CreatesTheProject()
    {
        NewProjectService service = CreateService();
        string deep = _folder;
        for (int i = 0; i < 30; i++)
        {
            deep = Path.Combine(deep, "d" + i);
        }

        NewProjectResult result = await service.ExecuteAsync(Request(Path.Combine(deep, "Game")), null, CancellationToken.None);

        result.Files.Success.ShouldBeTrue(result.Files.ErrorMessage);
        File.Exists(Path.Combine(deep, "Game", "Game.gumj")).ShouldBeTrue();
    }

    [Fact]
    public async Task Execute_TheSameRequestTwice_RefusesTheSecondAndLeavesTheFirstUntouched()
    {
        NewProjectService service = CreateService();
        NewProjectRequest request = Request(Path.Combine(_folder, "Twice"), "raylib");
        NewProjectResult first = await service.ExecuteAsync(request, null, CancellationToken.None);
        first.Files.Success.ShouldBeTrue(first.Files.ErrorMessage);
        Dictionary<string, byte[]> before = RelativeFiles(Path.Combine(_folder, "Twice"))
            .ToDictionary(file => file, file => File.ReadAllBytes(Path.Combine(_folder, "Twice", file)));

        NewProjectResult second = await service.ExecuteAsync(request, null, CancellationToken.None);

        second.Files.Success.ShouldBeFalse();
        second.Files.ErrorMessage.ShouldContain("already exists");
        RelativeFiles(Path.Combine(_folder, "Twice")).ShouldBe(before.Keys.OrderBy(file => file, StringComparer.Ordinal).ToList());
        foreach (KeyValuePair<string, byte[]> file in before)
        {
            File.ReadAllBytes(Path.Combine(_folder, "Twice", file.Key)).ShouldBe(file.Value);
        }
    }

    [Theory]
    [InlineData("Proj.gumx", "Proj", false)]
    [InlineData("Proj.gumj", "Proj", true)]
    [InlineData("Proj.gumx", "Proj/Proj.gumj", false)]
    [InlineData("Proj.gumj", "Proj/Proj.gumx", false)]
    public async Task Validate_GumOnlyNextToAProjectOfTheOtherFormat_IsRefusedAsAlreadyExisting(
        string existingFile, string requestPath, bool useXml)
    {
        NewProjectService service = CreateService();
        string existing = Path.Combine(_folder, "Proj", existingFile);
        NewProjectResult first = await service.ExecuteAsync(Request(existing), null, CancellationToken.None);
        first.Files.Success.ShouldBeTrue(first.Files.ErrorMessage);

        NewProjectValidation validation = service.Validate(new NewProjectRequest
        {
            Path = Path.Combine(_folder, requestPath.Replace('/', Path.DirectorySeparatorChar)),
            Template = "empty",
            UseXmlFormat = useXml
        });

        validation.Error.ShouldNotBeNull();
        validation.Error.Kind.ShouldBe(NewProjectErrorKind.ProjectAlreadyExists);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Validate_PlatformFolderHoldingAProjectOfEitherFormat_IsRefusedAsAlreadyExisting(bool existingIsXml)
    {
        NewProjectService service = CreateService();
        string game = Path.Combine(_folder, "Game");
        NewProjectResult first = await service.ExecuteAsync(
            new NewProjectRequest
            {
                Path = game, Platform = "raylib", Template = "empty", Restore = false, UseXmlFormat = existingIsXml
            },
            null, CancellationToken.None);
        first.Files.Success.ShouldBeTrue(first.Files.ErrorMessage);
        File.Delete(Path.Combine(game, "Game.csproj"));

        NewProjectValidation validation = service.Validate(new NewProjectRequest
        {
            Path = game, Platform = "raylib", Template = "empty", UseXmlFormat = !existingIsXml
        });

        validation.Error.ShouldNotBeNull();
        validation.Error.Kind.ShouldBe(NewProjectErrorKind.ProjectAlreadyExists);
    }

    #endregion

    #region Source-linked

    [Theory]
    [InlineData("monogame")]
    [InlineData("kni")]
    [InlineData("kniskia")]
    [InlineData("raylib")]
    [InlineData("stride")]
    [InlineData("silknet")]
    public async Task Execute_SourceLinked_ReferencesARuntimeProjectThatExistsInTheCheckout(string platform)
    {
        NewProjectService service = CreateService();
        string game = Path.Combine(_folder, "Linked");

        NewProjectResult result = await service.ExecuteAsync(
            new NewProjectRequest { Path = game, Platform = platform, Template = "empty", SourceLinked = true, Restore = false },
            null, CancellationToken.None);

        result.Files.Success.ShouldBeTrue(result.Files.ErrorMessage);
        ReferencedProject(result.Files.CsprojPath!).ShouldNotBeNull();
        File.Exists(ReferencedProject(result.Files.CsprojPath!)!).ShouldBeTrue();
    }

    [Fact]
    public async Task Execute_SourceLinkedToACheckoutMissingTheRuntimeProject_FailsAndLeavesNothing()
    {
        string partial = Path.Combine(_folder, "Partial");
        Directory.CreateDirectory(Path.Combine(partial, "GumCommon"));
        File.WriteAllText(Path.Combine(partial, "GumCommon", "GumCommon.csproj"), "<Project />");
        NewProjectService service = CreateService(partial);
        string game = Path.Combine(_folder, "Out", "Game");

        NewProjectResult result = await service.ExecuteAsync(
            new NewProjectRequest { Path = game, Platform = "monogame", Template = "empty", SourceLinked = true, Restore = false },
            null, CancellationToken.None);

        result.Files.Success.ShouldBeFalse();
        result.Files.ErrorMessage.ShouldContain("Gum source not found");
        Directory.Exists(Path.Combine(_folder, "Out")).ShouldBeFalse();
    }

    [Theory]
    [InlineData("Gum Checkout")]
    [InlineData("R&D")]
    [InlineData("it's <here>")]
    [InlineData("say \"hi\"")]
    [InlineData("100%")]
    [InlineData("100%41")]
    [InlineData("a;b")]
    [InlineData("$(HOME)")]
    [InlineData("me@work")]
    [InlineData("wild*card?")]
    public void Create_SourceLinkedToACheckoutWithSpecialCharacters_WritesAWellFormedProjectThatPointsAtTheRuntime(string checkoutName)
    {
        if (OperatingSystem.IsWindows() && checkoutName.IndexOfAny(new[] { '<', '>', '"', '*', '?' }) >= 0)
        {
            return;
        }

        string checkout = Path.Combine(_folder, checkoutName);
        string runtime = Path.Combine(checkout, "MonoGameGum", "MonoGameGum.csproj");
        Directory.CreateDirectory(Path.GetDirectoryName(runtime)!);
        File.WriteAllText(runtime, "<Project />");
        string game = Path.Combine(_folder, "Game");

        // Code generation setup may stop on the stand-in runtime project; the host project is written first.
        try
        {
            new PlatformProjectScaffolder().Create(game, HostPlatform.MonoGame, false, checkout);
        }
        catch (Exception exception) when (exception is InvalidOperationException or IOException)
        {
        }

        string include = XDocument.Load(Path.Combine(game, "Game.csproj"))
            .Descendants("ProjectReference").Single().Attribute("Include")!.Value;
        // MSBuild reads %XX in an item include as the character with that hex code.
        string path = Regex.Replace(include, "%([0-9A-Fa-f]{2})", match => ((char)Convert.ToInt32(match.Groups[1].Value, 16)).ToString());
        File.Exists(Path.GetFullPath(Path.Combine(game, path.Replace('\\', Path.DirectorySeparatorChar)))).ShouldBeTrue();
        // ';' separates items and '*' and '?' are wildcards in an include, so none may be left bare.
        include.IndexOfAny(new[] { ';', '*', '?', '$', '@' }).ShouldBe(-1);
    }

    private static string? ReferencedProject(string csproj)
    {
        string? include = XDocument.Load(csproj).Descendants("ProjectReference").SingleOrDefault()?.Attribute("Include")?.Value;
        return include == null
            ? null
            : Path.GetFullPath(Path.Combine(Path.GetDirectoryName(csproj)!, include.Replace('\\', Path.DirectorySeparatorChar)));
    }

    #endregion

    #region Themes and locators

    [Fact]
    public void Validate_ThemeWithOddCasing_ResolvesToTheThemeFolder()
    {
        NewProjectService service = CreateService();

        NewProjectValidation validation = service.Validate(new NewProjectRequest
        {
            Path = Path.Combine(_folder, "Themed"), Template = "forms", Theme = "mEaDoW"
        });

        validation.IsValid.ShouldBeTrue(validation.Error?.Message);
        Path.GetFileName(validation.Resolved!.ThemeDirectory).ShouldBe("Meadow");
    }

    [Fact]
    public void FormsThemeLocator_SameThemeInTwoRoots_TheEarlierRootWinsAndTheNameIsListedOnce()
    {
        string first = MakeThemeRoot("First", "meadow");
        string second = MakeThemeRoot("Second", "Meadow", "Other");
        FormsThemeLocator locator = new FormsThemeLocator(new[] { first, second });

        locator.FindThemeDirectory("MEADOW").ShouldBe(Path.Combine(first, "meadow"));
        locator.GetAvailableThemes().Count(name => string.Equals(name, "meadow", StringComparison.OrdinalIgnoreCase)).ShouldBe(1);
        locator.GetAvailableThemes().ShouldContain("Other");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" Meadow")]
    [InlineData("../Meadow")]
    [InlineData("Meadow/")]
    [InlineData("Meadow/..")]
    public void FormsThemeLocator_NameThatIsNotExactlyAThemeFolder_FindsNothing(string themeName)
    {
        string root = MakeThemeRoot("Roots", "Meadow");
        FormsThemeLocator locator = new FormsThemeLocator(new[] { root, Path.Combine(_folder, "MissingRoot") });

        locator.FindThemeDirectory(themeName).ShouldBeNull();
    }

    [Fact]
    public void FormsThemeLocator_FolderWithoutAThemeProject_IsNeitherListedNorFound()
    {
        string root = MakeThemeRoot("Roots", "Meadow");
        Directory.CreateDirectory(Path.Combine(root, "NotATheme"));
        FormsThemeLocator locator = new FormsThemeLocator(new[] { root });

        locator.GetAvailableThemes().ShouldBe(new[] { "Meadow" });
        locator.FindThemeDirectory("NotATheme").ShouldBeNull();
    }

    private string MakeThemeRoot(string rootName, params string[] themeNames)
    {
        string root = Path.Combine(_folder, rootName);
        foreach (string themeName in themeNames)
        {
            Directory.CreateDirectory(Path.Combine(root, themeName));
            File.WriteAllText(Path.Combine(root, themeName, "GumProject.gumx"), "<GumProjectSave />");
        }

        return root;
    }

    [Fact]
    public void GumSourceLocator_StartInsideACheckoutFolderThatDoesNotExist_FindsTheCheckout()
    {
        string checkout = MakeCheckout("Checkout");

        GumSourceLocator.Find(Path.Combine(checkout, "No", "Such", "Folder")).ShouldBe(checkout);
    }

    [Fact]
    public void GumSourceLocator_StartIsAFileInsideACheckout_FindsTheCheckout()
    {
        string checkout = MakeCheckout("Checkout");
        string file = Path.Combine(checkout, "GumCommon", "GumCommon.csproj");

        GumSourceLocator.Find(file).ShouldBe(checkout);
    }

    [Fact]
    public void GumSourceLocator_NestedCheckouts_TheNearestWins()
    {
        string outer = MakeCheckout("Outer");
        string inner = MakeCheckout(Path.Combine("Outer", "Vendor", "Inner"));

        GumSourceLocator.Find(Path.Combine(inner, "Tools")).ShouldBe(inner);
        GumSourceLocator.Find(Path.Combine(outer, "Vendor")).ShouldBe(outer);
    }

    [Fact]
    public void GumSourceLocator_FolderNamedLikeTheProjectFile_IsNotACheckout()
    {
        string notCheckout = Path.Combine(_folder, "Fake");
        Directory.CreateDirectory(Path.Combine(notCheckout, "GumCommon", "GumCommon.csproj"));

        GumSourceLocator.Find(notCheckout).ShouldBeNull();
    }

    private string MakeCheckout(string relative)
    {
        string checkout = Path.Combine(_folder, relative);
        Directory.CreateDirectory(Path.Combine(checkout, "GumCommon"));
        File.WriteAllText(Path.Combine(checkout, "GumCommon", "GumCommon.csproj"), "<Project />");
        return checkout;
    }

    #endregion

    #region Undo

    [Fact]
    public async Task CreateFiles_WhenCreationFailsInAnExistingGumOnlyFolder_RemovesTheFoldersAndFilesItAddedAndKeepsTheRest()
    {
        NewProjectService service = CreateService();
        string folder = Path.Combine(_folder, "Gum");
        Directory.CreateDirectory(Path.Combine(folder, "Fonts"));
        File.WriteAllText(Path.Combine(folder, "notes.txt"), "mine");
        File.WriteAllText(Path.Combine(folder, "Fonts", "Mine.ttf"), "my font");
        // A file where the Screens folder must go fails the creation after Fonts and the standards exist.
        File.WriteAllText(Path.Combine(folder, "Screens"), "in the way");
        ResolvedNewProject project = service.Validate(Request(folder)).Resolved!;

        NewProjectFilesResult result = await service.CreateFilesAsync(project, CancellationToken.None);

        result.Success.ShouldBeFalse();
        RelativeFiles(folder).ShouldBe(new List<string> { "Fonts/Mine.ttf", "Screens", "notes.txt" });
        Directory.GetDirectories(folder).Select(Path.GetFileName).ShouldBe(new List<string?> { "Fonts" });
    }

    #endregion

    private class NoRestoreRunner : IRestoreRunner
    {
        public Task<RestoreResult> RestoreAsync(string csprojPath, Action<string>? onOutputLine, CancellationToken cancellationToken) =>
            Task.FromResult(new RestoreResult { Status = RestoreStatus.Succeeded });
    }
}
