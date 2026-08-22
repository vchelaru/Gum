using System;
using System.IO;
using Gum.Content.AnimationChain;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Content;

// Covers AnimationChainListSave.FromFile's .achj (JSON) dialect, added alongside the existing
// .achx (XML) dialect (issue #4476). The JSON schema matches FlatRedBall2's AnimationChain.Common
// .achj writer (camelCase property names) so files authored by the FlatRedBall Animation Editor
// load into Gum without a conversion step.
public class AnimationChainListSaveAchjTests
{
    [Fact]
    public void FromFile_AchjExtension_ParsesListLevelProperties()
    {
        WithTempFile(".achj", """
        {
          "fileRelativeTextures": false,
          "timeMeasurementUnit": "Millisecond",
          "coordinateType": "Pixel",
          "animationChains": []
        }
        """, path =>
        {
            AnimationChainListSave save = AnimationChainListSave.FromFile(path);

            save.FileRelativeTextures.ShouldBeFalse();
            save.TimeMeasurementUnit.ShouldBe(TimeMeasurementUnit.Millisecond);
            save.CoordinateType.ShouldBe(TextureCoordinateType.Pixel);
            save.AnimationChains.ShouldBeEmpty();
        });
    }

    [Fact]
    public void FromFile_AchjExtension_ParsesChainsAndFrames()
    {
        WithTempFile(".achj", """
        {
          "fileRelativeTextures": true,
          "timeMeasurementUnit": "Second",
          "coordinateType": "UV",
          "animationChains": [
            {
              "name": "Walk",
              "frames": [
                {
                  "textureName": "walk_0.png",
                  "frameLength": 0.1,
                  "leftCoordinate": 0.0,
                  "rightCoordinate": 0.5,
                  "topCoordinate": 0.0,
                  "bottomCoordinate": 0.5,
                  "flipHorizontal": true,
                  "flipDiagonal": true,
                  "relativeX": 2.5,
                  "relativeY": -1.5,
                  "alpha": 128,
                  "red": 10,
                  "green": 20,
                  "blue": 30,
                  "colorOperation": "Multiply"
                }
              ]
            }
          ]
        }
        """, path =>
        {
            AnimationChainListSave save = AnimationChainListSave.FromFile(path);

            save.AnimationChains.Count.ShouldBe(1);
            AnimationChainSave chain = save.AnimationChains[0];
            chain.Name.ShouldBe("Walk");
            chain.Frames.Count.ShouldBe(1);

            AnimationFrameSave frame = chain.Frames[0];
            frame.TextureName.ShouldBe("walk_0.png");
            frame.FrameLength.ShouldBe(0.1f);
            frame.LeftCoordinate.ShouldBe(0.0f);
            frame.RightCoordinate.ShouldBe(0.5f);
            frame.TopCoordinate.ShouldBe(0.0f);
            frame.BottomCoordinate.ShouldBe(0.5f);
            frame.FlipHorizontal.ShouldBeTrue();
            frame.FlipVertical.ShouldBeFalse();
            frame.FlipDiagonal.ShouldBeTrue();
            frame.RelativeX.ShouldBe(2.5f);
            frame.RelativeY.ShouldBe(-1.5f);
            frame.Alpha.ShouldBe(128);
            frame.Red.ShouldBe(10);
            frame.Green.ShouldBe(20);
            frame.Blue.ShouldBe(30);
            frame.ColorOperation.ShouldBe(AnimationFrameColorOperation.Multiply);
        });
    }

    [Fact]
    public void FromFile_AchjExtension_FramesOmittingOptionalFields_UseAchxDefaults()
    {
        // Mirrors .achx's XmlDeserialize defaults: RightCoordinate/BottomCoordinate default to 1,
        // flip flags and RelativeX/Y default to false/0 when the writer omits unset values.
        WithTempFile(".achj", """
        {
          "animationChains": [
            { "name": "Idle", "frames": [ { "textureName": "idle.png", "frameLength": 0.2 } ] }
          ]
        }
        """, path =>
        {
            AnimationFrameSave frame = AnimationChainListSave.FromFile(path).AnimationChains[0].Frames[0];

            frame.RightCoordinate.ShouldBe(1f);
            frame.BottomCoordinate.ShouldBe(1f);
            frame.FlipHorizontal.ShouldBeFalse();
            frame.FlipVertical.ShouldBeFalse();
            frame.FlipDiagonal.ShouldBeFalse();
            frame.RelativeX.ShouldBe(0f);
            frame.RelativeY.ShouldBe(0f);
            frame.Alpha.ShouldBeNull();
            frame.Red.ShouldBeNull();
            frame.Green.ShouldBeNull();
            frame.Blue.ShouldBeNull();
            frame.ColorOperation.ShouldBeNull();
        });
    }

    [Fact]
    public void FromFile_AchjExtension_ParsesAddColorOperation()
    {
        // Add is parsed into the data model like Multiply, even though only Multiply is applied
        // to rendering today (#4477 tracks Add, which needs a per-backend shader).
        WithTempFile(".achj", """
        {
          "animationChains": [
            {
              "name": "Flash",
              "frames": [
                { "textureName": "flash.png", "frameLength": 0.05, "red": 255, "green": 0, "blue": 0, "colorOperation": "Add" }
              ]
            }
          ]
        }
        """, path =>
        {
            AnimationFrameSave frame = AnimationChainListSave.FromFile(path).AnimationChains[0].Frames[0];

            frame.Red.ShouldBe(255);
            frame.Green.ShouldBe(0);
            frame.Blue.ShouldBe(0);
            frame.ColorOperation.ShouldBe(AnimationFrameColorOperation.Add);
        });
    }

    [Fact]
    public void FromFile_AchjExtension_UnknownFrbTwoOnlyFields_AreIgnoredNotThrown()
    {
        // Per-frame shapes are an FRB2 addition Gum doesn't model yet (#4479) — an achj file
        // carrying them must still load.
        WithTempFile(".achj", """
        {
          "animationChains": [
            {
              "name": "Flash",
              "frames": [
                {
                  "textureName": "flash.png",
                  "frameLength": 0.05,
                  "shapes": { "rectangles": [], "circles": [], "polygons": [] }
                }
              ]
            }
          ]
        }
        """, path =>
        {
            Should.NotThrow(() => AnimationChainListSave.FromFile(path))
                .AnimationChains[0].Frames[0].TextureName.ShouldBe("flash.png");
        });
    }

    [Fact]
    public void FromFile_AchxExtension_StillParsesAsXml()
    {
        // Extension-based dialect selection must not regress the existing XML path.
        WithTempFile(".achx", """
        <?xml version="1.0" encoding="utf-8"?>
        <AnimationChainArraySave>
          <FileRelativeTextures>true</FileRelativeTextures>
          <TimeMeasurementUnit>Second</TimeMeasurementUnit>
          <CoordinateType>UV</CoordinateType>
          <AnimationChain>
            <Name>Walk</Name>
            <Frame>
              <TextureName>walk_0.png</TextureName>
              <FrameLength>0.1</FrameLength>
              <LeftCoordinate>0</LeftCoordinate>
              <RightCoordinate>1</RightCoordinate>
              <TopCoordinate>0</TopCoordinate>
              <BottomCoordinate>1</BottomCoordinate>
              <FlipDiagonal>true</FlipDiagonal>
              <Alpha>200</Alpha>
              <Red>10</Red>
              <Green>20</Green>
              <Blue>30</Blue>
              <ColorOperation>Multiply</ColorOperation>
            </Frame>
          </AnimationChain>
        </AnimationChainArraySave>
        """, path =>
        {
            AnimationChainListSave save = AnimationChainListSave.FromFile(path);

            save.AnimationChains.Count.ShouldBe(1);
            AnimationFrameSave frame = save.AnimationChains[0].Frames[0];
            frame.TextureName.ShouldBe("walk_0.png");
            frame.FlipDiagonal.ShouldBeTrue();
            frame.Alpha.ShouldBe(200);
            frame.Red.ShouldBe(10);
            frame.Green.ShouldBe(20);
            frame.Blue.ShouldBe(30);
            frame.ColorOperation.ShouldBe(AnimationFrameColorOperation.Multiply);
        });
    }

    private static void WithTempFile(string extension, string content, Action<string> action)
    {
        string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + extension);
        File.WriteAllText(path, content);
        try
        {
            action(path);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
