using RenderingLibrary;
using RenderingLibrary.Graphics;
using Shouldly;
using SkiaGum.Renderables;
using SkiaSharp;

namespace SkiaGum.Tests.Renderables;

/// <summary>
/// Drawing a block's cached text blobs must give the same bytes as painting it through RichTextKit (the first
/// frame of a block), and the cached blobs must follow every change of the text.
/// </summary>
public class TextDirectBlobTests
{
    public static IEnumerable<object[]> Cases()
    {
        string[] kinds = { "Plain", "BbCodeColors", "Outline", "Shadow", "OutlineShadow", "Paragraph", "BoldItalic", "FontSizes" };
        float[] scales = { 1f, 1.5f, 2f, 3f };
        foreach (string kind in kinds)
        {
            foreach (float scale in scales)
            {
                yield return new object[] { kind, scale, 0f, false, 255 };
                yield return new object[] { kind, scale, 17f, true, 128 };
            }
        }
    }

    private static Text CreateText(string kind)
    {
        Text text = new Text();
        text.FontSize = 18;
        text.Width = 400;
        text.Height = 120;
        text.X = 3.37f;
        text.Y = 5.61f;
        switch (kind)
        {
            case "Plain":
                text.RawText = "42";
                break;
            case "BbCodeColors":
                text.RawText = "[Color=Red]Red[/Color] [Color=Green]Green[/Color] [Color=Blue]Blue[/Color] [Color=Yellow]Yellow[/Color]";
                break;
            case "Outline":
                text.RawText = "Outlined text";
                text.OutlineThickness = 2;
                break;
            case "Shadow":
                text.RawText = "Shadowed text";
                text.HasDropshadow = true;
                text.DropshadowOffsetX = 2;
                text.DropshadowOffsetY = 3;
                text.DropshadowBlurX = 2;
                text.DropshadowBlurY = 2;
                break;
            case "OutlineShadow":
                text.RawText = "Both effects";
                text.OutlineThickness = 1;
                text.HasDropshadow = true;
                text.DropshadowOffsetX = 2;
                text.DropshadowOffsetY = 2;
                break;
            case "Paragraph":
                text.RawText = "The quick brown fox jumps over the lazy dog, then keeps running across the field until the "
                    + "paragraph wraps onto several lines of text inside its four hundred pixel width.";
                break;
            case "BoldItalic":
                text.RawText = "Plain [IsBold=true]bold[/IsBold] and [IsItalic=true]italic[/IsItalic] runs";
                break;
            case "FontSizes":
                text.RawText = "Small [FontSize=30]large[/FontSize] [FontSize=12]tiny[/FontSize]";
                break;
        }
        return text;
    }

    private static byte[] Render(string kind, float scale, float rotation, bool clip, int alpha, int frames)
    {
        Text text = CreateText(kind);
        text.Rotation = rotation;
        text.Alpha = alpha;
        if (clip)
        {
            SolidRectangle clipper = new SolidRectangle();
            clipper.X = 1;
            clipper.Y = 2;
            clipper.Width = 150;
            clipper.Height = 40;
            clipper.ClipsChildren = true;
            text.Parent = clipper;
        }

        using SKSurface surface = SKSurface.Create(new SKImageInfo(1300, 420));
        surface.Canvas.Scale(scale);
        SystemManagers managers = new SystemManagers { Canvas = surface.Canvas };
        // The first frame paints through RichTextKit; later frames draw the cached blobs.
        for (int i = 0; i < frames; i++)
        {
            surface.Canvas.Clear(SKColors.Transparent);
            text.Render(managers);
        }
        text.IsDrawingDirectBlobs.ShouldBe(frames > 1);
        using SKImage image = surface.Snapshot();
        using SKBitmap bitmap = SKBitmap.FromImage(image);
        return bitmap.Bytes;
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void Render_CachedBlobs_MatchesPaintingThroughRichTextKit(string kind, float scale, float rotation, bool clip, int alpha)
    {
        byte[] richTextKit = Render(kind, scale, rotation, clip, alpha, frames: 1);
        byte[] blobs = Render(kind, scale, rotation, clip, alpha, frames: 3);

        richTextKit.Any(b => b != 0).ShouldBeTrue();
        blobs.ShouldBe(richTextKit);
    }

    [Theory]
    [InlineData("RawText")]
    [InlineData("FontSize")]
    [InlineData("Font")]
    [InlineData("Color")]
    [InlineData("OutlineThickness")]
    [InlineData("Dropshadow")]
    [InlineData("Width")]
    [InlineData("Markup")]
    public void Render_ChangeAfterCaching_MatchesAFreshText(string property)
    {
        Text changed = CreateText("Paragraph");
        using SKSurface surface = SKSurface.Create(new SKImageInfo(600, 200));
        SystemManagers managers = new SystemManagers { Canvas = surface.Canvas };
        for (int i = 0; i < 3; i++)
        {
            surface.Canvas.Clear(SKColors.Transparent);
            changed.Render(managers);
        }
        byte[] before = Snapshot(surface);

        Apply(changed, property);
        surface.Canvas.Clear(SKColors.Transparent);
        changed.Render(managers);
        surface.Canvas.Clear(SKColors.Transparent);
        changed.Render(managers);
        byte[] after = Snapshot(surface);

        Text fresh = CreateText("Paragraph");
        Apply(fresh, property);
        using SKSurface freshSurface = SKSurface.Create(new SKImageInfo(600, 200));
        fresh.Render(new SystemManagers { Canvas = freshSurface.Canvas });

        after.ShouldNotBe(before);
        after.ShouldBe(Snapshot(freshSurface));
    }

    private static void Apply(Text text, string property)
    {
        switch (property)
        {
            case "RawText": text.RawText = "Different words entirely"; break;
            case "FontSize": text.FontSize = 26; break;
            case "Font": text.FontName = "Courier New"; break;
            case "Color": text.Color = SKColors.Orange; break;
            case "OutlineThickness": text.OutlineThickness = 2; break;
            case "Dropshadow":
                text.HasDropshadow = true;
                text.DropshadowColor = SKColors.Blue;
                text.DropshadowOffsetX = 3;
                text.DropshadowOffsetY = 3;
                break;
            case "Width": text.Width = 200; break;
            case "Markup": text.RawText = "The quick [Color=Red]brown[/Color] fox"; break;
        }
    }

    private static byte[] Snapshot(SKSurface surface)
    {
        using SKImage image = surface.Snapshot();
        using SKBitmap bitmap = SKBitmap.FromImage(image);
        return bitmap.Bytes;
    }
}
