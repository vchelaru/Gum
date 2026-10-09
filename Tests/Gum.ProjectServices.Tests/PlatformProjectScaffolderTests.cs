using Gum.ProjectServices;
using Gum.ProjectServices.CodeGeneration;
using Shouldly;

namespace Gum.ProjectServices.Tests;

public class PlatformProjectScaffolderTests : IDisposable
{
    private readonly PlatformProjectScaffolder _sut;
    private readonly string _tempDirectory;

    public PlatformProjectScaffolderTests()
    {
        _sut = new PlatformProjectScaffolder();
        _tempDirectory = Path.Combine(Path.GetTempPath(), "GumPlatformScaffolderTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDirectory);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }

    [Theory]
    [InlineData(HostPlatform.MonoGame, "Gum.MonoGame", OutputLibrary.MonoGameForms)]
    [InlineData(HostPlatform.Kni, "Gum.KNI", OutputLibrary.MonoGameForms)]
    [InlineData(HostPlatform.Raylib, "Gum.raylib", OutputLibrary.MonoGameForms)]
    [InlineData(HostPlatform.Stride, "Gum.Stride", OutputLibrary.MonoGameForms)]
    [InlineData(HostPlatform.SilkNet, "Gum.SilkNet", OutputLibrary.MonoGameForms)]
    public void Create_ShouldReferenceGumPackageAndPointCodegenAtHostProject(
        HostPlatform platform, string expectedPackage, OutputLibrary expectedLibrary)
    {
        string projectDirectory = Path.Combine(_tempDirectory, "MyGame");

        PlatformProjectResult result = _sut.Create(projectDirectory, platform, includeFormsTemplate: true);

        result.Success.ShouldBeTrue(result.ErrorMessage);
        result.CsprojPath.ShouldBe(Path.Combine(projectDirectory, "MyGame.csproj"));
        string csproj = File.ReadAllText(result.CsprojPath);
        csproj.ShouldContain($"<PackageReference Include=\"{expectedPackage}\"");
        csproj.ShouldNotContain("ProjectReference");

        string gumFolder = Path.Combine(projectDirectory, "Content", "GumProject");
        result.GumProjectPath.ShouldBe(Path.Combine(gumFolder, "GumProject.gumj"));
        File.Exists(result.GumProjectPath).ShouldBeTrue();
        File.Exists(Path.Combine(gumFolder, "Components", "Controls", "ButtonStandard.gucj")).ShouldBeTrue();

        CodeOutputProjectSettings? settings = new CodeOutputProjectSettingsManager(
            new NullCodeGenLogger(), new FixedProjectDirectoryProvider(gumFolder + Path.DirectorySeparatorChar))
            .TryLoadSettingsForProject();
        settings.ShouldNotBeNull();
        settings.OutputLibrary.ShouldBe(expectedLibrary);
        settings.RootNamespace.ShouldBe("MyGame");
        settings.CsprojPath.ShouldBe("../../MyGame.csproj");
    }

    [Fact]
    public void Create_Stride_ShouldTargetPlainNet10SoTheCpuRenderPathRuns()
    {
        // The net10.0-windows7.0 target selects Gum.Stride's ANGLE GPU path, which needs native ANGLE
        // libraries that do not load on a clean machine.
        string projectDirectory = Path.Combine(_tempDirectory, "MyGame");

        PlatformProjectResult result = _sut.Create(projectDirectory, HostPlatform.Stride, includeFormsTemplate: true);

        string csproj = File.ReadAllText(result.CsprojPath);
        csproj.ShouldContain("<TargetFramework>net10.0</TargetFramework>");
    }

    [Fact]
    public void Create_SilkNet_ShouldReferenceSdlWindowingAndInputButNoAngleNatives()
    {
        // Desktop OpenGL needs no ANGLE natives, so the project restores and runs on Windows, macOS and
        // Linux from NuGet packages alone. Silk.NET.Input.Sdl is required or window.CreateInput() throws.
        string projectDirectory = Path.Combine(_tempDirectory, "MyGame");

        PlatformProjectResult result = _sut.Create(projectDirectory, HostPlatform.SilkNet, includeFormsTemplate: true);

        string csproj = File.ReadAllText(result.CsprojPath);
        csproj.ShouldContain("<PackageReference Include=\"Silk.NET.Windowing.Sdl\"");
        csproj.ShouldContain("<PackageReference Include=\"Silk.NET.Input.Sdl\"");
        csproj.ShouldNotContain("ANGLE", Case.Insensitive);
        csproj.ShouldNotContain("-windows");
        string program = File.ReadAllText(Path.Combine(projectDirectory, "Program.cs"));
        program.ShouldContain("MyGame");
        // GetProcAddress throws for the optional GL functions Skia probes for, and FramebufferResize never fires.
        program.ShouldContain("TryGetProcAddress");
        program.ShouldContain("window.Resize");
    }

    [Fact]
    public void Create_SilkNet_ShouldPinSdlNativeLibraryWithValidMacSignature()
    {
        // Ultz.Native.SDL 2.30.1, which Silk.NET.Windowing.Sdl 2.21.0 resolves on its own, ships a macOS
        // arm64 libSDL2 with an invalid code signature, so macOS kills the app at launch. 2.32.10 is signed.
        string projectDirectory = Path.Combine(_tempDirectory, "MyGame");

        PlatformProjectResult result = _sut.Create(projectDirectory, HostPlatform.SilkNet, includeFormsTemplate: true);

        string csproj = File.ReadAllText(result.CsprojPath);
        csproj.ShouldContain("<PackageReference Include=\"Ultz.Native.SDL\" Version=\"2.32.10\"");
    }

    [Fact]
    public void Create_WithoutFormsTemplate_ShouldCreateEmptyGumProject()
    {
        string projectDirectory = Path.Combine(_tempDirectory, "MyGame");

        PlatformProjectResult result = _sut.Create(projectDirectory, HostPlatform.MonoGame, includeFormsTemplate: false);

        result.Success.ShouldBeTrue(result.ErrorMessage);
        File.Exists(result.GumProjectPath).ShouldBeTrue();
        File.Exists(Path.Combine(projectDirectory, "Content", "GumProject", "Components", "Controls", "ButtonStandard.gucj")).ShouldBeFalse();
    }

    [Theory]
    [InlineData("MonoGameGum")]
    [InlineData("KniGum")]
    [InlineData("RaylibGum")]
    [InlineData("SkiaGum")]
    [InlineData("SilkNetGum")]
    [InlineData("GumCommon")]
    public void Create_WhenNameCollidesWithRuntimeAssembly_ShouldFailWithoutWritingFiles(string name)
    {
        string projectDirectory = Path.Combine(_tempDirectory, name);

        PlatformProjectResult result = _sut.Create(projectDirectory, HostPlatform.MonoGame, includeFormsTemplate: true);

        result.Success.ShouldBeFalse();
        result.ErrorMessage.ShouldContain(name);
        Directory.Exists(projectDirectory).ShouldBeFalse();
    }

    [Fact]
    public void Create_WhenCsprojAlreadyExists_ShouldFailAndLeaveItUntouched()
    {
        string projectDirectory = Path.Combine(_tempDirectory, "MyGame");
        Directory.CreateDirectory(projectDirectory);
        string csprojPath = Path.Combine(projectDirectory, "MyGame.csproj");
        File.WriteAllText(csprojPath, "original");

        PlatformProjectResult result = _sut.Create(projectDirectory, HostPlatform.MonoGame, includeFormsTemplate: true);

        result.Success.ShouldBeFalse();
        File.ReadAllText(csprojPath).ShouldBe("original");
    }

    [Fact]
    public void Create_WhenNameIsNotAValidProjectName_ShouldFail()
    {
        string projectDirectory = Path.Combine(_tempDirectory, "my game!");

        PlatformProjectResult result = _sut.Create(projectDirectory, HostPlatform.MonoGame, includeFormsTemplate: true);

        result.Success.ShouldBeFalse();
        Directory.Exists(projectDirectory).ShouldBeFalse();
    }

    [Theory]
    [InlineData(HostPlatform.MonoGame, "MonoGameGum/MonoGameGum.csproj", "Gum.MonoGame")]
    [InlineData(HostPlatform.Kni, "MonoGameGum/KniGum/KniGum.csproj", "Gum.KNI")]
    [InlineData(HostPlatform.Raylib, "Runtimes/RaylibGum/RaylibGum.csproj", "Gum.raylib")]
    [InlineData(HostPlatform.Stride, "Runtimes/StrideGum/StrideGum.csproj", "Gum.Stride")]
    [InlineData(HostPlatform.SilkNet, "Runtimes/SilkNetGum/SilkNetGum.csproj", "Gum.SilkNet")]
    public void Create_WithGumSource_ShouldReferenceRuntimeProjectInsteadOfPackage(
        HostPlatform platform, string runtimeCsproj, string packageName)
    {
        string gumSource = CreateFakeGumSource(runtimeCsproj);
        string projectDirectory = Path.Combine(_tempDirectory, "work", "MyGame");

        PlatformProjectResult result = _sut.Create(projectDirectory, platform, includeFormsTemplate: true, gumSource);

        result.Success.ShouldBeTrue(result.ErrorMessage);
        string csproj = File.ReadAllText(result.CsprojPath);
        csproj.ShouldNotContain($"<PackageReference Include=\"{packageName}\"");
        csproj.ShouldNotContain("{{");

        string expectedInclude = Path.GetRelativePath(projectDirectory, Path.Combine(gumSource, runtimeCsproj))
            .Replace('/', '\\');
        csproj.ShouldContain($"<ProjectReference Include=\"{expectedInclude}\" />");
    }

    [Fact]
    public void Create_WithGumSource_ShouldLetCodegenDetectSyntaxVersionFromTheProjectReference()
    {
        string gumSource = CreateFakeGumSource("MonoGameGum/MonoGameGum.csproj");
        File.WriteAllText(
            Path.Combine(gumSource, "MonoGameGum", "AssemblyAttributes.cs"),
            "[assembly: GumSyntaxVersion(Version = 7)]");
        string projectDirectory = Path.Combine(_tempDirectory, "work", "MyGame");

        PlatformProjectResult result = _sut.Create(projectDirectory, HostPlatform.MonoGame, includeFormsTemplate: false, gumSource);

        result.Success.ShouldBeTrue(result.ErrorMessage);
        string gumFolder = Path.Combine(projectDirectory, "Content", "GumProject");
        CodeOutputProjectSettings? settings = new CodeOutputProjectSettingsManager(
            new NullCodeGenLogger(), new FixedProjectDirectoryProvider(gumFolder + Path.DirectorySeparatorChar))
            .TryLoadSettingsForProject();
        settings.ShouldNotBeNull();
        SyntaxVersionResult version = new SyntaxVersionDetectionService(new NullCodeGenLogger())
            .Detect(settings, gumFolder + Path.DirectorySeparatorChar);
        version.Source.ShouldBe(SyntaxVersionSource.ProjectReference);
        version.Version.ShouldBe(7);
    }

    [Fact]
    public void Create_WithGumSourceMissingTheRuntimeProject_ShouldFailWithoutWritingFiles()
    {
        string gumSource = Path.Combine(_tempDirectory, "NotGum");
        Directory.CreateDirectory(gumSource);
        string projectDirectory = Path.Combine(_tempDirectory, "MyGame");

        PlatformProjectResult result = _sut.Create(projectDirectory, HostPlatform.Raylib, includeFormsTemplate: true, gumSource);

        result.Success.ShouldBeFalse();
        result.ErrorMessage.ShouldContain("RaylibGum.csproj");
        Directory.Exists(projectDirectory).ShouldBeFalse();
    }

    private string CreateFakeGumSource(string runtimeCsproj)
    {
        string gumSource = Path.Combine(_tempDirectory, "GumSource");
        string csprojPath = Path.Combine(gumSource, runtimeCsproj);
        Directory.CreateDirectory(Path.GetDirectoryName(csprojPath)!);
        File.WriteAllText(csprojPath, "<Project />");
        return gumSource;
    }

    private class NullCodeGenLogger : ICodeGenLogger
    {
        public void PrintOutput(string message) { }
        public void PrintError(string message) { }
    }
}
