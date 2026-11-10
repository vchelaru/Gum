using RaylibGum.Tests.Helpers;
using Gum.Content.AnimationChain;
using Gum.DataTypes;
using Gum.Wireframe;
using Gum.Managers;
using Gum.GueDeriving;
using Gum.Graphics.Animation;
using Raylib_cs;
using RenderingLibrary.Graphics;
using RenderingLibrary.Graphics.Animation;
using static Raylib_cs.Raylib;

namespace RaylibGum.Tests.Rendering;

/// <summary>
/// Every color operation gives the same pixel on every sprite and nine-slice draw path: plain, source
/// rectangle, wrap, nine-slice, animation chain and rotated. The texture is opaque gray (40,40,40), the
/// tint is (255,128,0,a) and the backdrop is (70,70,90); the expected pixel is
/// <c>backdrop * (1 - a) + source * a</c>, where source is tex * tint (Modulate), tint
/// (ColorTextureAlpha) or clamp(tex + tint) (Add).
/// </summary>
public class SpriteColorOperationPathTests : BaseTestClass
{
    static Color Backdrop => new Color((byte)70, (byte)70, (byte)90, (byte)255);

    static readonly string[] Paths = { "plain", "srcrect", "wrap", "nineslice", "chain", "rotated" };

    public static IEnumerable<object[]> Cases()
    {
        foreach (var op in new[] { ColorOperation.Add, ColorOperation.Modulate, ColorOperation.ColorTextureAlpha })
        foreach (byte a in new byte[] { 255, 128, 64 })
        foreach (var path in Paths)
        {
            yield return new object[] { op, a, path };
        }
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void Draw_EveryPath_MatchesFormula(ColorOperation op, byte alpha, string path)
    {
        Texture2D texture = CreateGrayTexture();
        try
        {
            PixelGrid pixels = RaylibPixelHarness.Render(64, 64, Backdrop, c => c.Children.Add(Build(path, op, alpha, texture)));
            Color actual = pixels[32, 32];

            (int r, int g, int b) source = op switch
            {
                ColorOperation.Add => (255, 168, 40),
                ColorOperation.ColorTextureAlpha => (255, 128, 0),
                _ => (40, 20, 0),
            };
            byte expectedR = Blend(Backdrop.R, source.r, alpha);
            byte expectedG = Blend(Backdrop.G, source.g, alpha);
            byte expectedB = Blend(Backdrop.B, source.b, alpha);

            bool matches = Math.Abs(actual.R - expectedR) <= 3 && Math.Abs(actual.G - expectedG) <= 3 && Math.Abs(actual.B - expectedB) <= 3;
            Assert.True(matches, $"{op} a={alpha} {path}: expected ({expectedR},{expectedG},{expectedB}) +-3 but drew ({actual.R},{actual.G},{actual.B})");
        }
        finally
        {
            UnloadTexture(texture);
        }
    }

    [Fact]
    public void Draw_AddOnEveryPath_AllocatesNothingPerFrame()
    {
        Texture2D texture = CreateGrayTexture();
        var items = new List<GraphicalUiElement>();
        try
        {
            foreach (var path in new[] { "plain", "srcrect", "wrap", "nineslice", "rotated" })
            {
                items.Add(Build(path, ColorOperation.Add, 128, texture));
            }
            items.Add(Build("plain", ColorOperation.Modulate, 128, texture));
            foreach (var item in items)
            {
                Gum.GumService.Default.Root.Children.Add(item);
            }
            Gum.GumService.Default.Root.UpdateLayout();

            long best = long.MaxValue;
            for (int attempt = 0; attempt < 3; attempt++)
            {
                for (int i = 0; i < 20; i++) Frame();
                long before = GC.GetAllocatedBytesForCurrentThread();
                for (int i = 0; i < 30; i++) Frame();
                best = Math.Min(best, GC.GetAllocatedBytesForCurrentThread() - before);
            }

            Assert.Equal(0, best);
        }
        finally
        {
            foreach (var item in items) Gum.GumService.Default.Root.Children.Remove(item);
            UnloadTexture(texture);
        }
    }

    static byte Blend(int backdrop, int source, byte alpha) =>
        (byte)Math.Round(backdrop + (source - backdrop) * alpha / 255.0);

    static Texture2D CreateGrayTexture()
    {
        Image image = GenImageColor(12, 12, new Color((byte)40, (byte)40, (byte)40, (byte)255));
        Texture2D texture = LoadTextureFromImage(image);
        UnloadImage(image);
        return texture;
    }

    static void Frame()
    {
        BeginDrawing();
        Gum.GumService.Default.Draw();
        EndDrawing();
    }

    static GraphicalUiElement Build(string path, ColorOperation op, byte a, Texture2D texture)
    {
        var tint = new Color((byte)255, (byte)128, (byte)0, a);
        if (path == "nineslice")
        {
            var nineSlice = new NineSliceRuntime();
            nineSlice.WidthUnits = DimensionUnitType.Absolute;
            nineSlice.HeightUnits = DimensionUnitType.Absolute;
            nineSlice.X = 16; nineSlice.Y = 16; nineSlice.Width = 32; nineSlice.Height = 32;
            nineSlice.Texture = texture;
            nineSlice.Color = tint;
            nineSlice.ColorOperation = op;
            return nineSlice;
        }

        var sprite = new SpriteRuntime();
        sprite.WidthUnits = DimensionUnitType.Absolute;
        sprite.HeightUnits = DimensionUnitType.Absolute;
        sprite.X = 16; sprite.Y = 16; sprite.Width = 32; sprite.Height = 32;
        sprite.Texture = texture;
        sprite.Color = tint;
        sprite.ColorOperation = op;
        switch (path)
        {
            case "srcrect":
                sprite.TextureAddress = TextureAddress.Custom;
                sprite.TextureLeft = 2; sprite.TextureTop = 2; sprite.TextureWidth = 6; sprite.TextureHeight = 6;
                break;
            case "wrap":
                sprite.TextureAddress = TextureAddress.Custom;
                sprite.TextureLeft = 0; sprite.TextureTop = 0; sprite.TextureWidth = 24; sprite.TextureHeight = 24;
                sprite.SetProperty("Wrap", true);
                break;
            case "rotated":
                sprite.X = 48; sprite.Y = 48; sprite.Rotation = 180;
                break;
            case "chain":
                var frame = new AnimationFrame { Texture = texture, FrameLength = 10f };
                if (op == ColorOperation.ColorTextureAlpha)
                {
                    frame.RenderColorOperation = ColorOperation.ColorTextureAlpha;
                    frame.Alpha = a;
                }
                else
                {
                    // The frame's own color operation and RGBA replace the sprite's.
                    frame.ColorOperation = op == ColorOperation.Add ? AnimationFrameColorOperation.Add : AnimationFrameColorOperation.Multiply;
                    frame.Red = 255; frame.Green = 128; frame.Blue = 0; frame.Alpha = a;
                    sprite.Color = new Color((byte)255, (byte)255, (byte)255, (byte)255);
                    sprite.ColorOperation = ColorOperation.Modulate;
                }
                var chain = new AnimationChain { Name = "C" };
                chain.Add(frame);
                sprite.AnimationChains = new AnimationChainList { chain };
                sprite.CurrentChainName = "C";
                sprite.Animate = true;
                sprite.AnimateSelf(0.001);
                break;
        }
        return sprite;
    }
}
