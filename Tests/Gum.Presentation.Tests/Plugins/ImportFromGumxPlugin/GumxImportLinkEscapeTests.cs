using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Logic.FileWatch;
using Gum.Plugins.ImportPlugin.Manager;
using Gum.Plugins.ImportPlugin.Services;
using ImportFromGumxPlugin.Services;
using ImportFromGumxPlugin.ViewModels;
using Moq;
using Shouldly;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Gum.ProjectServices;
using Fakes = Gum.Presentation.Tests.Plugins.ImportFromGumxPlugin.GumxImportAnimationChainTextureTests;

namespace Gum.Presentation.Tests.Plugins.ImportFromGumxPlugin;

/// <summary>
/// A junction or symlink inside the source project that resolves outside it is never followed (#7392): the
/// file is reported as skipped and not copied. Links that stay inside the project keep working.
/// </summary>
public class GumxImportLinkEscapeTests : IDisposable
{
    private const string LinkReason = "resolves outside the source project (through a link) and was not copied";

    private readonly string _root;
    private readonly string _projectDir;
    private readonly string _sourceDir;
    private readonly string _outsideDir;
    private readonly GumxImportService _sut;

    public GumxImportLinkEscapeTests()
    {
        _root = Path.Combine(Path.GetTempPath(), $"GumLinkTests_{Guid.NewGuid():N}");
        _projectDir = Path.Combine(_root, "dest");
        _sourceDir = Path.Combine(_root, "src");
        _outsideDir = Path.Combine(_root, "outside");
        Directory.CreateDirectory(_projectDir);
        Directory.CreateDirectory(_sourceDir);
        Directory.CreateDirectory(_outsideDir);
        File.WriteAllText(Path.Combine(_outsideDir, "secret.png"), "secret");

        GumProjectSave project = new GumProjectSave { FullFileName = Path.Combine(_projectDir, "Test.gumx") };
        _sut = new GumxImportService(
            new Fakes.FakeImportLogic(_projectDir),
            new Fakes.FakeProjectState(_projectDir, project),
            new Fakes.FakeFileCommands(),
            new GumxSourceService(),
            new ImportVersionGate(),
            new Mock<IFileWatchIgnoreList>().Object,
            new Mock<IFileWatchManager>().Object);
    }

    public void Dispose()
    {
        // Remove the links first, without walking into them, so the recursive delete never reaches the outside folder.
        RemoveLinks(_root);
        if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
    }

    private static void RemoveLinks(string directory)
    {
        foreach (string entry in Directory.EnumerateFileSystemEntries(directory).ToList())
        {
            if (new FileInfo(entry).LinkTarget != null)
            {
                if (Directory.Exists(entry)) Directory.Delete(entry); else File.Delete(entry);
            }
            else if (Directory.Exists(entry))
            {
                RemoveLinks(entry);
            }
        }
    }

    [SkippableFact]
    public async Task ImportAsync_SourceFileSymlinkToOutside_IsSkippedAndNotCopied()
    {
        CreateFileLink(Path.Combine(_sourceDir, "pics", "link.png"), Path.Combine(_outsideDir, "secret.png"));

        ImportResult result = await Import("pics/link.png");

        result.SkippedElements.ShouldContain("pics/link.png");
        result.SkippedReasons["pics/link.png"].ShouldContain(LinkReason);
        File.Exists(Path.Combine(_projectDir, "pics", "link.png")).ShouldBeFalse();
        result.CopiedAssets.ShouldBeEmpty();
    }

    [SkippableFact]
    public async Task ImportAsync_ChainTextureThroughDirectoryLinkToOutside_IsSkippedAndNotCopied()
    {
        WriteSource("anim/hero.achx", Achx("link/secret.png"));
        CreateDirectoryLink(Path.Combine(_sourceDir, "anim", "link"), _outsideDir);

        ImportResult result = await Import("anim/hero.achx");

        result.SkippedElements.ShouldContain("anim/link/secret.png");
        result.SkippedReasons["anim/link/secret.png"].ShouldContain(LinkReason);
        File.Exists(Path.Combine(_projectDir, "anim", "link", "secret.png")).ShouldBeFalse();
        File.Exists(Path.Combine(_projectDir, "anim", "hero.achx")).ShouldBeTrue();
    }

    [SkippableFact]
    public async Task ImportAsync_SourceFileThroughDirectoryLinkToOutside_IsSkippedAndNotCopied()
    {
        CreateDirectoryLink(Path.Combine(_sourceDir, "pics"), _outsideDir);

        ImportResult result = await Import("pics/secret.png");

        result.SkippedElements.ShouldContain("pics/secret.png");
        result.SkippedReasons["pics/secret.png"].ShouldContain(LinkReason);
        File.Exists(Path.Combine(_projectDir, "pics", "secret.png")).ShouldBeFalse();
    }

    [SkippableFact]
    public async Task ImportAsync_ShaderThroughDirectoryLinkToOutside_IsSkippedAndNotCopied()
    {
        File.WriteAllText(Path.Combine(_outsideDir, "blur.fx"), "fx");
        CreateDirectoryLink(Path.Combine(_sourceDir, "shaders"), _outsideDir);

        ImportResult result = await Import(new[] { "shaders/blur.fx" }, "SourceShaderFile");

        result.SkippedElements.ShouldContain("shaders/blur.fx");
        File.Exists(Path.Combine(_projectDir, "shaders", "blur.fx")).ShouldBeFalse();
    }

    [SkippableFact]
    public async Task ImportAsync_DirectoryLinkToInside_IsCopied()
    {
        WriteSource("real/hero.png", "hero");
        CreateDirectoryLink(Path.Combine(_sourceDir, "alias"), Path.Combine(_sourceDir, "real"));

        ImportResult result = await Import("alias/hero.png");

        File.ReadAllText(Path.Combine(_projectDir, "alias", "hero.png")).ShouldBe("hero");
        result.SkippedElements.ShouldBeEmpty();
    }

    [SkippableFact]
    public async Task ImportAsync_FileSymlinkToInside_IsCopied()
    {
        WriteSource("real/hero.png", "hero");
        CreateFileLink(Path.Combine(_sourceDir, "alias.png"), Path.Combine(_sourceDir, "real", "hero.png"));

        ImportResult result = await Import("alias.png");

        File.ReadAllText(Path.Combine(_projectDir, "alias.png")).ShouldBe("hero");
        result.SkippedElements.ShouldBeEmpty();
    }

    [SkippableFact]
    public async Task ImportAsync_LinkChainEndingOutside_IsSkipped()
    {
        string middle = Path.Combine(_sourceDir, "middle");
        CreateDirectoryLink(middle, _outsideDir);
        CreateDirectoryLink(Path.Combine(_sourceDir, "first"), middle);

        ImportResult result = await Import("first/secret.png");

        result.SkippedElements.ShouldContain("first/secret.png");
        File.Exists(Path.Combine(_projectDir, "first", "secret.png")).ShouldBeFalse();
    }

    [SkippableFact]
    public async Task ImportAsync_BrokenLinkInside_IsSkippedAndNotCopied()
    {
        CreateFileLink(Path.Combine(_sourceDir, "gone.png"), Path.Combine(_sourceDir, "nothing.png"));

        ImportResult result = await Import("gone.png");

        result.SkippedElements.ShouldContain("gone.png");
        File.Exists(Path.Combine(_projectDir, "gone.png")).ShouldBeFalse();
    }

    [SkippableFact]
    public async Task ImportAsync_RelativeSymlinkToOutside_IsSkipped()
    {
        CreateFileLink(Path.Combine(_sourceDir, "rel.png"), Path.Combine("..", "outside", "secret.png"));

        ImportResult result = await Import("rel.png");

        result.SkippedElements.ShouldContain("rel.png");
        result.SkippedReasons["rel.png"].ShouldContain(LinkReason);
        File.Exists(Path.Combine(_projectDir, "rel.png")).ShouldBeFalse();
    }

    [SkippableFact]
    public async Task ImportAsync_ProjectFolderItselfUnderLink_StillImportsItsOwnFiles()
    {
        WriteSource("pics/hero.png", "hero");
        string viaLink = Path.Combine(_root, "viaLink");
        CreateDirectoryLink(viaLink, _sourceDir);

        ImportResult result = await Import(new[] { "pics/hero.png" }, "SourceFile", sourceDir: viaLink);

        File.ReadAllText(Path.Combine(_projectDir, "pics", "hero.png")).ShouldBe("hero");
        result.SkippedElements.ShouldBeEmpty();
    }

    [SkippableFact]
    public async Task ImportAsync_ProjectFolderUnderLinkAndInnerLinkToOutside_IsSkipped()
    {
        string viaLink = Path.Combine(_root, "viaLink");
        CreateDirectoryLink(viaLink, _sourceDir);
        CreateDirectoryLink(Path.Combine(_sourceDir, "pics"), _outsideDir);

        ImportResult result = await Import(new[] { "pics/secret.png" }, "SourceFile", sourceDir: viaLink);

        result.SkippedElements.ShouldContain("pics/secret.png");
        File.Exists(Path.Combine(_projectDir, "pics", "secret.png")).ShouldBeFalse();
    }

    [Fact]
    public async Task ImportAsync_SourceFileEscapesWithDotDot_IsRefusedAndNotCopied()
    {
        ImportResult result = await Import("../outside/secret.png");

        result.SkippedElements.ShouldContain("../outside/secret.png");
        result.CopiedAssets.ShouldBeEmpty();
        Directory.GetFiles(_projectDir, "secret.png", SearchOption.AllDirectories).ShouldBeEmpty();
        File.Exists(Path.Combine(_root, "outside", "secret.png")).ShouldBeTrue();
    }

    [Fact]
    public async Task ImportAsync_ChainTextureEscapesWithDotDot_IsStillRefused()
    {
        WriteSource("anim/hero.achx", Achx("../../outside/secret.png"));

        ImportResult result = await Import("anim/hero.achx");

        result.SkippedElements.ShouldContain("../../outside/secret.png");
        result.SkippedReasons["../../outside/secret.png"].ShouldContain("leaves the source project");
    }

    [Fact]
    public async Task ImportAsync_NoLinksAnywhere_CopiesNormally()
    {
        WriteSource("pics/hero.png", "hero");

        ImportResult result = await Import("pics/hero.png");

        File.ReadAllText(Path.Combine(_projectDir, "pics", "hero.png")).ShouldBe("hero");
        result.SkippedElements.ShouldBeEmpty();
    }

    // ── helpers ───────────────────────────────────────────────────────────

    private static void CreateFileLink(string link, string target)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(link)!);
        try
        {
            File.CreateSymbolicLink(link, target);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or PlatformNotSupportedException)
        {
            throw new SkipException("Cannot create a file symlink here (Windows needs developer mode or elevation): " + exception.Message);
        }
    }

    private static void CreateDirectoryLink(string link, string target)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(link)!);
        if (OperatingSystem.IsWindows())
        {
            // A junction needs no privileges, unlike a directory symlink.
            ProcessStartInfo startInfo = new ProcessStartInfo("cmd.exe")
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
            startInfo.ArgumentList.Add("/c");
            startInfo.ArgumentList.Add("mklink");
            startInfo.ArgumentList.Add("/J");
            startInfo.ArgumentList.Add(link);
            startInfo.ArgumentList.Add(target);
            using Process process = Process.Start(startInfo)!;
            process.StandardOutput.ReadToEnd();
            process.StandardError.ReadToEnd();
            process.WaitForExit();
            if (process.ExitCode != 0 || !Directory.Exists(link))
                throw new SkipException("Cannot create a junction with mklink /J here.");
        }
        else
        {
            try
            {
                Directory.CreateSymbolicLink(link, target);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                throw new SkipException("Cannot create a directory symlink here: " + exception.Message);
            }
        }
    }

    private Task<ImportResult> Import(params string[] sourceFiles) => Import(sourceFiles, "SourceFile");

    private async Task<ImportResult> Import(string[] sourceFiles, string variableName, string? sourceDir = null)
    {
        StateSave state = new StateSave { Name = "Default" };
        for (int i = 0; i < sourceFiles.Length; i++)
        {
            state.Variables.Add(new VariableSave { Name = i == 0 ? variableName : variableName + i, Value = sourceFiles[i] });
        }
        ComponentSave component = new ComponentSave { Name = "Holder", States = new() { state } };
        string componentFile = Path.Combine(_sourceDir, "Components", "Holder." + GumProjectSave.ComponentExtension);
        Directory.CreateDirectory(Path.GetDirectoryName(componentFile)!);
        File.WriteAllText(componentFile, "<ComponentSave><Name>Holder</Name></ComponentSave>");

        GumProjectSave source = new GumProjectSave();
        source.Components.Add(component);
        ImportSelections selections = new ImportSelections
        {
            DirectComponents = new() { component },
            TransitiveComponents = new(),
            DirectScreens = new(),
            Behaviors = new(),
            Standards = new(),
        };
        return await _sut.ImportAsync(selections, source, sourceDir ?? _sourceDir, destinationSubfolder: "");
    }

    private void WriteSource(string relativePath, string contents)
    {
        string path = Path.Combine(_sourceDir, relativePath.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, contents);
    }

    private static string Achx(params string[] textures) =>
        "<AnimationChainArraySave><FileRelativeTextures>true</FileRelativeTextures><TimeMeasurementUnit>Second</TimeMeasurementUnit><AnimationChain><Name>a</Name>"
        + string.Concat(textures.Select(t => $"<Frame><TextureName>{t}</TextureName><FrameLength>0.1</FrameLength></Frame>"))
        + "</AnimationChain></AnimationChainArraySave>";
}
