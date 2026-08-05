using Gum.Bundle;
using Gum.DataTypes;
using Shouldly;
using ToolsUtilities;

namespace Gum.Cli.Tests;

public class PackCommandTests : IDisposable
{
    private readonly string _tempDirectory;
    private readonly Func<string, Stream>? _previousHook;

    public PackCommandTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "GumCliPackTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDirectory);
        // GumBundleLoader.Resolve installs FileManager.CustomGetStreamFromFile as global mutable
        // state; stash/restore around it so this test class stays isolated from others.
        _previousHook = FileManager.CustomGetStreamFromFile;
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
    public void Pack_exits_with_2_when_include_is_empty()
    {
        string projectPath = CreateCleanProject("EmptyInclude");
        string outputPath = Path.Combine(Path.GetDirectoryName(projectPath)!, "out.gumpkg");

        CliTestHelper result = CliTestHelper.Run("pack", projectPath, "-o", outputPath, "--include", "");

        result.ExitCode.ShouldBe(2);
        result.StandardError.ShouldContain("--include");
        File.Exists(outputPath).ShouldBeFalse();
    }

    [Fact]
    public void Pack_exits_with_2_when_include_value_is_unknown()
    {
        string projectPath = CreateCleanProject("BogusInclude");
        string outputPath = Path.Combine(Path.GetDirectoryName(projectPath)!, "out.gumpkg");

        CliTestHelper result = CliTestHelper.Run("pack", projectPath, "-o", outputPath, "--include", "bogus");

        result.ExitCode.ShouldBe(2);
        result.StandardError.ShouldContain("Unknown");
        File.Exists(outputPath).ShouldBeFalse();
    }

    [Fact]
    public void Pack_exits_with_2_when_project_file_does_not_exist()
    {
        string projectPath = Path.Combine(_tempDirectory, "DoesNotExist", "Missing.gumx");

        CliTestHelper result = CliTestHelper.Run("pack", projectPath);

        result.ExitCode.ShouldBe(2);
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
    public void Pack_summary_output_includes_counts_and_byte_sizes()
    {
        string projectPath = CreateProjectWithSpriteAndFont("Summary");
        string outputPath = Path.Combine(Path.GetDirectoryName(projectPath)!, "out.gumpkg");

        CliTestHelper result = CliTestHelper.Run("pack", projectPath, "-o", outputPath);

        result.ExitCode.ShouldBe(0);
        result.StandardOutput.ShouldContain("Packed");
        result.StandardOutput.ShouldContain("Core:");
        result.StandardOutput.ShouldContain("FontCache:");
        result.StandardOutput.ShouldContain("External:");
        result.StandardOutput.ShouldContain("Uncompressed:");
        result.StandardOutput.ShouldContain("Compressed:");
        result.StandardOutput.ShouldContain("Ratio:");
    }

    [Fact]
    public void Pack_with_core_and_external_excludes_fontcache()
    {
        string projectPath = CreateProjectWithSpriteAndFont("CoreExternal");
        string outputPath = Path.Combine(Path.GetDirectoryName(projectPath)!, "out.gumpkg");

        CliTestHelper result = CliTestHelper.Run("pack", projectPath, "-o", outputPath, "--include", "core,external");

        result.ExitCode.ShouldBe(0);
        Dictionary<string, byte[]> entries = ReadBundleEntries(outputPath);
        entries.Keys.ShouldContain("Components/SpriteHolder.gucx");
        entries.Keys.ShouldContain("Textures/bg.png");
        entries.Keys.Any(k => k.StartsWith("FontCache/")).ShouldBeFalse();
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
    public void Pack_with_external_only_excludes_core_and_fontcache()
    {
        string projectPath = CreateProjectWithSpriteAndFont("ExternalOnly");
        string outputPath = Path.Combine(Path.GetDirectoryName(projectPath)!, "out.gumpkg");

        CliTestHelper result = CliTestHelper.Run("pack", projectPath, "-o", outputPath, "--include", "external");

        result.ExitCode.ShouldBe(0);
        Dictionary<string, byte[]> entries = ReadBundleEntries(outputPath);
        entries.Keys.ShouldContain("Textures/bg.png");
        entries.Keys.ShouldNotContain("Components/SpriteHolder.gucx");
        entries.Keys.Any(k => k.EndsWith(".gumx")).ShouldBeFalse();
    }

    [Fact]
    public void Pack_with_fontcache_only_produces_empty_bundle_when_project_has_no_fonts()
    {
        string projectPath = CreateProjectWithSpriteAndFont("FontCacheOnly");
        string outputPath = Path.Combine(Path.GetDirectoryName(projectPath)!, "out.gumpkg");

        CliTestHelper result = CliTestHelper.Run("pack", projectPath, "-o", outputPath, "--include", "fontcache");

        result.ExitCode.ShouldBe(0);
        Dictionary<string, byte[]> entries = ReadBundleEntries(outputPath);
        entries.Keys.ShouldNotContain("Components/SpriteHolder.gucx");
        entries.Keys.ShouldNotContain("Textures/bg.png");
    }

    [Fact]
    public void Pack_resolves_JSON_extensions_for_JSON_format_project()
    {
        // Regression guard for #4345: gumcli new -> convert-to-json -> pack must not report every
        // referenced screen/component/standard/behavior as missing because of a hardcoded XML extension.
        // "empty" template (not the default "forms" one): forms pulls in SourcePath-linked
        // behaviors that are only staged via `gumcli stage-forms-behaviors`, which is an
        // unrelated, pre-existing gap - not part of this JSON-extension regression.
        string projectDir = Path.Combine(_tempDirectory, "JsonProject");
        string projectPath = Path.Combine(projectDir, "JsonProject.gumx");
        CliTestHelper.Run("new", projectPath, "--template", "empty").ExitCode.ShouldBe(0);
        CliTestHelper.Run("convert-to-json", projectPath).ExitCode.ShouldBe(0);
        string gumjPath = Path.Combine(projectDir, "JsonProject.gumj");
        string outputPath = Path.Combine(projectDir, "out.gumpkg");

        // convert-to-json writes JSON siblings without touching the XML originals, so a converted
        // project has both on disk. Delete the XML originals to reproduce a real JSON-only project
        // (e.g. one where the user removed the XML after converting) - otherwise the old, broken
        // extension resolution would silently succeed by finding the XML sibling instead.
        string[] xmlExtensions = { "gumx", "gusx", "gucx", "gutx", "behx", "ganx" };
        foreach (string xmlFile in Directory.EnumerateFiles(projectDir, "*.*", SearchOption.AllDirectories)
                     .Where(f => xmlExtensions.Contains(Path.GetExtension(f).TrimStart('.'), StringComparer.OrdinalIgnoreCase)))
        {
            File.Delete(xmlFile);
        }

        CliTestHelper result = CliTestHelper.Run("pack", gumjPath, "-o", outputPath, "--include", "core");

        result.StandardError.ShouldNotContain("missing:");
        result.ExitCode.ShouldBe(0);
        Dictionary<string, byte[]> entries = ReadBundleEntries(outputPath);
        entries.Keys.ShouldContain("JsonProject.gumj");
        entries.Keys.Any(k => k.EndsWith(".gutj")).ShouldBeTrue();
    }

    [Fact]
    public void Pack_of_JSON_format_project_loads_back_through_GumBundleLoader_Resolve()
    {
        // Regression guard for #4350: gumcli pack succeeding (and gumcli check passing) is not
        // proof the resulting .gumpkg can actually be loaded - GumBundleLoader.Resolve used to
        // hardcode ".gumx" for the bundle's internal project entry, so a bundle packed from a
        // JSON-format (.gumj) project failed to load with a FileNotFoundException even though
        // packing it reported success.
        string projectDir = Path.Combine(_tempDirectory, "JsonLoadProject");
        string projectPath = Path.Combine(projectDir, "JsonLoadProject.gumx");
        CliTestHelper.Run("new", projectPath, "--template", "empty").ExitCode.ShouldBe(0);
        CliTestHelper.Run("convert-to-json", projectPath).ExitCode.ShouldBe(0);
        string gumjPath = Path.Combine(projectDir, "JsonLoadProject.gumj");
        // GumBundleLoader.Resolve derives the internal project entry's base name from the
        // .gumpkg's own base name, so the two must match - use the project's name, not "out".
        string outputPath = Path.Combine(projectDir, "JsonLoadProject.gumpkg");

        string[] xmlExtensions = { "gumx", "gusx", "gucx", "gutx", "behx", "ganx" };
        foreach (string xmlFile in Directory.EnumerateFiles(projectDir, "*.*", SearchOption.AllDirectories)
                     .Where(f => xmlExtensions.Contains(Path.GetExtension(f).TrimStart('.'), StringComparer.OrdinalIgnoreCase)))
        {
            File.Delete(xmlFile);
        }

        CliTestHelper.Run("pack", gumjPath, "-o", outputPath, "--include", "core").ExitCode.ShouldBe(0);

        ProjectResolution resolution = GumBundleLoader.Resolve(outputPath);

        resolution.UsedBundle.ShouldBeTrue();
        resolution.ResolvedGumxPath.ShouldBe(Path.Combine(projectDir, "JsonLoadProject.gumj"));

        GumProjectSave? loaded = GumProjectSave.Load(resolution.ResolvedGumxPath, out GumLoadResult loadResult);

        loadResult.ErrorMessage.ShouldBeNullOrEmpty();
        loadResult.MissingFiles.ShouldBeEmpty();
        loaded.ShouldNotBeNull();
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
        FileManager.CustomGetStreamFromFile = _previousHook;
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
