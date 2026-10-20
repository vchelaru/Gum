using Gum.DataTypes;
using Gum.GueDeriving;
using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using XnaColor = Microsoft.Xna.Framework.Color;

namespace MonoGameGum.IntegrationTests.MonoGameGum.Rendering;

public enum TextMaskScenario { SingleLine, MultiLineCentered, Rotated, Dropshadow, ScaledAndSpaced }

/// <summary>
/// Issue #6237: text drawn with ReplaceAlpha or MinAlpha inside a render-target container masks its
/// whole bounds (the measured block plus 2px, plus the drop shadow offset), like Skia, not only the
/// glyph cells. Each text is drawn over an opaque red fill in the render target, which is then
/// composited over a blue-gray backdrop. A text drawn with Normal blend gives the glyph coverage; the
/// masked render must then show the backdrop through every pixel of the bounds that no glyph covers
/// (and the glyph's coverage elsewhere), and must leave every pixel outside the bounds alone.
/// </summary>
[Collection(SharedGame.CollectionName)]
public class TextAlphaBoundsMaskTests : BaseTestClass
{
    internal const int CaptureWidth = 170;
    internal const int CaptureHeight = 130;
    private const float TextX = 40;
    private const float TextY = 40;
    private const float Padding = 2;

    // Pixels this close to the bounds edge are skipped: the edge pixel is only partly covered.
    private const double EdgeBand = 1.5;

    internal static readonly XnaColor Backdrop = new XnaColor((byte)70, (byte)70, (byte)90, (byte)255);
    internal static readonly XnaColor Fill = new XnaColor((byte)200, (byte)30, (byte)30, (byte)255);

    [Theory]
    [InlineData(Gum.RenderingLibrary.Blend.ReplaceAlpha, TextMaskScenario.SingleLine)]
    [InlineData(Gum.RenderingLibrary.Blend.ReplaceAlpha, TextMaskScenario.MultiLineCentered)]
    [InlineData(Gum.RenderingLibrary.Blend.ReplaceAlpha, TextMaskScenario.Rotated)]
    [InlineData(Gum.RenderingLibrary.Blend.ReplaceAlpha, TextMaskScenario.Dropshadow)]
    [InlineData(Gum.RenderingLibrary.Blend.ReplaceAlpha, TextMaskScenario.ScaledAndSpaced)]
    [InlineData(Gum.RenderingLibrary.Blend.MinAlpha, TextMaskScenario.SingleLine)]
    [InlineData(Gum.RenderingLibrary.Blend.MinAlpha, TextMaskScenario.MultiLineCentered)]
    [InlineData(Gum.RenderingLibrary.Blend.MinAlpha, TextMaskScenario.Rotated)]
    [InlineData(Gum.RenderingLibrary.Blend.MinAlpha, TextMaskScenario.Dropshadow)]
    [InlineData(Gum.RenderingLibrary.Blend.MinAlpha, TextMaskScenario.ScaledAndSpaced)]
    public void Text_MasksItsWholeBounds_AndNothingOutside(Gum.RenderingLibrary.Blend blend, TextMaskScenario scenario)
    {
        List<string> violations = Audit(blend, scenario, out _);

        violations.Count.ShouldBe(0,
            $"{blend}/{scenario}: {violations.Count} bad pixels, first {Math.Min(violations.Count, 10)}:"
            + Environment.NewLine + string.Join(Environment.NewLine, violations.Take(10)));
    }

    /// <summary>
    /// The rule the Skia runtime follows too (SkiaGum.Tests' BlendTests): the blend changes the measured
    /// text block plus 2 pixels on every side, and nothing outside it.
    /// </summary>
    [Theory]
    [InlineData(Gum.RenderingLibrary.Blend.ReplaceAlpha)]
    [InlineData(Gum.RenderingLibrary.Blend.MinAlpha)]
    public void Text_ChangesItsMeasuredBlockPlusTwoPixels(Gum.RenderingLibrary.Blend blend)
    {
        TextMaskCapture capture = Render(blend, TextMaskScenario.SingleLine, "HOUZ 0123\nNLM", out float width, out float height);

        int left = int.MaxValue, top = int.MaxValue, right = int.MinValue, bottom = int.MinValue;
        for (int y = 0; y < CaptureHeight; y++)
        {
            for (int x = 0; x < CaptureWidth; x++)
            {
                if (capture.Pixels[(y * CaptureWidth) + x].R != Fill.R)
                {
                    left = Math.Min(left, x);
                    top = Math.Min(top, y);
                    right = Math.Max(right, x + 1);
                    bottom = Math.Max(bottom, y + 1);
                }
            }
        }

        Math.Abs(left - (TextX - Padding)).ShouldBeLessThanOrEqualTo(1, "left");
        Math.Abs(top - (TextY - Padding)).ShouldBeLessThanOrEqualTo(1, "top");
        Math.Abs((right - left) - (width + (2 * Padding))).ShouldBeLessThanOrEqualTo(1, $"width, text measured {width}");
        Math.Abs((bottom - top) - (height + (2 * Padding))).ShouldBeLessThanOrEqualTo(1, $"height, text measured {height}");
    }

    [Fact]
    public void Text_DoesNotLeaveStencilStateForLaterSiblings()
    {
        TextMaskCapture capture = Render(Gum.RenderingLibrary.Blend.ReplaceAlpha, TextMaskScenario.SingleLine, null,
            out _, out _, lateSibling: true);

        // A later sibling with the same blend draws with the stencil test off: a 50% white ReplaceAlpha
        // rectangle over the fill shows the fill at half alpha over the backdrop.
        XnaColor pixel = capture.Pixels[(110 * CaptureWidth) + 20];
        Math.Abs(pixel.R - 135).ShouldBeLessThanOrEqualTo(2);
        Math.Abs(pixel.G - 50).ShouldBeLessThanOrEqualTo(2);
    }

    [Fact]
    public void Text_InsideClippingContainer_ErasesInsideTheClip_AndTouchesNothingOutside()
    {
        // The text starts at (40, 40); the clip is (60, 30) to (100, 70). The bounds extend past it on all sides.
        TextMaskCapture capture = Render(Gum.RenderingLibrary.Blend.ReplaceAlpha, TextMaskScenario.SingleLine, null,
            out _, out _, clipRectangle: new System.Drawing.Rectangle(60, 30, 40, 40));

        XnaColor Pixel(int x, int y) => capture.Pixels[(y * CaptureWidth) + x];

        // Inside the clip and the text's bounds, between the glyphs: erased.
        bool anyErasedInClip = false;
        for (int y = 30; y < 70; y++)
        {
            for (int x = 60; x < 100; x++)
            {
                anyErasedInClip |= Pixel(x, y).R == Backdrop.R;
                if (y == 39)
                {
                    // Above the glyph cells, inside the text's bounds: erased.
                    Pixel(x, y).R.ShouldBe(Backdrop.R, $"({x},{y})");
                }
                if (y < 38 || y > 62)
                {
                    // Above and below the text's bounds, inside the clip: untouched.
                    Pixel(x, y).R.ShouldBe(Fill.R, $"({x},{y})");
                }
            }
        }
        anyErasedInClip.ShouldBeTrue();

        // Inside the bounds, outside the clip: untouched.
        Pixel(50, 55).R.ShouldBe(Fill.R);
        Pixel(110, 55).R.ShouldBe(Fill.R);
    }

    /// <summary>
    /// Renders the scene with Normal blend (glyph coverage) and with the blend under test, and returns
    /// every pixel that is wrong: inside the bounds the masked pixel must show the glyph's coverage over
    /// the backdrop, outside the bounds it must still be the fill.
    /// </summary>
    internal static List<string> Audit(Gum.RenderingLibrary.Blend blend, TextMaskScenario scenario, out TextMaskCapture masked)
    {
        TextMaskCapture normal = Render(null, scenario);
        masked = Render(blend, scenario);

        List<string> violations = new();
        for (int y = 0; y < CaptureHeight; y++)
        {
            for (int x = 0; x < CaptureWidth; x++)
            {
                double distance = masked.SignedDistanceInsideBounds(x + 0.5, y + 0.5);
                if (Math.Abs(distance) < EdgeBand)
                {
                    continue;
                }

                XnaColor normalPixel = normal.Pixels[(y * CaptureWidth) + x];
                double coverage = Math.Clamp((normalPixel.R - Fill.R) / (double)(255 - Fill.R), 0, 1);

                double expectedR = distance > 0
                    ? Backdrop.R + ((Fill.R - Backdrop.R) * coverage)
                    : Fill.R;
                XnaColor actual = masked.Pixels[(y * CaptureWidth) + x];
                if (Math.Abs(actual.R - expectedR) > 6)
                {
                    violations.Add($"({x},{y}) {(distance > 0 ? "inside" : "outside")} bounds: R {actual.R}, expected {expectedR:0}");
                }
            }
        }

        return violations;
    }

    internal sealed class TextMaskCapture
    {
        public XnaColor[] Pixels = null!;

        // Bounds in the text's local space (origin at its top-left, before rotation), padding included.
        public double Left;
        public double Top;
        public double Right;
        public double Bottom;
        public double RotationDegrees;

        public double SignedDistanceInsideBounds(double px, double py)
        {
            double r = RotationDegrees * Math.PI / 180.0;
            double dx = px - TextX;
            double dy = py - TextY;
            double lx = (dx * Math.Cos(r)) - (dy * Math.Sin(r));
            double ly = (dx * Math.Sin(r)) + (dy * Math.Cos(r));
            return Math.Min(Math.Min(lx - Left, Right - lx), Math.Min(ly - Top, Bottom - ly));
        }
    }

    internal static TextMaskCapture Render(Gum.RenderingLibrary.Blend? blend, TextMaskScenario scenario) =>
        Render(blend, scenario, null, out _, out _);

    internal static TextMaskCapture Render(Gum.RenderingLibrary.Blend? blend, TextMaskScenario scenario, string? textOverride,
        out float measuredWidth, out float measuredHeight, bool lateSibling = false, System.Drawing.Rectangle? clipRectangle = null)
    {
        using MinimalGame game = new();
        game.RunOneFrame();
        GraphicsDevice gd = game.GraphicsDevice;
        SystemManagers managers = SystemManagers.Default;

        ContainerRuntime root = new();
        root.Width = CaptureWidth;
        root.Height = CaptureHeight;
#pragma warning disable CS0618 // ColoredRectangleRuntime is obsolete; simplest solid fill.
        root.AddChild(new ColoredRectangleRuntime { Width = CaptureWidth, Height = CaptureHeight, Color = Backdrop });
        ContainerRuntime renderTarget = new();
        renderTarget.Width = CaptureWidth;
        renderTarget.Height = CaptureHeight;
        renderTarget.IsRenderTarget = true;
        renderTarget.AddChild(new ColoredRectangleRuntime { Width = CaptureWidth, Height = CaptureHeight, Color = Fill });
#pragma warning restore CS0618

        TextRuntime text = new();
        text.WidthUnits = DimensionUnitType.RelativeToChildren;
        text.HeightUnits = DimensionUnitType.RelativeToChildren;
        text.X = TextX;
        text.Y = TextY;
        text.Color = XnaColor.White;
        text.Text = "HOUZ 0123 NLM";
        double extraPadding = 0;
        switch (scenario)
        {
            case TextMaskScenario.MultiLineCentered:
                text.Text = "HOUZ 0123 NLM\nNLM\nZUOH 2468";
                text.HorizontalAlignment = RenderingLibrary.Graphics.HorizontalAlignment.Center;
                break;
            case TextMaskScenario.Rotated:
                text.Text = "HOUZ\n0123 NLM";
                text.Rotation = 25;
                break;
            case TextMaskScenario.Dropshadow:
                text.HasDropshadow = true;
                text.DropshadowOffsetX = 3;
                text.DropshadowOffsetY = 2;
                extraPadding = 3;
                break;
            case TextMaskScenario.ScaledAndSpaced:
                text.Text = "HOUZ\nNLM0";
                text.FontScale = 1.5f;
                text.LineHeightMultiplier = 1.4f;
                break;
        }

        if (blend != null)
        {
            text.Blend = blend;
        }
        if (textOverride != null)
        {
            text.Text = textOverride;
        }

        if (clipRectangle != null)
        {
            ContainerRuntime clip = new();
            clip.X = clipRectangle.Value.X;
            clip.Y = clipRectangle.Value.Y;
            clip.Width = clipRectangle.Value.Width;
            clip.Height = clipRectangle.Value.Height;
            clip.ClipsChildren = true;
            text.X = TextX - clip.X;
            text.Y = TextY - clip.Y;
            clip.AddChild(text);
            renderTarget.AddChild(clip);
        }
        else
        {
            renderTarget.AddChild(text);
        }

        if (lateSibling)
        {
#pragma warning disable CS0618 // ColoredRectangleRuntime is obsolete; simplest solid fill.
            ColoredRectangleRuntime sibling = new() { X = 10, Y = 100, Width = 30, Height = 20, Color = XnaColor.White };
            sibling.Alpha = 128;
            sibling.Blend = Gum.RenderingLibrary.Blend.ReplaceAlpha;
            renderTarget.AddChild(sibling);
#pragma warning restore CS0618
        }

        root.AddChild(renderTarget);

        root.AddToManagers(managers, null);
        root.UpdateLayout();

        TextMaskCapture capture = new();
        capture.Left = -Padding - extraPadding;
        capture.Top = -Padding - extraPadding;
        capture.Right = text.GetAbsoluteWidth() + Padding + extraPadding;
        capture.Bottom = text.GetAbsoluteHeight() + Padding + extraPadding;
        capture.RotationDegrees = text.Rotation;
        measuredWidth = text.GetAbsoluteWidth();
        measuredHeight = text.GetAbsoluteHeight();

        using RenderTarget2D target = new(gd, CaptureWidth, CaptureHeight, false, SurfaceFormat.Color,
            DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
        for (int i = 0; i < 2; i++)
        {
            gd.SetRenderTarget(target);
            gd.Clear(XnaColor.Black);
            managers.Renderer.Draw(managers);
        }
        gd.SetRenderTarget(null);

        capture.Pixels = new XnaColor[CaptureWidth * CaptureHeight];
        target.GetData(capture.Pixels);
        root.RemoveFromManagers();
        return capture;
    }

    private sealed class MinimalGame : SharedGameLease
    {
        protected override void OnInitialize() => Gum.GumService.Default.Initialize(Game, Gum.Forms.DefaultVisualsVersion.V3);

        protected override void OnDispose()
        {
            if (Gum.GumService.Default.IsInitialized)
            {
                Gum.GumService.Default.Uninitialize();
            }
        }
    }
}
