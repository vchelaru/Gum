using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Wireframe;
using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary;
using RenderingLibrary.Content;
using RenderingLibrary.Graphics;
using RenderingLibrary.Math.Geometry;
using Shouldly;
using Xunit;
using XnaColor = Microsoft.Xna.Framework.Color;

namespace MonoGameGum.IntegrationTests.MonoGameGum.Rendering;

/// <summary>
/// Content drawn inside an <c>IsRenderTarget</c> container (path B) must render the same pixels as the same
/// content in a plain container (path A), in both alpha modes. Each case draws solid colors over a solid
/// background and checks both paths, pixel by pixel, against an image worked out by hand with the
/// "over" operator: with a = texelAlpha * elementAlpha * containerAlpha,
///   opaque background:      rgb = src * a + dst * (1 - a), alpha = 255;
///   transparent background: rgb = src * a + dst * (1 - a), alpha = a + dstA * (1 - a) (premultiplied form).
/// Transparent-background cases draw with <see cref="Renderer.IsDrawingToTransparentTarget"/>, as Export as
/// Image and <c>gumcli screenshot</c> do (#7992), and are checked RGBA on both paths. The Editor host backs
/// the container with a <see cref="LineRectangle"/>, as the Gum tool does; its outline pixels are excluded.
/// Set GUM_RT_PARITY_DIR to save the A, B, expected and diff PNGs plus results.txt.
/// </summary>
[Collection(SharedGame.CollectionName)]
public class RenderTargetVsDirectParityTests : BaseTestClass
{
    private const int Width = 128;
    private const int Height = 96;
    private const int Tolerance = 2;

    // The container sits at (8,8); content positions below are absolute.
    private const int ContainerX = 8;
    private const int ContainerY = 8;
    private const int ContainerWidth = 112;
    private const int ContainerHeight = 80;

    private const string TextToDraw = "HOUZ 0123";
    private static readonly Rgba TextColor = new(255, 128, 0, 255);
    private const int TextAlpha = 160;

    public enum Case
    {
        PartialAlphaSprite,
        SpriteAlpha128,
        OverlappingSprites,
        NestedRenderTarget,
        ContainerAlpha128,
        SemiTransparentColoredRectangle,
        Text,
        TransparentClearSprite,
        TransparentClearOverlappingSprites,
    }

    public enum Host { Runtime, Editor }

    private sealed record Rgba(byte R, byte G, byte B, byte A);

    // A solid quad: absolute pixel rectangle, straight color, and the alpha the element multiplies in.
    private sealed record Quad(int X, int Y, int W, int H, Rgba Color, int ElementAlpha);

    // Background null = transparent clear (no background sprite).
    private sealed record Scene(Rgba? Background, Quad[] Quads, int ContainerAlpha, bool Nested, bool IsText,
        bool IsColoredRectangle);

    public override void Dispose()
    {
        Renderer.UsePremultipliedAlpha = false;
        base.Dispose();
    }

    public static IEnumerable<object[]> AllCases()
    {
        foreach (Case c in Enum.GetValues<Case>())
        {
            foreach (bool premultiplied in new[] { false, true })
            {
                foreach (Host host in Enum.GetValues<Host>())
                {
                    yield return new object[] { c, premultiplied, host };
                }
            }
        }
    }

    [Theory]
    [MemberData(nameof(AllCases))]
    public void RenderTargetContainer_MatchesPlainContainerAndHandMath(Case c, bool premultiplied, Host host)
    {
        Scene scene = SceneFor(c);

        XnaColor[] a = Render(scene, premultiplied, host, viaRenderTarget: false, textAlpha: TextAlpha);
        XnaColor[] b = Render(scene, premultiplied, host, viaRenderTarget: true, textAlpha: TextAlpha);

        bool[] mask = OutlineMask(host);
        float[] coverage = scene.IsText
            ? TextCoverage(Render(scene, premultiplied, Host.Runtime, viaRenderTarget: false, textAlpha: 255), scene)
            : QuadCoverage(scene);
        if (scene.IsText)
        {
            // Only pixels whose glyph coverage is known exactly (fully covered, or untouched) are scored.
            for (int i = 0; i < mask.Length; i++)
            {
                mask[i] &= coverage[i] is 0f or 1f;
            }
        }

        XnaColor[] expected = ExpectedImage(scene, coverage);

        Stats aVsB = Compare(a, b, mask);
        Stats aVsExpected = Compare(a, expected, mask);
        Stats bVsExpected = Compare(b, expected, mask);

        string name = $"{c}_{(premultiplied ? "Premultiplied" : "Straight")}_{host}";
        Report(name, scene, coverage, a, b, expected, mask, aVsB, aVsExpected, bVsExpected);

        bVsExpected.MaxDelta.ShouldBeLessThanOrEqualTo(Tolerance, $"{name}: B vs expected (RGBA)");

        // The canvas shows every frame opaque (AvaloniaRenderSurface.Push with forceOpaque), so over an opaque
        // background the straight pipeline's alpha channel (srcA * srcA from NonPremultiplied) is never seen.
        bool alphaObservable = premultiplied || scene.Background == null;
        Compare(a, expected, mask, ignoreAlpha: !alphaObservable).MaxDelta
            .ShouldBeLessThanOrEqualTo(Tolerance, $"{name}: A vs expected ({(alphaObservable ? "RGBA" : "RGB")})");
    }

    private static Scene SceneFor(Case c)
    {
        Rgba blue = new(0, 0, 255, 255);
        Rgba yellow = new(255, 255, 0, 255);
        Rgba gray = new(60, 60, 60, 255);
        Rgba redHalf = new(255, 0, 0, 128);
        Rgba greenHalf = new(0, 255, 0, 128);

        // Content rectangle 24..71 x 24..63 (container (8,8) + child (16,16)).
        Quad redQuad = new(24, 24, 48, 40, redHalf, 255);
        Quad greenQuad = new(48, 40, 48, 40, greenHalf, 255);

        return c switch
        {
            Case.PartialAlphaSprite => new(blue, new[] { redQuad }, 255, false, false, false),
            Case.SpriteAlpha128 => new(yellow, new[] { new Quad(24, 24, 48, 40, new(0, 128, 255, 192), 128) }, 255, false, false, false),
            Case.OverlappingSprites => new(blue, new[] { redQuad, greenQuad }, 255, false, false, false),
            Case.NestedRenderTarget => new(gray, new[] { new Quad(24, 24, 48, 40, new(255, 200, 0, 100), 255) }, 255, true, false, false),
            Case.ContainerAlpha128 => new(blue, new[] { redQuad }, 128, false, false, false),
            Case.SemiTransparentColoredRectangle => new(yellow, new[] { new Quad(24, 24, 48, 40, new(200, 80, 40, 128), 255) }, 255, false, false, true),
            Case.Text => new(blue, new[] { new Quad(24, 24, 0, 0, TextColor, TextAlpha) }, 255, false, true, false),
            Case.TransparentClearSprite => new(null, new[] { redQuad }, 255, false, false, false),
            Case.TransparentClearOverlappingSprites => new(null, new[] { redQuad, greenQuad }, 255, false, false, false),
            _ => throw new ArgumentOutOfRangeException(nameof(c)),
        };
    }

    // Per-pixel coverage of each quad, 1 inside, 0 outside; summed into one map only for the expected image.
    private static float[] QuadCoverage(Scene scene)
    {
        float[] coverage = new float[Width * Height];
        foreach (Quad q in scene.Quads)
        {
            for (int y = q.Y; y < q.Y + q.H; y++)
            {
                for (int x = q.X; x < q.X + q.W; x++)
                {
                    coverage[y * Width + x] = 1f;
                }
            }
        }
        return coverage;
    }

    // Glyph coverage read from an opaque-text render: exactly the text color = fully covered (1), exactly the
    // background = untouched (0), anything else = an antialiased edge (excluded from scoring).
    private static float[] TextCoverage(XnaColor[] opaqueText, Scene scene)
    {
        float[] coverage = new float[Width * Height];
        Rgba bg = scene.Background!;
        for (int i = 0; i < coverage.Length; i++)
        {
            XnaColor p = opaqueText[i];
            if (p.R == TextColor.R && p.G == TextColor.G && p.B == TextColor.B)
            {
                coverage[i] = 1f;
            }
            else if (p.R == bg.R && p.G == bg.G && p.B == bg.B)
            {
                coverage[i] = 0f;
            }
            else
            {
                coverage[i] = 0.5f;
            }
        }
        coverage.Count(x => x == 1f).ShouldBeGreaterThan(20, "the opaque text render should have fully covered glyph pixels");
        return coverage;
    }

    // The hand-computed image: the over operator, quad by quad.
    private static XnaColor[] ExpectedImage(Scene scene, float[] textCoverage)
    {
        int n = Width * Height;
        float[] r = new float[n], g = new float[n], b = new float[n], alpha = new float[n];
        for (int i = 0; i < n; i++)
        {
            Rgba? bg = scene.Background;
            r[i] = bg?.R ?? 0;
            g[i] = bg?.G ?? 0;
            b[i] = bg?.B ?? 0;
            alpha[i] = bg == null ? 0 : 1;
        }

        void Over(int i, Rgba color, float a)
        {
            r[i] = color.R * a + r[i] * (1 - a);
            g[i] = color.G * a + g[i] * (1 - a);
            b[i] = color.B * a + b[i] * (1 - a);
            alpha[i] = a + alpha[i] * (1 - a);
        }

        foreach (Quad q in scene.Quads)
        {
            // GPU sees the alphas as 8-bit values: texel byte, and the element alpha byte in the vertex color.
            float a = q.Color.A / 255f * (q.ElementAlpha / 255f) * (scene.ContainerAlpha / 255f);
            if (scene.IsText)
            {
                for (int i = 0; i < n; i++)
                {
                    if (textCoverage[i] == 1f)
                    {
                        Over(i, q.Color, a);
                    }
                }
                continue;
            }
            for (int y = q.Y; y < q.Y + q.H; y++)
            {
                for (int x = q.X; x < q.X + q.W; x++)
                {
                    Over(y * Width + x, q.Color, a);
                }
            }
        }

        static byte B(float v) => (byte)Math.Round(Math.Clamp(v, 0, 255));
        XnaColor[] expected = new XnaColor[n];
        for (int i = 0; i < n; i++)
        {
            expected[i] = new XnaColor(B(r[i]), B(g[i]), B(b[i]), B(alpha[i] * 255));
        }
        return expected;
    }

    // Editor host: the LineRectangle outline (and one pixel either side) is not part of the content.
    private static bool[] OutlineMask(Host host)
    {
        bool[] mask = new bool[Width * Height];
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                bool nearVertical = (Math.Abs(x - ContainerX) <= 1 || Math.Abs(x - (ContainerX + ContainerWidth - 1)) <= 1 ||
                                     Math.Abs(x - (ContainerX + ContainerWidth)) <= 1)
                                    && y >= ContainerY - 1 && y <= ContainerY + ContainerHeight + 1;
                bool nearHorizontal = (Math.Abs(y - ContainerY) <= 1 || Math.Abs(y - (ContainerY + ContainerHeight - 1)) <= 1 ||
                                       Math.Abs(y - (ContainerY + ContainerHeight)) <= 1)
                                      && x >= ContainerX - 1 && x <= ContainerX + ContainerWidth + 1;
                mask[y * Width + x] = !(host == Host.Editor && (nearVertical || nearHorizontal));
            }
        }
        return mask;
    }

    private XnaColor[] Render(Scene scene, bool premultiplied, Host host, bool viaRenderTarget, int textAlpha)
    {
        using MinimalGame game = new(premultiplied);
        game.RunOneFrame();
        GraphicsDevice gd = game.GraphicsDevice;
        SystemManagers managers = SystemManagers.Default;
        List<string> tempFiles = new();

        ContainerRuntime root = new();
        try
        {
            root.Width = Width;
            root.Height = Height;
            root.WidthUnits = DimensionUnitType.Absolute;
            root.HeightUnits = DimensionUnitType.Absolute;
            if (scene.Background != null)
            {
                root.AddChild(SolidSprite(gd, tempFiles, scene.Background, 0, 0, Width, Height, 255));
            }

            GraphicalUiElement container = CreateContainer(managers, host, viaRenderTarget);
            container.X = ContainerX;
            container.Y = ContainerY;
            container.Width = ContainerWidth;
            container.Height = ContainerHeight;
            container.WidthUnits = DimensionUnitType.Absolute;
            container.HeightUnits = DimensionUnitType.Absolute;
            root.AddChild(container);

            GraphicalUiElement parent = container;
            if (scene.Nested)
            {
                GraphicalUiElement inner = CreateContainer(managers, Host.Runtime, viaRenderTarget);
                inner.X = 0;
                inner.Y = 0;
                inner.Width = ContainerWidth;
                inner.Height = ContainerHeight;
                inner.WidthUnits = DimensionUnitType.Absolute;
                inner.HeightUnits = DimensionUnitType.Absolute;
                container.AddChild(inner);
                parent = inner;
            }

            // Container alpha on the render-target path; the plain container carries no group alpha, so the
            // same factor goes on the child instead (identical for a single non-overlapping child).
            int childAlphaFactor = 255;
            if (scene.ContainerAlpha != 255)
            {
                if (!viaRenderTarget)
                {
                    childAlphaFactor = scene.ContainerAlpha;
                }
                else if (container is ContainerRuntime runtimeContainer)
                {
                    runtimeContainer.Alpha = scene.ContainerAlpha;
                }
                else
                {
                    container.SetProperty("Alpha", scene.ContainerAlpha);
                }
            }

            foreach (Quad q in scene.Quads)
            {
                int elementAlpha = (int)Math.Round(q.ElementAlpha * childAlphaFactor / 255f);
                int x = q.X - ContainerX;
                int y = q.Y - ContainerY;
                GraphicalUiElement child;
                if (scene.IsText)
                {
                    TextRuntime text = new();
                    text.X = x;
                    text.Y = y;
                    text.Color = new XnaColor(q.Color.R, q.Color.G, q.Color.B, q.Color.A);
                    text.Alpha = textAlpha;
                    text.Text = TextToDraw;
                    child = text;
                }
                else if (scene.IsColoredRectangle)
                {
#pragma warning disable CS0618 // ColoredRectangleRuntime is obsolete; it is the case under test.
                    child = new ColoredRectangleRuntime
                    {
                        X = x,
                        Y = y,
                        Width = q.W,
                        Height = q.H,
                        Color = new XnaColor(q.Color.R, q.Color.G, q.Color.B, q.Color.A),
                    };
#pragma warning restore CS0618
                }
                else
                {
                    child = SolidSprite(gd, tempFiles, q.Color, x, y, q.W, q.H, elementAlpha);
                }
                parent.AddChild(child);
            }

            root.AddToManagers(managers, null);
            root.UpdateLayout();

            using RenderTarget2D target = new(gd, Width, Height, false, SurfaceFormat.Color,
                DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
            for (int i = 0; i < 2; i++)
            {
                gd.SetRenderTarget(target);
                // Opaque scenes draw a full-size background sprite over this; transparent ones keep it.
                gd.Clear(XnaColor.Transparent);
                managers.Renderer.IsDrawingToTransparentTarget = scene.Background == null;
                try
                {
                    managers.Renderer.Draw(managers);
                }
                finally
                {
                    managers.Renderer.IsDrawingToTransparentTarget = false;
                }
            }
            gd.SetRenderTarget(null);

            XnaColor[] pixels = new XnaColor[Width * Height];
            target.GetData(pixels);
            return pixels;
        }
        finally
        {
            root.RemoveFromManagers();
            foreach (string file in tempFiles)
            {
                try { File.Delete(file); } catch { /* best effort */ }
            }
        }
    }

    private static GraphicalUiElement CreateContainer(SystemManagers managers, Host host, bool isRenderTarget)
    {
        if (host == Host.Editor)
        {
            // The Gum tool backs a Container with a LineRectangle (FallbackRenderableFactory).
            LineRectangle lineRectangle = new(managers);
            lineRectangle.Color = System.Drawing.Color.FromArgb(255, 255, 255, 255);
            lineRectangle.IsRenderTarget = isRenderTarget;
            return new GraphicalUiElement(lineRectangle);
        }

        ContainerRuntime container = new();
        container.IsRenderTarget = isRenderTarget;
        return container;
    }

    // A sprite with a solid straight-alpha texture, written to PNG and loaded through LoaderManager the way a
    // host loads one, so the premultiplied pipeline premultiplies it on load.
    private static SpriteRuntime SolidSprite(GraphicsDevice gd, List<string> tempFiles, Rgba color,
        int x, int y, int w, int h, int elementAlpha)
    {
        string pngPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".png");
        tempFiles.Add(pngPath);
        using (Texture2D source = new(gd, 4, 4, false, SurfaceFormat.Color))
        {
            source.SetData(Enumerable.Repeat(new XnaColor(color.R, color.G, color.B, color.A), 16).ToArray());
            using FileStream fs = File.Create(pngPath);
            source.SaveAsPng(fs, 4, 4);
        }

        SpriteRuntime sprite = new();
        sprite.Texture = LoaderManager.Self.LoadContent<Texture2D>(pngPath)!;
        sprite.WidthUnits = DimensionUnitType.Absolute;
        sprite.HeightUnits = DimensionUnitType.Absolute;
        sprite.X = x;
        sprite.Y = y;
        sprite.Width = w;
        sprite.Height = h;
        sprite.Alpha = elementAlpha;
        return sprite;
    }

    private sealed record Stats(int MaxDelta, int PixelsOver);

    private static int Delta(XnaColor x, XnaColor y, bool ignoreAlpha = false)
    {
        int alpha = ignoreAlpha ? 0 : Math.Abs(x.A - y.A);
        return Math.Max(alpha, Math.Max(Math.Abs(x.R - y.R), Math.Max(Math.Abs(x.G - y.G), Math.Abs(x.B - y.B))));
    }

    private static Stats Compare(XnaColor[] a, XnaColor[] b, bool[] mask, bool ignoreAlpha = false)
    {
        int max = 0;
        int over = 0;
        for (int i = 0; i < a.Length; i++)
        {
            if (!mask[i])
            {
                continue;
            }
            int d = Delta(a[i], b[i], ignoreAlpha);
            max = Math.Max(max, d);
            if (d > 1)
            {
                over++;
            }
        }
        return new Stats(max, over);
    }

    // Sample points: inside the first quad (or a fully covered glyph pixel), outside, and the overlap.
    private static (string Name, int X, int Y)[] SamplePoints(Scene scene, float[] coverage)
    {
        List<(string, int, int)> points = new();
        if (scene.IsText)
        {
            int first = Array.IndexOf(coverage, 1f);
            points.Add(("inside-glyph", first % Width, first / Width));
        }
        else
        {
            points.Add(("inside", 30, 30));
        }
        points.Add(("outside", 100, 20));
        if (scene.Quads.Length > 1)
        {
            points.Add(("overlap", 60, 50));
            points.Add(("second-only", 90, 70));
        }
        return points.ToArray();
    }

    private static void Report(string name, Scene scene, float[] coverage, XnaColor[] a, XnaColor[] b,
        XnaColor[] expected, bool[] mask, Stats aVsB, Stats aVsExpected, Stats bVsExpected)
    {
        string? dir = Environment.GetEnvironmentVariable("GUM_RT_PARITY_DIR");
        if (string.IsNullOrEmpty(dir))
        {
            return;
        }
        Directory.CreateDirectory(dir);

        static string Fmt(XnaColor c) => $"({c.R},{c.G},{c.B},{c.A})";

        List<string> lines = new()
        {
            $"case={name} AvsB maxd={aVsB.MaxDelta} over1={aVsB.PixelsOver}" +
            $" AvsExp maxd={aVsExpected.MaxDelta} over1={aVsExpected.PixelsOver}" +
            $" BvsExp maxd={bVsExpected.MaxDelta} over1={bVsExpected.PixelsOver} (RGBA)",
        };
        foreach ((string point, int x, int y) in SamplePoints(scene, coverage))
        {
            int i = y * Width + x;
            lines.Add($"  {point} ({x},{y}) expected={Fmt(expected[i])} A={Fmt(a[i])} B={Fmt(b[i])}" +
                      $" dA={Delta(a[i], expected[i])} dB={Delta(b[i], expected[i])}");
        }
        File.AppendAllLines(Path.Combine(dir, "results.txt"), lines);

        SavePng(Path.Combine(dir, name + "_A.png"), a);
        SavePng(Path.Combine(dir, name + "_B.png"), b);
        SavePng(Path.Combine(dir, name + "_expected.png"), expected);
        XnaColor[] diff = new XnaColor[a.Length];
        for (int i = 0; i < a.Length; i++)
        {
            // RGBA difference, amplified so a 1-2 step difference is visible.
            int d = mask[i] ? Math.Min(255, Delta(a[i], b[i]) * 32) : 0;
            diff[i] = new XnaColor((byte)d, (byte)d, (byte)d, (byte)255);
        }
        SavePng(Path.Combine(dir, name + "_diffAB.png"), diff);
    }

    // Saved as-is, alpha included, so the transparent-clear cases show what an exported PNG would hold.
    private static void SavePng(string path, XnaColor[] pixels)
    {
        using PlainLease lease = new();
        using Texture2D texture = new(lease.GraphicsDevice, Width, Height, false, SurfaceFormat.Color);
        texture.SetData(pixels);
        using FileStream fs = File.Create(path);
        texture.SaveAsPng(fs, Width, Height);
    }

    private sealed class PlainLease : SharedGameLease
    {
        public PlainLease() : base(Width, Height)
        {
        }
    }

    private sealed class MinimalGame : SharedGameLease
    {
        private readonly bool _premultiplied;

        public MinimalGame(bool premultiplied) : base(Width, Height)
        {
            _premultiplied = premultiplied;
        }

        protected override void OnInitialize()
        {
            Renderer.UsePremultipliedAlpha = _premultiplied;
            Gum.GumService.Default.Initialize(Game, Gum.Forms.DefaultVisualsVersion.V3);
        }

        protected override void OnDispose()
        {
            if (Gum.GumService.Default.IsInitialized)
            {
                Gum.GumService.Default.Uninitialize();
            }
            Renderer.UsePremultipliedAlpha = false;
        }
    }
}
