using Gum.GueDeriving;
using Gum.RenderingLibrary;
using Raylib_cs;
using RenderingLibrary.Graphics;
using Shouldly;
using static Raylib_cs.Raylib;

namespace RaylibGum.Tests.Rendering;

/// <summary>
/// Pixel-readback tests for the raylib render-to-texture path (issue #3434, PR 1). A container
/// with <see cref="ContainerRuntime.IsRenderTarget"/> bakes its subtree into an offscreen
/// <see cref="RenderTexture2D"/> in a pre-pass, then composites that texture back to the screen.
/// These tests drive a real headless raylib frame and read back the baked RT contents (and,
/// for the composite/group-alpha cases, a nested outer RT that the inner group composites into,
/// which gives a deterministic surface to sample without screen readback).
/// </summary>
public class RenderTargetTests : BaseTestClass
{
    private static void DrawOnce()
    {
        BeginDrawing();
        GumService.Default.Draw();
        EndDrawing();
    }

    private static Color ReadRenderTargetCenter(RenderTexture2D renderTexture)
    {
        return ReadRenderTargetPixel(renderTexture,
            renderTexture.Texture.Width / 2, renderTexture.Texture.Height / 2);
    }

    // Reads a pixel in top-left-origin draw space. A render texture is stored bottom-up in GL, so
    // LoadImageFromTexture yields an image whose rows are flipped relative to draw space — hence the
    // (height - 1 - y) flip here.
    private static Color ReadRenderTargetPixel(RenderTexture2D renderTexture, int x, int y)
    {
        Image image = LoadImageFromTexture(renderTexture.Texture);
        try
        {
            return GetImageColor(image, x, renderTexture.Texture.Height - 1 - y);
        }
        finally
        {
            UnloadImage(image);
        }
    }

    private static int DrawAndCountDrawCalls()
    {
        DrawOnce();
        return Renderer.Self.RenderStateChangeStatistics.DrawCallCount;
    }

    [Fact]
    public void Draw_RenderTargetContainer_BakesChildSubtreeIntoRenderTarget()
    {
        ContainerRuntime container = new();
        container.X = 0;
        container.Y = 0;
        container.Width = 100;
        container.Height = 100;
        container.IsRenderTarget = true;

        ColoredRectangleRuntime rectangle = new();
        rectangle.Width = 100;
        rectangle.Height = 100;
        rectangle.Color = new Color((byte)0, (byte)0, (byte)255, (byte)255);
        container.Children.Add(rectangle);

        GumService.Default.Root.Children.Add(container);
        GumService.Default.Root.UpdateLayout();

        DrawOnce();

        Renderer.Self.HasBakedRenderTargetFor(container).ShouldBeTrue();

        RenderTexture2D renderTexture = Renderer.Self.TryGetBakedRenderTargetFor(container)!.Value;
        Color center = ReadRenderTargetCenter(renderTexture);

        // The blue child rect should have baked into the container's render target.
        center.B.ShouldBeGreaterThan((byte)200);
        center.R.ShouldBeLessThan((byte)50);
        center.A.ShouldBeGreaterThan((byte)200);

        GumService.Default.Root.Children.Clear();
    }

    [Fact]
    public void Draw_NestedRenderTargetContainers_CompositeInnerIntoOuter()
    {
        ContainerRuntime outer = new();
        outer.X = 0;
        outer.Y = 0;
        outer.Width = 100;
        outer.Height = 100;
        outer.IsRenderTarget = true;

        ContainerRuntime inner = new();
        inner.Width = 100;
        inner.Height = 100;
        inner.IsRenderTarget = true;

        ColoredRectangleRuntime rectangle = new();
        rectangle.Width = 100;
        rectangle.Height = 100;
        rectangle.Color = new Color((byte)255, (byte)0, (byte)0, (byte)255);
        inner.Children.Add(rectangle);
        outer.Children.Add(inner);

        GumService.Default.Root.Children.Add(outer);
        GumService.Default.Root.UpdateLayout();

        DrawOnce();

        // Both the inner and outer containers must have baked (post-order, innermost first).
        Renderer.Self.HasBakedRenderTargetFor(inner).ShouldBeTrue();
        Renderer.Self.HasBakedRenderTargetFor(outer).ShouldBeTrue();

        // Reading the OUTER render target proves the inner group was composited into it while the
        // outer container baked — a deterministic view of the composite step without screen readback.
        RenderTexture2D outerRenderTexture = Renderer.Self.TryGetBakedRenderTargetFor(outer)!.Value;
        Color center = ReadRenderTargetCenter(outerRenderTexture);

        center.R.ShouldBeGreaterThan((byte)200);
        center.B.ShouldBeLessThan((byte)50);
        center.A.ShouldBeGreaterThan((byte)200);

        GumService.Default.Root.Children.Clear();
    }

    [Fact]
    public void Draw_ReducedGroupAlpha_ProducesDimmerCompositedPixels()
    {
        ContainerRuntime outer = new();
        outer.X = 0;
        outer.Y = 0;
        outer.Width = 100;
        outer.Height = 100;
        outer.IsRenderTarget = true;

        ContainerRuntime inner = new();
        inner.Width = 100;
        inner.Height = 100;
        inner.IsRenderTarget = true;

        ColoredRectangleRuntime rectangle = new();
        rectangle.Width = 100;
        rectangle.Height = 100;
        rectangle.Color = new Color((byte)255, (byte)0, (byte)0, (byte)255);
        inner.Children.Add(rectangle);
        outer.Children.Add(inner);

        GumService.Default.Root.Children.Add(outer);
        GumService.Default.Root.UpdateLayout();

        inner.Alpha = 255;
        DrawOnce();
        byte fullRed = ReadRenderTargetCenter(Renderer.Self.TryGetBakedRenderTargetFor(outer)!.Value).R;

        inner.Alpha = 64;
        DrawOnce();
        byte dimRed = ReadRenderTargetCenter(Renderer.Self.TryGetBakedRenderTargetFor(outer)!.Value).R;

        // Group alpha is applied when the inner group's baked texture is composited, so lowering it
        // must dim the composited red without touching the inner bake itself.
        fullRed.ShouldBeGreaterThan((byte)200);
        dimRed.ShouldBeLessThan(fullRed);

        GumService.Default.Root.Children.Clear();
    }

    // Fix 1: a render-target container whose clamped bounds are degenerate (0-sized) must not drop
    // its subtree. Here a 0x0 inner RT still renders its child directly into the outer RT.
    [Fact]
    public void Draw_DegenerateSizeRenderTarget_RendersChildrenDirectly()
    {
        ContainerRuntime outer = new();
        outer.X = 0;
        outer.Y = 0;
        outer.Width = 100;
        outer.Height = 100;
        outer.IsRenderTarget = true;

        ContainerRuntime degenerate = new();
        degenerate.Width = 0;
        degenerate.Height = 0;
        degenerate.IsRenderTarget = true;

        ColoredRectangleRuntime rectangle = new();
        rectangle.Width = 100;
        rectangle.Height = 100;
        rectangle.Color = new Color((byte)0, (byte)255, (byte)0, (byte)255);
        degenerate.Children.Add(rectangle);
        outer.Children.Add(degenerate);

        GumService.Default.Root.Children.Add(outer);
        GumService.Default.Root.UpdateLayout();

        DrawOnce();

        // The degenerate container bakes nothing, but its green child must still appear in the outer
        // render target rather than vanishing.
        Renderer.Self.HasBakedRenderTargetFor(degenerate).ShouldBeFalse();
        Color center = ReadRenderTargetCenter(Renderer.Self.TryGetBakedRenderTargetFor(outer)!.Value);
        center.G.ShouldBeGreaterThan((byte)200);
        center.R.ShouldBeLessThan((byte)50);

        GumService.Default.Root.Children.Clear();
    }

    // Fix 2: after a blend-toggling child (a nested inner RT composite), a following semi-transparent
    // sibling must still bake under the premultiply pass — not straight alpha, which would halve its
    // coverage (the double-blend fringe). The sibling occupies the right half at full alpha coverage.
    [Fact]
    public void Draw_SiblingAfterNestedRenderTarget_KeepsPremultipliedCoverage()
    {
        ContainerRuntime outer = new();
        outer.X = 0;
        outer.Y = 0;
        outer.Width = 100;
        outer.Height = 100;
        outer.IsRenderTarget = true;

        ContainerRuntime inner = new();
        inner.X = 0;
        inner.Y = 0;
        inner.Width = 50;
        inner.Height = 100;
        inner.IsRenderTarget = true;

        ColoredRectangleRuntime innerFill = new();
        innerFill.Width = 50;
        innerFill.Height = 100;
        innerFill.Color = new Color((byte)255, (byte)0, (byte)0, (byte)255);
        inner.Children.Add(innerFill);

        ColoredRectangleRuntime sibling = new();
        sibling.X = 50;
        sibling.Y = 0;
        sibling.Width = 50;
        sibling.Height = 100;
        // Half-alpha white: premultiplied coverage keeps alpha ~128; straight-alpha double-blend
        // would square it to ~64.
        sibling.Color = new Color((byte)255, (byte)255, (byte)255, (byte)128);

        // Order matters: the nested RT composites first (toggling blend), then the sibling bakes.
        outer.Children.Add(inner);
        outer.Children.Add(sibling);

        GumService.Default.Root.Children.Add(outer);
        GumService.Default.Root.UpdateLayout();

        DrawOnce();

        RenderTexture2D outerRenderTexture = Renderer.Self.TryGetBakedRenderTargetFor(outer)!.Value;
        Color innerRegion = ReadRenderTargetPixel(outerRenderTexture, 25, 50);
        Color siblingRegion = ReadRenderTargetPixel(outerRenderTexture, 75, 50);

        // Inner RT still composited its red.
        innerRegion.R.ShouldBeGreaterThan((byte)200);
        // Sibling kept full premultiplied coverage (~128), proving the premultiply pass was
        // re-established after the nested composite toggled blend.
        siblingRegion.A.ShouldBeGreaterThan((byte)100);

        GumService.Default.Root.Children.Clear();
    }

    // Fix 3: a top-level render-target container (added straight to the layer, where the walk is not
    // Visible-gated) must not composite last frame's stale texture for one frame after being hidden.
    [Fact]
    public void Draw_HiddenTopLevelRenderTarget_DoesNotCompositeStaleTexture()
    {
        int baseline = DrawAndCountDrawCalls();

        ContainerRuntime container = new();
        container.Width = 50;
        container.Height = 50;
        container.IsRenderTarget = true;

        ColoredRectangleRuntime rectangle = new();
        rectangle.Width = 50;
        rectangle.Height = 50;
        rectangle.Color = new Color((byte)0, (byte)255, (byte)0, (byte)255);
        container.Children.Add(rectangle);
        container.AddToManagers();
        container.UpdateLayout();

        int visibleCalls = DrawAndCountDrawCalls();
        Renderer.Self.HasBakedRenderTargetFor(container).ShouldBeTrue();

        container.Visible = false;
        int hiddenCalls = DrawAndCountDrawCalls();

        // Hidden: no bake and no composite, so the frame's draw-call count returns to the empty
        // baseline. Under the ghost bug the stale cached texture would still be blitted (baseline + 1).
        visibleCalls.ShouldBeGreaterThan(baseline);
        hiddenCalls.ShouldBe(baseline);

        container.RemoveFromManagers();
    }

    // Fix 4: an additive-blend render-target container composites its premultiplied texture with an
    // additive-onto-premultiplied blend, not raylib's BlendMode.Additive (which multiplies by source
    // alpha again and renders the glow too dim). A half-alpha additive layer over a dark background
    // must brighten it by ~the premultiplied color, not ~half of it.
    [Fact]
    public void Draw_AdditiveRenderTarget_AddsPremultipliedColorWithoutDoubleAlpha()
    {
        ContainerRuntime outer = new();
        outer.X = 0;
        outer.Y = 0;
        outer.Width = 100;
        outer.Height = 100;
        outer.IsRenderTarget = true;

        ColoredRectangleRuntime background = new();
        background.Width = 100;
        background.Height = 100;
        background.Color = new Color((byte)50, (byte)0, (byte)0, (byte)255);

        ContainerRuntime additive = new();
        additive.Width = 100;
        additive.Height = 100;
        additive.IsRenderTarget = true;
        additive.Blend = Blend.Additive;

        ColoredRectangleRuntime glow = new();
        glow.Width = 100;
        glow.Height = 100;
        // Half-alpha red -> premultiplied ~100 red. Correct additive adds ~100 to the background's
        // 50; the double-alpha bug would add only ~50.
        glow.Color = new Color((byte)200, (byte)0, (byte)0, (byte)128);
        additive.Children.Add(glow);

        outer.Children.Add(background);
        outer.Children.Add(additive);

        GumService.Default.Root.Children.Add(outer);
        GumService.Default.Root.UpdateLayout();

        DrawOnce();

        Color center = ReadRenderTargetCenter(Renderer.Self.TryGetBakedRenderTargetFor(outer)!.Value);
        center.R.ShouldBeGreaterThan((byte)130);

        GumService.Default.Root.Children.Clear();
    }

    // A ClipsChildren descendant inside a render-target container clips within the RT (#3440). The
    // clip rect (a 50x50 window at 0,25 inside the 100x100 RT) is deliberately NOT full-height and
    // NOT full-width: an over-sized red child must be clipped on all four sides. This discriminates
    // the correct RT-local scissor rebasing from the earlier #3436 attempt, which over-compensated
    // the scissor Y by the screen height — that only survived a full-height clip (where the wrong
    // formula happens to coincide with the right one) and broke outright under software GL. Verified
    // headless on both hardware GL and CI's Mesa llvmpipe.
    [Fact]
    public void Draw_ClipsChildrenInsideRenderTarget_ClipsWithinTheTarget()
    {
        ContainerRuntime outer = new();
        outer.X = 0;
        outer.Y = 0;
        outer.Width = 100;
        outer.Height = 100;
        outer.IsRenderTarget = true;

        ContainerRuntime clip = new();
        clip.X = 0;
        clip.Y = 25;
        clip.Width = 50;
        clip.Height = 50;
        clip.ClipsChildren = true;

        ColoredRectangleRuntime wide = new();
        wide.X = 0;
        wide.Y = -25;
        wide.Width = 200;
        wide.Height = 200;
        wide.Color = new Color((byte)255, (byte)0, (byte)0, (byte)255);
        clip.Children.Add(wide);
        outer.Children.Add(clip);

        GumService.Default.Root.Children.Add(outer);
        GumService.Default.Root.UpdateLayout();

        DrawOnce();

        RenderTexture2D outerRenderTexture = Renderer.Self.TryGetBakedRenderTargetFor(outer)!.Value;
        // Center of the 50x50 clip window (x in [0,50), y in [25,75)).
        Color insideClip = ReadRenderTargetPixel(outerRenderTexture, 25, 50);
        Color rightOfClip = ReadRenderTargetPixel(outerRenderTexture, 75, 50);
        Color aboveClip = ReadRenderTargetPixel(outerRenderTexture, 25, 10);
        Color belowClip = ReadRenderTargetPixel(outerRenderTexture, 25, 90);

        // Inside the clip window: the red child shows. Beyond it on every side: clipped to transparent.
        insideClip.R.ShouldBeGreaterThan((byte)200);
        rightOfClip.A.ShouldBeLessThan((byte)50);
        aboveClip.A.ShouldBeLessThan((byte)50);
        belowClip.A.ShouldBeLessThan((byte)50);

        GumService.Default.Root.Children.Clear();
    }

    // Item 2 invariant (#3434, #3436): a semi-transparent element must composite to the SAME pixel
    // whether or not it lives inside a render target. This pins the premultiplied-alpha round-trip:
    // a 50%-white rect over an opaque frame, drawn directly, must match the same rect drawn inside a
    // nested render target (and composited back). Both scenarios are captured in their own readable
    // outer render target so the comparison is a straight pixel diff with no screen readback.
    [Fact]
    public void Draw_SemiTransparentElement_InsideRenderTargetMatchesDirectDraw()
    {
        Color frameColor = new((byte)70, (byte)70, (byte)90, (byte)255);
        Color semiTransparentWhite = new((byte)255, (byte)255, (byte)255, (byte)128);

        // Direct draw: frame, then the semi-transparent rect straight on top — both inside the
        // readable outer render target.
        ContainerRuntime directOuter = new();
        directOuter.X = 0;
        directOuter.Y = 0;
        directOuter.Width = 100;
        directOuter.Height = 100;
        directOuter.IsRenderTarget = true;
        directOuter.Children.Add(FullRect(frameColor));
        directOuter.Children.Add(FullRect(semiTransparentWhite));

        // Inside a render target: the same semi-transparent rect lives in a nested RT that composites
        // over the frame. This is the "Nested RT + sibling" sample path.
        ContainerRuntime nestedOuter = new();
        nestedOuter.X = 0;
        nestedOuter.Y = 0;
        nestedOuter.Width = 100;
        nestedOuter.Height = 100;
        nestedOuter.IsRenderTarget = true;
        nestedOuter.Children.Add(FullRect(frameColor));

        ContainerRuntime inner = new();
        inner.Width = 100;
        inner.Height = 100;
        inner.IsRenderTarget = true;
        inner.Children.Add(FullRect(semiTransparentWhite));
        nestedOuter.Children.Add(inner);

        GumService.Default.Root.Children.Add(directOuter);
        GumService.Default.Root.Children.Add(nestedOuter);
        GumService.Default.Root.UpdateLayout();

        DrawOnce();

        Color direct = ReadRenderTargetCenter(Renderer.Self.TryGetBakedRenderTargetFor(directOuter)!.Value);
        Color nested = ReadRenderTargetCenter(Renderer.Self.TryGetBakedRenderTargetFor(nestedOuter)!.Value);

        // Within a small tolerance (byte rounding across the extra composite step), the in-RT pixel
        // must equal the direct-draw pixel. A premultiplied-alpha double-blend would darken the
        // in-RT pixel and fail this.
        const int tolerance = 4;
        System.Math.Abs(nested.R - direct.R).ShouldBeLessThanOrEqualTo(tolerance);
        System.Math.Abs(nested.G - direct.G).ShouldBeLessThanOrEqualTo(tolerance);
        System.Math.Abs(nested.B - direct.B).ShouldBeLessThanOrEqualTo(tolerance);
        System.Math.Abs(nested.A - direct.A).ShouldBeLessThanOrEqualTo(tolerance);

        GumService.Default.Root.Children.Clear();
    }

    // The render target is sized to the container's bounds, so a child larger than the container is
    // truncated to the container (the "Overflow clipped" sample cell). A checkerboard board larger
    // than the 90x90 target: the target texture stays 90x90 (not the board's 176x176), and an
    // in-bounds cell reads its opaque color — proving texture-size truncation while the pattern
    // survives the bake. (The sample uses only solid rects here — no Apos.Shapes primitives — so it
    // renders identically on both backends.)
    [Fact]
    public void Draw_ChildLargerThanRenderTarget_TruncatesToContainerBounds()
    {
        ContainerRuntime container = new();
        container.X = 0;
        container.Y = 0;
        container.Width = 90;
        container.Height = 90;
        container.IsRenderTarget = true;

        // 8x8 board of 22px cells (176x176), offset so it overflows the 90x90 target on all sides.
        const int cellSize = 22;
        Color green = new((byte)80, (byte)200, (byte)120, (byte)255);
        Color orange = new((byte)230, (byte)150, (byte)40, (byte)255);
        ContainerRuntime board = new();
        board.X = -18;
        board.Y = -18;
        board.Width = 8 * cellSize;
        board.Height = 8 * cellSize;
        for (int row = 0; row < 8; row++)
        {
            for (int column = 0; column < 8; column++)
            {
                ColoredRectangleRuntime cell = new();
                cell.X = column * cellSize;
                cell.Y = row * cellSize;
                cell.Width = cellSize;
                cell.Height = cellSize;
                cell.Color = (row + column) % 2 == 0 ? green : orange;
                board.Children.Add(cell);
            }
        }
        container.Children.Add(board);

        GumService.Default.Root.Children.Add(container);
        GumService.Default.Root.UpdateLayout();

        DrawOnce();

        RenderTexture2D renderTexture = Renderer.Self.TryGetBakedRenderTargetFor(container)!.Value;

        // Truncated to the 90x90 container, not the 176x176 board.
        renderTexture.Texture.Width.ShouldBe(90);
        renderTexture.Texture.Height.ShouldBe(90);

        // The center cell (row/col 2, even -> green) baked as an opaque cell color.
        Color mid = ReadRenderTargetPixel(renderTexture, 45, 45);
        mid.A.ShouldBeGreaterThan((byte)200);
        mid.G.ShouldBeGreaterThan((byte)150);
        mid.R.ShouldBeLessThan((byte)150);

        GumService.Default.Root.Children.Clear();
    }

    private static ColoredRectangleRuntime FullRect(Color color)
    {
        ColoredRectangleRuntime rect = new();
        rect.X = 0;
        rect.Y = 0;
        rect.Width = 100;
        rect.Height = 100;
        rect.Color = color;
        return rect;
    }

    // Issue #3449 (split off #3434 item 3): a Sprite whose RenderTargetTextureSource points at
    // another container's baked RT must display that texture right-side-up. An RT is stored
    // bottom-up in GL, so this specifically pins the Y-flip the Sprite must apply — a top/bottom
    // split source is required because a uniform-color source can't distinguish "flipped" from
    // "not flipped."
    [Fact]
    public void Draw_SpriteWithRenderTargetTextureSource_PreservesVerticalOrientation()
    {
        ContainerRuntime source = new();
        source.Width = 64;
        source.Height = 64;
        source.IsRenderTarget = true;

        ColoredRectangleRuntime top = new();
        top.X = 0;
        top.Y = 0;
        top.Width = 64;
        top.Height = 32;
        top.Color = new Color((byte)255, (byte)0, (byte)0, (byte)255);
        source.Children.Add(top);

        ColoredRectangleRuntime bottom = new();
        bottom.X = 0;
        bottom.Y = 32;
        bottom.Width = 64;
        bottom.Height = 32;
        bottom.Color = new Color((byte)0, (byte)255, (byte)0, (byte)255);
        source.Children.Add(bottom);

        ContainerRuntime readableOuter = new();
        readableOuter.X = 0;
        readableOuter.Y = 0;
        readableOuter.Width = 64;
        readableOuter.Height = 64;
        readableOuter.IsRenderTarget = true;

        SpriteRuntime sprite = new();
        sprite.X = 0;
        sprite.Y = 0;
        sprite.WidthUnits = Gum.DataTypes.DimensionUnitType.Absolute;
        sprite.HeightUnits = Gum.DataTypes.DimensionUnitType.Absolute;
        sprite.Width = 64;
        sprite.Height = 64;
        sprite.RenderTargetTextureSource = source;
        readableOuter.Children.Add(sprite);

        GumService.Default.Root.Children.Add(source);
        GumService.Default.Root.Children.Add(readableOuter);
        GumService.Default.Root.UpdateLayout();

        DrawOnce();

        RenderTexture2D outerRenderTexture = Renderer.Self.TryGetBakedRenderTargetFor(readableOuter)!.Value;
        Color topPixel = ReadRenderTargetPixel(outerRenderTexture, 32, 10);
        Color bottomPixel = ReadRenderTargetPixel(outerRenderTexture, 32, 54);

        topPixel.R.ShouldBeGreaterThan((byte)200);
        bottomPixel.G.ShouldBeGreaterThan((byte)200);

        GumService.Default.Root.Children.Clear();
    }

    // Issue #3452: a Sprite referencing an INVISIBLE IsRenderTarget container must still show the
    // baked texture. An invisible source is skipped by the plain Visible-gated bake walk, so the
    // referenced-owners collection must force it to bake anyway (mirrors MonoGame #1643). The source
    // is hidden but referenced by a visible sprite inside a readable outer RT; the outer RT's center
    // must show the source's baked red.
    [Fact]
    public void Draw_SpriteReferencingInvisibleRenderTarget_ShowsBakedTexture()
    {
        ContainerRuntime source = new();
        source.Width = 64;
        source.Height = 64;
        source.IsRenderTarget = true;
        source.Visible = false;

        ColoredRectangleRuntime fill = new();
        fill.Width = 64;
        fill.Height = 64;
        fill.Color = new Color((byte)255, (byte)0, (byte)0, (byte)255);
        source.Children.Add(fill);

        ContainerRuntime readableOuter = new();
        readableOuter.X = 0;
        readableOuter.Y = 0;
        readableOuter.Width = 64;
        readableOuter.Height = 64;
        readableOuter.IsRenderTarget = true;

        SpriteRuntime sprite = new();
        sprite.X = 0;
        sprite.Y = 0;
        sprite.WidthUnits = Gum.DataTypes.DimensionUnitType.Absolute;
        sprite.HeightUnits = Gum.DataTypes.DimensionUnitType.Absolute;
        sprite.Width = 64;
        sprite.Height = 64;
        sprite.RenderTargetTextureSource = source;
        readableOuter.Children.Add(sprite);

        GumService.Default.Root.Children.Add(source);
        GumService.Default.Root.Children.Add(readableOuter);
        GumService.Default.Root.UpdateLayout();

        DrawOnce();

        // The invisible source still baked because a visible sprite references it.
        Renderer.Self.HasBakedRenderTargetFor(source).ShouldBeTrue();

        Color center = ReadRenderTargetCenter(Renderer.Self.TryGetBakedRenderTargetFor(readableOuter)!.Value);
        center.R.ShouldBeGreaterThan((byte)200);
        center.G.ShouldBeLessThan((byte)50);

        GumService.Default.Root.Children.Clear();
    }

    // Issue #3452 perf guard: an invisible IsRenderTarget container that NOTHING references must not
    // bake. This keeps the fast-path intact — screens with no visible/referenced render targets skip
    // the bake pre-pass entirely rather than baking hidden content for nobody.
    [Fact]
    public void Draw_InvisibleUnreferencedRenderTarget_DoesNotBake()
    {
        ContainerRuntime container = new();
        container.Width = 64;
        container.Height = 64;
        container.IsRenderTarget = true;
        container.Visible = false;

        ColoredRectangleRuntime fill = new();
        fill.Width = 64;
        fill.Height = 64;
        fill.Color = new Color((byte)0, (byte)0, (byte)255, (byte)255);
        container.Children.Add(fill);

        GumService.Default.Root.Children.Add(container);
        GumService.Default.Root.UpdateLayout();

        DrawOnce();

        Renderer.Self.HasBakedRenderTargetFor(container).ShouldBeFalse();

        GumService.Default.Root.Children.Clear();
    }

    // A Sprite referencing a container that never baked (not an IsRenderTarget) must draw nothing
    // rather than crash on a null lookup.
    [Fact]
    public void Draw_SpriteWithUnbakedRenderTargetTextureSource_DrawsNothing()
    {
        int baseline = DrawAndCountDrawCalls();

        ContainerRuntime notARenderTarget = new();
        notARenderTarget.Width = 64;
        notARenderTarget.Height = 64;

        SpriteRuntime sprite = new();
        sprite.X = 0;
        sprite.Y = 0;
        sprite.Width = 64;
        sprite.Height = 64;
        sprite.RenderTargetTextureSource = notARenderTarget;
        sprite.AddToManagers();
        sprite.UpdateLayout();

        int callsWithUnbakedSource = DrawAndCountDrawCalls();

        callsWithUnbakedSource.ShouldBe(baseline);

        sprite.RemoveFromManagers();
    }
}
