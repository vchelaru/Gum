#pragma warning disable CS0618
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Shouldly;
using System.IO;
using System.Linq;
using Xunit;
using static MonoGameGum.Tests.Hot.PreviewAccuracy2World;

namespace MonoGameGum.Tests.Hot;

// Each test here is red: the tree differs from a restart and the result says it matches, or the reverse.
// See the issues linked from the PR. The two *_InPreview / Arrives*AfterTheProjectWasSaved font tests only
// pass while the Preview font copy fails against itself, so they stay here too.
public partial class PreviewAccuracy2Tests
{
    [Fact]
    public void Font_SizeChanged_AndTheNewFontFileIsOnDisk_InPreview()
    {
        PreviewVerdict2 verdict = Run(
            project => Std(MainScreen(project), "Caption", "FontSize", 24),
            dir => WriteFont(dir, "Font24Pixel", advance: 12, lineHeight: 28));
        ShouldBeCorrect(verdict);
    }

    [Fact]
    public void Font_SizeChanged_AndTheFontFileArrivesAfterTheProjectWasSaved_GameRunningFromItsBuildFolder()
    {
        PreviewAccuracy2World world = Boot(separateBin: true);
        world.Save(project => Std(MainScreen(project), "Caption", "FontSize", 24));

        WriteFont(world.Dir, "Font24Pixel", advance: 12, lineHeight: 28);
        PreviewVerdict2 verdict = world.Reload(Fnt(world.Dir, "Font24Pixel"));

        ShouldNotBeStaleSilently(verdict);
    }

    [Fact]
    public void Font_RegeneratedUnderTheSameName_GameRunningFromItsBuildFolder()
    {
        PreviewAccuracy2World world = Boot(separateBin: true);
        WriteFont(world.Dir, "Font20Pixel", advance: 13, lineHeight: 30);
        PreviewVerdict2 verdict = world.Reload(Fnt(world.Dir, "Font20Pixel"));
        ShouldNotBeStaleSilently(verdict);
    }

    [Fact]
    public void Font_RegeneratedUnderTheSameName_ThenTheTextIsEdited()
    {
        PreviewAccuracy2World world = Boot(separateBin: true);
        WriteFont(world.Dir, "Font20Pixel", advance: 13, lineHeight: 30);
        PreviewVerdict2 verdict = world.Save(
            project => Std(MainScreen(project), "Caption", "Text", "Hello Again"),
            dir => { });
        ShouldNotBeStaleSilently(verdict);
    }

    // The font was missing when a Text first asked for it. Once it exists, the next Text that asks gets it.
    [Fact]
    public void Font_AskedForWhileMissing_ThenGeneratedAndAskedForByAnotherText()
    {
        PreviewAccuracy2World world = Boot(separateBin: true);
        world.Save(project => Std(MainScreen(project), "Caption", "FontSize", 24));
        WriteFont(world.Dir, "Font24Pixel", advance: 12, lineHeight: 28);
        world.Reload(Fnt(world.Dir, "Font24Pixel"));

        PreviewVerdict2 verdict = world.Save(project =>
        {
            Std(MainScreen(project), "Caption", "FontSize", 24);
            Std(MainScreen(project), "Inner", "FontSize", 24);
        });

        // Inner must show the generated font; only Caption (asked for before the file existed) may lag, which the previous test covers.
        verdict.Hot.Split('\n').First(line => line.TrimStart().StartsWith("Inner <Text>")).ShouldContain("r.lineH=28");
    }

    [Fact]
    public void Font_CustomFontFile_EditedInPlace()
    {
        PreviewAccuracy2World world = Boot();
        world.Save(
            project =>
            {
                Std(MainScreen(project), "Caption", "UseCustomFont", true);
                Std(MainScreen(project), "Caption", "CustomFontFile", "Fonts/Custom.fnt");
            },
            dir => WriteFont(dir, "Custom", advance: 16, lineHeight: 32, folder: "Fonts"));
        string custom = WriteFont(world.Dir, "Custom", advance: 22, lineHeight: 36, folder: "Fonts");
        PreviewVerdict2 verdict = world.Reload(custom);
        ShouldNotBeStaleSilently(verdict);
    }

    // Same font name, regenerated with other metrics: a different character set, outline or spacing.
    [Fact]
    public void Font_RegeneratedUnderTheSameName_InPreview()
    {
        PreviewAccuracy2World world = Boot();
        WriteFont(world.Dir, "Font20Pixel", advance: 13, lineHeight: 30);
        PreviewVerdict2 verdict = world.Reload(Fnt(world.Dir, "Font20Pixel"));
        ShouldNotBeStaleSilently(verdict);
    }

    // The tool saves the project first and the generator writes the .fnt a moment later.
    [Fact]
    public void Font_SizeChanged_AndTheFontFileArrivesAfterTheProjectWasSaved()
    {
        PreviewAccuracy2World world = Boot();
        world.Save(project => Std(MainScreen(project), "Caption", "FontSize", 24)).ShouldNotBeNull();

        WriteFont(world.Dir, "Font24Pixel", advance: 12, lineHeight: 28);
        PreviewVerdict2 verdict = world.Reload(Fnt(world.Dir, "Font24Pixel"));

        ShouldNotBeStaleSilently(verdict);
    }

    [Fact]
    public void Localization_CsvEditedOnDisk_AndTheProjectSavedInTheSameBurst()
    {
        PreviewAccuracy2World world = new PreviewAccuracy2World { UseLocalization = true };
        _worlds.Add(world);
        world.Boot();
        PreviewVerdict2 verdict = world.Save(project => Std(MainScreen(project), "Box", "Red", 99), dir => WriteLocalization(dir, "Howdy"));
        ShouldNotBeStaleSilently(verdict);
    }

    [Fact]
    public void Localization_NewCsvAddedToTheProject_IsNotLoaded()
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
        ShouldNotBeStaleSilently(verdict);
    }

    [Fact]
    public void Texture_AnimationChainFile_EditedOnDisk_AndTheProjectSavedInTheSameBurst()
    {
        PreviewAccuracy2World world = Boot();
        world.Save(
            project =>
            {
                Std(MainScreen(project), "Pic", "SourceFile", "Content/run.achx");
                Std(MainScreen(project), "Pic", "CurrentChainName", "Run");
            },
            dir => WriteChain(dir, "run.achx", "red.png", 0.1f));
        PreviewVerdict2 verdict = world.Save(
            project =>
            {
                Std(MainScreen(project), "Pic", "SourceFile", "Content/run.achx");
                Std(MainScreen(project), "Pic", "CurrentChainName", "Run");
                Std(MainScreen(project), "Box", "Red", 99);
            },
            dir => WriteChain(dir, "run.achx", "other.png", 0.5f, frames: 3));
        ShouldNotBeStaleSilently(verdict);
    }

    [Fact]
    public void Reference_CircularReferenceAdded_DoesNotHangAndMatchesARestart()
    {
        PreviewAccuracy2World world = new PreviewAccuracy2World();
        _worlds.Add(world);
        world.Boot(editBaseline: project => Follow(project, "Follower", "Red = Swatch.Red"));
        PreviewVerdict2 verdict = world.Save(project =>
        {
            Follow(project, "Follower", "Red = Swatch.Red");
            Follow(project, "Swatch", "Red = Follower.Red");
        });
        ShouldBeCorrect(verdict);
    }
}
