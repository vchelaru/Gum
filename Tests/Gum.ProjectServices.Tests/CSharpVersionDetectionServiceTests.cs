using System;
using System.IO;
using Gum.ProjectServices.CodeGeneration;
using Shouldly;

namespace Gum.ProjectServices.Tests;

public class CSharpVersionDetectionServiceTests : IDisposable
{
    private readonly CSharpVersionDetectionService _sut;
    private readonly string _tempDirectory;

    public CSharpVersionDetectionServiceTests()
    {
        _sut = new CSharpVersionDetectionService();
        _tempDirectory = Path.Combine(Path.GetTempPath(), "GumCSharpVersionTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDirectory);
    }

    [Theory]
    // Unity's generated project: explicit LangVersion next to an old-style framework version
    [InlineData("<LangVersion>9.0</LangVersion><TargetFrameworkVersion>v4.7.1</TargetFrameworkVersion>", 9)]
    [InlineData("<LangVersion>10</LangVersion>", 10)]
    [InlineData("<LangVersion>7.3</LangVersion>", 7)]
    [InlineData("<LangVersion>latest</LangVersion><TargetFramework>netstandard2.0</TargetFramework>", null)]
    [InlineData("<LangVersion>preview</LangVersion>", null)]
    [InlineData("<TargetFramework>net8.0</TargetFramework>", 12)]
    [InlineData("<TargetFramework>net6.0-windows</TargetFramework>", 10)]
    [InlineData("<TargetFramework>net5.0</TargetFramework>", 9)]
    [InlineData("<TargetFramework>netstandard2.1</TargetFramework>", 8)]
    [InlineData("<TargetFramework>net472</TargetFramework>", 7)]
    [InlineData("<TargetFrameworks>net8.0;netstandard2.0</TargetFrameworks>", 7)]
    [InlineData("<TargetFrameworks>$(GumFrameworks)</TargetFrameworks>", null)]
    [InlineData("<TargetFrameworkVersion>v4.7.1</TargetFrameworkVersion>", 7)]
    [InlineData("", null)]
    public void ParseCSharpLanguageVersion_ReturnsExpectedMajorVersion(string csprojBody, int? expected)
    {
        CSharpVersionDetectionService.ParseCSharpLanguageVersion($"<Project><PropertyGroup>{csprojBody}</PropertyGroup></Project>")
            .ShouldBe(expected);
    }

    [Fact]
    public void Detect_ReadsCsprojInCodeProjectRoot()
    {
        File.WriteAllText(Path.Combine(_tempDirectory, "Assembly-CSharp.csproj"),
            "<Project><PropertyGroup><LangVersion>9.0</LangVersion></PropertyGroup></Project>");

        CodeOutputProjectSettings settings = new CodeOutputProjectSettings { CodeProjectRoot = "./" };

        _sut.Detect(settings, _tempDirectory).ShouldBe(9);
    }

    [Fact]
    public void Detect_CsprojPathSet_ReadsThatCsproj()
    {
        File.WriteAllText(Path.Combine(_tempDirectory, "Game.csproj"),
            "<Project><PropertyGroup><LangVersion>12.0</LangVersion></PropertyGroup></Project>");
        string otherDirectory = Path.Combine(_tempDirectory, "src", "Game.Ui");
        Directory.CreateDirectory(otherDirectory);
        File.WriteAllText(Path.Combine(otherDirectory, "Game.Ui.csproj"),
            "<Project><PropertyGroup><LangVersion>9.0</LangVersion></PropertyGroup></Project>");

        // Backslashes, as a .codsj saved on Windows has them
        CodeOutputProjectSettings settings = new CodeOutputProjectSettings
        {
            CodeProjectRoot = "./",
            CsprojPath = "src\\Game.Ui\\Game.Ui.csproj"
        };

        _sut.Detect(settings, _tempDirectory).ShouldBe(9);
    }

    [Fact]
    public void Detect_NoCsproj_ReturnsNull()
    {
        CodeOutputProjectSettings settings = new CodeOutputProjectSettings { CodeProjectRoot = "./" };

        _sut.Detect(settings, _tempDirectory).ShouldBeNull();
    }

    [Fact]
    public void Detect_UnchangedCsproj_IsNotReadAgain_ChangedCsprojIs()
    {
        string csprojPath = Path.Combine(_tempDirectory, "MyGame.csproj");
        DateTime writeTime = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        File.WriteAllText(csprojPath, "<Project><PropertyGroup><LangVersion>9.0</LangVersion></PropertyGroup></Project>");
        File.SetLastWriteTimeUtc(csprojPath, writeTime);
        CodeOutputProjectSettings settings = new CodeOutputProjectSettings { CodeProjectRoot = "./" };

        _sut.Detect(settings, _tempDirectory).ShouldBe(9);

        // Same timestamp: the cached value stands even though the contents differ.
        File.WriteAllText(csprojPath, "<Project><PropertyGroup><LangVersion>11.0</LangVersion></PropertyGroup></Project>");
        File.SetLastWriteTimeUtc(csprojPath, writeTime);
        _sut.Detect(settings, _tempDirectory).ShouldBe(9);

        File.SetLastWriteTimeUtc(csprojPath, writeTime.AddMinutes(1));
        _sut.Detect(settings, _tempDirectory).ShouldBe(11);
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
        catch
        {
            // Best-effort temp cleanup.
        }
    }
}
