using System.Numerics;
using System.Runtime.InteropServices;
using RenderingLibrary.Content;
using SokolGum;
using SokolGum.Animation;
using SokolGum.GueDeriving;
using SokolGum.Renderables;
using static Sokol.SApp;
using static Sokol.SG;
using static Sokol.SGP;
using static Sokol.SGlue;
using static Sokol.SLog;

namespace SokolGumSample;

/// <summary>
/// Minimal code-only Gum sample running on the SokolGum backend. Mirrors the
/// shape of Samples/raylib — no .gumx project, just instantiate runtime
/// wrappers and add them to the main layer.
///
/// Exercises every renderable: ColoredRectangle, Sprite (procedural + PNG),
/// NineSlice, Text at multiple sizes, plus the line primitives (LineRectangle
/// solid/dotted, LineCircle, LinePolygon). Also demonstrates per-renderable
/// blend mode switching and z-ordering.
/// </summary>
public static unsafe class Program
{
    private static sg_pass_action _passAction;
    private static SystemManagers? _systemManagers;
    private static Texture2D? _gradientTexture;
    private static Texture2D? _logoTexture;
    private static Texture2D? _nineSliceTexture;
    private static Font? _font;
    private static AnimationChainList? _characterAnimations;

    public static void Main()
    {
        // When launched outside a debugger, the working directory is the
        // binary's location — so Content relative paths resolve correctly.
        if (!System.Diagnostics.Debugger.IsAttached)
        {
            var appPath = Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule!.FileName)!;
            Directory.SetCurrentDirectory(appPath);
        }

        sapp_run(new sapp_desc
        {
            init_cb = &Init,
            frame_cb = &Frame,
            cleanup_cb = &Cleanup,
            width = 1280,
            height = 720,
            sample_count = 4,
            window_title = "SokolGum Sample — Gum UI via sokol_gp",
            icon = { sokol_default = true },
            logger = { func = &slog_func },
        });
    }

    [UnmanagedCallersOnly]
    private static void Init()
    {
        sg_setup(new sg_desc
        {
            environment = sglue_environment(),
            logger = { func = &slog_func },
        });
        sgp_setup(new sgp_desc());

        _passAction = default;
        _passAction.colors[0].load_action = sg_load_action.SG_LOADACTION_CLEAR;
        _passAction.colors[0].clear_value = new sg_color { r = 0.10f, g = 0.12f, b = 0.15f, a = 1.0f };

        _systemManagers = new SystemManagers();
        SystemManagers.Default = _systemManagers;
        _systemManagers.Initialize();

        _gradientTexture = BuildGradientTexture(128, 128);
        _nineSliceTexture = BuildNineSliceTestTexture();
        _logoTexture = LoaderManager.Self.ContentLoader.LoadContent<Texture2D>("Assets/sokol_logo.png");
        _font = LoaderManager.Self.ContentLoader.LoadContent<Font>("Assets/DroidSerif-Regular.ttf");
        _characterAnimations = LoaderManager.Self.ContentLoader.LoadContent<AnimationChainList>("Assets/CharacterAnimations.achx");

        var root = _systemManagers.Renderer.MainLayer;

        // Row 1 — ColoredRectangles (SolidRectangle via sgp_draw_filled_rect).
        // The third is rotated 15° counter-clockwise to exercise the rotation
        // transform applied centrally in Renderer.DrawGumRecursively.
        root.Add(new ColoredRectangleRuntime { X = 40,  Y = 40, Width = 200, Height = 100, Color = new Color(255, 90, 140) });
        root.Add(new ColoredRectangleRuntime { X = 260, Y = 40, Width = 200, Height = 100, Color = new Color(60, 200, 200) });
        root.Add(new ColoredRectangleRuntime { X = 480, Y = 40, Width = 200, Height = 100, Color = new Color(240, 190, 70), Rotation = 15f });

        // Row 2 — Sprites from the procedural gradient texture (sgp_draw_textured_rect).
        root.Add(new SpriteRuntime { X = 40,  Y = 160, Width = 200, Height = 100, Texture = _gradientTexture });
        root.Add(new SpriteRuntime { X = 260, Y = 160, Width = 200, Height = 100, Texture = _gradientTexture, Tint = new Color(255, 180, 180) });
        root.Add(new SpriteRuntime { X = 480, Y = 160, Width = 200, Height = 100, Texture = _gradientTexture, FlipHorizontal = true, FlipVertical = true });

        // Row 3 — Sprite from a loaded PNG (stb_image via ContentLoader).
        if (_logoTexture is not null)
        {
            root.Add(new SpriteRuntime { X = 40,  Y = 280, Width = _logoTexture.Width,     Height = _logoTexture.Height,     Texture = _logoTexture });
            root.Add(new SpriteRuntime { X = 160, Y = 280, Width = _logoTexture.Width * 2, Height = _logoTexture.Height * 2, Texture = _logoTexture });
            root.Add(new SpriteRuntime { X = 400, Y = 280, Width = _logoTexture.Width,     Height = _logoTexture.Height,     Texture = _logoTexture, Tint = new Color(180, 255, 180) });
        }

        // Row 4 — NineSlice at three sizes. The test texture has distinctly
        // colored corners/edges/center so any mis-mapped region is obvious.
        // The third uses CustomFrameTextureCoordinateWidth to pull the border
        // in from the default source/3, so its corners look half as thick.
        root.Add(new NineSliceRuntime { X = 40,  Y = 400, Width = 80,  Height = 60,  Texture = _nineSliceTexture });
        root.Add(new NineSliceRuntime { X = 140, Y = 400, Width = 200, Height = 120, Texture = _nineSliceTexture });
        root.Add(new NineSliceRuntime
        {
            X = 360, Y = 400, Width = 320, Height = 140,
            Texture = _nineSliceTexture,
            CustomFrameTextureCoordinateWidth = 8f,
        });

        // Animated sprites — .achx chain list plays CharacterSheet.png frames
        // (16×32 per frame, 0.1s each, looped). Sized 3× into the unused
        // gap between the PNG-sprite row (ends near x=528) and the right
        // text column (starts at x=720). Renderer.Update ticks every
        // animated Sprite each frame.
        if (_characterAnimations is not null)
        {
            root.Add(new SpriteRuntime
            {
                X = 570, Y = 295, Width = 48, Height = 96,
                AnimationChains = _characterAnimations,
                CurrentChainName = "IdleLeft",
            });
            root.Add(new SpriteRuntime
            {
                X = 640, Y = 295, Width = 48, Height = 96,
                AnimationChains = _characterAnimations,
                CurrentChainName = "IdleRight",
                AnimationSpeed = 2f,
            });
            root.Add(new TextRuntime
            {
                X = 550, Y = 268, Width = 170, Height = 18,
                Font = _font, FontSize = 12f,
                RawText = ".achx  1×  ·  2×",
                Color = new Color(255, 200, 140),
            });
        }

        // Row 5 — Line primitives: solid + dotted LineRectangle, LineCircle,
        // star LinePolygon, single Line, LineGrid. Each at 100px stride.
        root.Add(new LineRectangle
        {
            X = 40, Y = 560, Width = 90, Height = 90,
            Color = new Color(220, 220, 220), IsDotted = false,
        });
        root.Add(new LineRectangle
        {
            X = 140, Y = 560, Width = 90, Height = 90,
            Color = new Color(255, 200, 120), IsDotted = true, DashLength = 4f,
        });
        root.Add(new LineCircle
        {
            X = 240, Y = 560, Radius = 45,
            CircleOrigin = CircleOrigin.TopLeft,
            Color = new Color(120, 220, 255),
        });
        var star = new LinePolygon
        {
            X = 340, Y = 560,
            Color = new Color(255, 160, 200),
        };
        star.SetPoints(BuildStarPoints(45, 20, 5));
        root.Add(star);
        root.Add(new Line
        {
            X = 440, Y = 560, RelativePoint = new Vector2(90, 90),
            Color = new Color(180, 255, 180),
        });
        root.Add(new LineGrid
        {
            X = 540, Y = 560,
            ColumnWidth = 15, RowWidth = 15,
            ColumnCount = 6, RowCount = 6,
            Color = new Color(100, 110, 160),
        });

        // Right column — Text labels at multiple sizes/colors (Text via
        // fontstash render callbacks that emit sgp draws — batches alongside
        // the rest in scene-graph order, honoring Z).
        if (_font is not null)
        {
            root.Add(new TextRuntime { X = 720, Y = 40,  Width = 520, Height = 40, Font = _font, FontSize = 32f, RawText = "SokolGum Sample",                  Color = new Color(120, 220, 255) });
            root.Add(new TextRuntime { X = 720, Y = 90,  Width = 520, Height = 24, Font = _font, FontSize = 16f, RawText = "Gum UI rendered via sokol_gp + sokol_gfx", Color = new Color(180, 180, 180) });

            root.Add(new TextRuntime { X = 720, Y = 160, Width = 520, Height = 24, Font = _font, FontSize = 18f, RawText = "ColoredRectangles",            Color = new Color(255, 140, 180) });
            root.Add(new TextRuntime { X = 720, Y = 190, Width = 520, Height = 20, Font = _font, FontSize = 12f, RawText = "SolidRectangle via sgp_draw_filled_rect", Color = new Color(160, 160, 160) });

            root.Add(new TextRuntime { X = 720, Y = 280, Width = 520, Height = 24, Font = _font, FontSize = 18f, RawText = "Sprites (procedural + PNG)",  Color = new Color(180, 220, 180) });
            root.Add(new TextRuntime { X = 720, Y = 400, Width = 520, Height = 24, Font = _font, FontSize = 18f, RawText = "NineSlices",                   Color = new Color(220, 180, 140) });
            root.Add(new TextRuntime { X = 720, Y = 560, Width = 520, Height = 24, Font = _font, FontSize = 18f, RawText = "Line primitives",              Color = new Color(255, 220, 180) });

            // Wrapped + centred + outlined text. Exercises word-wrapping to
            // Width, per-line HorizontalAlignment.Center, and the 8-direction
            // OutlineThickness stroke. Drawn inside a dim panel to make the
            // outline clearly visible against a busy background.
            root.Add(new ColoredRectangleRuntime
            {
                X = 720, Y = 440, Width = 520, Height = 100,
                Color = new Color(40, 40, 50, 200),
            });
            root.Add(new TextRuntime
            {
                X = 720, Y = 440, Width = 520, Height = 100,
                Font = _font, FontSize = 18f,
                RawText = "Multi-line wrapped text, centred horizontally and vertically, with a 2-pixel outline.",
                Color = new Color(255, 230, 160),
                OutlineThickness = 2,
                OutlineColor = new Color(20, 20, 20),
                HorizontalAlignment = RenderingLibrary.Graphics.HorizontalAlignment.Center,
                VerticalAlignment = RenderingLibrary.Graphics.VerticalAlignment.Center,
                WrapTextInsideBlock = true,
            });

            // Z-order demonstration. Text at Z=0 behind a semi-transparent
            // rect at Z=1 — proves text batches into sgp rather than going
            // through a separate pipeline that always draws on top.
            root.Add(new TextRuntime
            {
                X = 720, Y = 640, Z = 0, Width = 520, Height = 28,
                Font = _font, FontSize = 20f,
                RawText = "Z-order: text at Z=0, rect at Z=1 over right half",
                Color = new Color(240, 240, 240),
            });
            root.Add(new ColoredRectangleRuntime
            {
                X = 960, Y = 635, Z = 1, Width = 280, Height = 40,
                Color = new Color(200, 60, 60, 180),
            });
        }
    }

    /// <summary>
    /// Build an N-point star as a closed polyline — alternating outer and
    /// inner vertices around a common centre. Coordinates are local to the
    /// LinePolygon; the polygon's (X, Y) is added at draw time.
    /// </summary>
    private static IEnumerable<Vector2> BuildStarPoints(float outerRadius, float innerRadius, int points)
    {
        var cx = outerRadius;
        var cy = outerRadius;
        for (int i = 0; i <= points * 2; i++)
        {
            var r = (i % 2 == 0) ? outerRadius : innerRadius;
            var angle = -MathF.PI / 2 + i * MathF.PI / points;
            yield return new Vector2(cx + MathF.Cos(angle) * r, cy + MathF.Sin(angle) * r);
        }
    }

    /// <summary>
    /// Build a diagonal gradient texture at runtime so the sprite test doesn't
    /// depend on asset files. Red varies along X, green along Y, blue is half.
    /// </summary>
    private static Texture2D BuildGradientTexture(int width, int height)
    {
        var pixels = new byte[width * height * 4];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int i = (y * width + x) * 4;
                pixels[i + 0] = (byte)(x * 255 / (width - 1));
                pixels[i + 1] = (byte)(y * 255 / (height - 1));
                pixels[i + 2] = 128;
                pixels[i + 3] = 255;
            }
        }
        return Texture2D.FromRgba8(pixels, width, height, "gradient");
    }

    /// <summary>
    /// 48×48 nine-slice test texture with distinctly colored regions:
    /// corners red, top/bottom orange (stretch horizontally), left/right
    /// green (stretch vertically), centre blue (stretch both ways).
    /// </summary>
    private static Texture2D BuildNineSliceTestTexture()
    {
        const int size = 48;
        const int border = 16;
        var pixels = new byte[size * size * 4];
        Span<byte> corner         = stackalloc byte[] { 220, 80, 80, 255 };
        Span<byte> horizontalEdge = stackalloc byte[] { 255, 180, 80, 255 };
        Span<byte> verticalEdge   = stackalloc byte[] { 80, 200, 100, 255 };
        Span<byte> center         = stackalloc byte[] { 80, 130, 255, 255 };

        for (int y = 0; y < size; y++)
        {
            bool inTop = y < border;
            bool inBottom = y >= size - border;
            bool inMidV = !inTop && !inBottom;
            for (int x = 0; x < size; x++)
            {
                bool inLeft = x < border;
                bool inRight = x >= size - border;
                bool inMidH = !inLeft && !inRight;

                Span<byte> c =
                    (inTop || inBottom) && (inLeft || inRight) ? corner
                    : (inTop || inBottom) && inMidH ? horizontalEdge
                    : inMidV && (inLeft || inRight) ? verticalEdge
                    : center;

                int i = (y * size + x) * 4;
                pixels[i + 0] = c[0];
                pixels[i + 1] = c[1];
                pixels[i + 2] = c[2];
                pixels[i + 3] = c[3];
            }
        }
        return Texture2D.FromRgba8(pixels, size, size, "nineslice-test");
    }

    [UnmanagedCallersOnly]
    private static void Frame()
    {
        var width = sapp_width();
        var height = sapp_height();
        var dt = sapp_frame_duration();

        sg_begin_pass(new sg_pass { action = _passAction, swapchain = sglue_swapchain() });

        _systemManagers!.Renderer.Update(dt);
        _systemManagers.Renderer.BeginFrame(width, height);
        _systemManagers.Renderer.Draw(_systemManagers);
        _systemManagers.Renderer.EndFrame();

        sg_end_pass();
        sg_commit();
    }

    [UnmanagedCallersOnly]
    private static void Cleanup()
    {
        _font?.Dispose();
        _gradientTexture?.Dispose();
        _nineSliceTexture?.Dispose();
        _logoTexture?.Dispose();
        _systemManagers?.Dispose();
        sgp_shutdown();
        sg_shutdown();
    }
}
