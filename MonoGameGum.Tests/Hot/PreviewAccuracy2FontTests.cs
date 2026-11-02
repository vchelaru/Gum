#pragma warning disable CS0618
using Gum.DataTypes;
using Shouldly;
using System.Linq;
using System.IO;
using Xunit;
using static MonoGameGum.Tests.Hot.PreviewAccuracy2World;

namespace MonoGameGum.Tests.Hot;

// Fonts: the variables that pick a .fnt, the .fnt regenerated or removed on disk, and sizes measured from the font.
public partial class PreviewAccuracy2Tests
{
    // A game runs from its build output, a folder other than the one the tool saves into.
    private PreviewVerdict2 RunGame(System.Action<GumProjectSave> edit, System.Action<string>? assets = null) =>
        Boot(separateBin: true).Save(edit, assets);

    private static string Fnt(string dir, string name) => Path.Combine(dir, "FontCache", name + ".fnt");


    [Fact]
    public void Font_SizeChanged_AndTheFontFileExistsInTheBuildOutputToo_AGameRunningFromItsBuildFolder()
    {
        PreviewAccuracy2World world = Boot(separateBin: true);
        PreviewVerdict2 verdict = world.Save(
            project => Std(MainScreen(project), "Caption", "FontSize", 24),
            dir => WriteFont(dir, "Font24Pixel", advance: 12, lineHeight: 28));
        ShouldBeCorrect(verdict);
    }

    [Fact]
    public void Font_NameChanged_AndTheNewFontFileIsOnDisk()
    {
        PreviewVerdict2 verdict = RunGame(
            project => Std(MainScreen(project), "Caption", "Font", "Chunky"),
            dir => WriteFont(dir, "Font20Chunky", advance: 14, lineHeight: 26));
        ShouldBeCorrect(verdict);
    }

    [Fact]
    public void Font_SizeChanged_AndTheFontFileWasNeverGenerated_ShowsTheDefaultFontLikeARestart()
    {
        ShouldBeCorrect(RunGame(project => Std(MainScreen(project), "Caption", "FontSize", 33)));
    }






    [Fact]
    public void Font_FontFileDeletedFromDisk()
    {
        PreviewAccuracy2World world = Boot(separateBin: true);
        File.Delete(Fnt(world.Dir, "Font20Pixel"));
        PreviewVerdict2 verdict = world.Reload(Fnt(world.Dir, "Font20Pixel"));
        ShouldNotBeStaleSilently(verdict);
    }

    [Fact]
    public void Font_OutlineThicknessChanged_AndTheOutlineFontIsOnDisk()
    {
        PreviewVerdict2 verdict = RunGame(
            project => Std(MainScreen(project), "Caption", "OutlineThickness", 2),
            dir => WriteFont(dir, "Font20Pixel_o2", advance: 14, lineHeight: 28));
        ShouldBeCorrect(verdict);
    }

    [Fact]
    public void Font_BoldAndItalicChanged_AndTheStyledFontIsOnDisk()
    {
        PreviewVerdict2 verdict = RunGame(
            project =>
            {
                Std(MainScreen(project), "Caption", "IsBold", true);
                Std(MainScreen(project), "Caption", "IsItalic", true);
            },
            dir => WriteFont(dir, "Font20Pixel_Italic_Bold", advance: 15, lineHeight: 24));
        ShouldBeCorrect(verdict);
    }

    [Fact]
    public void Font_SmoothingTurnedOff_AndTheFontIsOnDisk()
    {
        PreviewVerdict2 verdict = RunGame(
            project => Std(MainScreen(project), "Caption", "UseFontSmoothing", false),
            dir => WriteFont(dir, "Font20Pixel_noSmooth", advance: 11, lineHeight: 24));
        ShouldBeCorrect(verdict);
    }

    [Fact]
    public void Font_CustomFontFileSet()
    {
        PreviewVerdict2 verdict = Run(
            project =>
            {
                Std(MainScreen(project), "Caption", "UseCustomFont", true);
                Std(MainScreen(project), "Caption", "CustomFontFile", "Fonts/Custom.fnt");
            },
            dir => WriteFont(dir, "Custom", advance: 16, lineHeight: 32, folder: "Fonts"));
        ShouldBeCorrect(verdict);
    }

    [Fact]
    public void Font_CustomFontFile_SwitchedToAnotherFile()
    {
        PreviewAccuracy2World world = Boot();
        world.Save(
            project =>
            {
                Std(MainScreen(project), "Caption", "UseCustomFont", true);
                Std(MainScreen(project), "Caption", "CustomFontFile", "Fonts/Custom.fnt");
            },
            dir => WriteFont(dir, "Custom", advance: 16, lineHeight: 32, folder: "Fonts"));
        PreviewVerdict2 verdict = world.Save(
            project =>
            {
                Std(MainScreen(project), "Caption", "UseCustomFont", true);
                Std(MainScreen(project), "Caption", "CustomFontFile", "Fonts/Wide.fnt");
            },
            dir => WriteFont(dir, "Wide", advance: 20, lineHeight: 32, folder: "Fonts"));
        ShouldBeCorrect(verdict);
    }


    // A size that comes from the font: the container wraps a text whose width and height the font measures.
    [Fact]
    public void Font_SizeChanged_ResizesAContainerThatWrapsTheText()
    {
        PreviewVerdict2 verdict = RunGame(
            project => Std(MainScreen(project), "Inner", "FontSize", 24),
            dir => WriteFont(dir, "Font24Pixel", advance: 12, lineHeight: 28));
        ShouldBeCorrect(verdict);
        verdict.Hot.ShouldContain("r.measured=");
    }

    [Fact]
    public void Font_ComponentTextSizeChanged_ReachesTheInstanceOnTheScreen()
    {
        PreviewVerdict2 verdict = RunGame(
            project => Std(Component(project, "Widgets/Badge"), "Name", "FontSize", 24),
            dir => WriteFont(dir, "Font24Pixel", advance: 12, lineHeight: 28));
        ShouldBeCorrect(verdict);
    }

    [Fact]
    public void Font_TextEdited_ResizesAnAutoSizedText()
    {
        ShouldBeCorrect(RunGame(project => Std(MainScreen(project), "Caption", "Text", "A much longer caption now")));
    }

    [Fact]
    public void Font_FontScaleChanged_OnAnAutoSizedText()
    {
        ShouldBeCorrect(RunGame(project => Std(MainScreen(project), "Caption", "FontScale", 2f)));
    }

}
