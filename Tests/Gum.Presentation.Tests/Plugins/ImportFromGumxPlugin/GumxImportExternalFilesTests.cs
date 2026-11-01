using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Logic.FileWatch;
using Gum.Plugins.ImportPlugin.Manager;
using Gum.Plugins.ImportPlugin.Services;
using Gum.ProjectServices;
using ImportFromGumxPlugin.Services;
using ImportFromGumxPlugin.ViewModels;
using Moq;
using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Fakes = Gum.Presentation.Tests.Plugins.ImportFromGumxPlugin.GumxImportAnimationChainTextureTests;

namespace Gum.Presentation.Tests.Plugins.ImportFromGumxPlugin;

/// <summary>
/// Asset files the source project names from outside its folder (a <c>..</c> path or an absolute path) are copied
/// into the destination's <c>external</c> folder and every reference follows the copy (#7392).
/// </summary>
public class GumxImportExternalFilesTests : IDisposable
{
    private readonly string _root;
    private readonly string _projectDir;
    private readonly string _sourceDir;
    private readonly GumxImportService _sut;

    public GumxImportExternalFilesTests()
    {
        _root = Path.Combine(Path.GetTempPath(), $"GumExternalTests_{Guid.NewGuid():N}");
        _projectDir = Path.Combine(_root, "dest");
        _sourceDir = Path.Combine(_root, "game", "UI", "src");
        Directory.CreateDirectory(_projectDir);
        Directory.CreateDirectory(_sourceDir);

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
        if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
    }

    [Fact]
    public async Task ImportAsync_SourceFileOutsideProject_IsCopiedToExternalAndEveryStateFollowsIt()
    {
        WriteOutside("Content/x.png", "x");

        ImportResult result = await Import(DefaultAndCategoryState("../../Content/x.png"));

        File.ReadAllText(Path.Combine(_projectDir, "external", "x.png")).ShouldBe("x");
        string saved = ReadSavedComponent();
        Regex.Matches(saved, "external/x\\.png").Count.ShouldBe(2);
        AssertNothingPointsOutside(saved);
        result.SkippedElements.ShouldBeEmpty();
    }

    [Fact]
    public async Task ImportAsync_HostileRelativePath_IsCopiedIntoExternalOnly()
    {
        WriteOutside("../hostile/win.png", "win");

        await Import(DefaultAndCategoryState("..\\..\\..\\hostile\\win.png"));

        File.ReadAllText(Path.Combine(_projectDir, "external", "win.png")).ShouldBe("win");
        Directory.GetFiles(_projectDir, "win.png", SearchOption.AllDirectories)
            .ShouldBe(new[] { Path.Combine(_projectDir, "external", "win.png") });
        Directory.GetFiles(_root, "win.png", SearchOption.AllDirectories).Length.ShouldBe(2);
        AssertNothingPointsOutside(ReadSavedComponent());
    }

    [Fact]
    public async Task ImportAsync_AbsolutePathOutsideProject_IsCopiedToExternal()
    {
        string absolute = WriteOutside("Content/abs.png", "abs");

        await Import(DefaultAndCategoryState(absolute));

        File.ReadAllText(Path.Combine(_projectDir, "external", "abs.png")).ShouldBe("abs");
        AssertNothingPointsOutside(ReadSavedComponent());
    }

    [Fact]
    public async Task ImportAsync_SecondImportOfSameSource_DoesNotDuplicateFiles()
    {
        WriteOutside("Content/x.png", "x");

        await Import(DefaultAndCategoryState("../../Content/x.png"));
        await Import(DefaultAndCategoryState("../../Content/x.png"));

        Directory.GetFiles(Path.Combine(_projectDir, "external")).Select(Path.GetFileName)
            .ShouldBe(new[] { "x.png" });
    }

    [Fact]
    public async Task ImportAsync_ExistingExternalFileWithSameContent_IsReused()
    {
        WriteOutside("Content/x.png", "same");
        Directory.CreateDirectory(Path.Combine(_projectDir, "external"));
        File.WriteAllText(Path.Combine(_projectDir, "external", "x.png"), "same");

        await Import(DefaultAndCategoryState("../../Content/x.png"));

        Directory.GetFiles(Path.Combine(_projectDir, "external")).Length.ShouldBe(1);
        ReadSavedComponent().ShouldContain("external/x.png");
    }

    [Fact]
    public async Task ImportAsync_ExistingExternalFileWithDifferentContent_GetsUniqueNameAndLeavesTheOtherAlone()
    {
        WriteOutside("Content/x.png", "new");
        Directory.CreateDirectory(Path.Combine(_projectDir, "external"));
        File.WriteAllText(Path.Combine(_projectDir, "external", "x.png"), "old");

        await Import(DefaultAndCategoryState("../../Content/x.png"));

        File.ReadAllText(Path.Combine(_projectDir, "external", "x.png")).ShouldBe("old");
        File.ReadAllText(Path.Combine(_projectDir, "external", "x-2.png")).ShouldBe("new");
        string saved = ReadSavedComponent();
        saved.ShouldContain("external/x-2.png");
        saved.ShouldNotContain("external/x.png");
    }

    [Fact]
    public async Task ImportAsync_TwoOutsideFilesWithSameName_GetDistinctNames()
    {
        WriteOutside("A/x.png", "a");
        WriteOutside("B/x.png", "b");

        await Import(DefaultAndCategoryState("../../A/x.png", secondValue: "../../B/x.png"));

        File.ReadAllText(Path.Combine(_projectDir, "external", "x.png")).ShouldBe("a");
        File.ReadAllText(Path.Combine(_projectDir, "external", "x-2.png")).ShouldBe("b");
        string saved = ReadSavedComponent();
        saved.ShouldContain("external/x.png");
        saved.ShouldContain("external/x-2.png");
    }

    [Fact]
    public async Task ImportAsync_ExistingExternalFileDiffersOnlyByCase_GetsUniqueName()
    {
        WriteOutside("Content/x.png", "new");
        Directory.CreateDirectory(Path.Combine(_projectDir, "external"));
        File.WriteAllText(Path.Combine(_projectDir, "external", "X.PNG"), "old");

        await Import(DefaultAndCategoryState("../../Content/x.png"));

        ReadSavedComponent().ShouldContain("external/x-2.png");
        File.ReadAllText(Path.Combine(_projectDir, "external", "x-2.png")).ShouldBe("new");
    }

    [Fact]
    public async Task ImportAsync_UserFolderCalledExternal_IsReused()
    {
        WriteOutside("Content/x.png", "x");
        Directory.CreateDirectory(Path.Combine(_projectDir, "External"));

        await Import(DefaultAndCategoryState("../../Content/x.png"));

        File.ReadAllText(Path.Combine(_projectDir, "External", "x.png")).ShouldBe("x");
        ReadSavedComponent().ShouldContain("External/x.png");
        Directory.GetDirectories(_projectDir).Count(d => Path.GetFileName(d).Equals("external", StringComparison.OrdinalIgnoreCase)).ShouldBe(1);
    }

    [Fact]
    public async Task ImportAsync_MissingOutsideFile_IsReportedAndNoExternalFolderIsCreated()
    {
        ImportResult result = await Import(DefaultAndCategoryState("../../Content/gone.png"));

        result.SkippedElements.ShouldContain("Holder");
        Directory.Exists(Path.Combine(_projectDir, "external")).ShouldBeFalse();
    }

    [Fact]
    public async Task ImportAsync_NothingOutside_CreatesNoExternalFolder()
    {
        File.WriteAllText(Path.Combine(_sourceDir, "in.png"), "in");

        await Import(DefaultAndCategoryState("in.png"));

        Directory.Exists(Path.Combine(_projectDir, "external")).ShouldBeFalse();
        File.Exists(Path.Combine(_projectDir, "in.png")).ShouldBeTrue();
    }

    [Fact]
    public async Task ImportAsync_ChainOutsideProject_TexturesFollowItWithRelativeLayoutKept()
    {
        WriteOutside("Content/anim/hero.achx", Achx("sheets/walk.png", "../shared/shadow.png"));
        WriteOutside("Content/anim/sheets/walk.png", "walk");
        WriteOutside("Content/shared/shadow.png", "shadow");

        ImportResult result = await Import(DefaultAndCategoryState("../../Content/anim/hero.achx"));

        File.Exists(Path.Combine(_projectDir, "external", "anim", "hero.achx")).ShouldBeTrue();
        File.ReadAllText(Path.Combine(_projectDir, "external", "anim", "sheets", "walk.png")).ShouldBe("walk");
        File.ReadAllText(Path.Combine(_projectDir, "external", "shared", "shadow.png")).ShouldBe("shadow");
        ReadSavedComponent().ShouldContain("external/anim/hero.achx");
        result.SkippedElements.ShouldBeEmpty();
        AssertNothingPointsOutside(ReadSavedComponent());
    }

    // ── helpers ───────────────────────────────────────────────────────────

    private string WriteOutside(string relativeToGameFolder, string contents)
    {
        string path = Path.Combine(_root, "game", relativeToGameFolder.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, contents);
        return path;
    }

    // Every state that sets the variable: Default and a category state, so a rewrite that only
    // touches Default is caught.
    private static ComponentSave DefaultAndCategoryState(string value, string? secondValue = null)
    {
        ComponentSave component = new ComponentSave { Name = "Holder" };
        StateSave defaultState = new StateSave { Name = "Default" };
        defaultState.Variables.Add(new VariableSave { Name = "SourceFile", Value = value });
        component.States.Add(defaultState);
        StateSaveCategory category = new StateSaveCategory { Name = "Look" };
        StateSave other = new StateSave { Name = "Other" };
        other.Variables.Add(new VariableSave { Name = "SourceFile", Value = secondValue ?? value });
        category.States.Add(other);
        component.Categories.Add(category);
        return component;
    }

    private async Task<ImportResult> Import(ComponentSave component)
    {
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

    private string ReadSavedComponent() =>
        File.ReadAllText(Path.Combine(_projectDir, "Components", "Holder." + GumProjectSave.ComponentExtension));

    // No file path in the imported component leaves the destination, and each one that names a file finds it.
    private void AssertNothingPointsOutside(string savedComponent)
    {
        foreach (System.Text.RegularExpressions.Match match in Regex.Matches(savedComponent, @">([^<>]+\.(png|achx|achj|fx|fnt|ttf))<", RegexOptions.IgnoreCase))
        {
            string value = match.Groups[1].Value;
            Path.IsPathRooted(value).ShouldBeFalse(value);
            value.ShouldNotContain("..");
            File.Exists(Path.Combine(_projectDir, value.Replace('/', Path.DirectorySeparatorChar))).ShouldBeTrue(value);
        }
    }

    private static string Achx(params string[] textures) =>
        "<AnimationChainArraySave><FileRelativeTextures>true</FileRelativeTextures><TimeMeasurementUnit>Second</TimeMeasurementUnit><AnimationChain><Name>a</Name>"
        + string.Concat(textures.Select(t => $"<Frame><TextureName>{t}</TextureName><FrameLength>0.1</FrameLength></Frame>"))
        + "</AnimationChain></AnimationChainArraySave>";
}
