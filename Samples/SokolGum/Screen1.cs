using System.Numerics;
using RenderingLibrary.Content;
using RenderingLibrary.Graphics;
using SokolGum;
using Gum.DataTypes;
using Gum.Graphics.Animation;
using Gum.GueDeriving;
using Gum.Renderables;

namespace SokolGumSample;

/// <summary>
/// Renderable-exercise screen — exercises every renderable: ColoredRectangle,
/// Sprite (procedural + PNG), NineSlice, Text at multiple sizes, plus the
/// line primitives. Also demonstrates per-renderable blend mode switching
/// and z-ordering.
///
/// The line primitives (Line, LineRectangle, LineCircle, LinePolygon,
/// LineGrid) are raw IRenderableIpso renderables without GraphicalUiElement
/// wrappers, so they can't be Children of a ContainerRuntime. They live on
/// the Layer directly and are tracked here so SetVisible can toggle them
/// alongside the GUE subtree.
///
/// Owns its own content — procedural textures, PNG, font, and animations
/// are built/loaded in the constructor and released via <see cref="Dispose"/>.
/// </summary>
public class Screen1 : ContainerRuntime, IDisposable
{
    private readonly List<IRenderableIpso> _extraRenderables = new();
    private readonly Layer _layer;

    private readonly Texture2D _gradientTexture;
    private readonly Texture2D _nineSliceTexture;
    private readonly Texture2D? _logoTexture;

    public Screen1(Layer layer)
    {
        _layer = layer;

        _gradientTexture = BuildGradientTexture(128, 128);
        _nineSliceTexture = BuildNineSliceTestTexture();
        _logoTexture = LoaderManager.Self.ContentLoader.LoadContent<Texture2D>("Assets/sokol_logo.png");
        var font = LoaderManager.Self.ContentLoader.LoadContent<Font>("Assets/DroidSerif-Regular.ttf");
        var characterAnimations = LoaderManager.Self.ContentLoader.LoadContent<AnimationChainList>("Assets/CharacterAnimations.achx");

        Width = 0; WidthUnits = DimensionUnitType.RelativeToParent;
        Height = 0; HeightUnits = DimensionUnitType.RelativeToParent;
        Name = "Screen1";

        // Row 1 — ColoredRectangles (SolidRectangle via sgp_draw_filled_rect).
        // The third is rotated 15° counter-clockwise to exercise the rotation
        // transform applied centrally in Renderer.DrawGumRecursively.
        Children.Add(new ColoredRectangleRuntime { X = 40,  Y = 40, Width = 200, Height = 100, Color = new Color(255, 90, 140) });
        Children.Add(new ColoredRectangleRuntime { X = 260, Y = 40, Width = 200, Height = 100, Color = new Color(60, 200, 200) });
        Children.Add(new ColoredRectangleRuntime { X = 480, Y = 40, Width = 200, Height = 100, Color = new Color(240, 190, 70), Rotation = 15f });

        // Row 2 — Sprites from the procedural gradient texture (sgp_draw_textured_rect).
        Children.Add(new SpriteRuntime { X = 40,  Y = 160, Width = 200, Height = 100, Texture = _gradientTexture });
        Children.Add(new SpriteRuntime { X = 260, Y = 160, Width = 200, Height = 100, Texture = _gradientTexture, Color = new Color(255, 180, 180) });
        Children.Add(new SpriteRuntime { X = 480, Y = 160, Width = 200, Height = 100, Texture = _gradientTexture, FlipHorizontal = true, FlipVertical = true });

        // Row 3 — Sprite from a loaded PNG (stb_image via ContentLoader).
        if (_logoTexture is not null)
        {
            Children.Add(new SpriteRuntime { X = 40,  Y = 280, Width = _logoTexture.Width,     Height = _logoTexture.Height,     Texture = _logoTexture });
            Children.Add(new SpriteRuntime { X = 160, Y = 280, Width = _logoTexture.Width * 2, Height = _logoTexture.Height * 2, Texture = _logoTexture });
            Children.Add(new SpriteRuntime { X = 400, Y = 280, Width = _logoTexture.Width,     Height = _logoTexture.Height,     Texture = _logoTexture, Color = new Color(180, 255, 180) });
        }

        // Row 4 — NineSlice at three sizes. The test texture has distinctly
        // colored corners/edges/center so any mis-mapped region is obvious.
        // The third uses CustomFrameTextureCoordinateWidth to pull the border
        // in from the default source/3, so its corners look half as thick.
        Children.Add(new NineSliceRuntime { X = 40,  Y = 400, Width = 80,  Height = 60,  Texture = _nineSliceTexture });
        Children.Add(new NineSliceRuntime { X = 140, Y = 400, Width = 200, Height = 120, Texture = _nineSliceTexture });
        Children.Add(new NineSliceRuntime
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
        if (characterAnimations is not null)
        {
            // SpriteAnimationLogic.Animate defaults to false (matches
            // RaylibGum/Skia convention); callers opt in explicitly.
            Children.Add(new SpriteRuntime
            {
                X = 570, Y = 295, Width = 48, Height = 96,
                AnimationChains = characterAnimations,
                CurrentChainName = "IdleLeft",
                Animate = true,
            });
            Children.Add(new SpriteRuntime
            {
                X = 640, Y = 295, Width = 48, Height = 96,
                AnimationChains = characterAnimations,
                CurrentChainName = "IdleRight",
                AnimationSpeed = 2f,
                Animate = true,
            });
            Children.Add(new TextRuntime
            {
                X = 550, Y = 268, Width = 170, Height = 18,
                CustomFont = font, FontSize = 12,
                Text = ".achx  1×  ·  2×",
                Color = new Color(255, 200, 140),
            });
        }

        // Row 5 — Line primitives: solid + dotted LineRectangle, LineCircle,
        // star LinePolygon, single Line, LineGrid. Each at 100px stride.
        // These are raw IRenderableIpso renderables (no GraphicalUiElement
        // wrapper), so they're added to the Layer directly and tracked in
        // _extraRenderables for SetVisible to toggle.
        AddExtra(new LineRectangle
        {
            X = 40, Y = 560, Width = 90, Height = 90,
            Color = new Color(220, 220, 220), IsDotted = false,
        });
        AddExtra(new LineRectangle
        {
            X = 140, Y = 560, Width = 90, Height = 90,
            Color = new Color(255, 200, 120), IsDotted = true, DashLength = 4f,
        });
        AddExtra(new LineCircle
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
        AddExtra(star);
        AddExtra(new Line
        {
            X = 440, Y = 560, RelativePoint = new Vector2(90, 90),
            Color = new Color(180, 255, 180),
        });
        AddExtra(new LineGrid
        {
            X = 540, Y = 560,
            ColumnWidth = 15, RowWidth = 15,
            ColumnCount = 6, RowCount = 6,
            Color = new Color(100, 110, 160),
        });

        // Row 6 — ClipsChildren + nested rotation combo demo. A container
        // rotated 10° holds a second container that clips its children
        // and is itself rotated 15°. Inside that sits an oversized rect
        // whose right half visually disappears at the clip boundary while
        // the combined rotations take it into double-tilted space. Proves
        // three code paths at once: scissor stacking, parent→child
        // rotation composition, and sub-pixel scissor rounding.
        var outer = new ContainerRuntime
        {
            X = 40, Y = 670, Width = 180, Height = 40,
            Rotation = 10f,
        };
        var clipper = new ContainerRuntime
        {
            X = 0, Y = 0, Width = 180, Height = 40,
            Rotation = 15f,
            ClipsChildren = true,
        };
        // Over-wide rect inside the clipper: only the first 180px of its
        // 260px width stays visible after scissoring.
        clipper.Children.Add(new ColoredRectangleRuntime
        {
            X = 0, Y = 0, Width = 260, Height = 40,
            Color = new Color(120, 200, 160),
        });
        outer.Children.Add(clipper);
        Children.Add(outer);

        Children.Add(new TextRuntime
        {
            X = 230, Y = 675, Width = 260, Height = 20,
            CustomFont = font, FontSize = 11,
            Text = "ClipsChildren + nested rotation",
            Color = new Color(160, 200, 255),
        });

        // Right column — Text labels at multiple sizes/colors (Text via
        // fontstash render callbacks that emit sgp draws — batches alongside
        // the rest in scene-graph order, honoring Z).
        if (font is not null)
        {
            Children.Add(new TextRuntime { X = 720, Y = 40,  Width = 520, Height = 40, CustomFont = font, FontSize = 32, Text = "SokolGum Sample",                  Color = new Color(120, 220, 255) });
            Children.Add(new TextRuntime { X = 720, Y = 90,  Width = 520, Height = 24, CustomFont = font, FontSize = 16, Text = "Gum UI rendered via sokol_gp + sokol_gfx", Color = new Color(180, 180, 180) });

            Children.Add(new TextRuntime { X = 720, Y = 160, Width = 520, Height = 24, CustomFont = font, FontSize = 18, Text = "ColoredRectangles",            Color = new Color(255, 140, 180) });
            Children.Add(new TextRuntime { X = 720, Y = 190, Width = 520, Height = 20, CustomFont = font, FontSize = 12, Text = "SolidRectangle via sgp_draw_filled_rect", Color = new Color(160, 160, 160) });

            Children.Add(new TextRuntime { X = 720, Y = 280, Width = 520, Height = 24, CustomFont = font, FontSize = 18, Text = "Sprites (procedural + PNG)",  Color = new Color(180, 220, 180) });
            Children.Add(new TextRuntime { X = 720, Y = 400, Width = 520, Height = 24, CustomFont = font, FontSize = 18, Text = "NineSlices",                   Color = new Color(220, 180, 140) });
            Children.Add(new TextRuntime { X = 720, Y = 560, Width = 520, Height = 24, CustomFont = font, FontSize = 18, Text = "Line primitives",              Color = new Color(255, 220, 180) });

            // Outline-thickness progression — same word "Gum" drawn four times
            // at thicknesses 0 (none) through 3. The 8-direction offset
            // technique stamps the word that many times around each center
            // in OutlineColor, then draws the main word on top. Halo grows
            // linearly with thickness; cost is (8 * thickness + 1) draws per
            // line. Over a dim panel so the outline is visible against the
            // main text colour.
            Children.Add(new ColoredRectangleRuntime
            {
                X = 720, Y = 220, Width = 520, Height = 50,
                Color = new Color(30, 30, 40, 220),
            });
            for (int t = 0; t <= 3; t++)
            {
                int columnX = 720 + t * 130;
                Children.Add(new TextRuntime
                {
                    X = columnX, Y = 225, Width = 130, Height = 32,
                    CustomFont = font, FontSize = 24,
                    Text = "Gum",
                    // Black text + white outline on the dark panel: each
                    // successive column sprouts a more visible white halo
                    // around otherwise-camouflaged black glyphs, making
                    // the 8-direction stamp pattern obvious.
                    Color = new Color(0, 0, 0),
                    OutlineColor = new Color(255, 255, 255),
                    OutlineThickness = t,
                    HorizontalAlignment = RenderingLibrary.Graphics.HorizontalAlignment.Center,
                });
                Children.Add(new TextRuntime
                {
                    X = columnX, Y = 255, Width = 130, Height = 14,
                    CustomFont = font, FontSize = 10,
                    Text = $"outline {t}px",
                    Color = new Color(180, 180, 200),
                    HorizontalAlignment = RenderingLibrary.Graphics.HorizontalAlignment.Center,
                });
            }

            // Wrapped + centred + outlined text. Exercises word-wrapping to
            // Width, per-line HorizontalAlignment.Center, and the 8-direction
            // OutlineThickness stroke. Drawn inside a dim panel to make the
            // outline clearly visible against a busy background.
            Children.Add(new ColoredRectangleRuntime
            {
                X = 720, Y = 440, Width = 520, Height = 100,
                Color = new Color(40, 40, 50, 200),
            });
            Children.Add(new TextRuntime
            {
                X = 720, Y = 440, Width = 520, Height = 100,
                CustomFont = font, FontSize = 18,
                Text = "Multi-line wrapped text, centred horizontally and vertically, with a 2-pixel outline.",
                Color = new Color(255, 230, 160),
                OutlineColor = new Color(20, 20, 20),
                OutlineThickness = 2,
                HorizontalAlignment = RenderingLibrary.Graphics.HorizontalAlignment.Center,
                VerticalAlignment = RenderingLibrary.Graphics.VerticalAlignment.Center,
            });

            // Z-order demonstration. Text at Z=0 behind a semi-transparent
            // rect at Z=1 — proves text batches into sgp rather than going
            // through a separate pipeline that always draws on top.
            Children.Add(new TextRuntime
            {
                X = 720, Y = 640, Z = 0, Width = 520, Height = 28,
                CustomFont = font, FontSize = 20,
                Text = "Z-order: text at Z=0, rect at Z=1 over right half",
                Color = new Color(240, 240, 240),
            });
            Children.Add(new ColoredRectangleRuntime
            {
                X = 960, Y = 635, Z = 1, Width = 280, Height = 40,
                Color = new Color(200, 60, 60, 180),
            });
        }
    }

    /// <summary>
    /// Adds the screen (and its raw line renderables) to the scene. The GUE
    /// subtree goes under GumService.Default.Root; the raw renderables go
    /// directly on the Layer because they have no GraphicalUiElement wrapper.
    /// </summary>
    public void AddToRoot()
    {
        GumService.Default.Root.Children.Add(this);
        foreach (var r in _extraRenderables)
        {
            _layer.Add(r);
        }
    }

    /// <summary>
    /// Reverses <see cref="AddToRoot"/> — detaches from Root and removes the
    /// raw renderables from the Layer.
    /// </summary>
    public void RemoveFromRoot()
    {
        Parent = null;
        foreach (var r in _extraRenderables)
        {
            _layer.Remove(r);
        }
    }

    private void AddExtra<T>(T renderable) where T : IRenderableIpso
    {
        _extraRenderables.Add(renderable);
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

    /// <summary>
    /// Disposes textures owned by this screen. Font and AnimationChainList
    /// are cached inside the ContentLoader and disposed with the
    /// SystemManagers, so they're not released here.
    /// </summary>
    public void Dispose()
    {
        _gradientTexture?.Dispose();
        _nineSliceTexture?.Dispose();
        _logoTexture?.Dispose();
    }
}
