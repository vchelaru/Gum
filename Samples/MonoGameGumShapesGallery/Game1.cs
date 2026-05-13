using System;
using Apos.Shapes;
using Gum.GueDeriving;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameAndGum.Renderables;

namespace MonoGameGumShapesGallery;

// Visual smoke test for Gum.Shapes.MonoGame. Three stacked sections:
//
//   1. Shape gallery — every primitive Apos.Shapes exposes (Circle, Rectangle, RoundedRect
//      uniform, Line, Hexagon, Triangle) across every visual variant (Filled, Border,
//      Fill+Border, Linear gradient, Radial gradient). Same layout as Apostolique's reference
//      sample, ported to use the in-repo MonoGameGumShapes wiring (ShapeRenderer.Self) so the
//      shipped XNB is what's being exercised.
//
//   2. Per-corner radii showcase — five RoundedRectangle configurations using the new
//      CornerRadii overload added in Apos.Shapes 0.6.9 (PR #32). The "Leaf" cell matches the
//      Forest Glade theme's signature silhouette (TL=2, TR=12, BR=2, BL=12); the rest cover
//      tabs, diagonals, asymmetric corners. The leaf cell is rendered through Gum's
//      RoundedRectangle renderable (not the raw Apos call) so any wrapper regression jumps out
//      visually next to the raw cells.
//
//   3. ArcRuntime showcase — five ArcRuntime instances exercising the unified runtime from
//      issue #2728. First cell uses the bare default constructor (verifies the locked-in
//      defaults: flat caps, 90 sweep, thickness 10, white). The rest vary IsEndRounded,
//      SweepAngle, Thickness, and gradient to surface any regression in the Apos-side
//      rendering path.
public class Game1 : Game
{
    private readonly GraphicsDeviceManager _graphics;
    private ShapeBatch _shapeBatch = default!;
    private RoundedRectangle _gumWrapperLeaf = default!;
    private ArcRuntime[] _arcs = default!;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        if (GraphicsAdapter.DefaultAdapter.IsProfileSupported(GraphicsProfile.HiDef))
        {
            _graphics.GraphicsProfile = GraphicsProfile.HiDef;
        }
        _graphics.PreferredBackBufferWidth = 1280;
        _graphics.PreferredBackBufferHeight = 900;
        IsMouseVisible = true;
        Window.AllowUserResizing = true;
        Window.Title = "Gum Shapes Gallery — Apos.Shapes 0.6.9 + per-corner radii + ArcRuntime";
        // ContentManager's default RootDirectory is empty; the pre-built apos-shapes.xnb
        // ships under Content/ in the build output (via Link= on the Content item in the
        // csproj), so point Content there before any Load<T>("apos-shapes") call hits.
        Content.RootDirectory = "Content";
    }

    protected override void LoadContent()
    {
        // Initialize ShapeRenderer.Self so the same singleton ShapeBatch is shared between
        // raw shapeBatch calls in this sample and Gum's RoundedRectangleRuntime.Render path.
        // This is what consumers of MonoGameGumShapes hit in production.
        MonoGameAndGum.Renderables.ShapeRenderer.Self.Initialize(GraphicsDevice, Content);
        _shapeBatch = MonoGameAndGum.Renderables.ShapeRenderer.Self.ShapeBatch;

        // Build a single RoundedRectangle (the Gum-side renderable, the one the
        // RoundedRectangleRuntime wraps) with the leaf-shape per-corner radii. Drawn in the
        // leaf column of Section 2 so its raw-API neighbors expose any wrapper regression
        // visually.
        _gumWrapperLeaf = new RoundedRectangle
        {
            Width = 140,
            Height = 36,
            CornerRadius = 0,
            CustomRadiusTopLeft = 2,
            CustomRadiusTopRight = 12,
            CustomRadiusBottomRight = 2,
            CustomRadiusBottomLeft = 12,
            IsFilled = true,
            Color = new Color(112, 220, 80),
        };

        _arcs = BuildArcShowcase();
    }

    private static ArcRuntime[] BuildArcShowcase()
    {
        // Bare default — verifies the locked defaults from issue #2728:
        //   IsEndRounded = false (flat caps, the Apos-breaking change)
        //   SweepAngle = 90
        //   Thickness = 10
        //   Color = white
        ArcRuntime def = new ArcRuntime();

        // Same as default but with rounded caps - the toggle Apos consumers must set
        // explicitly post-unification if they relied on the previous true default.
        ArcRuntime rounded = new ArcRuntime
        {
            IsEndRounded = true,
        };

        // Full ring - exercises the SweepAngle=360 path (DrawRing on Apos for !rounded).
        ArcRuntime fullRing = new ArcRuntime
        {
            SweepAngle = 360,
            Thickness = 8,
        };

        // Thick rounded half-circle - bigger stroke + rounded caps stress the radius math.
        ArcRuntime thickHalf = new ArcRuntime
        {
            IsEndRounded = true,
            SweepAngle = 180,
            Thickness = 18,
        };

        // Gradient arc - exercises the UseGradient branch in Arc.Render.
        ArcRuntime gradient = new ArcRuntime
        {
            IsEndRounded = true,
            SweepAngle = 270,
            Thickness = 14,
            UseGradient = true,
        };
        // Gradient ctor seeds Red1/Green1/Blue1/Red2 = 255, Green2 = 255, Blue2 = 0 (yellow-tint
        // gradient stop). Leave those at the seeded values; only override what differs.

        return new[] { def, rounded, fullRing, thickHalf, gradient };
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(new Color(30, 30, 30));

        int w = GraphicsDevice.Viewport.Width;
        int h = GraphicsDevice.Viewport.Height;

        // Top 65% for the primitive gallery, bottom 35% split between per-corner radii and arcs.
        int galleryHeight = (int)(h * 0.65f);
        int lowerSectionTop = galleryHeight;
        int lowerSectionHeight = h - galleryHeight;
        int cornerHeight = lowerSectionHeight / 2;
        int arcTop = lowerSectionTop + cornerHeight;
        int arcHeight = lowerSectionHeight - cornerHeight;

        _shapeBatch.Begin();

        DrawGalleryGrid(w, galleryHeight);
        DrawPerCornerShowcase(0, lowerSectionTop, w, cornerHeight);
        DrawArcShowcase(0, arcTop, w, arcHeight);

        _shapeBatch.End();
        base.Draw(gameTime);
    }

    private void DrawGalleryGrid(int width, int height)
    {
        const int cols = 5; // Filled, Border, Fill+Border, Linear gradient, Radial gradient
        const int rows = 6; // Circle, Rectangle, Rounded uniform, Line, Hexagon, Triangle

        float cellW = width / (float)cols;
        float cellH = height / (float)rows;
        float shapeSize = MathF.Min(cellW, cellH) * 0.42f;

        for (int row = 0; row < rows; row++)
        {
            float cy = row * cellH + cellH / 2f;
            for (int col = 0; col < cols; col++)
            {
                float cx = col * cellW + cellW / 2f;
                DrawGalleryCell(row, col, new Vector2(cx, cy), shapeSize);
            }
        }
    }

    private void DrawGalleryCell(int row, int col, Vector2 center, float shapeSize)
    {
        Color fill = new Color(80, 180, 220);
        Color border = new Color(220, 160, 60);
        Color fillBorder = new Color(60, 180, 130);
        Gradient linearGrad = new Gradient(
            center - new Vector2(shapeSize, 0), new Color(220, 60, 80),
            center + new Vector2(shapeSize, 0), new Color(60, 140, 220),
            Gradient.Shape.Linear);
        Gradient radialGrad = new Gradient(
            center, new Color(255, 220, 60),
            center + new Vector2(shapeSize, 0), new Color(140, 40, 200),
            Gradient.Shape.Radial);
        const float thickness = 2f;

        switch (row)
        {
            case 0: // Circle
                switch (col)
                {
                    case 0: _shapeBatch.FillCircle(center, shapeSize, fill); break;
                    case 1: _shapeBatch.BorderCircle(center, shapeSize, border, thickness); break;
                    case 2: _shapeBatch.DrawCircle(center, shapeSize, fillBorder, border, thickness); break;
                    case 3: _shapeBatch.FillCircle(center, shapeSize, linearGrad); break;
                    case 4: _shapeBatch.FillCircle(center, shapeSize, radialGrad); break;
                }
                break;
            case 1: // Rectangle
                {
                    Vector2 size = new Vector2(shapeSize * 2f, shapeSize * 1.3f);
                    Vector2 xy = center - size / 2f;
                    switch (col)
                    {
                        case 0: _shapeBatch.FillRectangle(xy, size, fill); break;
                        case 1: _shapeBatch.BorderRectangle(xy, size, border, thickness); break;
                        case 2: _shapeBatch.DrawRectangle(xy, size, fillBorder, border, thickness); break;
                        case 3: _shapeBatch.FillRectangle(xy, size, linearGrad); break;
                        case 4: _shapeBatch.FillRectangle(xy, size, radialGrad); break;
                    }
                }
                break;
            case 2: // Rounded uniform
                {
                    Vector2 size = new Vector2(shapeSize * 2f, shapeSize * 1.3f);
                    Vector2 xy = center - size / 2f;
                    float rounded = shapeSize * 0.35f;
                    switch (col)
                    {
                        case 0: _shapeBatch.FillRectangle(xy, size, fill, rounded); break;
                        case 1: _shapeBatch.BorderRectangle(xy, size, border, thickness, rounded); break;
                        case 2: _shapeBatch.DrawRectangle(xy, size, fillBorder, border, thickness, rounded); break;
                        case 3: _shapeBatch.FillRectangle(xy, size, linearGrad, rounded); break;
                        case 4: _shapeBatch.FillRectangle(xy, size, radialGrad, rounded); break;
                    }
                }
                break;
            case 3: // Line
                {
                    Vector2 a = center - new Vector2(shapeSize, 0);
                    Vector2 b = center + new Vector2(shapeSize, 0);
                    float radius = shapeSize * 0.15f;
                    switch (col)
                    {
                        case 0: _shapeBatch.FillLine(a, b, radius, fill); break;
                        case 1: _shapeBatch.BorderLine(a, b, radius, border, thickness); break;
                        case 2: _shapeBatch.DrawLine(a, b, radius, fillBorder, border, thickness); break;
                        case 3: _shapeBatch.FillLine(a, b, radius, linearGrad); break;
                        case 4: _shapeBatch.FillLine(a, b, radius, radialGrad); break;
                    }
                }
                break;
            case 4: // Hexagon
                switch (col)
                {
                    case 0: _shapeBatch.FillHexagon(center, shapeSize, fill); break;
                    case 1: _shapeBatch.BorderHexagon(center, shapeSize, border, thickness); break;
                    case 2: _shapeBatch.DrawHexagon(center, shapeSize, fillBorder, border, thickness); break;
                    case 3: _shapeBatch.FillHexagon(center, shapeSize, linearGrad); break;
                    case 4: _shapeBatch.FillHexagon(center, shapeSize, radialGrad); break;
                }
                break;
            case 5: // Triangle
                switch (col)
                {
                    case 0: _shapeBatch.FillEquilateralTriangle(center, shapeSize * 0.55f, fill); break;
                    case 1: _shapeBatch.BorderEquilateralTriangle(center, shapeSize * 0.55f, border, thickness); break;
                    case 2: _shapeBatch.DrawEquilateralTriangle(center, shapeSize * 0.55f, fillBorder, border, thickness); break;
                    case 3: _shapeBatch.FillEquilateralTriangle(center, shapeSize * 0.55f, linearGrad); break;
                    case 4: _shapeBatch.FillEquilateralTriangle(center, shapeSize * 0.55f, radialGrad); break;
                }
                break;
        }
    }

    private void DrawPerCornerShowcase(int x, int y, int width, int height)
    {
        // Five per-corner radii configurations. The leaf cell (column 1) goes through the
        // Gum wrapper (RoundedRectangle renderable); the rest use raw Apos calls. A visual
        // regression in the wrapper would make the leaf cell stand out from its neighbors.
        (string label, CornerRadii corners, Color color)[] cells =
        {
            ("Uniform 12",         new CornerRadii(12),                  new Color(80, 180, 220)),
            ("Leaf (2,12,2,12)",   new CornerRadii(2, 12, 2, 12),        new Color(112, 220, 80)),
            ("Tab (16,16,0,0)",    new CornerRadii(16, 16, 0, 0),        new Color(220, 160, 60)),
            ("Diagonal (20,0,20,0)", new CornerRadii(20, 0, 20, 0),      new Color(220, 60, 80)),
            ("Asym (0,30,10,4)",   new CornerRadii(0, 30, 10, 4),        new Color(200, 120, 220)),
        };

        int cols = cells.Length;
        float cellW = width / (float)cols;
        Vector2 size = new Vector2(MathF.Min(cellW * 0.82f, 180f), 36f);
        float rowCenterY = y + height * 0.5f;

        for (int i = 0; i < cells.Length; i++)
        {
            float cx = x + i * cellW + cellW / 2f;
            Vector2 xy = new Vector2(cx, rowCenterY) - size / 2f;

            if (i == 1)
            {
                // Leaf via the Gum wrapper. Width/Height are restamped from the cell size in case
                // the viewport was resized.
                _gumWrapperLeaf.X = xy.X;
                _gumWrapperLeaf.Y = xy.Y;
                _gumWrapperLeaf.Width = size.X;
                _gumWrapperLeaf.Height = size.Y;
                _gumWrapperLeaf.Render(null!);
            }
            else
            {
                _shapeBatch.DrawRectangle(xy, size, cells[i].color, cells[i].color,
                    thickness: 1f, cornerRadii: cells[i].corners, rotation: 0f, aaSize: 1.5f);
            }
        }
    }

    private void DrawArcShowcase(int x, int y, int width, int height)
    {
        // ArcRuntime variants - the unified Apos↔Skia ArcRuntime from issue #2728. Rendering
        // is done by calling Render on the underlying Arc renderable (the same path Apos uses
        // when the renderer walks the layer's renderables). The runtime instances themselves
        // verify the locked-in ctor defaults; any regression in those would show up as a
        // visibly-different first cell (the "Default" arc).
        int cols = _arcs.Length;
        float cellW = width / (float)cols;
        float arcSize = MathF.Min(cellW * 0.6f, height * 0.85f);
        float rowCenterY = y + height * 0.5f;

        for (int i = 0; i < _arcs.Length; i++)
        {
            float cx = x + i * cellW + cellW / 2f;
            ArcRuntime runtime = _arcs[i];
            Arc renderable = (Arc)runtime.RenderableComponent;

            renderable.Width = arcSize;
            renderable.Height = arcSize;
            renderable.X = cx - arcSize / 2f;
            renderable.Y = rowCenterY - arcSize / 2f;

            // The renderable's PreRender invokes the runtime's PreRender hook, which pushes
            // StrokeWidth (held on the runtime auto-property) down to the renderable. Without
            // this, the renderable keeps its own ctor default (10) and Thickness overrides on
            // individual cells (e.g. thickHalf = 18) would not render.
            renderable.PreRender();
            renderable.Render(null!);
        }
    }
}
