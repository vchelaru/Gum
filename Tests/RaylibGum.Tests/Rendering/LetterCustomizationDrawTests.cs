using Gum.GueDeriving;
using Raylib_cs;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using Shouldly;
using static Raylib_cs.Raylib;
using LetterCustomization = Gum.Renderables.LetterCustomization;
using Text = Gum.Renderables.Text;

namespace RaylibGum.Tests.Rendering;

/// <summary>
/// #6550: a [Custom] letter's ScaleX/ScaleY/RotationDegrees change only how it is drawn, as on MonoGame and
/// Skia: it is scaled about the cell point ScaleXOrigin/ScaleYOrigin name (Center by default) with both
/// scales, turned counterclockwise about the scaled cell's top-left, and the next letter does not move.
/// </summary>
public class LetterCustomizationDrawTests : BaseTestClass
{
    private const int CanvasWidth = 240;
    private const int CanvasHeight = 240;

    [Fact]
    public void Draw_CustomTagWithNoTransform_DrawsSamePixelsAsPlainText()
    {
        Text.Customizations["NoTransform"] = (index, block) => new LetterCustomization();

        Color[] plain = Render("I", left: 80, top: 80);
        Color[] customized = Render("[Custom=NoTransform]I[/Custom]", left: 80, top: 80);

        customized.SequenceEqual(plain).ShouldBeTrue();
    }

    [Fact]
    public void Draw_CustomTagNonUniformScale_ScalesEachAxisAboutTheCellCenter()
    {
        Text.Customizations["Wide"] = (index, block) => new LetterCustomization
        {
            ScaleX = 3,
            ScaleY = 1,
        };

        Bounds plain = InkBounds(Render("I", left: 80, top: 80));
        Bounds scaled = InkBounds(Render("[Custom=Wide]I[/Custom]", left: 80, top: 80));

        scaled.Width.ShouldBe(3 * plain.Width, tolerance: 3);
        scaled.Height.ShouldBe(plain.Height, tolerance: 1);
        scaled.Top.ShouldBe(plain.Top, tolerance: 1);
    }

    [Fact]
    public void Draw_CustomTagScale_KeepsTheCellCenterFixedByDefault()
    {
        Text.Customizations["Double"] = (index, block) => new LetterCustomization
        {
            ScaleX = 2,
            ScaleY = 2,
        };

        Text plainText = BuildText("I", 80, 80, out TextRuntime runtime);
        float cellWidth = plainText.WrappedTextWidth;
        float cellHeight = plainText.WrappedTextHeight;
        Gum.GumService.Default.Root.Children!.Remove(runtime);
        float centerX = 80 + cellWidth / 2;
        float centerY = 80 + cellHeight / 2;

        Bounds plain = InkBounds(Render("I", left: 80, top: 80));
        Bounds scaled = InkBounds(Render("[Custom=Double]I[/Custom]", left: 80, top: 80));

        // Scaling about the cell center doubles every ink edge's distance from it.
        (scaled.Left - centerX).ShouldBe(2 * (plain.Left - centerX), tolerance: 2);
        (scaled.Right - centerX).ShouldBe(2 * (plain.Right - centerX), tolerance: 2);
        (scaled.Top - centerY).ShouldBe(2 * (plain.Top - centerY), tolerance: 2);
        (scaled.Bottom - centerY).ShouldBe(2 * (plain.Bottom - centerY), tolerance: 2);
    }

    [Fact]
    public void Draw_CustomTagScaleWithRightBottomOrigin_KeepsTheCellBottomRightFixed()
    {
        Text.Customizations["DoubleFromBottomRight"] = (index, block) => new LetterCustomization
        {
            ScaleX = 2,
            ScaleY = 2,
            ScaleXOrigin = HorizontalAlignment.Right,
            ScaleYOrigin = VerticalAlignment.Bottom,
        };

        Text plainText = BuildText("I", 80, 80, out TextRuntime runtime);
        float right = 80 + plainText.WrappedTextWidth;
        float bottom = 80 + plainText.WrappedTextHeight;
        Gum.GumService.Default.Root.Children!.Remove(runtime);

        Bounds plain = InkBounds(Render("I", left: 80, top: 80));
        Bounds scaled = InkBounds(Render("[Custom=DoubleFromBottomRight]I[/Custom]", left: 80, top: 80));

        (scaled.Left - right).ShouldBe(2 * (plain.Left - right), tolerance: 2);
        (scaled.Right - right).ShouldBe(2 * (plain.Right - right), tolerance: 2);
        (scaled.Top - bottom).ShouldBe(2 * (plain.Top - bottom), tolerance: 2);
        (scaled.Bottom - bottom).ShouldBe(2 * (plain.Bottom - bottom), tolerance: 2);
    }

    [Fact]
    public void Draw_CustomTagRotation_RotatesTheLetterCounterclockwiseAboutTheCellTopLeft()
    {
        Text.Customizations["Quarter"] = (index, block) => new LetterCustomization
        {
            RotationDegrees = 90,
        };

        Bounds plain = InkBounds(Render("I", left: 80, top: 80));
        Bounds rotated = InkBounds(Render("[Custom=Quarter]I[/Custom]", left: 80, top: 80));

        // A counterclockwise quarter turn about (80, 80) maps (dx, dy) to (dy, -dx): the letter lies above it.
        rotated.Left.ShouldBe(80 + (plain.Top - 80), tolerance: 2);
        rotated.Right.ShouldBe(80 + (plain.Bottom - 80), tolerance: 2);
        rotated.Top.ShouldBe(80 - (plain.Right - 80), tolerance: 2);
        rotated.Bottom.ShouldBe(80 - (plain.Left - 80), tolerance: 2);
    }

    [Fact]
    public void Draw_CustomTagScaleAndRotation_DoesNotMoveTheFollowingLetter()
    {
        Text.Customizations["SmallTurn"] = (index, block) => new LetterCustomization
        {
            ScaleX = 0.5f,
            ScaleY = 0.5f,
            RotationDegrees = 10,
        };

        Bounds plain = InkBounds(Render("IW", left: 20, top: 80));
        Bounds customized = InkBounds(Render("[Custom=SmallTurn]I[/Custom]W", left: 20, top: 80));

        // The W is the rightmost ink in both, so it is drawn at the same place.
        customized.Right.ShouldBe(plain.Right);
    }

    private readonly record struct Bounds(float Left, float Top, float Right, float Bottom)
    {
        public float Width => Right - Left;
        public float Height => Bottom - Top;
    }

    private static Bounds InkBounds(Color[] pixels)
    {
        int left = int.MaxValue, top = int.MaxValue, right = int.MinValue, bottom = int.MinValue;
        for (int i = 0; i < pixels.Length; i++)
        {
            if (pixels[i].A > 128)
            {
                int x = i % CanvasWidth;
                int y = i / CanvasWidth;
                left = Math.Min(left, x);
                top = Math.Min(top, y);
                right = Math.Max(right, x + 1);
                bottom = Math.Max(bottom, y + 1);
            }
        }
        left.ShouldNotBe(int.MaxValue, "because something is drawn");
        return new Bounds(left, top, right, bottom);
    }

    private static Text BuildText(string markup, float left, float top, out TextRuntime runtime)
    {
        runtime = new TextRuntime();
        runtime.X = left;
        runtime.Y = top;
        runtime.FontScale = 2;
        runtime.Text = markup;
        Gum.GumService.Default.Root.Children!.Add(runtime);
        Gum.GumService.Default.Root.UpdateLayout();
        return (Text)runtime.RenderableComponent!;
    }

    private static Color[] Render(string markup, float left, float top)
    {
        ContainerRuntime container = new();
        container.Width = CanvasWidth;
        container.Height = CanvasHeight;
        container.IsRenderTarget = true;

        TextRuntime text = new();
        text.X = left;
        text.Y = top;
        text.FontScale = 2;
        text.Text = markup;
        container.Children.Add(text);

        Gum.GumService.Default.Root.Children!.Add(container);
        Gum.GumService.Default.Root.UpdateLayout();

        BeginDrawing();
        Gum.GumService.Default.Draw();
        EndDrawing();

        RenderTexture2D rt = Renderer.Self.TryGetBakedRenderTargetFor(container)!.Value;
        Image image = LoadImageFromTexture(rt.Texture);
        Color[] result = new Color[CanvasWidth * CanvasHeight];
        for (int y = 0; y < CanvasHeight; y++)
        {
            for (int x = 0; x < CanvasWidth; x++)
            {
                result[y * CanvasWidth + x] = GetImageColor(image, x, rt.Texture.Height - 1 - y);
            }
        }
        UnloadImage(image);
        Gum.GumService.Default.Root.Children.Remove(container);
        return result;
    }
}
