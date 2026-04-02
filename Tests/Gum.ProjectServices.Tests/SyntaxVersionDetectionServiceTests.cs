using Gum.ProjectServices.CodeGeneration;
using Shouldly;
using System;
using System.IO;

namespace Gum.ProjectServices.Tests;

public class SyntaxVersionDetectionServiceTests : IDisposable
{
    private readonly SyntaxVersionDetectionService _sut;
    private readonly TestLogger _logger;
    private readonly string _tempDirectory;

    public SyntaxVersionDetectionServiceTests()
    {
        _logger = new TestLogger();
        _sut = new SyntaxVersionDetectionService(_logger);
        _tempDirectory = Path.Combine(Path.GetTempPath(), "GumSyntaxVersionTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDirectory);
    }

    [Fact]
    public void Detect_ExplicitVersion_ReturnsManualOverride()
    {
        CodeOutputProjectSettings settings = new CodeOutputProjectSettings
        {
            SyntaxVersion = "1"
        };

        SyntaxVersionResult result = _sut.Detect(settings, _tempDirectory);

        result.Version.ShouldBe(1);
        result.Source.ShouldBe(SyntaxVersionSource.ManualOverride);
    }

    [Fact]
    public void Detect_ExplicitVersionZero_ReturnsManualOverride()
    {
        CodeOutputProjectSettings settings = new CodeOutputProjectSettings
        {
            SyntaxVersion = "0"
        };

        SyntaxVersionResult result = _sut.Detect(settings, _tempDirectory);

        result.Version.ShouldBe(0);
        result.Source.ShouldBe(SyntaxVersionSource.ManualOverride);
    }

    [Fact]
    public void Detect_NoProjectDirectory_ReturnsFallback()
    {
        CodeOutputProjectSettings settings = new CodeOutputProjectSettings
        {
            SyntaxVersion = "*",
            CodeProjectRoot = "./"
        };

        SyntaxVersionResult result = _sut.Detect(settings, projectDirectory: null);

        result.Version.ShouldBe(0);
        result.Source.ShouldBe(SyntaxVersionSource.Fallback);
    }

    [Fact]
    public void Detect_NoCsprojInDirectory_ReturnsFallback()
    {
        CodeOutputProjectSettings settings = new CodeOutputProjectSettings
        {
            SyntaxVersion = "*",
            CodeProjectRoot = "./"
        };

        SyntaxVersionResult result = _sut.Detect(settings, _tempDirectory);

        result.Version.ShouldBe(0);
        result.Source.ShouldBe(SyntaxVersionSource.Fallback);
    }

    [Fact]
    public void Detect_CsprojWithNoGumReference_ReturnsFallback()
    {
        string csprojPath = Path.Combine(_tempDirectory, "MyGame.csproj");
        File.WriteAllText(csprojPath, "<Project Sdk=\"Microsoft.NET.Sdk\"></Project>");

        CodeOutputProjectSettings settings = new CodeOutputProjectSettings
        {
            SyntaxVersion = "*",
            CodeProjectRoot = "./"
        };

        SyntaxVersionResult result = _sut.Detect(settings, _tempDirectory);

        result.Version.ShouldBe(0);
        result.Source.ShouldBe(SyntaxVersionSource.Fallback);
    }

    [Fact]
    public void Detect_ProjectReference_ReadsVersionFromAssemblyAttributes()
    {
        // Create the referenced project directory with AssemblyAttributes.cs
        string referencedProjectDir = Path.Combine(_tempDirectory, "libs", "MonoGameGum");
        Directory.CreateDirectory(referencedProjectDir);

        string assemblyAttributesPath = Path.Combine(referencedProjectDir, "AssemblyAttributes.cs");
        File.WriteAllText(assemblyAttributesPath,
            "using Gum.DataTypes;\n\n[assembly: GumSyntaxVersion(Version = 0)]\n");

        // Create the game .csproj with a ProjectReference
        string gameDir = Path.Combine(_tempDirectory, "game");
        Directory.CreateDirectory(gameDir);

        string csprojPath = Path.Combine(gameDir, "MyGame.csproj");
        File.WriteAllText(csprojPath,
@"<Project Sdk=""Microsoft.NET.Sdk"">
  <ItemGroup>
    <ProjectReference Include=""..\libs\MonoGameGum\MonoGameGum.csproj"" />
  </ItemGroup>
</Project>");

        CodeOutputProjectSettings settings = new CodeOutputProjectSettings
        {
            SyntaxVersion = "*",
            CodeProjectRoot = "./"
        };

        SyntaxVersionResult result = _sut.Detect(settings, gameDir);

        result.Version.ShouldBe(0);
        result.Source.ShouldBe(SyntaxVersionSource.ProjectReference);
    }

    [Fact]
    public void Detect_ProjectReference_ReadsHigherVersion()
    {
        string referencedProjectDir = Path.Combine(_tempDirectory, "libs", "SkiaGum");
        Directory.CreateDirectory(referencedProjectDir);

        string assemblyAttributesPath = Path.Combine(referencedProjectDir, "AssemblyAttributes.cs");
        File.WriteAllText(assemblyAttributesPath,
            "using Gum.DataTypes;\n\n[assembly: GumSyntaxVersion(Version = 2)]\n");

        string gameDir = Path.Combine(_tempDirectory, "game");
        Directory.CreateDirectory(gameDir);

        string csprojPath = Path.Combine(gameDir, "MyGame.csproj");
        File.WriteAllText(csprojPath,
@"<Project Sdk=""Microsoft.NET.Sdk"">
  <ItemGroup>
    <ProjectReference Include=""..\libs\SkiaGum\SkiaGum.csproj"" />
  </ItemGroup>
</Project>");

        CodeOutputProjectSettings settings = new CodeOutputProjectSettings
        {
            SyntaxVersion = "*",
            CodeProjectRoot = "./"
        };

        SyntaxVersionResult result = _sut.Detect(settings, gameDir);

        result.Version.ShouldBe(2);
        result.Source.ShouldBe(SyntaxVersionSource.ProjectReference);
    }

    [Fact]
    public void Detect_ProjectReference_NoAssemblyAttributes_ReturnsFallback()
    {
        string referencedProjectDir = Path.Combine(_tempDirectory, "libs", "MonoGameGum");
        Directory.CreateDirectory(referencedProjectDir);
        // No AssemblyAttributes.cs file

        string gameDir = Path.Combine(_tempDirectory, "game");
        Directory.CreateDirectory(gameDir);

        string csprojPath = Path.Combine(gameDir, "MyGame.csproj");
        File.WriteAllText(csprojPath,
@"<Project Sdk=""Microsoft.NET.Sdk"">
  <ItemGroup>
    <ProjectReference Include=""..\libs\MonoGameGum\MonoGameGum.csproj"" />
  </ItemGroup>
</Project>");

        CodeOutputProjectSettings settings = new CodeOutputProjectSettings
        {
            SyntaxVersion = "*",
            CodeProjectRoot = "./"
        };

        SyntaxVersionResult result = _sut.Detect(settings, gameDir);

        result.Version.ShouldBe(0);
        result.Source.ShouldBe(SyntaxVersionSource.Fallback);
    }

    [Fact]
    public void ParseVersionFromSourceFile_StandardFormat_ReturnsVersion()
    {
        string filePath = Path.Combine(_tempDirectory, "AssemblyAttributes.cs");
        File.WriteAllText(filePath,
            "using Gum.DataTypes;\n\n[assembly: GumSyntaxVersion(Version = 0)]\n");

        int? version = SyntaxVersionDetectionService.ParseVersionFromSourceFile(filePath);

        version.ShouldNotBeNull();
        version.Value.ShouldBe(0);
    }

    [Fact]
    public void ParseVersionFromSourceFile_HigherVersion_ReturnsVersion()
    {
        string filePath = Path.Combine(_tempDirectory, "AssemblyAttributes.cs");
        File.WriteAllText(filePath,
            "using Gum.DataTypes;\n\n[assembly: GumSyntaxVersion(Version = 5)]\n");

        int? version = SyntaxVersionDetectionService.ParseVersionFromSourceFile(filePath);

        version.ShouldNotBeNull();
        version.Value.ShouldBe(5);
    }

    [Fact]
    public void ParseVersionFromSourceFile_NoAttribute_ReturnsNull()
    {
        string filePath = Path.Combine(_tempDirectory, "AssemblyAttributes.cs");
        File.WriteAllText(filePath,
            "using System;\n\n[assembly: InternalsVisibleTo(\"Tests\")]\n");

        int? version = SyntaxVersionDetectionService.ParseVersionFromSourceFile(filePath);

        version.ShouldBeNull();
    }

    [Fact]
    public void ParseVersionFromSourceFile_MissingFile_ReturnsNull()
    {
        string filePath = Path.Combine(_tempDirectory, "DoesNotExist.cs");

        int? version = SyntaxVersionDetectionService.ParseVersionFromSourceFile(filePath);

        version.ShouldBeNull();
    }

    #region ExtractProjectReferencePath tests

    [Fact]
    public void ExtractProjectReferencePath_SelfClosingTag_ReturnsPath()
    {
        string csproj = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <ItemGroup>
    <ProjectReference Include=""..\libs\MonoGameGum\MonoGameGum.csproj"" />
  </ItemGroup>
</Project>";

        string? result = SyntaxVersionDetectionService.ExtractProjectReferencePath(csproj, "MonoGameGum");

        result.ShouldNotBeNull();
        result.ShouldBe(@"..\libs\MonoGameGum\MonoGameGum.csproj");
    }

    [Fact]
    public void ExtractProjectReferencePath_OpenCloseTag_ReturnsPath()
    {
        string csproj = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <ItemGroup>
    <ProjectReference Include=""..\..\Runtimes\SkiaGum\SkiaGum.csproj"">
      <Name>SkiaGum</Name>
    </ProjectReference>
  </ItemGroup>
</Project>";

        string? result = SyntaxVersionDetectionService.ExtractProjectReferencePath(csproj, "SkiaGum");

        result.ShouldNotBeNull();
        result.ShouldBe(@"..\..\Runtimes\SkiaGum\SkiaGum.csproj");
    }

    [Fact]
    public void ExtractProjectReferencePath_KniGum_ReturnsPath()
    {
        string csproj = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <ItemGroup>
    <ProjectReference Include=""..\MonoGameGum\KniGum\KniGum.csproj"" />
  </ItemGroup>
</Project>";

        string? result = SyntaxVersionDetectionService.ExtractProjectReferencePath(csproj, "KniGum");

        result.ShouldNotBeNull();
        result.ShouldBe(@"..\MonoGameGum\KniGum\KniGum.csproj");
    }

    [Fact]
    public void ExtractProjectReferencePath_NotPresent_ReturnsNull()
    {
        string csproj = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <ItemGroup>
    <ProjectReference Include=""..\SomeOtherLib\SomeOtherLib.csproj"" />
  </ItemGroup>
</Project>";

        string? result = SyntaxVersionDetectionService.ExtractProjectReferencePath(csproj, "MonoGameGum");

        result.ShouldBeNull();
    }

    #endregion

    #region ExtractPackageReferenceVersion tests

    [Fact]
    public void ExtractPackageReferenceVersion_StandardFormat_ReturnsVersion()
    {
        string csproj = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <ItemGroup>
    <PackageReference Include=""FlatRedBall.MonoGameGum"" Version=""2026.4.1"" />
  </ItemGroup>
</Project>";

        string? result = SyntaxVersionDetectionService.ExtractPackageReferenceVersion(csproj, "FlatRedBall.MonoGameGum");

        result.ShouldNotBeNull();
        result.ShouldBe("2026.4.1");
    }

    [Fact]
    public void ExtractPackageReferenceVersion_OpenCloseTag_ReturnsVersion()
    {
        string csproj = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <ItemGroup>
    <PackageReference Include=""FlatRedBall.MonoGameGum"" Version=""2026.3.28.2"">
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
  </ItemGroup>
</Project>";

        string? result = SyntaxVersionDetectionService.ExtractPackageReferenceVersion(csproj, "FlatRedBall.MonoGameGum");

        result.ShouldNotBeNull();
        result.ShouldBe("2026.3.28.2");
    }

    [Fact]
    public void ExtractPackageReferenceVersion_PreReleaseTag_ReturnsFullVersion()
    {
        string csproj = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <ItemGroup>
    <PackageReference Include=""FlatRedBall.MonoGameGum"" Version=""2026.4.1-preview.1"" />
  </ItemGroup>
</Project>";

        string? result = SyntaxVersionDetectionService.ExtractPackageReferenceVersion(csproj, "FlatRedBall.MonoGameGum");

        result.ShouldNotBeNull();
        result.ShouldBe("2026.4.1-preview.1");
    }

    [Fact]
    public void ExtractPackageReferenceVersion_WrongPackage_ReturnsNull()
    {
        string csproj = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <ItemGroup>
    <PackageReference Include=""Newtonsoft.Json"" Version=""13.0.1"" />
  </ItemGroup>
</Project>";

        string? result = SyntaxVersionDetectionService.ExtractPackageReferenceVersion(csproj, "FlatRedBall.MonoGameGum");

        result.ShouldBeNull();
    }

    [Fact]
    public void ExtractPackageReferenceVersion_MultiplePackages_FindsCorrectOne()
    {
        string csproj = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <ItemGroup>
    <PackageReference Include=""MonoGame.Framework.DesktopGL"" Version=""3.8.1"" />
    <PackageReference Include=""FlatRedBall.MonoGameGum"" Version=""2026.4.1"" />
    <PackageReference Include=""Newtonsoft.Json"" Version=""13.0.1"" />
  </ItemGroup>
</Project>";

        string? result = SyntaxVersionDetectionService.ExtractPackageReferenceVersion(csproj, "FlatRedBall.MonoGameGum");

        result.ShouldNotBeNull();
        result.ShouldBe("2026.4.1");
    }

    #endregion

    public void Dispose()
    {
        try
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
        catch
        {
            // Best effort cleanup
        }
    }

    private class TestLogger : ICodeGenLogger
    {
        public void PrintOutput(string message) { }
        public void PrintError(string message) { }
    }
}
