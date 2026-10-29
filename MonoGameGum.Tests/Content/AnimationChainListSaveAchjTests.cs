using Gum.Graphics.Animation;
using System;
using System.IO;
using System.Xml.Serialization;
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
    public void FromFile_AchjExtension_ParsesRenderColorOperation()
    {
        // #4822: renderColorOperation is Gum's own Modulate/ColorTextureAlpha render-technique
        // selector - a key the FlatRedBall Animation Editor never writes (it has no such concept),
        // distinct from the FRB2-sourced "colorOperation" (Multiply/Add) key covered above.
        WithTempFile(".achj", """
        {
          "animationChains": [
            {
              "name": "Silhouette",
              "frames": [
                { "textureName": "bear.png", "frameLength": 0.1, "renderColorOperation": "ColorTextureAlpha" }
              ]
            }
          ]
        }
        """, path =>
        {
            AnimationFrameSave frame = AnimationChainListSave.FromFile(path).AnimationChains[0].Frames[0];

            frame.RenderColorOperation.ShouldBe(RenderingLibrary.Graphics.ColorOperation.ColorTextureAlpha);
        });
    }

    [Fact]
    public void FromFile_AchjExtension_FrameOmittingRenderColorOperation_IsNull()
    {
        WithTempFile(".achj", """
        {
          "animationChains": [
            { "name": "Idle", "frames": [ { "textureName": "idle.png", "frameLength": 0.2 } ] }
          ]
        }
        """, path =>
        {
            AnimationFrameSave frame = AnimationChainListSave.FromFile(path).AnimationChains[0].Frames[0];

            frame.RenderColorOperation.ShouldBeNull();
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
    public void FromFile_AchxExtension_ChainOmitsLoop_DefaultsToTrue()
    {
        // Issue #4708: an .achx written before Loop existed must keep looping by default.
        WithTempFile(".achx", """
        <?xml version="1.0" encoding="utf-8"?>
        <AnimationChainArraySave>
          <AnimationChain>
            <Name>Walk</Name>
          </AnimationChain>
        </AnimationChainArraySave>
        """, path =>
        {
            AnimationChainListSave save = AnimationChainListSave.FromFile(path);

            save.AnimationChains[0].Loop.ShouldBeTrue();
        });
    }

    [Fact]
    public void FromFile_AchxExtension_ChainLoopFalse_Parses()
    {
        WithTempFile(".achx", """
        <?xml version="1.0" encoding="utf-8"?>
        <AnimationChainArraySave>
          <AnimationChain>
            <Name>Attack</Name>
            <Loop>false</Loop>
          </AnimationChain>
        </AnimationChainArraySave>
        """, path =>
        {
            AnimationChainListSave save = AnimationChainListSave.FromFile(path);

            save.AnimationChains[0].Loop.ShouldBeFalse();
        });
    }

    [Fact]
    public void FromFile_AchjExtension_ChainOmitsLoop_DefaultsToTrue()
    {
        WithTempFile(".achj", """
        {
          "animationChains": [
            { "name": "Walk", "frames": [] }
          ]
        }
        """, path =>
        {
            AnimationChainListSave save = AnimationChainListSave.FromFile(path);

            save.AnimationChains[0].Loop.ShouldBeTrue();
        });
    }

    [Fact]
    public void FromFile_AchjExtension_ChainLoopFalse_Parses()
    {
        WithTempFile(".achj", """
        {
          "animationChains": [
            { "name": "Attack", "loop": false, "frames": [] }
          ]
        }
        """, path =>
        {
            AnimationChainListSave save = AnimationChainListSave.FromFile(path);

            save.AnimationChains[0].Loop.ShouldBeFalse();
        });
    }

    [Fact]
    public void FromFile_AchxExtension_PingPongAndReverse_Parse()
    {
        WithTempFile(".achx", """
        <?xml version="1.0" encoding="utf-8"?>
        <AnimationChainArraySave>
          <AnimationChain>
            <Name>Bounce</Name>
            <PingPong>true</PingPong>
            <Reverse>true</Reverse>
          </AnimationChain>
        </AnimationChainArraySave>
        """, path =>
        {
            AnimationChainListSave save = AnimationChainListSave.FromFile(path);

            save.AnimationChains[0].PingPong.ShouldBeTrue();
            save.AnimationChains[0].Reverse.ShouldBeTrue();
        });
    }

    [Fact]
    public void FromFile_AchxExtension_ChainOmitsPingPongAndReverse_DefaultsToFalse()
    {
        WithTempFile(".achx", """
        <?xml version="1.0" encoding="utf-8"?>
        <AnimationChainArraySave>
          <AnimationChain>
            <Name>Walk</Name>
          </AnimationChain>
        </AnimationChainArraySave>
        """, path =>
        {
            AnimationChainListSave save = AnimationChainListSave.FromFile(path);

            save.AnimationChains[0].PingPong.ShouldBeFalse();
            save.AnimationChains[0].Reverse.ShouldBeFalse();
        });
    }

    [Fact]
    public void FromFile_AchjExtension_PingPongAndReverse_Parse()
    {
        WithTempFile(".achj", """
        {
          "animationChains": [
            { "name": "Bounce", "pingPong": true, "reverse": true, "frames": [] }
          ]
        }
        """, path =>
        {
            AnimationChainListSave save = AnimationChainListSave.FromFile(path);

            save.AnimationChains[0].PingPong.ShouldBeTrue();
            save.AnimationChains[0].Reverse.ShouldBeTrue();
        });
    }

    [Fact]
    public void FromFile_AchjExtension_ChainOmitsPingPongAndReverse_DefaultsToFalse()
    {
        WithTempFile(".achj", """
        {
          "animationChains": [
            { "name": "Walk", "frames": [] }
          ]
        }
        """, path =>
        {
            AnimationChainListSave save = AnimationChainListSave.FromFile(path);

            save.AnimationChains[0].PingPong.ShouldBeFalse();
            save.AnimationChains[0].Reverse.ShouldBeFalse();
        });
    }

    [Fact]
    public void ToAnimationChain_ShouldCarryPingPongAndReverse()
    {
        AnimationChainSave save = new() { Name = "Bounce", PingPong = true, Reverse = true };

        Gum.Graphics.Animation.AnimationChain chain = save.ToAnimationChain(TimeMeasurementUnit.Second);

        chain.PingPong.ShouldBeTrue();
        chain.IsReversed.ShouldBeTrue();
    }

    [Fact]
    public void Achx_Save_ShouldRoundTripPingPongAndReverseAndOmitThemWhenFalse()
    {
        AnimationChainListSave list = new();
        list.AnimationChains.Add(new AnimationChainSave { Name = "A", PingPong = true, Reverse = true });
        list.AnimationChains.Add(new AnimationChainSave { Name = "B" });
        XmlSerializer serializer = new(typeof(AnimationChainListSave));
        using StringWriter writer = new();
        serializer.Serialize(writer, list);
        // StringWriter declares utf-16, but the file is written as UTF-8.
        string xml = writer.ToString().Replace("utf-16", "utf-8");

        xml.Split("<PingPong>").Length.ShouldBe(2);
        xml.Split("<Reverse>").Length.ShouldBe(2);
        WithTempFile(".achx", xml, path =>
        {
            AnimationChainListSave loaded = AnimationChainListSave.FromFile(path);

            loaded.AnimationChains[0].PingPong.ShouldBeTrue();
            loaded.AnimationChains[0].Reverse.ShouldBeTrue();
            loaded.AnimationChains[1].PingPong.ShouldBeFalse();
            loaded.AnimationChains[1].Reverse.ShouldBeFalse();
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

    [Fact]
    public void FromFile_AchxExtension_ParsesRenderColorOperation()
    {
        WithTempFile(".achx", """
        <?xml version="1.0" encoding="utf-8"?>
        <AnimationChainArraySave>
          <AnimationChain>
            <Name>Silhouette</Name>
            <Frame>
              <TextureName>bear.png</TextureName>
              <FrameLength>0.1</FrameLength>
              <RenderColorOperation>ColorTextureAlpha</RenderColorOperation>
            </Frame>
          </AnimationChain>
        </AnimationChainArraySave>
        """, path =>
        {
            AnimationFrameSave frame = AnimationChainListSave.FromFile(path).AnimationChains[0].Frames[0];

            frame.RenderColorOperation.ShouldBe(RenderingLibrary.Graphics.ColorOperation.ColorTextureAlpha);
        });
    }

    [Fact]
    public void FromFile_AchxExtension_FrameOmittingRenderColorOperation_IsNull()
    {
        WithTempFile(".achx", """
        <?xml version="1.0" encoding="utf-8"?>
        <AnimationChainArraySave>
          <AnimationChain>
            <Name>Walk</Name>
            <Frame>
              <TextureName>walk_0.png</TextureName>
              <FrameLength>0.1</FrameLength>
            </Frame>
          </AnimationChain>
        </AnimationChainArraySave>
        """, path =>
        {
            AnimationFrameSave frame = AnimationChainListSave.FromFile(path).AnimationChains[0].Frames[0];

            frame.RenderColorOperation.ShouldBeNull();
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
