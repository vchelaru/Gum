using Gum.Bundle;
using Shouldly;

namespace Gum.Cli.Tests;

public class PackCommandTests : IDisposable
{
    private readonly string _tempDirectory;

    public PackCommandTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "GumCliPackTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDirectory);
    }

    [Fact]
    public void Pack_default_inclusion_includes_core_fontcache_and_external_files()
    {
        string projectPath = CreateProjectWithSpriteAndFont("DefaultIncl");
        string outputPath = Path.Combine(Path.GetDirectoryName(projectPath)!, "out.gumpkg");

        CliTestHelper result = CliTestHelper.Run("pack", projectPath, "-o", outputPath);

        result.ExitCode.ShouldBe(0);
        Dictionary<string, byte[]> entries = ReadBundleEntries(outputPath);
        entries.Keys.ShouldContain("Components/SpriteHolder.gucx");
        entries.Keys.ShouldContain("Textures/bg.png");
    }

    [Fact]
    public void Pack_exits_nonzero_when_referenced_file_missing()
    {
        string projectPath = CreateProjectWithMissingTexture("MissingFile");
        string outputPath = Path.Combine(Path.GetDirectoryName(projectPath)!, "out.gumpkg");

        CliTestHelper result = CliTestHelper.Run("pack", projectPath, "-o", outputPath);

        result.ExitCode.ShouldBe(1);
        File.Exists(outputPath).ShouldBeFalse();
    }

    [Fact]
    public void Pack_is_deterministic_across_invocations()
    {
        string projectPath = CreateProjectWithSpriteAndFont("Deterministic");
        string outputA = Path.Combine(Path.GetDirectoryName(projectPath)!, "a.gumpkg");
        string outputB = Path.Combine(Path.GetDirectoryName(projectPath)!, "b.gumpkg");

        CliTestHelper resultA = CliTestHelper.Run("pack", projectPath, "-o", outputA);
        CliTestHelper resultB = CliTestHelper.Run("pack", projectPath, "-o", outputB);

        resultA.ExitCode.ShouldBe(0);
        resultB.ExitCode.ShouldBe(0);
        byte[] bytesA = File.ReadAllBytes(outputA);
        byte[] bytesB = File.ReadAllBytes(outputB);
        bytesA.ShouldBe(bytesB);
    }

    [Fact]
    public void Pack_lists_missing_files_to_stderr()
    {
        string projectPath = CreateProjectWithMissingTexture("MissingStderr");
        string outputPath = Path.Combine(Path.GetDirectoryName(projectPath)!, "out.gumpkg");

        CliTestHelper result = CliTestHelper.Run("pack", projectPath, "-o", outputPath);

        result.ExitCode.ShouldBe(1);
        result.StandardError.ShouldContain("Textures/missing.png");
    }

    [Fact]
    public void Pack_output_starts_with_GUMP_magic_and_version_byte()
    {
        string projectPath = CreateCleanProject("MagicByte");
        string outputPath = Path.Combine(Path.GetDirectoryName(projectPath)!, "out.gumpkg");

        CliTestHelper result = CliTestHelper.Run("pack", projectPath, "-o", outputPath);

        result.ExitCode.ShouldBe(0);
        byte[] bytes = File.ReadAllBytes(outputPath);
        bytes.Length.ShouldBeGreaterThanOrEqualTo(5);
        bytes[0].ShouldBe((byte)0x47);
        bytes[1].ShouldBe((byte)0x55);
        bytes[2].ShouldBe((byte)0x4D);
        bytes[3].ShouldBe((byte)0x50);
        bytes[4].ShouldBe((byte)0x01);
    }

    [Fact]
    public void Pack_with_core_only_excludes_fontcache_and_external()
    {
        string projectPath = CreateProjectWithSpriteAndFont("CoreOnly");
        string outputPath = Path.Combine(Path.GetDirectoryName(projectPath)!, "out.gumpkg");

        CliTestHelper result = CliTestHelper.Run("pack", projectPath, "-o", outputPath, "--include", "core");

        result.ExitCode.ShouldBe(0);
        Dictionary<string, byte[]> entries = ReadBundleEntries(outputPath);
        entries.Keys.ShouldNotContain("Textures/bg.png");
        entries.Keys.ShouldContain("Components/SpriteHolder.gucx");
    }

    [Fact]
    public void Pack_writes_output_to_default_path_when_no_dash_o()
    {
        string projectPath = CreateCleanProject("DefaultOut");

        CliTestHelper result = CliTestHelper.Run("pack", projectPath);

        result.ExitCode.ShouldBe(0);
        string expectedPath = Path.Combine(
            Path.GetDirectoryName(projectPath)!,
            Path.GetFileNameWithoutExtension(projectPath) + ".gumpkg");
        File.Exists(expectedPath).ShouldBeTrue();
    }

    [Fact]
    public void Pack_writes_output_to_specified_path_when_dash_o()
    {
        string projectPath = CreateCleanProject("SpecifiedOut");
        string outputPath = Path.Combine(_tempDirectory, "custom-name.gumpkg");

        CliTestHelper result = CliTestHelper.Run("pack", projectPath, "-o", outputPath);

        result.ExitCode.ShouldBe(0);
        File.Exists(outputPath).ShouldBeTrue();
    }

    /// <summary>
    /// Creates a minimal project on disk (just the .gumx file with no element references)
    /// and returns the .gumx path. Avoids the gumcli "new" template which pulls in standard
    /// elements with default font references that show up as missing FontCache files.
    /// </summary>
    private string CreateCleanProject(string name)
    {
        string projectDir = Path.Combine(_tempDirectory, name);
        Directory.CreateDirectory(projectDir);
        string filePath = Path.Combine(projectDir, name + ".gumx");

        const string gumxContent =
            """
            <?xml version="1.0" encoding="utf-8"?>
            <GumProjectSave xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema">
            </GumProjectSave>
            """;
        File.WriteAllText(filePath, gumxContent);
        return filePath;
    }

    /// <summary>
    /// Creates a project containing a component with a sprite instance pointing at an existing texture file.
    /// </summary>
    private string CreateProjectWithSpriteAndFont(string name)
    {
        string filePath = CreateCleanProject(name);
        string projectDir = Path.GetDirectoryName(filePath)!;

        WriteSpriteHolderComponent(projectDir, sourceFileRelative: "Textures/bg.png");

        string textureDir = Path.Combine(projectDir, "Textures");
        Directory.CreateDirectory(textureDir);
        File.WriteAllBytes(Path.Combine(textureDir, "bg.png"), new byte[] { 0x89, 0x50, 0x4E, 0x47 });

        AppendComponentReference(filePath, "SpriteHolder");
        return filePath;
    }

    /// <summary>
    /// Creates a project that references a texture file that does not exist on disk.
    /// </summary>
    private string CreateProjectWithMissingTexture(string name)
    {
        string filePath = CreateCleanProject(name);
        string projectDir = Path.GetDirectoryName(filePath)!;

        WriteSpriteHolderComponent(projectDir, sourceFileRelative: "Textures/missing.png");

        AppendComponentReference(filePath, "SpriteHolder");
        return filePath;
    }

    private static void WriteSpriteHolderComponent(string projectDir, string sourceFileRelative)
    {
        string componentDir = Path.Combine(projectDir, "Components");
        Directory.CreateDirectory(componentDir);

        string componentXml = $$"""
            <?xml version="1.0" encoding="utf-8"?>
            <ComponentSave xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
              <Name>SpriteHolder</Name>
              <BaseType>Container</BaseType>
              <State>
                <Name>Default</Name>
                <Variable IsFile="true" Type="string" Name="Sprite.SourceFile" SetsValue="true">
                  <Value xsi:type="xsd:string">{{sourceFileRelative}}</Value>
                </Variable>
              </State>
              <Instance Name="Sprite" BaseType="Sprite" />
            </ComponentSave>
            """;
        File.WriteAllText(Path.Combine(componentDir, "SpriteHolder.gucx"), componentXml);
    }

    private static void AppendComponentReference(string gumxPath, string componentName)
    {
        string gumxContent = File.ReadAllText(gumxPath);
        string componentRef = $"  <ComponentReference Name=\"{componentName}\" />";
        gumxContent = gumxContent.Replace("</GumProjectSave>", componentRef + "\n</GumProjectSave>");
        File.WriteAllText(gumxPath, gumxContent);
    }

    private static Dictionary<string, byte[]> ReadBundleEntries(string bundlePath)
    {
        using FileStream stream = File.OpenRead(bundlePath);
        GumBundle bundle = GumBundleReader.Read(stream);
        return new Dictionary<string, byte[]>(bundle.Entries);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            try
            {
                Directory.Delete(_tempDirectory, recursive: true);
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }
}
