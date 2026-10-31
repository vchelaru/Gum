#pragma warning disable xUnit1031 // The helpers create the project synchronously; nothing here can deadlock (no synchronization context).
using Gum.ProjectServices;
using Gum.ProjectServices.CodeGeneration;
using System.Text.RegularExpressions;
using Shouldly;

namespace Gum.ProjectServices.Tests;

/// <summary>
/// The scaffolded host shows the project's demo screen (#7268): generated class, runtime load, or nothing, and
/// initializes the packages that were added.
/// </summary>
public class NewProjectDemoScreenHostTests : IDisposable
{
    private readonly string _tempDirectory;
    private readonly List<string> _calls = new();

    public NewProjectDemoScreenHostTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "GumDemoHostTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDirectory);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }

    private static string HostFileName(HostPlatform platform) =>
        NewProjectLayout.WritesGame1(platform) ? NewProjectLayout.Game1FileName : NewProjectLayout.ProgramFileName;

    private NewProjectService CreateService(FakeCodegen? codegen = null, FakePackageAdd? add = null) =>
        new NewProjectService(
            new FakeThemes(), new FakeRestore(_calls), add ?? new FakePackageAdd(_calls), new[] { _tempDirectory },
            codegen ?? new FakeCodegen(_calls));

    private (NewProjectService Service, ResolvedNewProject Resolved, NewProjectFilesResult Files) Create(
        HostPlatform platform, string template = "forms", bool packages = false, bool generate = false,
        string? theme = null, FakeCodegen? codegen = null, string name = "MyGame")
    {
        NewProjectService service = CreateService(codegen);
        NewProjectValidation validation = service.Validate(new NewProjectRequest
        {
            Path = Path.Combine(_tempDirectory, name),
            Platform = platform.ToString().ToLowerInvariant(),
            Template = template,
            Theme = theme,
            IncludeRecommendedPackages = packages,
            GenerateCode = generate,
            Restore = false,
        });
        validation.Error.ShouldBeNull(validation.Error?.Message);
        NewProjectFilesResult files = service.CreateFilesAsync(validation.Resolved!, CancellationToken.None).GetAwaiter().GetResult();
        files.Success.ShouldBeTrue(files.ErrorMessage);
        return (service, validation.Resolved!, files);
    }

    private string ReadHost(HostPlatform platform, string name = "MyGame") =>
        File.ReadAllText(Path.Combine(_tempDirectory, name, HostFileName(platform)));

    [Theory]
    [InlineData(HostPlatform.MonoGame)]
    [InlineData(HostPlatform.Kni)]
    [InlineData(HostPlatform.KniSkia)]
    [InlineData(HostPlatform.Raylib)]
    [InlineData(HostPlatform.SilkNet)]
    [InlineData(HostPlatform.Stride)]
    public void Host_WithTheDemoScreen_LoadsItFromTheProjectAtRuntime(HostPlatform platform)
    {
        Create(platform);

        string host = ReadHost(platform);
        host.ShouldNotContain("new Button()");
        host.ShouldNotContain("Clicked at");
        host.ShouldContain("ObjectFinder.Self.GetElementSave(\"DemoScreenGum\")");
        host.ShouldContain(".ToGraphicalUiElement(GumService.Default.SystemManagers, addToManagers: false)");
        host.ShouldContain(".AddToRoot()");
        host.ShouldContain("using Gum.Managers;");
        host.ShouldContain("using GumRuntime;");
        // The core overload (systemManagers, addToManagers) exists on every runtime, stable and source; the no-argument one is MonoGameGum only.
    }

    [Theory]
    [InlineData(HostPlatform.MonoGame)]
    [InlineData(HostPlatform.Raylib)]
    public void Host_WithoutAScreen_ExplainsHowToAddOne(HostPlatform platform)
    {
        Create(platform, template: "empty");

        string host = ReadHost(platform);
        host.ShouldNotContain("new Button()");
        host.ShouldNotContain("ToGraphicalUiElement");
        host.ShouldContain("new MyScreen().AddToRoot();");
    }

    [Fact]
    public void Host_WithTheDemoLeftOut_ShowsNothing()
    {
        (NewProjectService service, ResolvedNewProject resolved, NewProjectFilesResult files) = Create(HostPlatform.MonoGame);
        // The dialog removes the demo screen after the project loads, then asks for the host to be rewritten.
        NewProjectFilesResult withoutDemo = new NewProjectFilesResult
        {
            Success = true, GumProjectPath = files.GumProjectPath, CsprojPath = files.CsprojPath
        };
        service.UpdateHost(resolved, withoutDemo, packagesAdded: false, screenCodeGenerated: false);

        string host = ReadHost(HostPlatform.MonoGame);
        host.ShouldNotContain("DemoScreenGum");
        host.ShouldContain("new MyScreen().AddToRoot();");
    }

    [Fact]
    public void Host_WithAThemedDemoScreen_UsesTheFolderName()
    {
        string theme = Path.Combine(FindRepoRoot(), "Tools", "Gum.ProjectServices", "Templates", "FormsThemes", "Bubblegum");
        NewProjectService service = new NewProjectService(
            new FakeThemes { Directory = theme }, new FakeRestore(_calls), new FakePackageAdd(_calls), new[] { _tempDirectory },
            new FakeCodegen(_calls));

        NewProjectValidation validation = service.Validate(new NewProjectRequest
        {
            Path = Path.Combine(_tempDirectory, "Themed"), Platform = "monogame", Theme = "Bubblegum", Restore = false
        });
        validation.Error.ShouldBeNull(validation.Error?.Message);
        NewProjectFilesResult files = service.CreateFilesAsync(validation.Resolved!, CancellationToken.None).GetAwaiter().GetResult();
        files.Success.ShouldBeTrue(files.ErrorMessage);

        files.DemoScreen.ShouldNotBeNull();
        files.DemoScreen!.Name.ShouldBe("Bubblegum/DemoScreenGum");
        files.DemoScreen.ClassName.ShouldBe("DemoScreenGum");
        files.DemoScreen.Namespace.ShouldBe("Themed.Screens.Bubblegum");
        ReadHost(HostPlatform.MonoGame, "Themed").ShouldContain("GetElementSave(\"Bubblegum/DemoScreenGum\")");
    }

    [Theory]
    [InlineData(HostPlatform.MonoGame)]
    [InlineData(HostPlatform.Raylib)]
    [InlineData(HostPlatform.SilkNet)]
    [InlineData(HostPlatform.Stride)]
    public void UpdateHost_AfterCodeIsGenerated_CreatesTheScreenClass(HostPlatform platform)
    {
        (NewProjectService service, ResolvedNewProject resolved, NewProjectFilesResult files) = Create(platform, generate: true);

        service.UpdateHost(resolved, files, packagesAdded: false, screenCodeGenerated: true);

        string host = ReadHost(platform);
        host.ShouldContain("new DemoScreenGum().AddToRoot();");
        host.ShouldContain("using MyGame.Screens;");
        host.ShouldContain("using Gum.Forms.Controls;");
        host.ShouldNotContain("ToGraphicalUiElement");
    }

    [Fact]
    public void Host_BeforeCodeIsGenerated_DoesNotReferenceTheGeneratedClass()
    {
        Create(HostPlatform.MonoGame, generate: true);

        // Continue after a failed restore must still leave a project that builds.
        ReadHost(HostPlatform.MonoGame).ShouldNotContain("new DemoScreenGum()");
    }

    [Theory]
    [InlineData(HostPlatform.MonoGame, new[]
    {
        "Gum.Wireframe.CustomSetPropertyOnRenderable.InMemoryFontCreator = new KernSmith.Gum.KernSmithFontCreator(GraphicsDevice);",
        "Gum.Expressions.GumExpressionService.Initialize();"
    })]
    [InlineData(HostPlatform.Kni, new[]
    {
        "Gum.Wireframe.CustomSetPropertyOnRenderable.InMemoryFontCreator = new KernSmith.Gum.KernSmithFontCreator(GraphicsDevice);",
        "Gum.Expressions.GumExpressionService.Initialize();"
    })]
    [InlineData(HostPlatform.Raylib, new[]
    {
        "RaylibGum.Renderables.CustomSetPropertyOnRenderable.InMemoryFontCreator = new KernSmith.Gum.KernSmithRaylibFontCreator();",
        "Gum.Expressions.GumExpressionService.Initialize();"
    })]
    [InlineData(HostPlatform.SilkNet, new[] { "Gum.Expressions.GumExpressionService.Initialize();" })]
    [InlineData(HostPlatform.Stride, new[] { "Gum.Expressions.GumExpressionService.Initialize();" })]
    public void UpdateHost_AfterThePackagesAreAdded_InitializesEachOfThem(HostPlatform platform, string[] expectedLines)
    {
        (NewProjectService service, ResolvedNewProject resolved, NewProjectFilesResult files) = Create(platform, packages: true);

        service.UpdateHost(resolved, files, packagesAdded: true, screenCodeGenerated: false);

        string host = ReadHost(platform);
        foreach (string line in expectedLines)
        {
            host.ShouldContain(line);
        }
        // In the order the setup docs give, after Gum's own Initialize.
        int previous = host.IndexOf(".Initialize(", StringComparison.Ordinal);
        foreach (string line in expectedLines)
        {
            int index = host.IndexOf(line, StringComparison.Ordinal);
            index.ShouldBeGreaterThan(previous);
            previous = index;
        }
    }

    [Theory]
    [InlineData(HostPlatform.MonoGame)]
    [InlineData(HostPlatform.Raylib)]
    public void Host_WithoutThePackagesAdded_InitializesNone(HostPlatform platform)
    {
        (NewProjectService service, ResolvedNewProject resolved, NewProjectFilesResult files) = Create(platform, packages: false);

        service.UpdateHost(resolved, files, packagesAdded: true, screenCodeGenerated: false);

        string host = ReadHost(platform);
        host.ShouldNotContain("GumExpressionService");
        host.ShouldNotContain("ShapeRenderer");
        host.ShouldNotContain("InMemoryFontCreator");
    }

    // A table package whose init line names a type the stable runtime lacks breaks every default project (stable 2026.10.1.1 has
    // no Gum.Renderables.ShapeRenderer). Until a stable release carries it, no row may add a Shapes package.
    [Theory]
    [InlineData(HostPlatform.MonoGame)]
    [InlineData(HostPlatform.Kni)]
    [InlineData(HostPlatform.Raylib)]
    [InlineData(HostPlatform.SilkNet)]
    [InlineData(HostPlatform.Stride)]
    public void TablePackages_AllHaveAnInitLine_AndNoneIsShapes(HostPlatform platform)
    {
        (NewProjectService service, ResolvedNewProject resolved, NewProjectFilesResult files) = Create(platform, packages: true);

        service.UpdateHost(resolved, files, packagesAdded: true, screenCodeGenerated: false);

        resolved.RecommendedPackages.ShouldNotContain(package => package.StartsWith("Gum.Shapes."));
        string host = ReadHost(platform);
        host.ShouldNotContain("ShapeRenderer");
        foreach (string package in resolved.RecommendedPackages)
        {
            host.ShouldContain(PlatformHostContent.GetInitLines(new[] { package }).Single(), customMessage: package);
        }
    }

    [Fact]
    public void Host_BeforeThePackagesAreAdded_InitializesNone()
    {
        Create(HostPlatform.MonoGame, packages: true);

        ReadHost(HostPlatform.MonoGame).ShouldNotContain("GumExpressionService");
    }

    #region Service steps

    [Fact]
    public async Task ExecuteAsync_RunsAddRestoreThenCodegen_AndRewritesTheHost()
    {
        NewProjectService service = CreateService();
        NewProjectResult result = await service.ExecuteAsync(
            new NewProjectRequest
            {
                Path = Path.Combine(_tempDirectory, "MyGame"), Platform = "monogame", IncludeRecommendedPackages = true,
                GenerateCode = true, Restore = true
            },
            progress: null, CancellationToken.None);

        _calls.Select(call => call.Split(' ')[0]).ToArray()
            .ShouldBe(new[] { "add", "add", "restore", "codegen" });
        result.Codegen.ShouldNotBeNull();
        result.Codegen!.Success.ShouldBeTrue();

        string host = ReadHost(HostPlatform.MonoGame);
        host.ShouldContain("new DemoScreenGum().AddToRoot();");
        host.ShouldContain("GumExpressionService.Initialize();");
    }

    [Fact]
    public async Task ExecuteAsync_WithoutRestore_RunsCodegenAfterTheAdds()
    {
        NewProjectService service = CreateService();
        await service.ExecuteAsync(
            new NewProjectRequest
            {
                Path = Path.Combine(_tempDirectory, "MyGame"), Platform = "monogame", IncludeRecommendedPackages = true,
                GenerateCode = true, Restore = false
            },
            progress: null, CancellationToken.None);

        _calls.Select(call => call.Split(' ')[0]).ToArray().ShouldBe(new[] { "add", "add", "codegen" });
    }

    [Fact]
    public async Task ExecuteAsync_AFailedCodegen_KeepsTheFilesAndTheRuntimeLoadHost()
    {
        NewProjectService service = CreateService(new FakeCodegen(_calls) { Fail = true });
        NewProjectResult result = await service.ExecuteAsync(
            new NewProjectRequest
            {
                Path = Path.Combine(_tempDirectory, "MyGame"), Platform = "monogame", GenerateCode = true, Restore = false
            },
            progress: null, CancellationToken.None);

        result.Codegen!.Success.ShouldBeFalse();
        result.Codegen.OutputTail.ShouldContain("boom");
        File.Exists(result.Files.CsprojPath!).ShouldBeTrue();
        ReadHost(HostPlatform.MonoGame).ShouldNotContain("new DemoScreenGum()");
    }

    [Fact]
    public async Task ExecuteAsync_ARestoreFailure_SkipsCodegen()
    {
        NewProjectService service = new NewProjectService(
            new FakeThemes(), new FailingRestore(), new FakePackageAdd(_calls), new[] { _tempDirectory }, new FakeCodegen(_calls));
        NewProjectResult result = await service.ExecuteAsync(
            new NewProjectRequest
            {
                Path = Path.Combine(_tempDirectory, "MyGame"), Platform = "monogame", GenerateCode = true, Restore = true
            },
            progress: null, CancellationToken.None);

        result.Codegen.ShouldBeNull();
        _calls.ShouldNotContain(call => call.StartsWith("codegen"));
    }

    [Fact]
    public async Task ExecuteAsync_WithNoDemoScreen_RunsNoCodegen()
    {
        NewProjectService service = CreateService();
        NewProjectResult result = await service.ExecuteAsync(
            new NewProjectRequest
            {
                Path = Path.Combine(_tempDirectory, "MyGame"), Platform = "monogame", Template = "empty",
                GenerateCode = true, Restore = false
            },
            progress: null, CancellationToken.None);

        result.Codegen.ShouldBeNull();
        _calls.ShouldNotContain(call => call.StartsWith("codegen"));
    }

    [Fact]
    public void Validate_DemoScreenCodeWithoutAPlatform_IsRefused()
    {
        NewProjectValidation validation = CreateService().Validate(new NewProjectRequest
        {
            Path = Path.Combine(_tempDirectory, "Proj"), GenerateCode = true
        });

        validation.Error!.Kind.ShouldBe(NewProjectErrorKind.GenerateCodeNeedsPlatform);
    }

    #endregion

    #region Runtime sources the host lines rely on

    // The host builds against whatever the NuGet runtime offers, which a unit test cannot restore. This pins that the members the runtime-load
    // line uses are still declared in the runtime sources, so a signature change fails here and not in a user's build (the zero-argument
    // ToGraphicalUiElement is MonoGameGum only and was the CS1501 on the Skia runtimes).
    [Fact]
    public void TheRuntimeLoadLine_UsesMembersEveryRuntimeDeclares()
    {
        string root = FindRepoRoot();
        string extensions = File.ReadAllText(Path.Combine(root, "GumRuntime", "ElementSaveExtensions.GumRuntime.cs"));
        Regex.IsMatch(extensions, @"public static GraphicalUiElement ToGraphicalUiElement\(this ElementSave elementSave,\s+ISystemManagers systemManagers,\s+bool addToManagers")
            .ShouldBeTrue("the core overload the host calls");

        // GumService.Default.SystemManagers: the MonoGame-family service and the Skia base (Silk.NET, Stride, KNI Skia) both declare it.
        File.ReadAllText(Path.Combine(root, "MonoGameGum", "GumService.cs")).ShouldContain("public SystemManagers SystemManagers");
        File.ReadAllText(Path.Combine(root, "Runtimes", "SkiaGum", "GumServiceSkiaBase.cs")).ShouldContain("public SystemManagers SystemManagers");
        File.ReadAllText(Path.Combine(root, "GumRuntime", "GraphicalUiElement.Hierarchy.cs")).ShouldContain("public void AddToRoot()");
        File.ReadAllText(Path.Combine(root, "Gum", "Managers", "ObjectFinder.cs")).ShouldContain("public ElementSave? GetElementSave(string? elementName)");
    }

    #endregion

    #region Real code generation

    [Fact]
    public void RealCodegen_GeneratesTheClassTheHostNames()
    {
        NewProjectService service = new NewProjectService(
            new FakeThemes(), new FakeRestore(_calls), new FakePackageAdd(_calls), new[] { _tempDirectory },
            new HeadlessCodegenRunnerAdapter());
        NewProjectValidation validation = service.Validate(new NewProjectRequest
        {
            Path = Path.Combine(_tempDirectory, "RealGame"), Platform = "monogame", GenerateCode = true, Restore = false
        });
        NewProjectFilesResult files = service.CreateFilesAsync(validation.Resolved!, CancellationToken.None).GetAwaiter().GetResult();

        GenerateCodeResult result = service.GenerateCodeAsync(validation.Resolved!, files, null, CancellationToken.None)
            .GetAwaiter().GetResult();

        result.Success.ShouldBeTrue(result.OutputTail);
        string generated = File.ReadAllText(
            Directory.GetFiles(Path.Combine(_tempDirectory, "RealGame"), "DemoScreenGum.Generated.cs", SearchOption.AllDirectories).Single());
        generated.ShouldContain($"namespace {files.DemoScreen!.Namespace}");
        generated.ShouldContain($"partial class {files.DemoScreen.ClassName} :");
        // Everything the screen references is generated too, and the other screens/components are not required.
        Directory.GetFiles(Path.Combine(_tempDirectory, "RealGame"), "*.Generated.cs", SearchOption.AllDirectories)
            .Length.ShouldBeGreaterThan(1);
    }

    #endregion

    #region Fakes

    private static string FindRepoRoot()
    {
        string current = AppContext.BaseDirectory;
        while (!Directory.Exists(Path.Combine(current, "Tools", "Gum.ProjectServices", "Templates")))
        {
            current = Path.GetDirectoryName(current) ?? throw new InvalidOperationException("Gum checkout not found.");
        }

        return current;
    }

    // The product runner as the service's runner, without GumExpressions (the Gum.ProjectServices tests do not reference it).
    private class HeadlessCodegenRunnerAdapter : IHeadlessCodegenRunner
    {
        private readonly HeadlessCodegenRunner _inner = new HeadlessCodegenRunner();

        public int Run(string projectPath, IReadOnlyList<string>? elementNames, bool prune, TextWriter output, TextWriter error) =>
            _inner.Run(projectPath, elementNames, prune, output, error);
    }

    private class FakeCodegen : IHeadlessCodegenRunner
    {
        private readonly List<string> _calls;

        public FakeCodegen(List<string> calls) => _calls = calls;

        public bool Fail { get; set; }

        public int Run(string projectPath, IReadOnlyList<string>? elementNames, bool prune, TextWriter output, TextWriter error)
        {
            _calls.Add($"codegen {Path.GetFileName(projectPath)} {string.Join(",", elementNames ?? Array.Empty<string>())}");
            if (Fail)
            {
                error.WriteLine("boom");
                return 1;
            }
            return 0;
        }
    }

    private class FakeRestore : IRestoreRunner
    {
        private readonly List<string> _calls;

        public FakeRestore(List<string> calls) => _calls = calls;

        public Task<RestoreResult> RestoreAsync(string csprojPath, Action<string>? onOutputLine, CancellationToken cancellationToken)
        {
            _calls.Add($"restore {csprojPath}");
            return Task.FromResult(new RestoreResult { Status = RestoreStatus.Succeeded, ExitCode = 0 });
        }
    }

    private class FailingRestore : IRestoreRunner
    {
        public Task<RestoreResult> RestoreAsync(string csprojPath, Action<string>? onOutputLine, CancellationToken cancellationToken) =>
            Task.FromResult(new RestoreResult { Status = RestoreStatus.Failed, ExitCode = 1, OutputTail = "offline" });
    }

    private class FakePackageAdd : IPackageAddRunner
    {
        private readonly List<string> _calls;

        public FakePackageAdd(List<string> calls) => _calls = calls;

        public Task<RestoreResult> AddAsync(string csprojPath, string packageId, Action<string>? onOutputLine, CancellationToken cancellationToken)
        {
            _calls.Add($"add {packageId}");
            return Task.FromResult(new RestoreResult { Status = RestoreStatus.Succeeded, ExitCode = 0 });
        }
    }

    private class FakeThemes : IFormsThemeLocator
    {
        public string? Directory { get; init; }

        public string? FindThemeDirectory(string themeName) => Directory;

        public IReadOnlyList<string> GetAvailableThemes() => Array.Empty<string>();
    }

    #endregion
}
