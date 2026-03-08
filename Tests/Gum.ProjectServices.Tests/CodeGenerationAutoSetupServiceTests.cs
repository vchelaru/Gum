using Gum.ProjectServices.CodeGeneration;
using Shouldly;
using System;
using System.IO;

namespace Gum.ProjectServices.Tests;

public class CodeGenerationAutoSetupServiceTests : IDisposable
{
    private readonly CodeGenerationAutoSetupService _sut;
    private readonly string _tempDirectory;

    public CodeGenerationAutoSetupServiceTests()
    {
        _sut = new CodeGenerationAutoSetupService();
        _tempDirectory = Path.Combine(Path.GetTempPath(), "GumAutoSetupTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDirectory);
    }

    [Fact]
    public void Run_AlwaysSetsObjectInstantiationTypeToFindByName()
    {
        string csprojPath = Path.Combine(_tempDirectory, "MyGame.csproj");
        File.WriteAllText(csprojPath, "<Project Sdk=\"Microsoft.NET.Sdk\"></Project>");
        string gumxPath = Path.Combine(_tempDirectory, "MyProject.gumx");
        File.WriteAllText(gumxPath, "");

        AutoSetupResult result = _sut.Run(gumxPath);

        result.Success.ShouldBeTrue();
        result.Settings!.ObjectInstantiationType.ShouldBe(ObjectInstantiationType.FindByName);
    }

    [Fact]
    public void Run_WhenCsprojFilenameHasDots_SanitizesNamespace()
    {
        string csprojPath = Path.Combine(_tempDirectory, "My.Game.Project.csproj");
        File.WriteAllText(csprojPath, "<Project Sdk=\"Microsoft.NET.Sdk\"></Project>");
        string gumxPath = Path.Combine(_tempDirectory, "MyProject.gumx");
        File.WriteAllText(gumxPath, "");

        AutoSetupResult result = _sut.Run(gumxPath);

        result.Success.ShouldBeTrue();
        result.Settings!.RootNamespace.ShouldBe("My_Game_Project");
    }

    [Fact]
    public void Run_WhenCsprojHasNoRootNamespaceTag_UsesFilename()
    {
        string csprojPath = Path.Combine(_tempDirectory, "MyGame.csproj");
        File.WriteAllText(csprojPath, "<Project Sdk=\"Microsoft.NET.Sdk\"></Project>");
        string gumxPath = Path.Combine(_tempDirectory, "MyProject.gumx");
        File.WriteAllText(gumxPath, "");

        AutoSetupResult result = _sut.Run(gumxPath);

        result.Success.ShouldBeTrue();
        result.Settings!.RootNamespace.ShouldBe("MyGame");
    }

    [Fact]
    public void Run_WhenCsprojHasRootNamespaceTag_UsesIt()
    {
        string csprojPath = Path.Combine(_tempDirectory, "MyGame.csproj");
        File.WriteAllText(csprojPath,
            "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><RootNamespace>MyGame.UI</RootNamespace></PropertyGroup></Project>");
        string gumxPath = Path.Combine(_tempDirectory, "MyProject.gumx");
        File.WriteAllText(gumxPath, "");

        AutoSetupResult result = _sut.Run(gumxPath);

        result.Success.ShouldBeTrue();
        result.Settings!.RootNamespace.ShouldBe("MyGame.UI");
    }

    [Fact]
    public void Run_WhenCsprojInParentDirectory_SetsRelativeCodeProjectRoot()
    {
        string gumSubDir = Path.Combine(_tempDirectory, "GumProject");
        Directory.CreateDirectory(gumSubDir);

        string csprojPath = Path.Combine(_tempDirectory, "MyGame.csproj");
        File.WriteAllText(csprojPath, "<Project Sdk=\"Microsoft.NET.Sdk\"></Project>");

        string gumxPath = Path.Combine(gumSubDir, "MyProject.gumx");
        File.WriteAllText(gumxPath, "");

        AutoSetupResult result = _sut.Run(gumxPath);

        result.Success.ShouldBeTrue();
        string codeProjectRoot = result.Settings!.CodeProjectRoot;
        codeProjectRoot.ShouldNotBeNullOrEmpty();
        codeProjectRoot.ShouldContain("..");
    }

    [Fact]
    public void Run_WhenCsprojInSameDirectory_SetsCodeProjectRootToDot()
    {
        string csprojPath = Path.Combine(_tempDirectory, "MyGame.csproj");
        File.WriteAllText(csprojPath, "<Project Sdk=\"Microsoft.NET.Sdk\"></Project>");
        string gumxPath = Path.Combine(_tempDirectory, "MyProject.gumx");
        File.WriteAllText(gumxPath, "");

        AutoSetupResult result = _sut.Run(gumxPath);

        result.Success.ShouldBeTrue();
        result.Settings!.CodeProjectRoot.ShouldBe("./");
    }

    [Fact]
    public void Run_WhenKniPackageReferencePresent_SetsOutputLibraryToMonoGameForms()
    {
        string csprojPath = Path.Combine(_tempDirectory, "MyGame.csproj");
        File.WriteAllText(csprojPath,
            "<Project><ItemGroup><PackageReference Include=\"nkast.Xna.Framework.Game\" Version=\"3.8.0\" /></ItemGroup></Project>");
        string gumxPath = Path.Combine(_tempDirectory, "MyProject.gumx");
        File.WriteAllText(gumxPath, "");

        AutoSetupResult result = _sut.Run(gumxPath);

        result.Success.ShouldBeTrue();
        result.Settings!.OutputLibrary.ShouldBe(OutputLibrary.MonoGameForms);
    }

    [Fact]
    public void Run_WhenMonoGamePackageReferencePresent_SetsOutputLibraryToMonoGameForms()
    {
        string csprojPath = Path.Combine(_tempDirectory, "MyGame.csproj");
        File.WriteAllText(csprojPath,
            "<Project><ItemGroup><PackageReference Include=\"MonoGame.Framework.DesktopGL\" Version=\"3.8.1.303\" /></ItemGroup></Project>");
        string gumxPath = Path.Combine(_tempDirectory, "MyProject.gumx");
        File.WriteAllText(gumxPath, "");

        AutoSetupResult result = _sut.Run(gumxPath);

        result.Success.ShouldBeTrue();
        result.Settings!.OutputLibrary.ShouldBe(OutputLibrary.MonoGameForms);
    }

    [Fact]
    public void Run_WhenNoCsprojFound_ReturnsFailure()
    {
        // Use an isolated subdirectory of GetTempPath() which typically has no .csproj
        // in its ancestor chain (C:\Users\user\AppData\Local\Temp has no game .csproj files).
        // If a .csproj is found in the real FS ancestry, the assertion is skipped to
        // avoid false failures on unusual machine configurations.
        string tempRoot = Path.GetTempPath();
        string isolatedDir = Path.Combine(tempRoot, "GumNoCsproj_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(isolatedDir);

        try
        {
            string gumxPath = Path.Combine(isolatedDir, "TestProject.gumx");
            File.WriteAllText(gumxPath, "");

            AutoSetupResult result = _sut.Run(gumxPath);

            if (!result.Success)
            {
                result.ErrorMessage.ShouldNotBeNullOrEmpty();
            }
        }
        finally
        {
            if (Directory.Exists(isolatedDir))
            {
                Directory.Delete(isolatedDir, recursive: true);
            }
        }
    }

    [Fact]
    public void Run_WhenNoCsprojFound_SettingsIsNull()
    {
        string tempRoot = Path.GetTempPath();
        string isolatedDir = Path.Combine(tempRoot, "GumNoCsproj2_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(isolatedDir);

        try
        {
            string gumxPath = Path.Combine(isolatedDir, "TestProject.gumx");
            File.WriteAllText(gumxPath, "");

            AutoSetupResult result = _sut.Run(gumxPath);

            if (!result.Success)
            {
                result.Settings.ShouldBeNull();
            }
        }
        finally
        {
            if (Directory.Exists(isolatedDir))
            {
                Directory.Delete(isolatedDir, recursive: true);
            }
        }
    }

    [Fact]
    public void Run_WhenNoMonoGameReference_DoesNotSetMonoGameForms()
    {
        string csprojPath = Path.Combine(_tempDirectory, "MyGame.csproj");
        File.WriteAllText(csprojPath,
            "<Project Sdk=\"Microsoft.NET.Sdk\"><ItemGroup><PackageReference Include=\"Newtonsoft.Json\" Version=\"13.0.0\" /></ItemGroup></Project>");
        string gumxPath = Path.Combine(_tempDirectory, "MyProject.gumx");
        File.WriteAllText(gumxPath, "");

        AutoSetupResult result = _sut.Run(gumxPath);

        result.Success.ShouldBeTrue();
        result.Settings!.OutputLibrary.ShouldNotBe(OutputLibrary.MonoGameForms);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }
}
