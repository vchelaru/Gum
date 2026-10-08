using Gum.ProjectServices.CodeGeneration;
using Shouldly;
using System;
using System.Collections.Generic;
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

        result.Version.ShouldBe(GumCommonSyntaxVersion);
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

        result.Version.ShouldBe(GumCommonSyntaxVersion);
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

        result.Version.ShouldBe(GumCommonSyntaxVersion);
        result.Source.ShouldBe(SyntaxVersionSource.Fallback);
    }

    [Fact]
    public void Detect_RuntimeReferencedButVersionUnreadable_StaysAtLegacyVersionZero()
    {
        // A referenced runtime whose version cannot be read is more likely an old build than a new
        // one, so only a project with no Gum runtime reference at all is assumed to be the latest.
        string gameDir = Path.Combine(_tempDirectory, "game");
        Directory.CreateDirectory(gameDir);
        File.WriteAllText(Path.Combine(gameDir, "MyGame.csproj"),
@"<Project Sdk=""Microsoft.NET.Sdk"">
  <ItemGroup>
    <PackageReference Include=""Gum.MonoGame"" Version=""1.2.3"" />
  </ItemGroup>
</Project>");

        CodeOutputProjectSettings settings = new CodeOutputProjectSettings
        {
            SyntaxVersion = "*",
            CodeProjectRoot = "./"
        };

        SyntaxVersionResult result = new SyntaxVersionDetectionService(_logger, Path.Combine(_tempDirectory, "empty-cache"))
            .Detect(settings, gameDir);

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
    public void Detect_BackslashCodeProjectRoot_FindsCsprojOnEveryOS()
    {
        // A project saved on Windows stores CodeProjectRoot with backslashes. On macOS/Linux a
        // backslash is a file-name character, so an unnormalized "..\..\" names a directory
        // that does not exist and detection fell back instead of reading the csproj.
        string referencedProjectDir = Path.Combine(_tempDirectory, "libs", "MonoGameGum");
        Directory.CreateDirectory(referencedProjectDir);
        File.WriteAllText(Path.Combine(referencedProjectDir, "AssemblyAttributes.cs"),
            "[assembly: GumSyntaxVersion(Version = 3)]\n");

        string gameDir = Path.Combine(_tempDirectory, "game");
        string gumProjectDir = Path.Combine(gameDir, "Content", "GumProject");
        Directory.CreateDirectory(gumProjectDir);
        File.WriteAllText(Path.Combine(gameDir, "MyGame.csproj"),
@"<Project Sdk=""Microsoft.NET.Sdk"">
  <ItemGroup>
    <ProjectReference Include=""..\libs\MonoGameGum\MonoGameGum.csproj"" />
  </ItemGroup>
</Project>");

        CodeOutputProjectSettings settings = new CodeOutputProjectSettings
        {
            SyntaxVersion = "*",
            CodeProjectRoot = "..\\..\\"
        };

        SyntaxVersionResult result = _sut.Detect(settings, gumProjectDir + Path.DirectorySeparatorChar);

        result.Source.ShouldBe(SyntaxVersionSource.ProjectReference);
        result.Version.ShouldBe(3);
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
    public void Detect_ProjectReference_SilkNetGum_ReadsVersion()
    {
        string referencedProjectDir = Path.Combine(_tempDirectory, "libs", "SilkNetGum");
        Directory.CreateDirectory(referencedProjectDir);

        string assemblyAttributesPath = Path.Combine(referencedProjectDir, "AssemblyAttributes.cs");
        File.WriteAllText(assemblyAttributesPath,
            "using Gum.DataTypes;\n\n[assembly: GumSyntaxVersion(Version = 3)]\n");

        string gameDir = Path.Combine(_tempDirectory, "game");
        Directory.CreateDirectory(gameDir);

        string csprojPath = Path.Combine(gameDir, "MyGame.csproj");
        File.WriteAllText(csprojPath,
@"<Project Sdk=""Microsoft.NET.Sdk"">
  <ItemGroup>
    <ProjectReference Include=""..\libs\SilkNetGum\SilkNetGum.csproj"" />
  </ItemGroup>
</Project>");

        CodeOutputProjectSettings settings = new CodeOutputProjectSettings
        {
            SyntaxVersion = "*",
            CodeProjectRoot = "./"
        };

        SyntaxVersionResult result = _sut.Detect(settings, gameDir);

        result.Version.ShouldBe(3);
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
    public void ExtractPackageReferenceVersion_NestedVersionElement_ReturnsVersion()
    {
        string csproj = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <ItemGroup>
    <PackageReference Include=""FlatRedBall.MonoGameGum"">
      <Version>2026.4.1</Version>
    </PackageReference>
  </ItemGroup>
</Project>";

        string? result = SyntaxVersionDetectionService.ExtractPackageReferenceVersion(csproj, "FlatRedBall.MonoGameGum");

        result.ShouldNotBeNull();
        result.ShouldBe("2026.4.1");
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

    #region NuGet PackageReference detection tests

    [Theory]
    [InlineData("Gum.MonoGame")]
    [InlineData("Gum.KNI")]
    [InlineData("Gum.FNA")]
    [InlineData("Gum.SkiaSharp")]
    [InlineData("Gum.raylib")]
    [InlineData("Gum.sokol")]
    [InlineData("Gum.SilkNet")]
    public void ExtractPackageReferenceVersion_RealPublishedPackageId_ReturnsVersion(string packageId)
    {
        string csproj = $@"<Project Sdk=""Microsoft.NET.Sdk"">
  <ItemGroup>
    <PackageReference Include=""{packageId}"" Version=""2026.4.1"" />
  </ItemGroup>
</Project>";

        string? result = SyntaxVersionDetectionService.ExtractPackageReferenceVersion(csproj, packageId);

        result.ShouldNotBeNull();
        result.ShouldBe("2026.4.1");
    }

    [Fact]
    public void Detect_NuGetPackage_RealPackageId_MatchesAndLogsCacheMiss()
    {
        string nuGetCacheRoot = Path.Combine(_tempDirectory, "nuget-cache");
        SyntaxVersionDetectionService sut = new SyntaxVersionDetectionService(_logger, nuGetCacheRoot);

        string gameDir = Path.Combine(_tempDirectory, "game");
        Directory.CreateDirectory(gameDir);

        string csprojPath = Path.Combine(gameDir, "MyGame.csproj");
        File.WriteAllText(csprojPath,
@"<Project Sdk=""Microsoft.NET.Sdk"">
  <ItemGroup>
    <PackageReference Include=""Gum.MonoGame"" Version=""2026.4.1"" />
  </ItemGroup>
</Project>");

        CodeOutputProjectSettings settings = new CodeOutputProjectSettings
        {
            SyntaxVersion = "*",
            CodeProjectRoot = "./"
        };

        SyntaxVersionResult result = sut.Detect(settings, gameDir);

        result.Source.ShouldBe(SyntaxVersionSource.Fallback);
        _logger.OutputMessages.ShouldContain(message => message.Contains("Gum.MonoGame") && message.Contains("NuGet cache"));
    }

    [Fact]
    public void FindDllInNuGetCache_DllNameDiffersFromPackageId_ReturnsDllPath()
    {
        string nuGetCacheRoot = Path.Combine(_tempDirectory, "nuget-cache");
        string tfmDir = Path.Combine(nuGetCacheRoot, "gum.monogame", "2026.4.1", "lib", "net8.0");
        Directory.CreateDirectory(tfmDir);
        string dllPath = Path.Combine(tfmDir, "MonoGameGum.dll");
        File.WriteAllText(dllPath, "");

        SyntaxVersionDetectionService sut = new SyntaxVersionDetectionService(_logger, nuGetCacheRoot);

        string? result = sut.FindDllInNuGetCache("Gum.MonoGame", "2026.4.1");

        result.ShouldBe(dllPath);
    }

    [Fact]
    public void FindDllInNuGetCache_PrefersHigherPriorityTfm()
    {
        string nuGetCacheRoot = Path.Combine(_tempDirectory, "nuget-cache");
        string net6Dir = Path.Combine(nuGetCacheRoot, "gum.skiasharp", "1.0.0", "lib", "net6.0");
        string net8Dir = Path.Combine(nuGetCacheRoot, "gum.skiasharp", "1.0.0", "lib", "net8.0");
        Directory.CreateDirectory(net6Dir);
        Directory.CreateDirectory(net8Dir);
        File.WriteAllText(Path.Combine(net6Dir, "SkiaGum.dll"), "");
        string net8DllPath = Path.Combine(net8Dir, "SkiaGum.dll");
        File.WriteAllText(net8DllPath, "");

        SyntaxVersionDetectionService sut = new SyntaxVersionDetectionService(_logger, nuGetCacheRoot);

        string? result = sut.FindDllInNuGetCache("Gum.SkiaSharp", "1.0.0");

        result.ShouldBe(net8DllPath);
    }

    [Fact]
    public void Detect_NuGetPackage_RealAssembly_ReadsSyntaxVersionFromAttribute()
    {
        // Uses the real GumCommon.dll already copied next to this test assembly (a transitive
        // build dependency) so ReadVersionFromAssembly's MetadataLoadContext usage is exercised
        // against a genuine multi-reference assembly instead of an empty placeholder file.
        string nuGetCacheRoot = Path.Combine(_tempDirectory, "nuget-cache");
        string tfmDir = Path.Combine(nuGetCacheRoot, "gum.monogame", "2026.1.1", "lib", "net8.0");
        Directory.CreateDirectory(tfmDir);
        File.Copy(
            Path.Combine(AppContext.BaseDirectory, "GumCommon.dll"),
            Path.Combine(tfmDir, "GumCommon.dll"));

        SyntaxVersionDetectionService sut = new SyntaxVersionDetectionService(_logger, nuGetCacheRoot);

        string gameDir = Path.Combine(_tempDirectory, "game");
        Directory.CreateDirectory(gameDir);
        string csprojPath = Path.Combine(gameDir, "MyGame.csproj");
        File.WriteAllText(csprojPath,
@"<Project Sdk=""Microsoft.NET.Sdk"">
  <ItemGroup>
    <PackageReference Include=""Gum.MonoGame"" Version=""2026.1.1"" />
  </ItemGroup>
</Project>");

        CodeOutputProjectSettings settings = new CodeOutputProjectSettings
        {
            SyntaxVersion = "*",
            CodeProjectRoot = "./"
        };

        SyntaxVersionResult result = sut.Detect(settings, gameDir);

        result.Source.ShouldBe(SyntaxVersionSource.NuGetPackage);
        result.Version.ShouldBe(5);
    }

    [Fact]
    public void Detect_NuGetPackage_AttributeTypeInSeparateReferencedAssembly_ReadsSyntaxVersion()
    {
        // Real published Gum runtime packages (Gum.MonoGame, etc.) declare their
        // [assembly: GumSyntaxVersion] attribute using a type defined in GumCommon, which
        // ships as its own separate NuGet package (FlatRedBall.GumCommon) -- so a consumer's
        // NuGet cache has the runtime's dll in one package folder and GumCommon's dll in a
        // DIFFERENT one. CrossAssemblyAttributeFixture.dll reproduces that shape. Deliberately
        // NOT copying GumCommon.dll alongside it here: reading the attribute must not require
        // resolving/loading the assembly that declares the attribute's type.
        string nuGetCacheRoot = Path.Combine(_tempDirectory, "nuget-cache");
        string tfmDir = Path.Combine(nuGetCacheRoot, "gum.monogame", "2026.1.1", "lib", "net8.0");
        Directory.CreateDirectory(tfmDir);
        File.Copy(
            Path.Combine(AppContext.BaseDirectory, "CrossAssemblyAttributeFixture.dll"),
            Path.Combine(tfmDir, "CrossAssemblyAttributeFixture.dll"));

        SyntaxVersionDetectionService sut = new SyntaxVersionDetectionService(_logger, nuGetCacheRoot);

        string gameDir = Path.Combine(_tempDirectory, "game");
        Directory.CreateDirectory(gameDir);
        string csprojPath = Path.Combine(gameDir, "MyGame.csproj");
        File.WriteAllText(csprojPath,
@"<Project Sdk=""Microsoft.NET.Sdk"">
  <ItemGroup>
    <PackageReference Include=""Gum.MonoGame"" Version=""2026.1.1"" />
  </ItemGroup>
</Project>");

        CodeOutputProjectSettings settings = new CodeOutputProjectSettings
        {
            SyntaxVersion = "*",
            CodeProjectRoot = "./"
        };

        SyntaxVersionResult result = sut.Detect(settings, gameDir);

        result.Source.ShouldBe(SyntaxVersionSource.NuGetPackage);
        result.Version.ShouldBe(5);
    }

    [Fact]
    public void Detect_NuGetPackage_Stride_ReadsSyntaxVersionFromWindowsTfmFolder()
    {
        string nuGetCacheRoot = Path.Combine(_tempDirectory, "nuget-cache");
        string tfmDir = Path.Combine(nuGetCacheRoot, "gum.stride", "2026.10.1.1", "lib", "net10.0-windows7.0");
        Directory.CreateDirectory(tfmDir);
        File.Copy(Path.Combine(AppContext.BaseDirectory, "GumCommon.dll"), Path.Combine(tfmDir, "GumCommon.dll"));

        SyntaxVersionDetectionService sut = new SyntaxVersionDetectionService(_logger, nuGetCacheRoot);

        string gameDir = Path.Combine(_tempDirectory, "game");
        Directory.CreateDirectory(gameDir);
        File.WriteAllText(Path.Combine(gameDir, "MyGame.csproj"),
@"<Project Sdk=""Microsoft.NET.Sdk"">
  <ItemGroup>
    <PackageReference Include=""Gum.Stride"" Version=""*"" />
  </ItemGroup>
</Project>");

        CodeOutputProjectSettings settings = new CodeOutputProjectSettings
        {
            SyntaxVersion = "*",
            CodeProjectRoot = "./"
        };

        SyntaxVersionResult result = sut.Detect(settings, gameDir);

        result.Source.ShouldBe(SyntaxVersionSource.NuGetPackage);
        result.Version.ShouldBe(5);
    }

    [Fact]
    public void Detect_ProjectReference_StrideGum_ReadsVersion()
    {
        string referencedProjectDir = Path.Combine(_tempDirectory, "libs", "StrideGum");
        Directory.CreateDirectory(referencedProjectDir);
        File.WriteAllText(Path.Combine(referencedProjectDir, "AssemblyAttributes.cs"),
            "using Gum.DataTypes;\n\n[assembly: GumSyntaxVersion(Version = 3)]\n");

        string gameDir = Path.Combine(_tempDirectory, "game");
        Directory.CreateDirectory(gameDir);
        File.WriteAllText(Path.Combine(gameDir, "MyGame.csproj"),
@"<Project Sdk=""Microsoft.NET.Sdk"">
  <ItemGroup>
    <ProjectReference Include=""..\libs\StrideGum\StrideGum.csproj"" />
  </ItemGroup>
</Project>");

        CodeOutputProjectSettings settings = new CodeOutputProjectSettings
        {
            SyntaxVersion = "*",
            CodeProjectRoot = "./"
        };

        SyntaxVersionResult result = _sut.Detect(settings, gameDir);

        result.Source.ShouldBe(SyntaxVersionSource.ProjectReference);
        result.Version.ShouldBe(3);
    }

    [Fact]
    public void Detect_NuGetPackage_FloatingVersion_UsesHighestRestoredVersion()
    {
        // The docs' csproj snippets use Version="*", and no folder in the NuGet cache is named "*".
        // The older cached version holds a placeholder that cannot be read, so only the newer one
        // can produce a syntax version.
        string nuGetCacheRoot = Path.Combine(_tempDirectory, "nuget-cache");
        string newerDir = Path.Combine(nuGetCacheRoot, "gum.monogame", "2026.10.1.1", "lib", "net8.0");
        string olderDir = Path.Combine(nuGetCacheRoot, "gum.monogame", "2026.9.2.1", "lib", "net8.0");
        string prereleaseDir = Path.Combine(nuGetCacheRoot, "gum.monogame", "2027.1.1.1-beta", "lib", "net8.0");
        Directory.CreateDirectory(newerDir);
        Directory.CreateDirectory(olderDir);
        Directory.CreateDirectory(prereleaseDir);
        File.Copy(Path.Combine(AppContext.BaseDirectory, "GumCommon.dll"), Path.Combine(newerDir, "GumCommon.dll"));
        File.WriteAllText(Path.Combine(olderDir, "Placeholder.dll"), "not an assembly");
        File.WriteAllText(Path.Combine(prereleaseDir, "Placeholder.dll"), "not an assembly");

        SyntaxVersionDetectionService sut = new SyntaxVersionDetectionService(_logger, nuGetCacheRoot);

        string gameDir = Path.Combine(_tempDirectory, "game");
        Directory.CreateDirectory(gameDir);
        File.WriteAllText(Path.Combine(gameDir, "MyGame.csproj"),
@"<Project Sdk=""Microsoft.NET.Sdk"">
  <ItemGroup>
    <PackageReference Include=""Gum.MonoGame"" Version=""*"" />
  </ItemGroup>
</Project>");

        CodeOutputProjectSettings settings = new CodeOutputProjectSettings
        {
            SyntaxVersion = "*",
            CodeProjectRoot = "./"
        };

        SyntaxVersionResult result = sut.Detect(settings, gameDir);

        result.Source.ShouldBe(SyntaxVersionSource.NuGetPackage);
        result.Version.ShouldBe(5);
    }

    [Theory]
    [InlineData("D:\\custom-packages", "D:\\custom-packages")]
    [InlineData("", null)]
    [InlineData(null, null)]
    public void GetDefaultNuGetCacheRoot_HonorsNuGetPackagesEnvironmentVariable(string? environmentValue, string? expected)
    {
        string result = SyntaxVersionDetectionService.GetDefaultNuGetCacheRoot(environmentValue);

        result.ShouldBe(expected ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nuget", "packages"));
    }

    [Fact]
    public void FindDllInNuGetCache_PackageNotInCache_ReturnsNull()
    {
        string nuGetCacheRoot = Path.Combine(_tempDirectory, "nuget-cache");
        Directory.CreateDirectory(nuGetCacheRoot);

        SyntaxVersionDetectionService sut = new SyntaxVersionDetectionService(_logger, nuGetCacheRoot);

        string? result = sut.FindDllInNuGetCache("Gum.raylib", "1.0.0");

        result.ShouldBeNull();
    }

    #endregion

    #region Assembly Reference (HintPath) detection

    private static int GumCommonSyntaxVersion =>
        typeof(Gum.DataTypes.GumSyntaxVersionAttribute).Assembly
            .GetCustomAttributes(typeof(Gum.DataTypes.GumSyntaxVersionAttribute), inherit: false)
            .Cast<Gum.DataTypes.GumSyntaxVersionAttribute>()
            .Single().Version;

    private static CodeOutputProjectSettings AutoDetectSettings => new CodeOutputProjectSettings
    {
        SyntaxVersion = "*",
        CodeProjectRoot = "./"
    };

    [Theory]
    [InlineData(@"<HintPath>Assets\Gum\DLLs\SkiaGum.dll</HintPath>")]
    // Unity writes <Private> before <HintPath>
    [InlineData(@"<Private>False</Private>
      <HintPath>Assets\Gum\DLLs\SkiaGum.dll</HintPath>")]
    public void Detect_AssemblyReference_RelativeHintPath_ReadsSyntaxVersionFromDll(string referenceBody)
    {
        string gameDir = Path.Combine(_tempDirectory, "game");
        string dllDir = Path.Combine(gameDir, "Assets", "Gum", "DLLs");
        Directory.CreateDirectory(dllDir);
        // GumCommon.dll carries the same [assembly: GumSyntaxVersion] as the runtimes, so it
        // stands in for a real SkiaGum.dll.
        File.Copy(Path.Combine(AppContext.BaseDirectory, "GumCommon.dll"), Path.Combine(dllDir, "SkiaGum.dll"));
        File.WriteAllText(Path.Combine(gameDir, "Assembly-CSharp.csproj"),
$@"<Project ToolsVersion=""4.0"">
  <ItemGroup>
    <Reference Include=""SkiaGum"">
      {referenceBody}
    </Reference>
  </ItemGroup>
</Project>");

        SyntaxVersionResult result = _sut.Detect(AutoDetectSettings, gameDir);

        result.Source.ShouldBe(SyntaxVersionSource.AssemblyReference);
        result.Version.ShouldBe(GumCommonSyntaxVersion);
    }

    [Fact]
    public void Detect_AssemblyReference_AbsoluteHintPath_ReadsSyntaxVersionFromDll()
    {
        string dllDir = Path.Combine(_tempDirectory, "dlls");
        Directory.CreateDirectory(dllDir);
        string dllPath = Path.Combine(dllDir, "SkiaGum.dll");
        File.Copy(Path.Combine(AppContext.BaseDirectory, "GumCommon.dll"), dllPath);

        string gameDir = Path.Combine(_tempDirectory, "game");
        Directory.CreateDirectory(gameDir);
        File.WriteAllText(Path.Combine(gameDir, "Assembly-CSharp.csproj"),
$@"<Project ToolsVersion=""4.0"">
  <ItemGroup>
    <Reference Include=""SkiaGum"">
      <HintPath>{dllPath}</HintPath>
    </Reference>
  </ItemGroup>
</Project>");

        SyntaxVersionResult result = _sut.Detect(AutoDetectSettings, gameDir);

        result.Source.ShouldBe(SyntaxVersionSource.AssemblyReference);
        result.Version.ShouldBe(GumCommonSyntaxVersion);
    }

    [Fact]
    public void Detect_AssemblyReference_DllMissing_ReturnsFallback()
    {
        string gameDir = Path.Combine(_tempDirectory, "game");
        Directory.CreateDirectory(gameDir);
        File.WriteAllText(Path.Combine(gameDir, "Assembly-CSharp.csproj"),
@"<Project ToolsVersion=""4.0"">
  <ItemGroup>
    <Reference Include=""SkiaGum"">
      <HintPath>Assets\Gum\DLLs\SkiaGum.dll</HintPath>
    </Reference>
  </ItemGroup>
</Project>");

        SyntaxVersionResult result = _sut.Detect(AutoDetectSettings, gameDir);

        result.Source.ShouldBe(SyntaxVersionSource.Fallback);
    }

    [Fact]
    public void ExtractReferenceHintPath_OtherDllWithSameSuffix_ReturnsNull()
    {
        string csproj =
@"<Reference Include=""NotSkiaGum"">
  <HintPath>Assets\NotSkiaGum.dll</HintPath>
</Reference>";

        SyntaxVersionDetectionService.ExtractReferenceHintPath(csproj, "SkiaGum").ShouldBeNull();
    }

    [Fact]
    public void ExtractReferenceHintPath_AmongOtherReferences_ReturnsMatchingPath()
    {
        string csproj =
@"<Reference Include=""GumCommon"">
  <HintPath>D:\Game\Assets\Gum\DLLs\GumCommon.dll</HintPath>
</Reference>
<Reference Include=""SkiaGum"">
  <Private>False</Private>
  <HintPath>D:\Game\Assets\Gum\DLLs\SkiaGum.dll</HintPath>
</Reference>";

        SyntaxVersionDetectionService.ExtractReferenceHintPath(csproj, "SkiaGum")
            .ShouldBe(@"D:\Game\Assets\Gum\DLLs\SkiaGum.dll");
    }

    #endregion

    #region Choosing among several .csproj files

    [Fact]
    public void Detect_SeveralCsprojFiles_PrefersAssemblyCSharp()
    {
        string referencedProjectDir = Path.Combine(_tempDirectory, "libs", "SkiaGum");
        Directory.CreateDirectory(referencedProjectDir);
        File.WriteAllText(Path.Combine(referencedProjectDir, "AssemblyAttributes.cs"),
            "using Gum.DataTypes;\n\n[assembly: GumSyntaxVersion(Version = 3)]\n");

        string gameDir = Path.Combine(_tempDirectory, "game");
        Directory.CreateDirectory(gameDir);
        // Sorts before Assembly-CSharp.csproj, so a directory-order pick lands on it.
        File.WriteAllText(Path.Combine(gameDir, "Assembly-CSharp-Editor.csproj"), "<Project></Project>");
        // A shorter name than Assembly-CSharp.csproj, so the shortest-name rule would pick it.
        File.WriteAllText(Path.Combine(gameDir, "Gum.Unity.csproj"), "<Project></Project>");
        File.WriteAllText(Path.Combine(gameDir, "Assembly-CSharp.csproj"),
@"<Project>
  <ItemGroup>
    <ProjectReference Include=""..\libs\SkiaGum\SkiaGum.csproj"" />
  </ItemGroup>
</Project>");

        SyntaxVersionResult result = _sut.Detect(AutoDetectSettings, gameDir);

        result.Source.ShouldBe(SyntaxVersionSource.ProjectReference);
        result.Version.ShouldBe(3);
    }

    [Fact]
    public void FindCsproj_NoAssemblyCSharp_PicksShortestName()
    {
        File.WriteAllText(Path.Combine(_tempDirectory, "MyGame.Tests.csproj"), "<Project></Project>");
        File.WriteAllText(Path.Combine(_tempDirectory, "MyGame.csproj"), "<Project></Project>");
        File.WriteAllText(Path.Combine(_tempDirectory, "MyGame.Android.csproj"), "<Project></Project>");

        string? result = CodeProjectCsprojLocator.FindCsproj(_tempDirectory);

        Path.GetFileName(result).ShouldBe("MyGame.csproj");
    }

    [Fact]
    public void Detect_NoCsprojInCodeProjectRoot_DoesNotReadACsprojInAParentFolder()
    {
        string gameDir = Path.Combine(_tempDirectory, "game");
        Directory.CreateDirectory(Path.Combine(gameDir, "Assets"));
        File.WriteAllText(Path.Combine(gameDir, "Assembly-CSharp.csproj"),
@"<Project>
  <ItemGroup>
    <ProjectReference Include=""..\libs\SkiaGum\SkiaGum.csproj"" />
  </ItemGroup>
</Project>");
        CodeOutputProjectSettings settings = new CodeOutputProjectSettings
        {
            SyntaxVersion = "*",
            CodeProjectRoot = "Assets/"
        };

        SyntaxVersionResult result = _sut.Detect(settings, gameDir);

        result.Source.ShouldBe(SyntaxVersionSource.Fallback);
    }

    [Fact]
    public void Detect_CsprojPathSet_ReadsThatCsprojAheadOfTheRule()
    {
        string referencedProjectDir = Path.Combine(_tempDirectory, "libs", "SkiaGum");
        Directory.CreateDirectory(referencedProjectDir);
        File.WriteAllText(Path.Combine(referencedProjectDir, "AssemblyAttributes.cs"),
            "using Gum.DataTypes;\n\n[assembly: GumSyntaxVersion(Version = 3)]\n");
        string gameDir = Path.Combine(_tempDirectory, "game");
        Directory.CreateDirectory(gameDir);
        // The rule picks Assembly-CSharp.csproj, which has no Gum reference when the game code
        // lives in an assembly definition.
        File.WriteAllText(Path.Combine(gameDir, "Assembly-CSharp.csproj"), "<Project></Project>");
        File.WriteAllText(Path.Combine(gameDir, "MyGame.Ui.csproj"),
@"<Project>
  <ItemGroup>
    <ProjectReference Include=""..\libs\SkiaGum\SkiaGum.csproj"" />
  </ItemGroup>
</Project>");
        CodeOutputProjectSettings settings = new CodeOutputProjectSettings
        {
            SyntaxVersion = "*",
            CodeProjectRoot = "./",
            CsprojPath = "MyGame.Ui.csproj"
        };

        SyntaxVersionResult result = _sut.Detect(settings, gameDir);

        result.Source.ShouldBe(SyntaxVersionSource.ProjectReference);
        result.Version.ShouldBe(3);
    }

    [Fact]
    public void Detect_CsprojPathSetToAMissingFile_FallsBackWithoutUsingAnotherCsproj()
    {
        File.WriteAllText(Path.Combine(_tempDirectory, "MyGame.csproj"), "<Project></Project>");
        CodeOutputProjectSettings settings = new CodeOutputProjectSettings
        {
            SyntaxVersion = "*",
            CodeProjectRoot = "./",
            CsprojPath = "Missing.csproj"
        };

        SyntaxVersionResult result = _sut.Detect(settings, _tempDirectory);

        result.Source.ShouldBe(SyntaxVersionSource.Fallback);
        result.Description.ShouldContain("Missing.csproj");
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
        public List<string> OutputMessages { get; } = new List<string>();
        public List<string> ErrorMessages { get; } = new List<string>();

        public void PrintOutput(string message)
        {
            OutputMessages.Add(message);
        }

        public void PrintError(string message)
        {
            ErrorMessages.Add(message);
        }
    }
}
