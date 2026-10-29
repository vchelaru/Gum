#pragma warning disable CS0618
using Shouldly;
using System.IO;
using System.Linq;
using Xunit;
using static MonoGameGum.Tests.Hot.PreviewAccuracy2World;

namespace MonoGameGum.Tests.Hot;

// Fonts that change on disk while a game or Preview runs. Preview runs from the source folder, so
// there the font a reload "copies" is already where the runtime loads it from (#7168).
public partial class PreviewAccuracy2Tests
{
    [Fact]
    public void Font_SizeChanged_AndTheNewFontFileIsOnDisk_InPreview()
    {
        PreviewVerdict2 verdict = Run(
            project => Std(MainScreen(project), "Caption", "FontSize", 24),
            dir => WriteFont(dir, "Font24Pixel", advance: 12, lineHeight: 28));

        ShouldBeCorrect(verdict);
        verdict.Kinds.ShouldNotContain("FontFailure");
    }

    [Fact]
    public void Font_ChangedFntAlreadyInTheLoadFolder_IsNotReportedAsAFailure_InPreview()
    {
        PreviewAccuracy2World world = Boot();
        WriteFont(world.Dir, "Font20Pixel", advance: 13, lineHeight: 30);

        PreviewVerdict2 verdict = world.Reload(Fnt(world.Dir, "Font20Pixel"));

        verdict.Kinds.ShouldNotContain("FontFailure");
        verdict.Result!.Issues.ShouldBeEmpty();
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
    public void Font_CustomFontFile_EditedInPlace()
    {
        PreviewAccuracy2World world = BootWithCustomFont(separateBin: false);
        string custom = WriteFont(world.Dir, "Custom", advance: 22, lineHeight: 36, folder: "Fonts");
        PreviewVerdict2 verdict = world.Reload(custom);
        ShouldNotBeStaleSilently(verdict);
        ShouldBeCorrect(verdict);
    }

    [Fact]
    public void Font_CustomFontFile_EditedInPlace_GameRunningFromItsBuildFolder()
    {
        PreviewAccuracy2World world = BootWithCustomFont(separateBin: true);
        string custom = WriteFont(world.Dir, "Custom", advance: 22, lineHeight: 36, folder: "Fonts");
        PreviewVerdict2 verdict = world.Reload(custom);
        ShouldBeCorrect(verdict);
        File.Exists(Path.Combine(world.Bin, "FontCache", "Custom.fnt")).ShouldBeFalse();
    }

    [Fact]
    public void Font_CustomFontFile_EditedInPlace_WithoutANotification_GameRunningFromItsBuildFolder()
    {
        PreviewAccuracy2World world = BootWithCustomFont(separateBin: true);
        WriteFont(world.Dir, "Custom", advance: 22, lineHeight: 36, folder: "Fonts");
        PreviewVerdict2 verdict = world.Save(project => Std(MainScreen(project), "Caption", "Text", "Hello Again"), dir => { });
        ShouldBeCorrect(verdict);
    }

    [Fact]
    public void Font_CustomFontFile_PageNamedDifferentlyFromTheFont_IsReloaded()
    {
        PreviewAccuracy2World world = BootWithCustomFont(separateBin: true);
        string custom = WriteFont(world.Dir, "Custom", advance: 22, lineHeight: 36, folder: "Fonts");
        File.WriteAllText(custom, File.ReadAllText(custom).Replace("file=\"Custom.png\"", "file=\"Atlas.png\""));
        WritePng(Path.Combine(world.Dir, "Fonts", "Atlas.png"), 128, 64);
        PreviewVerdict2 verdict = world.Reload(custom);
        ShouldBeCorrect(verdict);
        verdict.Hot.ShouldContain("Fonts/Atlas.png");
    }

    [Fact]
    public void Font_PageArrivesAfterTheFnt_IsLoadedWhenItArrives_GameRunningFromItsBuildFolder()
    {
        PreviewAccuracy2World world = Boot(separateBin: true);
        world.Save(project => Std(MainScreen(project), "Caption", "FontSize", 24));
        string fnt = WriteFont(world.Dir, "Font24Pixel", advance: 12, lineHeight: 28);
        string page = Path.Combine(world.Dir, "FontCache", "Font24Pixel.png");
        File.Delete(page);
        world.Reload(fnt);
        PageOf(world).ShouldBeNull();

        WritePng(page, 256, 256);
        PreviewVerdict2 verdict = world.Reload(page);

        PageOf(world).ShouldNotBeNull();
        ShouldBeCorrect(verdict);
    }

    [Theory]
    [InlineData("Content/Custom.ttf")]
    [InlineData("Fonts/Custom.OTF")]
    public void Font_SourceFileChanged_IsReportedAsNotApplied(string relativePath)
    {
        PreviewAccuracy2World world = Boot();
        string path = Path.Combine(world.Dir, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, "glyphs");

        PreviewVerdict2 verdict = world.Reload(path);

        verdict.Reloaded.ShouldBeTrue();
        verdict.MatchesRestart.ShouldBeFalse();
        verdict.Kinds.ShouldBe("FontSourceChanged");
        world.Results.Count.ShouldBe(1);
    }

    [Fact]
    public void Font_SourceFileChanged_WithTheProjectSavedInTheSameBurst_IsOneResult()
    {
        PreviewAccuracy2World world = Boot();
        string path = Path.Combine(world.Dir, "Content", "Custom.ttf");
        File.WriteAllText(path, "glyphs");
        world.Manager.NotifyFileChanged(path);

        PreviewVerdict2 verdict = world.Save(project => Std(MainScreen(project), "Caption", "Text", "Hello Again"));

        world.Results.Count.ShouldBe(1);
        verdict.Kinds.ShouldBe("FontSourceChanged");
        verdict.MatchesRestart.ShouldBeFalse();
    }

    [Fact]
    public void Font_SourceFileChanged_ThenNothingElse_IsReportedOnce()
    {
        PreviewAccuracy2World world = Boot();
        string path = Path.Combine(world.Dir, "Content", "Custom.ttf");
        File.WriteAllText(path, "glyphs");
        world.Reload(path);

        world.Settle().Reloaded.ShouldBeFalse();
    }

    private static Microsoft.Xna.Framework.Graphics.Texture2D? PageOf(PreviewAccuracy2World world) =>
        ((RenderingLibrary.Graphics.Text)world.Find("Caption")!.RenderableComponent).BitmapFont?.Textures[0];

    // A game that has loaded the project, whose Caption uses Fonts/Custom.fnt.
    private PreviewAccuracy2World BootWithCustomFont(bool separateBin)
    {
        PreviewAccuracy2World world = new PreviewAccuracy2World();
        _worlds.Add(world);
        world.Boot(
            separateBin: separateBin,
            editBaseline: project =>
            {
                Std(MainScreen(project), "Caption", "UseCustomFont", true);
                Std(MainScreen(project), "Caption", "CustomFontFile", "Fonts/Custom.fnt");
            },
            beforeBoot: dir => WriteFont(dir, "Custom", advance: 16, lineHeight: 32, folder: "Fonts"));
        return world;
    }
}
