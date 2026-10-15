using System.IO;
using System.Xml.Serialization;
using Gum.Content.AnimationChain;
using Gum.Graphics.Animation;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Content;

// Per-frame keyframe interpolation fields (FlatRedBall2 #1478) in .achx / .achj. The raw strings
// are stored as read so a value Gum does not understand survives a load/save cycle.
public class AnimationChainKeyframeFormatTests
{
    private static string ToXml(AnimationChainListSave save)
    {
        XmlSerializer serializer = new(typeof(AnimationChainListSave));
        using StringWriter writer = new();
        serializer.Serialize(writer, save);
        return writer.ToString().Replace("\r\n", "\n");
    }

    private static AnimationChainListSave FromXml(string xml)
    {
        XmlSerializer serializer = new(typeof(AnimationChainListSave));
        using StringReader reader = new(xml);
        return (AnimationChainListSave)serializer.Deserialize(reader)!;
    }

    private static AnimationChainListSave SaveWithFrame(AnimationFrameSave frame)
    {
        AnimationChainListSave save = new();
        AnimationChainSave chain = new() { Name = "c" };
        chain.Frames.Add(frame);
        save.AnimationChains.Add(chain);
        return save;
    }

    private static AnimationFrameSave FullyPopulated() => new()
    {
        FrameLength = 0.1f,
        SkipOffsetKeyframe = true,
        SkipColorKeyframe = true,
        OffsetInterpolation = "Cubic",
        OffsetEasing = "InOut",
        ColorInterpolation = "SomethingFromTheFuture",
        ColorEasing = "SidewaysEase",
    };

    private static void AssertPreserved(AnimationFrameSave frame)
    {
        frame.SkipOffsetKeyframe.ShouldBeTrue();
        frame.SkipColorKeyframe.ShouldBeTrue();
        frame.OffsetInterpolation.ShouldBe("Cubic");
        frame.OffsetEasing.ShouldBe("InOut");
        frame.ColorInterpolation.ShouldBe("SomethingFromTheFuture");
        frame.ColorEasing.ShouldBe("SidewaysEase");
    }

    [Fact]
    public void Xml_RoundTrip_PreservesEveryValueIncludingUnknownNames()
    {
        string xml = ToXml(SaveWithFrame(FullyPopulated()));

        AssertPreserved(FromXml(xml).AnimationChains[0].Frames[0]);
    }

    [Fact]
    public void Xml_SetFields_WriteTheDocumentedElementNames()
    {
        string xml = ToXml(SaveWithFrame(FullyPopulated()));

        xml.ShouldContain("<SkipOffsetKeyframe>true</SkipOffsetKeyframe>");
        xml.ShouldContain("<SkipColorKeyframe>true</SkipColorKeyframe>");
        xml.ShouldContain("<OffsetInterpolation>Cubic</OffsetInterpolation>");
        xml.ShouldContain("<OffsetEasing>InOut</OffsetEasing>");
        xml.ShouldContain("<ColorInterpolation>SomethingFromTheFuture</ColorInterpolation>");
        xml.ShouldContain("<ColorEasing>SidewaysEase</ColorEasing>");
    }

    [Fact]
    public void Xml_FieldsAtDefaults_WriteNoElements()
    {
        string xml = ToXml(SaveWithFrame(new AnimationFrameSave { FrameLength = 0.1f }));

        xml.ShouldNotContain("Keyframe");
        xml.ShouldNotContain("Interpolation");
        xml.ShouldNotContain("Easing");
    }

    [Fact]
    public void Xml_FileWithoutFields_LoadsWithDefaults()
    {
        AnimationFrameSave frame = FromXml(ToXml(SaveWithFrame(new AnimationFrameSave()))).AnimationChains[0].Frames[0];

        frame.SkipOffsetKeyframe.ShouldBeFalse();
        frame.SkipColorKeyframe.ShouldBeFalse();
        frame.OffsetInterpolation.ShouldBeNull();
        frame.OffsetEasing.ShouldBeNull();
        frame.ColorInterpolation.ShouldBeNull();
        frame.ColorEasing.ShouldBeNull();
    }

    [Fact]
    public void Xml_UnknownElements_AreStillIgnored()
    {
        string xml = ToXml(SaveWithFrame(FullyPopulated())).Replace(
            "<SkipOffsetKeyframe>", "<SomeFutureElement>1</SomeFutureElement><SkipOffsetKeyframe>");

        AssertPreserved(FromXml(xml).AnimationChains[0].Frames[0]);
    }

    [Fact]
    public void Achj_FieldsParsedAndPreservedAsRawStrings()
    {
        WithTempFile(".achj", """
        {
          "animationChains": [
            { "name": "c", "frames": [
              {
                "frameLength": 0.1,
                "skipOffsetKeyframe": true,
                "skipColorKeyframe": true,
                "offsetInterpolation": "Cubic",
                "offsetEasing": "InOut",
                "colorInterpolation": "SomethingFromTheFuture",
                "colorEasing": "SidewaysEase",
                "someFutureProperty": 3
              }
            ] }
          ]
        }
        """, path =>
        {
            AssertPreserved(AnimationChainListSave.FromFile(path).AnimationChains[0].Frames[0]);
        });
    }

    [Fact]
    public void Achj_FieldsAbsent_AreDefaults()
    {
        WithTempFile(".achj", """
        { "animationChains": [ { "name": "c", "frames": [ { "frameLength": 0.1 } ] } ] }
        """, path =>
        {
            AnimationFrameSave frame = AnimationChainListSave.FromFile(path).AnimationChains[0].Frames[0];

            frame.SkipOffsetKeyframe.ShouldBeFalse();
            frame.SkipColorKeyframe.ShouldBeFalse();
            frame.OffsetInterpolation.ShouldBeNull();
            frame.OffsetEasing.ShouldBeNull();
            frame.ColorInterpolation.ShouldBeNull();
            frame.ColorEasing.ShouldBeNull();
        });
    }

    [Fact]
    public void ToAnimationFrame_CopiesKeyframeFields()
    {
        AnimationFrame frame = FullyPopulated().ToAnimationFrame(loadTexture: false);

        frame.SkipOffsetKeyframe.ShouldBeTrue();
        frame.SkipColorKeyframe.ShouldBeTrue();
        frame.OffsetInterpolation.ShouldBe("Cubic");
        frame.OffsetEasing.ShouldBe("InOut");
        frame.ColorInterpolation.ShouldBe("SomethingFromTheFuture");
        frame.ColorEasing.ShouldBe("SidewaysEase");
    }

    [Fact]
    public void ToAnimationChain_MillisecondUnits_ConvertsLengthsSoGlidesUseSeconds()
    {
        AnimationChainSave save = new() { Name = "c" };
        save.Frames.Add(new AnimationFrameSave { FrameLength = 500, OffsetInterpolation = "Linear" });
        save.Frames.Add(new AnimationFrameSave { FrameLength = 500, RelativeX = 100 });

        AnimationChain chain = save.ToAnimationChain(TimeMeasurementUnit.Millisecond);

        KeyframeEvaluator.EvaluateOffset(chain, loop: true, 0, 0.25).X.ShouldBe(50f, 0.001f);
    }

    private static void WithTempFile(string extension, string content, System.Action<string> action)
    {
        string path = Path.Combine(Path.GetTempPath(), System.Guid.NewGuid().ToString("N") + extension);
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

    [Fact]
    public void Xml_FrameWithoutKeyframeFields_SerializesExactlyAsBefore()
    {
        AnimationChainListSave save = SaveWithFrame(new AnimationFrameSave
        {
            TextureName = "a.png",
            FrameLength = 0.1f,
            RelativeX = 2,
            Alpha = 128,
        });

        string xml = ToXml(save);

        xml.ShouldBe(
            "<?xml version=\"1.0\" encoding=\"utf-16\"?>\n" +
            "<AnimationChainArraySave xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">\n" +
            "  <FileRelativeTextures>true</FileRelativeTextures>\n" +
            "  <TimeMeasurementUnit>Undefined</TimeMeasurementUnit>\n" +
            "  <CoordinateType>UV</CoordinateType>\n" +
            "  <AnimationChain>\n" +
            "    <Name>c</Name>\n" +
            "    <Frame>\n" +
            "      <Alpha>128</Alpha>\n" +
            "      <TextureName>a.png</TextureName>\n" +
            "      <FrameLength>0.1</FrameLength>\n" +
            "      <LeftCoordinate>0</LeftCoordinate>\n" +
            "      <RightCoordinate>1</RightCoordinate>\n" +
            "      <TopCoordinate>0</TopCoordinate>\n" +
            "      <BottomCoordinate>1</BottomCoordinate>\n" +
            "      <RelativeX>2</RelativeX>\n" +
            "    </Frame>\n" +
            "    <Loop>true</Loop>\n" +
            "  </AnimationChain>\n" +
            "</AnimationChainArraySave>");
    }
}
