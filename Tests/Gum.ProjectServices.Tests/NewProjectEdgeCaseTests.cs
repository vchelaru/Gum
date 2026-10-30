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
        new NewProjectRequest { Path = path, Platform = platform, Template = "empty", Restore = false };

    private static List<string> RelativeFiles(string root) =>
        Directory.GetFiles(root, "*", SearchOption.AllDirectories)
            .Select(path => Path.GetRelativePath(root, path).Replace('\\', '/'))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToList();

    #region Names

    [Theory]
    [InlineData("monogame", "Game")]
    [InlineData("kni", "Game")]
    [InlineData("monogame", "Button")]
    [InlineData("monogame", "MyGame")]
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

    private const string XnaStubs = @"
namespace Microsoft.Xna.Framework
{
    public class GameTime { }
    public struct Color { public static Color CornflowerBlue; }
    public class ContentManager { public string RootDirectory { get; set; } }
    public class GraphicsDevice { public void Clear(Color color) { } }
    public class GraphicsDeviceManager { public GraphicsDeviceManager(Game game) { } }
    public class Game : System.IDisposable
    {
        public ContentManager Content { get; } = new ContentManager();
        public bool IsMouseVisible { get; set; }
        public GraphicsDevice GraphicsDevice { get; } = new GraphicsDevice();
        protected virtual void Initialize() { }
        protected virtual void Update(GameTime gameTime) { }
        protected virtual void Draw(GameTime gameTime) { }
        public void Run() { }
        public void Dispose() { }
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

    #endregion

    #region Existing files and folders

    [Theory]
    [InlineData("raylib")]
    [InlineData("monogame")]
    public async Task Execute_ExistingProgramCsInTheGameFolder_IsNotOverwritten(string platform)
    {
        NewProjectService service = CreateService();
        string game = Path.Combine(_folder, "Game");
        Directory.CreateDirectory(game);
        File.WriteAllText(Path.Combine(game, "Program.cs"), "// mine");

        NewProjectResult result = await service.ExecuteAsync(Request(game, platform), null, CancellationToken.None);

        // Either the request is refused, or the user's file is left as it was.
        (!result.Files.Success || File.ReadAllText(Path.Combine(game, "Program.cs")) == "// mine")
            .ShouldBeTrue("Program.cs was replaced by the template");
    }

    [Fact]
    public async Task CreateFiles_WhenCreationFailsLate_KeepsTheContentOfPlannedFilesThatWereThereBefore()
    {
        NewProjectService service = CreateService();
        string game = Path.Combine(_folder, "Game");
        string gumFolder = Path.Combine(game, "Content", "GumProject");
        Directory.CreateDirectory(gumFolder);
        File.WriteAllText(Path.Combine(game, "Program.cs"), "mine");
        // A folder where the code generation settings file goes fails the creation after the other files are written.
        Directory.CreateDirectory(Path.Combine(gumFolder, "ProjectCodeSettings.codsj"));
        ResolvedNewProject project = service.Validate(Request(game, "raylib")).Resolved!;

        NewProjectFilesResult result = await service.CreateFilesAsync(project, CancellationToken.None);

        result.Success.ShouldBeFalse();
        File.ReadAllText(Path.Combine(game, "Program.cs")).ShouldBe("mine");
    }

    [Fact]
    public void Validate_PlatformFolderHoldingAnotherCodeProject_IsRefused()
    {
        NewProjectService service = CreateService();
        string game = Path.Combine(_folder, "Game");
        Directory.CreateDirectory(game);
        File.WriteAllText(Path.Combine(game, "Other.csproj"), "<Project />");

        NewProjectValidation validation = service.Validate(Request(game, "raylib"));

        // Two project files in one folder make a plain `dotnet build` fail.
        validation.IsValid.ShouldBeFalse();
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

    #endregion

    #region Source-linked

    [Theory]
    [InlineData("Gum Checkout")]
    [InlineData("R&D")]
    [InlineData("it's <here>")]
    public void Create_SourceLinkedToACheckoutWithSpecialCharacters_WritesAWellFormedProjectThatPointsAtTheRuntime(string checkoutName)
    {
        if (OperatingSystem.IsWindows() && checkoutName.Contains('<'))
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

        string csproj = Path.Combine(game, "Game.csproj");
        XDocument document = XDocument.Load(csproj);
        string include = document.Descendants("ProjectReference").Single().Attribute("Include")!.Value;
        File.Exists(Path.GetFullPath(Path.Combine(game, include.Replace('\\', Path.DirectorySeparatorChar)))).ShouldBeTrue();
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

    private string MakeCheckout(string relative)
    {
        string checkout = Path.Combine(_folder, relative);
        Directory.CreateDirectory(Path.Combine(checkout, "GumCommon"));
        File.WriteAllText(Path.Combine(checkout, "GumCommon", "GumCommon.csproj"), "<Project />");
        return checkout;
    }

    #endregion

    private class NoRestoreRunner : IRestoreRunner
    {
        public Task<RestoreResult> RestoreAsync(string csprojPath, Action<string>? onOutputLine, CancellationToken cancellationToken) =>
            Task.FromResult(new RestoreResult { Status = RestoreStatus.Succeeded });
    }
}
