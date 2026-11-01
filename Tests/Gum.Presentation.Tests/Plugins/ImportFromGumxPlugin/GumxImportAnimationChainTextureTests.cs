using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.Logic.FileWatch;
using Gum.Plugins.ImportPlugin.Manager;
using Gum.Plugins.ImportPlugin.Services;
using Gum.ProjectServices;
using Gum.ToolStates;
using ImportFromGumxPlugin.Services;
using ImportFromGumxPlugin.ViewModels;
using Moq;
using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using ToolsUtilities;

namespace Gum.Presentation.Tests.Plugins.ImportFromGumxPlugin;

/// <summary>
/// Files an imported element reaches only through another file: the textures listed inside an
/// .achx/.achj animation chain (relative to the chain) and shader files (#7363).
/// </summary>
public class GumxImportAnimationChainTextureTests : IDisposable
{
    private readonly string _root;
    private readonly string _projectDir;
    private readonly string _sourceDir;
    private readonly GumxImportService _sut;

    public GumxImportAnimationChainTextureTests()
    {
        _root = Path.Combine(Path.GetTempPath(), $"GumChainTests_{Guid.NewGuid():N}");
        _projectDir = Path.Combine(_root, "dest");
        _sourceDir = Path.Combine(_root, "src");
        Directory.CreateDirectory(_projectDir);
        Directory.CreateDirectory(_sourceDir);

        GumProjectSave project = new GumProjectSave { FullFileName = Path.Combine(_projectDir, "Test.gumx") };
        _sut = new GumxImportService(
            new FakeImportLogic(_projectDir),
            new FakeProjectState(_projectDir, project),
            new FakeFileCommands(),
            new GumxSourceService(),
            new ImportVersionGate(),
            new Mock<IFileWatchIgnoreList>().Object,
            new Mock<IFileWatchManager>().Object);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
    }

    [Fact]
    public async Task ImportAsync_AchxWithTextureNextToIt_CopiesTexture()
    {
        WriteSource("anim/hero.achx", Achx("hero.png"));
        WriteSource("anim/hero.png", "png");

        ImportResult result = await Import("anim/hero.achx");

        File.ReadAllText(Path.Combine(_projectDir, "anim", "hero.png")).ShouldBe("png");
        result.CopiedAssets.ShouldContain("anim/hero.png");
        result.SkippedElements.ShouldBeEmpty();
    }

    [Fact]
    public async Task ImportAsync_AchjWithNestedAndParentRelativeTextures_KeepsPathsRelativeToChain()
    {
        WriteSource("anim/hero.achj", Achj("sheets/walk.png", "../shared/shadow.png"));
        WriteSource("anim/sheets/walk.png", "walk");
        WriteSource("shared/shadow.png", "shadow");

        ImportResult result = await Import("anim/hero.achj");

        File.ReadAllText(Path.Combine(_projectDir, "anim", "sheets", "walk.png")).ShouldBe("walk");
        File.ReadAllText(Path.Combine(_projectDir, "shared", "shadow.png")).ShouldBe("shadow");
        result.SkippedElements.ShouldBeEmpty();
    }

    [Fact]
    public async Task ImportAsync_TwoChainsShareTexture_CopiesItOnce()
    {
        WriteSource("a/one.achx", Achx("../common/tex.png"));
        WriteSource("b/two.achx", Achx("../common/tex.png"));
        WriteSource("common/tex.png", "tex");

        ImportResult result = await Import("a/one.achx", "b/two.achx");

        result.CopiedAssets.Count(p => p == "common/tex.png").ShouldBe(1);
        result.WrittenFiles.Count(p => p.EndsWith("tex.png")).ShouldBe(1);
    }

    [Fact]
    public async Task ImportAsync_ChainTextureMissing_ReportsTextureAndStillCopiesChain()
    {
        WriteSource("anim/hero.achx", Achx("gone.png"));

        ImportResult result = await Import("anim/hero.achx");

        result.SkippedElements.ShouldContain("anim/gone.png");
        result.SkippedReasons["anim/gone.png"].ShouldContain("hero.achx");
        File.Exists(Path.Combine(_projectDir, "anim", "hero.achx")).ShouldBeTrue();
    }

    [Fact]
    public async Task ImportAsync_ChainTextureEscapesSourceFolder_IsRefusedAndNotCopied()
    {
        WriteSource("anim/hero.achx", Achx("../../evil.png"));
        File.WriteAllText(Path.Combine(_root, "evil.png"), "evil");

        ImportResult result = await Import("anim/hero.achx");

        result.SkippedElements.ShouldContain("../../evil.png");
        result.CopiedAssets.ShouldNotContain("../../evil.png");
        Directory.GetFiles(_root, "evil.png", SearchOption.AllDirectories)
            .ShouldBe(new[] { Path.Combine(_root, "evil.png") });
    }

    [Fact]
    public async Task ImportAsync_ChainTextureIsAbsolutePath_IsRefused()
    {
        string absolute = Path.Combine(_root, "abs.png");
        File.WriteAllText(absolute, "abs");
        WriteSource("anim/hero.achx", Achx(absolute));

        ImportResult result = await Import("anim/hero.achx");

        result.SkippedElements.ShouldContain(absolute);
        result.CopiedAssets.ShouldNotContain(absolute);
    }

    [Fact]
    public async Task ImportAsync_ChainFailsToLoad_ReportsChainAndImportContinues()
    {
        WriteSource("anim/broken.achx", "<not xml");
        WriteSource("pics/ok.png", "ok");

        ImportResult result = await Import("anim/broken.achx", "pics/ok.png");

        result.SkippedElements.ShouldContain("anim/broken.achx");
        File.Exists(Path.Combine(_projectDir, "anim", "broken.achx")).ShouldBeTrue();
        File.Exists(Path.Combine(_projectDir, "pics", "ok.png")).ShouldBeTrue();
    }

    [Fact]
    public async Task ImportAsync_ShaderFile_IsCopied()
    {
        WriteSource("shaders/blur.fx", "fx");

        ImportResult result = await Import(new[] { "shaders/blur.fx" }, "SourceShaderFile");

        File.ReadAllText(Path.Combine(_projectDir, "shaders", "blur.fx")).ShouldBe("fx");
        result.CopiedAssets.ShouldContain("shaders/blur.fx");
    }

    [Fact]
    public async Task ImportAsync_SlangShaderFileMissing_ElementIsSkipped()
    {
        ImportResult result = await Import(new[] { "shaders/blur.slang" }, "SourceShaderFile");

        result.SkippedElements.ShouldContain("Holder");
    }

    // ── helpers ───────────────────────────────────────────────────────────

    private Task<ImportResult> Import(params string[] sourceFiles) => Import(sourceFiles, "SourceFile");

    private async Task<ImportResult> Import(string[] sourceFiles, string variableName)
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
        return await _sut.ImportAsync(selections, source, _sourceDir, destinationSubfolder: "");
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

    private static string Achj(params string[] textures) =>
        "{\"fileRelativeTextures\":true,\"timeMeasurementUnit\":\"Second\",\"animationChains\":[{\"name\":\"a\",\"frames\":["
        + string.Join(",", textures.Select(t => $"{{\"textureName\":\"{t}\",\"frameLength\":0.1}}"))
        + "]}]}";

    private class FakeImportLogic : IImportLogic
    {
        private readonly string _projectDir;
        public FakeImportLogic(string projectDir) { _projectDir = projectDir; }
        public ComponentSave? ImportComponent(FilePath filePath, string? desiredDirectory = null, bool saveProject = true) => new ComponentSave();
        public ComponentSave? ImportComponent(ComponentSave component, bool saveProject = true)
        {
            string destPath = Path.Combine(_projectDir, "Components", $"{component.Name}.{GumProjectSave.ComponentExtension}");
            Directory.CreateDirectory(Path.GetDirectoryName(destPath)!);
            component.Save(destPath, useCompactFormat: true);
            return component;
        }
        public ScreenSave? ImportScreen(FilePath filePath, string? desiredDirectory = null, bool saveProject = true) => new ScreenSave();
        public ScreenSave? ImportScreen(ScreenSave screen, bool saveProject = true) => screen;
        public BehaviorSave ImportBehavior(FilePath filePath, string? desiredDirectory = null, bool saveProject = false) => new BehaviorSave();
    }

    private class FakeProjectState : IProjectState
    {
        public FakeProjectState(string projectDirectory, GumProjectSave gumProjectSave)
        {
            ProjectDirectory = projectDirectory.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            GumProjectSave = gumProjectSave;
        }
        public GumProjectSave GumProjectSave { get; }
        public bool EffectiveUseStandardsPalette => true;
        public byte OutlineColorR => 255;
        public byte OutlineColorG => 255;
        public byte OutlineColorB => 255;
        public string? ProjectDirectory { get; }
        public FilePath ComponentFilePath => new FilePath(Path.Combine(ProjectDirectory!, "Components"));
        public FilePath ScreenFilePath => new FilePath(Path.Combine(ProjectDirectory!, "Screens"));
        public FilePath BehaviorFilePath => new FilePath(Path.Combine(ProjectDirectory!, "Behaviors"));
        public bool NeedsToSaveProject => false;
    }

    private class FakeFileCommands : IFileCommands
    {
#pragma warning disable CS0067
        public event Action? LocalizationLoaded;
#pragma warning restore CS0067
        public FilePath? ProjectDirectory => null;
        public bool TryAutoSaveProject(bool forceSaveContainedElements = false) => false;
        public Task LoadProjectAsync(string fileName) => Task.CompletedTask;
        public void DeleteDirectory(FilePath filePath) { }
        public void ClearDirectoryContents(FilePath filePath) { }
        public void MoveToRecycleBin(FilePath filePath) { }
        public void MoveToRecycleBin(IReadOnlyList<FilePath> filePaths) { }
        public string[] GetFiles(string path) => Array.Empty<string>();
        public string[] GetFiles(string path, string searchPattern, SearchOption searchOption) => Array.Empty<string>();
        public string ReadAllText(string path) => File.ReadAllText(path);
        public void MoveDirectory(string source, string destination) { }
        public void SaveEmbeddedResource(Assembly assembly, string resourceName, string targetFileName) { }
        public void TryAutoSaveCurrentObject() { }
        public void TryAutoSaveCurrentElement() { }
        public void TryAutoSaveElement(ElementSave elementSave) { }
        public void TryAutoSaveBehavior(BehaviorSave behavior) { }
        public void TryAutoSaveObject(object objectToSave) { }
        public Task NewProjectAsync() => Task.CompletedTask;
        public Task CloseProjectAsync() => Task.CompletedTask;
        public void ForceSaveProject(bool forceSaveContainedElements = false) { }
        public void ForceSaveElement(ElementSave element) { }
        public FilePath GetFullFileName(ElementSave element) => new FilePath(string.Empty);
        public void LoadLocalizationFile() { }
        public FilePath GetFullPathXmlFile(BehaviorSave behaviorSave) => new FilePath(string.Empty);
        public FilePath? GetFullPathXmlFile(ElementSave element, string elementName) => new FilePath(string.Empty);
        public void SaveGeneralSettings() { }
        public void SaveIfDiffers(FilePath filePath, string contents) { }
    }
}
