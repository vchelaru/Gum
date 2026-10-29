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
}
