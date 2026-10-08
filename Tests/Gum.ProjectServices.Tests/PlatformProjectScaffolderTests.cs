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

    private class NullCodeGenLogger : ICodeGenLogger
    {
        public void PrintOutput(string message) { }
        public void PrintError(string message) { }
    }
}
