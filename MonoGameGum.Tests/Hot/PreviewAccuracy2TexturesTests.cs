#pragma warning disable CS0618
using Gum.DataTypes;
using Gum.Managers;
using Shouldly;
using System.IO;
using Xunit;
using static MonoGameGum.Tests.Hot.PreviewAccuracy2World;

namespace MonoGameGum.Tests.Hot;

// Textures, animation chains, localization files: what a restart shows when the image or the file changed.
public partial class PreviewAccuracy2Tests
{
    private static string Content(string dir, string name) => Path.Combine(dir, "Content", name);

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

    // Documented: a texture replaced on disk keeps its cached copy until the game restarts.
    [Fact]
    public void Texture_ImageReplacedOnDiskUnderTheSameName_IsNotWatched()
    {
        PreviewAccuracy2World world = Boot();
        WritePng(Content(world.Dir, "red.png"), 32, 32);
        PreviewVerdict2 verdict = world.Reload(Content(world.Dir, "red.png"));

        verdict.Reloaded.ShouldBeFalse("a .png is not a file hot reload reacts to");
        verdict.Hot.ShouldContain("r.tex=16x16");
        verdict.Fresh.ShouldContain("r.tex=32x32");
    }

    // A file that did not exist when the sprite asked for it, and does now.
    [Fact]
    public void Texture_SpriteSourceFile_PointsToAFileThatIsCreatedLater_IsNotWatched()
    {
        PreviewAccuracy2World world = Boot();
        world.Save(project => Std(MainScreen(project), "Pic", "SourceFile", "Content/later.png"));
        WritePng(Content(world.Dir, "later.png"), 24, 24);
        PreviewVerdict2 verdict = world.Reload(Content(world.Dir, "later.png"));

        verdict.Reloaded.ShouldBeFalse();
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

    [Fact]
    public void Texture_AnimationChainFile_EditedOnDisk_IsNotWatched()
    {
        PreviewAccuracy2World world = Boot();
        world.Save(
            project =>
            {
                Std(MainScreen(project), "Pic", "SourceFile", "Content/run.achx");
                Std(MainScreen(project), "Pic", "CurrentChainName", "Run");
            },
            dir => WriteChain(dir, "run.achx", "red.png", 0.1f));
        WriteChain(world.Dir, "run.achx", "other.png", 0.5f, frames: 3);
        PreviewVerdict2 verdict = world.Reload(Content(world.Dir, "run.achx"));

        verdict.Reloaded.ShouldBeFalse("an .achx is not a file hot reload reacts to");
        verdict.Differs.ShouldBeTrue();
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
    public void Localization_CsvEditedOnDisk_IsNotWatched()
    {
        PreviewAccuracy2World world = new PreviewAccuracy2World { UseLocalization = true };
        _worlds.Add(world);
        world.Boot();
        WriteLocalization(world.Dir, "Howdy");
        PreviewVerdict2 verdict = world.Reload(Content(world.Dir, "loc.csv"));

        verdict.Reloaded.ShouldBeFalse("a .csv is not a file hot reload reacts to");
        verdict.Hot.ShouldContain("Text=Hello");
        verdict.Fresh.ShouldContain("Text=Howdy");
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
