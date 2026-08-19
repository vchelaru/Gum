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
                  "relativeX": 2.5,
                  "relativeY": -1.5
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
            frame.RelativeX.ShouldBe(2.5f);
            frame.RelativeY.ShouldBe(-1.5f);
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
            frame.RelativeX.ShouldBe(0f);
            frame.RelativeY.ShouldBe(0f);
        });
    }

    [Fact]
    public void FromFile_AchjExtension_UnknownFrbTwoOnlyFields_AreIgnoredNotThrown()
    {
        // FlipDiagonal, Red/Green/Blue/Alpha, ColorOperation, and shapes are FRB2 additions Gum
        // doesn't model yet (#4477, #4478, #4479) — an achj file carrying them must still load.
        WithTempFile(".achj", """
        {
          "animationChains": [
            {
              "name": "Flash",
              "frames": [
                {
                  "textureName": "flash.png",
                  "frameLength": 0.05,
                  "flipDiagonal": true,
                  "red": 255,
                  "green": 0,
                  "blue": 0,
                  "alpha": 255,
                  "colorOperation": "Add",
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
            </Frame>
          </AnimationChain>
        </AnimationChainArraySave>
        """, path =>
        {
            AnimationChainListSave save = AnimationChainListSave.FromFile(path);

            save.AnimationChains.Count.ShouldBe(1);
            save.AnimationChains[0].Frames[0].TextureName.ShouldBe("walk_0.png");
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
