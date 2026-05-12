using System;
using Apos.Shapes;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameAndGum.Renderables;

namespace MonoGameGumShapesGallery;

// Visual smoke test for Gum.Shapes.MonoGame. Two stacked sections:
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
//      tabs, diagonals, asymmetric corners.
//
// To verify the Gum-side wrapper, a single RoundedRectangleRuntime is constructed with the
// same per-corner values as the leaf cell and rendered alongside its raw-API counterpart.
// They should be visually identical; if they're not, the wrapper has regressed.
public class Game1 : Game
{
    private readonly GraphicsDeviceManager _graphics;
    private ShapeBatch _shapeBatch = default!;
    private RoundedRectangle _gumWrapperLeaf = default!;

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
        Window.Title = "Gum Shapes Gallery — Apos.Shapes 0.6.9 + per-corner radii";
    }

    protected override void LoadContent()
    {
        // Initialize ShapeRenderer.Self so the same singleton ShapeBatch is shared between
        // raw shapeBatch calls in this sample and Gum's RoundedRectangleRuntime.Render path.
        // This is what consumers of MonoGameGumShapes hit in production.
        MonoGameAndGum.Renderables.ShapeRenderer.Self.Initialize(GraphicsDevice, Content);
        _shapeBatch = MonoGameAndGum.Renderables.ShapeRenderer.Self.ShapeBatch;

        // Build a single RoundedRectangle (the Gum-side renderable, the one the
        // RoundedRectangleRuntime wraps) with the leaf-shape per-corner radii. Drawn at the
        // bottom of Section 2 next to its raw-API counterpart to verify the wrapper forwards
        // the per-corner properties correctly.
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
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(new Color(30, 30, 30));

        int w = GraphicsDevice.Viewport.Width;
        int h = GraphicsDevice.Viewport.Height;

        // Split the viewport ~70/30 between the gallery grid and the per-corner showcase.
        int gallerHeight = (int)(h * 0.7f);
        int cornerSectionTop = gallerHeight;
        int cornerSectionHeight = h - gallerHeight;

        _shapeBatch.Begin();

        DrawGalleryGrid(w, gallerHeight);
        DrawPerCornerShowcase(0, cornerSectionTop, w, cornerSectionHeight);

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
        // Five per-corner radii configurations. Each pair (raw Apos call + Gum-runtime call
        // where applicable) sits in the same column so visual regressions in the wrapper jump
        // out at a glance.
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

        // Row 1 — raw Apos.Shapes call
        float row1CenterY = y + height * 0.32f;
        for (int i = 0; i < cells.Length; i++)
        {
            float cx = x + i * cellW + cellW / 2f;
            Vector2 xy = new Vector2(cx, row1CenterY) - size / 2f;
            _shapeBatch.DrawRectangle(xy, size, cells[i].color, cells[i].color,
                thickness: 1f, cornerRadii: cells[i].corners, rotation: 0f, aaSize: 1.5f);
        }

        // Row 2 — Gum's RoundedRectangle renderable, leaf cell only (column 1). The other
        // columns stay raw-API for compactness; the leaf is the case the Forest Glade theme
        // depends on, so it's the one we want byte-for-byte parity verified.
        float row2CenterY = y + height * 0.74f;
        for (int i = 0; i < cells.Length; i++)
        {
            float cx = x + i * cellW + cellW / 2f;
            Vector2 xy = new Vector2(cx, row2CenterY) - size / 2f;

            if (i == 1)
            {
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
}
