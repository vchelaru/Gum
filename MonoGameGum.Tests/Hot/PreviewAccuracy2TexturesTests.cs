#pragma warning disable CS0618
using Gum.DataTypes;
using Gum;
using Gum.Forms.Controls;
using Gum.Graphics.Animation;
using Gum.GueDeriving;
using Gum.Managers;
using Gum.Mvvm;
using Gum.Wireframe;
using RenderingLibrary.Content;
using Shouldly;
using System.IO;
using System.Linq;
using Xunit;
using static MonoGameGum.Tests.Hot.PreviewAccuracy2World;

namespace MonoGameGum.Tests.Hot;

// Textures, animation chains, localization files: what a restart shows when the image or the file changed.
public partial class PreviewAccuracy2Tests
{
    private static string Content(string dir, string name) => Path.Combine(dir, "Content", name);

    // Several chains in one file, in the order given: (chain name, texture).
    private static void WriteChains(string dir, string name, params (string chain, string texture)[] chains)
    {
        System.Text.StringBuilder xml = new System.Text.StringBuilder();
        xml.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
        xml.AppendLine("<AnimationChainArraySave><FileRelativeTextures>true</FileRelativeTextures><TimeMeasurementUnit>Undefined</TimeMeasurementUnit><CoordinateType>Pixel</CoordinateType>");
        foreach ((string chain, string texture) in chains)
        {
            xml.AppendLine("<AnimationChain><Name>" + chain + "</Name><Frame><TextureName>" + texture
                + "</TextureName><FrameLength>0.1</FrameLength><LeftCoordinate>0</LeftCoordinate><RightCoordinate>8</RightCoordinate><TopCoordinate>0</TopCoordinate><BottomCoordinate>8</BottomCoordinate></Frame></AnimationChain>");
        }
        xml.AppendLine("</AnimationChainArraySave>");
        Directory.CreateDirectory(Path.GetDirectoryName(Content(dir, name))!);
        File.WriteAllText(Content(dir, name), xml.ToString());
    }

    private static void WriteChain(string dir, string name, string texture, float frameLength, int frames = 2, int right = 8)
    {
        System.Text.StringBuilder xml = new System.Text.StringBuilder();
        xml.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
        xml.AppendLine("<AnimationChainArraySave><FileRelativeTextures>true</FileRelativeTextures><TimeMeasurementUnit>Undefined</TimeMeasurementUnit><CoordinateType>Pixel</CoordinateType>");
        xml.AppendLine("<AnimationChain><Name>Run</Name>");
        for (int i = 0; i < frames; i++)
        {
            xml.AppendLine("<Frame><TextureName>" + texture + "</TextureName><FrameLength>" + frameLength + "</FrameLength><LeftCoordinate>" + i * 4
                + "</LeftCoordinate><RightCoordinate>" + (right + i * 4) + "</RightCoordinate><TopCoordinate>0</TopCoordinate><BottomCoordinate>8</BottomCoordinate></Frame>");
        }
        xml.AppendLine("</AnimationChain></AnimationChainArraySave>");
        Directory.CreateDirectory(Path.GetDirectoryName(Content(dir, name))!);
        File.WriteAllText(Content(dir, name), xml.ToString());
    }

    [Fact]
    public void Texture_SpriteSourceFile_ChangedToAnotherImage()
    {
        PreviewVerdict2 verdict = Run(project => Std(MainScreen(project), "Pic", "SourceFile", "Content/other.png"));
        ShouldBeCorrect(verdict);
        verdict.Hot.ShouldContain("r.tex=40x20");
    }

    [Fact]
    public void Texture_SpriteSourceFile_ChangedToAnImageAddedInTheSameSave()
    {
        ShouldBeCorrect(Run(
            project => Std(MainScreen(project), "Pic", "SourceFile", "Content/new.png"),
            dir => WritePng(Content(dir, "new.png"), 64, 48)));
    }

    [Fact]
    public void Texture_SpriteSourceFile_ChangedToAMissingFile()
    {
        ShouldBeCorrect(Run(project => Std(MainScreen(project), "Pic", "SourceFile", "Content/missing.png")));
    }

    [Fact]
    public void Texture_SpriteSourceFile_Cleared()
    {
        ShouldBeCorrect(Run(project => Std(MainScreen(project), "Pic", "SourceFile", "")));
    }

    [Fact]
    public void Texture_NineSliceSourceFile_ChangedToAnotherImage()
    {
        ShouldBeCorrect(Run(project => Std(MainScreen(project), "Frame", "SourceFile", "Content/other.png")));
    }

    [Fact]
    public void Texture_ComponentSpriteSourceFile_ChangedReachesTheInstanceOnTheScreen()
    {
        ShouldBeCorrect(Run(project => Std(Component(project, "Widgets/Badge"), "Pic", "SourceFile", "Content/other.png")));
    }

    [Fact]
    public void Texture_SpriteTextureCoordinates_EditedToACustomRectangle()
    {
        PreviewVerdict2 verdict = Run(project =>
        {
            Std(MainScreen(project), "Pic", "TextureAddress", "Custom");
            Std(MainScreen(project), "Pic", "TextureLeft", 2);
            Std(MainScreen(project), "Pic", "TextureTop", 3);
            Std(MainScreen(project), "Pic", "TextureWidth", 8);
            Std(MainScreen(project), "Pic", "TextureHeight", 6);
        });
        ShouldBeCorrect(verdict);
        verdict.Hot.ShouldContain("SourceRectangle={X:2_Y:3_Width:8_Height:6}");
    }

    // Documented: a texture replaced on disk keeps its cached copy until the game restarts, and the result says so.
    [Fact]
    public void Texture_ImageReplacedOnDiskUnderTheSameName_ReportsThatARestartIsNeeded()
    {
        PreviewAccuracy2World world = Boot();
        WritePng(Content(world.Dir, "red.png"), 32, 32);
        PreviewVerdict2 verdict = world.Reload(Content(world.Dir, "red.png"));

        verdict.Reloaded.ShouldBeTrue("a .png a visual shows is reported");
        verdict.MatchesRestart.ShouldBeFalse();
        verdict.Kinds.ShouldBe("ReloadFailed");
        verdict.Result!.Issues[0].Message.ShouldContain("restarts");
        verdict.Hot.ShouldContain("r.tex=16x16");
        verdict.Fresh.ShouldContain("r.tex=32x32");
    }

    // A file that did not exist when the sprite asked for it, and does now.
    [Fact]
    public void Texture_SpriteSourceFile_PointsToAFileThatIsCreatedLater_ReportsThatARestartIsNeeded()
    {
        PreviewAccuracy2World world = Boot();
        world.Save(project => Std(MainScreen(project), "Pic", "SourceFile", "Content/later.png"));
        WritePng(Content(world.Dir, "later.png"), 24, 24);
        PreviewVerdict2 verdict = world.Reload(Content(world.Dir, "later.png"));

        verdict.Reloaded.ShouldBeTrue();
        verdict.MatchesRestart.ShouldBeFalse();
        verdict.Hot.ShouldContain("r.tex=<null>");
        verdict.Fresh.ShouldContain("r.tex=24x24");
    }

    [Fact]
    public void Texture_SpriteSourceFile_ChangedToAnAnimationChainFile()
    {
        PreviewVerdict2 verdict = Run(
            project =>
            {
                Std(MainScreen(project), "Pic", "SourceFile", "Content/run.achx");
                Std(MainScreen(project), "Pic", "CurrentChainName", "Run");
            },
            dir => WriteChain(dir, "run.achx", "red.png", 0.1f));
        ShouldBeCorrect(verdict);
        verdict.Hot.ShouldContain("r.chains=Run:");
    }

    private PreviewAccuracy2World BootWithChain(string instance = "Pic")
    {
        PreviewAccuracy2World world = Boot();
        world.Save(
            project =>
            {
                Std(MainScreen(project), instance, "SourceFile", "Content/run.achx");
                Std(MainScreen(project), instance, "CurrentChainName", "Run");
            },
            dir => WriteChain(dir, "run.achx", "red.png", 0.1f));
        return world;
    }

    [Fact]
    public void Texture_AnimationChainFile_EditedOnDisk_IsReloadedAndReapplied()
    {
        PreviewAccuracy2World world = BootWithChain();
        WriteChain(world.Dir, "run.achx", "other.png", 0.5f, frames: 3);
        PreviewVerdict2 verdict = world.Reload(Content(world.Dir, "run.achx"));

        verdict.Reloaded.ShouldBeTrue("an .achx a Sprite plays is a file hot reload reacts to");
        ShouldBeCorrect(verdict);
    }

    [Fact]
    public void Texture_AnimationChainFile_EditedOnDisk_AndTheProjectSavedInTheSameBurst()
    {
        PreviewAccuracy2World world = BootWithChain();
        PreviewVerdict2 verdict = world.Save(
            project =>
            {
                Std(MainScreen(project), "Pic", "SourceFile", "Content/run.achx");
                Std(MainScreen(project), "Pic", "CurrentChainName", "Run");
                Std(MainScreen(project), "Box", "Red", 99);
            },
            dir => WriteChain(dir, "run.achx", "other.png", 0.5f, frames: 3));
        ShouldBeCorrect(verdict);
    }

    [Fact]
    public void Texture_AnimationChainFile_PlayedByANineSlice_EditedOnDisk_IsReloadedAndReapplied()
    {
        PreviewAccuracy2World world = BootWithChain("Frame");
        WriteChain(world.Dir, "run.achx", "other.png", 0.5f, frames: 3);
        ShouldBeCorrect(world.Reload(Content(world.Dir, "run.achx")));
    }

    [Fact]
    public void Texture_AnimationChainFile_NoVisualPlaysIt_IsIgnored()
    {
        PreviewAccuracy2World world = Boot();
        WriteChain(world.Dir, "unused.achx", "red.png", 0.5f);
        world.Reload(Content(world.Dir, "unused.achx")).Reloaded.ShouldBeFalse();
    }

    [Fact]
    public void Texture_AnimationChainFile_EditedWhileTheProjectIsCopiedToABinFolder_ReachesTheSprite()
    {
        PreviewAccuracy2World world = new PreviewAccuracy2World();
        _worlds.Add(world);
        world.Boot(
            separateBin: true,
            editBaseline: project =>
            {
                Std(MainScreen(project), "Pic", "SourceFile", "Content/run.achx");
                Std(MainScreen(project), "Pic", "CurrentChainName", "Run");
            },
            beforeBoot: dir => WriteChain(dir, "run.achx", "red.png", 0.1f));
        string cacheKey = global::ToolsUtilities.FileManager.RemoveDotDotSlash(global::ToolsUtilities.FileManager.RelativeDirectory + "Content/run.achx");
        System.IDisposable? cachedBefore = LoaderManager.Self.GetDisposable(cacheKey);
        cachedBefore.ShouldNotBeNull();
        WriteChain(world.Dir, "run.achx", "other.png", 0.5f, frames: 3);
        PreviewVerdict2 verdict = world.Reload(Content(world.Dir, "run.achx"));

        verdict.Reloaded.ShouldBeTrue();
        ShouldBeCorrect(verdict);
        // The cached chains were dropped and read again from the copy in the bin folder.
        System.IDisposable? cachedAfter = LoaderManager.Self.GetDisposable(cacheKey);
        cachedAfter.ShouldNotBeNull();
        cachedAfter.ShouldNotBeSameAs(cachedBefore);
    }

    [Fact]
    public void Texture_AnimationChainFile_PlayingChainRenamed_ReportsThatARestartIsNeeded()
    {
        PreviewAccuracy2World world = BootWithChain();
        WriteChains(world.Dir, "run.achx", ("Walk", "other.png"));
        PreviewVerdict2 verdict = world.Reload(Content(world.Dir, "run.achx"));

        verdict.MatchesRestart.ShouldBeFalse();
        verdict.Kinds.ShouldBe("ReloadFailed");
        // No chain of the new file plays, as nothing plays after a restart.
        verdict.Hot.ShouldNotContain("CurrentChainName=Walk");
    }

    [Fact]
    public void Texture_AnimationChainFile_PlayingChainDeleted_ReportsThatARestartIsNeeded()
    {
        PreviewAccuracy2World world = BootWithChain();
        WriteChains(world.Dir, "run.achx", ("Other", "other.png"));
        PreviewVerdict2 verdict = world.Reload(Content(world.Dir, "run.achx"));

        verdict.MatchesRestart.ShouldBeFalse();
        verdict.Kinds.ShouldBe("ReloadFailed");
    }

    [Fact]
    public void Texture_AnimationChainFile_ChainsReordered_PlayingChainFoundByName()
    {
        PreviewAccuracy2World world = Boot();
        world.Save(
            project =>
            {
                Std(MainScreen(project), "Pic", "SourceFile", "Content/run.achx");
                Std(MainScreen(project), "Pic", "CurrentChainName", "Run");
            },
            dir => WriteChains(dir, "run.achx", ("Run", "red.png"), ("Other", "other.png")));
        WriteChains(world.Dir, "run.achx", ("Other", "other.png"), ("Run", "red.png"));
        ShouldBeCorrect(world.Reload(Content(world.Dir, "run.achx")));
    }

    [Fact]
    public void Texture_AnimationChainFile_ChainsAssignedByCode_AreLeftAlone()
    {
        PreviewAccuracy2World world = BootWithChain();
        SpriteRuntime pic = (SpriteRuntime)world.Screen.GetGraphicalUiElementByName("Pic")!;
        AnimationChainList mine = new AnimationChainList { new AnimationChain { Name = "Mine" } };
        pic.AnimationChains = mine;

        WriteChain(world.Dir, "run.achx", "other.png", 0.5f, frames: 3);
        world.Reload(Content(world.Dir, "run.achx"));

        pic.AnimationChains.ShouldBeSameAs(mine);
    }

    [Fact]
    public void Texture_AnimationChainFile_PlayedInsideAComponentInstance_EditedOnDisk_IsReloadedAndReapplied()
    {
        PreviewAccuracy2World world = Boot();
        world.Save(
            project =>
            {
                Std(Component(project, "Widgets/Badge"), "Pic", "SourceFile", "Content/run.achx");
                Std(Component(project, "Widgets/Badge"), "Pic", "CurrentChainName", "Run");
            },
            dir => WriteChain(dir, "run.achx", "red.png", 0.1f));
        WriteChain(world.Dir, "run.achx", "other.png", 0.5f, frames: 3);
        ShouldBeCorrect(world.Reload(Content(world.Dir, "run.achx")));
    }

    [Fact]
    public void Texture_ImageOnlyAnimationChainFramesUse_ReplacedOnDisk_ReportsThatARestartIsNeeded()
    {
        PreviewAccuracy2World world = Boot();
        WritePng(Content(world.Dir, "frames.png"), 16, 16);
        world.Save(
            project =>
            {
                Std(MainScreen(project), "Pic", "SourceFile", "Content/run.achx");
                Std(MainScreen(project), "Pic", "CurrentChainName", "Run");
            },
            dir => WriteChains(dir, "run.achx", ("Run", "frames.png")));
        WritePng(Content(world.Dir, "frames.png"), 32, 32);
        PreviewVerdict2 verdict = world.Reload(Content(world.Dir, "frames.png"));

        verdict.Reloaded.ShouldBeTrue();
        verdict.MatchesRestart.ShouldBeFalse();
        verdict.Kinds.ShouldBe("ReloadFailed");
    }

    [Fact]
    public void Texture_ImageReplacedOnDisk_StaysListedInLaterResults()
    {
        PreviewAccuracy2World world = Boot();
        WritePng(Content(world.Dir, "red.png"), 32, 32);
        world.Reload(Content(world.Dir, "red.png")).MatchesRestart.ShouldBeFalse();

        PreviewVerdict2 later = world.Save(project => Std(MainScreen(project), "Box", "Red", 99));

        later.Reloaded.ShouldBeTrue();
        later.MatchesRestart.ShouldBeFalse();
        later.Kinds.ShouldContain("ReloadFailed");
    }

    [Fact]
    public void Texture_AnimationChain_CurrentChainNameChanged()
    {
        PreviewAccuracy2World world = Boot();
        world.Save(
            project => Std(MainScreen(project), "Pic", "SourceFile", "Content/run.achx"),
            dir => WriteChain(dir, "run.achx", "red.png", 0.1f));
        ShouldBeCorrect(world.Save(project =>
        {
            Std(MainScreen(project), "Pic", "SourceFile", "Content/run.achx");
            Std(MainScreen(project), "Pic", "CurrentChainName", "Run");
        }));
    }

    // ---- Localization files ----

    [Fact]
    public void Localization_CsvEditedOnDisk_IsReloadedAndReapplied()
    {
        PreviewAccuracy2World world = new PreviewAccuracy2World { UseLocalization = true };
        _worlds.Add(world);
        world.Boot();
        WriteLocalization(world.Dir, "Howdy");
        PreviewVerdict2 verdict = world.Reload(Content(world.Dir, "loc.csv"));

        verdict.Reloaded.ShouldBeTrue("a .csv the project lists is a file hot reload reacts to");
        verdict.Hot.ShouldContain("Text=Howdy");
        ShouldBeCorrect(verdict);
    }

    [Fact]
    public void Localization_CsvEditedOnDisk_AndTheProjectSavedInTheSameBurst()
    {
        PreviewAccuracy2World world = new PreviewAccuracy2World { UseLocalization = true };
        _worlds.Add(world);
        world.Boot();
        PreviewVerdict2 verdict = world.Save(project => Std(MainScreen(project), "Box", "Red", 99), dir => WriteLocalization(dir, "Howdy"));
        verdict.Hot.ShouldContain("Text=Howdy");
        ShouldBeCorrect(verdict);
    }

    [Fact]
    public void Localization_NewCsvAddedToTheProject_IsLoaded()
    {
        PreviewAccuracy2World world = new PreviewAccuracy2World { UseLocalization = true };
        _worlds.Add(world);
        world.Boot();
        PreviewVerdict2 verdict = world.Save(
            project =>
            {
                project.LocalizationFiles.Clear();
                project.LocalizationFiles.Add("Content/more.csv");
                Std(MainScreen(project), "Caption", "Text", "T_More");
            },
            dir => File.WriteAllText(Content(dir, "more.csv"), "StringId,English,Spanish\nT_More,More,Mas\n"));
        verdict.Hot.ShouldContain("Text=More");
        ShouldBeCorrect(verdict);
    }

    [Fact]
    public void Localization_ListedFileEmptiedFromTheProject_ClearsTheDatabase()
    {
        PreviewAccuracy2World world = new PreviewAccuracy2World { UseLocalization = true };
        _worlds.Add(world);
        world.Boot();
        PreviewVerdict2 verdict = world.Save(project => project.LocalizationFiles.Clear());
        ShouldBeCorrect(verdict);
    }

    [Fact]
    public void Localization_CsvNotListedByTheProject_IsIgnored()
    {
        PreviewAccuracy2World world = new PreviewAccuracy2World { UseLocalization = true };
        _worlds.Add(world);
        world.Boot();
        File.WriteAllText(Content(world.Dir, "unlisted.csv"), "StringId,English\nT_Hello,Nope\n");
        PreviewVerdict2 verdict = world.Reload(Content(world.Dir, "unlisted.csv"));

        verdict.Reloaded.ShouldBeFalse();
        verdict.Hot.ShouldContain("Text=Hello");
    }

    [Fact]
    public void Localization_ListedCsvDeletedFromDisk_ReportsTheMissingFile()
    {
        PreviewAccuracy2World world = new PreviewAccuracy2World { UseLocalization = true };
        _worlds.Add(world);
        world.Boot();
        File.Delete(Content(world.Dir, "loc.csv"));
        PreviewVerdict2 verdict = world.Reload(Content(world.Dir, "loc.csv"));

        verdict.Reloaded.ShouldBeTrue();
        verdict.Kinds.ShouldContain("MissingFile");
        verdict.MatchesRestart.ShouldBeFalse();
        // A restart with no usable file shows the string IDs.
        verdict.Hot.ShouldContain("Text=T_Hello");
    }

    [Fact]
    public void Localization_ResxSatelliteEditedOnDisk_IsReloadedAndReapplied()
    {
        PreviewAccuracy2World world = new PreviewAccuracy2World { UseLocalization = true };
        _worlds.Add(world);
        world.Boot();
        PreviewVerdict2 verdict = world.Save(
            project =>
            {
                project.LocalizationFiles.Clear();
                project.LocalizationFiles.Add("Content/Strings.resx");
            },
            dir =>
            {
                File.WriteAllText(Content(dir, "Strings.resx"), Resx("T_Hello", "Hello"));
                File.WriteAllText(Content(dir, "Strings.es.resx"), Resx("T_Hello", "Hola"));
            });
        ShouldBeCorrect(verdict);

        // Language 1 is the base file, 2 the Spanish satellite.
        CustomSetPropertyOnRenderable.LocalizationService!.CurrentLanguage = 2;
        File.WriteAllText(Content(world.Dir, "Strings.es.resx"), Resx("T_Hello", "Holaa"));
        verdict = world.Reload(Content(world.Dir, "Strings.es.resx"));

        verdict.Hot.ShouldContain("Text=Holaa");
        verdict.MatchesRestart.ShouldBeTrue();
    }

    private static string Resx(string name, string value) =>
        $"<?xml version=\"1.0\" encoding=\"utf-8\"?><root><data name=\"{name}\"><value>{value}</value></data></root>";

    private class KeyViewModel : ViewModel
    {
        public string? Key { get => Get<string>(); set => Set(value); }
    }

    [Fact]
    public void Localization_BoundTextAndBoundFormsLabel_AreTranslatedAgainAfterTheCsvChanges()
    {
        PreviewAccuracy2World world = new PreviewAccuracy2World { UseLocalization = true };
        _worlds.Add(world);
        world.Boot();
        KeyViewModel viewModel = new KeyViewModel { Key = "T_Hello" };
        TextRuntime boundText = new TextRuntime();
        world.Screen.AddChild(boundText);
        boundText.BindingContext = viewModel;
        boundText.SetBinding(nameof(TextRuntime.Text), nameof(KeyViewModel.Key));
        Label label = new Label();
        world.Screen.AddChild(label.Visual);
        label.BindingContext = viewModel;
        label.SetBinding(nameof(Label.Text), nameof(KeyViewModel.Key));
        boundText.Text.ShouldBe("Hello");
        label.Text.ShouldBe("Hello");

        WriteLocalization(world.Dir, "Howdy");
        world.Reload(Content(world.Dir, "loc.csv")).MatchesRestart.ShouldBeTrue();

        boundText.Text.ShouldBe("Howdy");
        label.Text.ShouldBe("Howdy");
    }

    // The oracle loads the saved folder fresh, which a malformed or locked file would break, so these read the tree directly.
    private static HotReloadResult? Tick(PreviewAccuracy2World world, params string[] changedFiles)
    {
        foreach (string file in changedFiles)
        {
            world.Manager.NotifyFileChanged(file);
        }
        int before = world.Results.Count;
        world.Now = world.Now.AddMilliseconds(300);
        world.Manager.Update(new[] { world.Screen });
        return world.Results.Count > before ? world.Results[world.Results.Count - 1] : null;
    }

    private static string CaptionText(PreviewAccuracy2World world) => ((TextRuntime)world.Screen.GetGraphicalUiElementByName("Caption")!).Text;

    private PreviewAccuracy2World BootLocalized()
    {
        PreviewAccuracy2World world = new PreviewAccuracy2World { UseLocalization = true };
        _worlds.Add(world);
        world.Boot();
        return world;
    }

    [Theory]
    [InlineData("StringId,English\n\"T_Hello,\"Hello\nT_Other,\"unterminated")]
    [InlineData("")]
    public void Localization_UnreadableCsv_KeepsTheOldStringsAndReports(string content)
    {
        PreviewAccuracy2World world = BootLocalized();
        File.WriteAllText(Content(world.Dir, "loc.csv"), content);

        // A failed read is tried once more before it is reported.
        Tick(world, Content(world.Dir, "loc.csv")).ShouldBeNull();
        HotReloadResult? result = Tick(world);

        result.ShouldNotBeNull();
        result.Issues.Select(issue => issue.Kind).ShouldBe(new[] { HotReloadIssueKind.ReloadFailed });
        result.MatchesRestart.ShouldBeFalse();
        CaptionText(world).ShouldBe("Hello");
        // The database itself, not only the text already on screen, is the old one.
        CustomSetPropertyOnRenderable.LocalizationService!.Translate("T_Hello").ShouldBe("Hello");
    }

    [Fact]
    public void Localization_ServiceProviderThrows_ReportsInsteadOfThrowing()
    {
        PreviewAccuracy2World world = BootLocalized();
        world.Manager.LocalizationServiceProvider = () => throw new System.InvalidOperationException("boom");
        WriteLocalization(world.Dir, "Howdy");

        HotReloadResult? result = Tick(world, Content(world.Dir, "loc.csv"));

        result.ShouldNotBeNull();
        result.MatchesRestart.ShouldBeFalse();
        result.Issues[0].Message.ShouldContain("boom");
    }

    [Fact]
    public void Localization_CsvLockedByTheEditor_IsTriedAgainAfterTheDebounce()
    {
        PreviewAccuracy2World world = BootLocalized();
        WriteLocalization(world.Dir, "Howdy");
        using (new FileStream(Content(world.Dir, "loc.csv"), FileMode.Open, FileAccess.ReadWrite, FileShare.None))
        {
            Tick(world, Content(world.Dir, "loc.csv")).ShouldBeNull("a failed read is tried again before it is reported");
        }

        HotReloadResult? result = Tick(world);

        result.ShouldNotBeNull();
        result.MatchesRestart.ShouldBeTrue();
        CaptionText(world).ShouldBe("Howdy");
    }

    [Fact]
    public void Localization_CsvStillLockedOnTheSecondTry_ReportsOnce()
    {
        PreviewAccuracy2World world = BootLocalized();
        WriteLocalization(world.Dir, "Howdy");
        using FileStream locked = new FileStream(Content(world.Dir, "loc.csv"), FileMode.Open, FileAccess.ReadWrite, FileShare.None);

        Tick(world, Content(world.Dir, "loc.csv")).ShouldBeNull();
        HotReloadResult? result = Tick(world);

        result.ShouldNotBeNull();
        result.Issues.Select(issue => issue.Kind).ShouldBe(new[] { HotReloadIssueKind.ReloadFailed });
        Tick(world).ShouldBeNull("nothing is tried again until the file changes");
        CaptionText(world).ShouldBe("Hello");
        CustomSetPropertyOnRenderable.LocalizationService!.Translate("T_Hello").ShouldBe("Hello");
    }

    [Fact]
    public void Localization_SecondLanguageSelected_StaysSelectedAfterTheCsvChanges()
    {
        PreviewAccuracy2World world = new PreviewAccuracy2World { UseLocalization = true };
        _worlds.Add(world);
        world.Boot();
        File.WriteAllText(Content(world.Dir, "loc.csv"), "StringId,English,Spanish\nT_Hello,Hello,Holaa\n");
        CustomSetPropertyOnRenderable.LocalizationService!.CurrentLanguage = 2;
        PreviewVerdict2 verdict = world.Reload(Content(world.Dir, "loc.csv"));

        verdict.Hot.ShouldContain("Text=Holaa");
        CustomSetPropertyOnRenderable.LocalizationService.CurrentLanguage.ShouldBe(2);
    }

    [Fact]
    public void Localization_TextKeyEditedToAnotherKey_UsesTheLoadedDatabase()
    {
        PreviewAccuracy2World world = new PreviewAccuracy2World { UseLocalization = true };
        _worlds.Add(world);
        world.Boot();
        ShouldBeCorrect(world.Save(project => Std(MainScreen(project), "Caption", "Text", "T_Missing")));
    }

}
