using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Renderables;
using Gum.Wireframe;
using Raylib_cs;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using Shouldly;
using static Raylib_cs.Raylib;

namespace RaylibGum.Tests.Rendering;

/// <summary>
/// Real-frame conformance suite for raylib's clip/cull walk (issue #4155), the off-screen-cull /
/// clip-walk counterpart to <see cref="RenderTargetTests"/>. raylib used to reimplement the cull
/// and clip walk inline rather than routing through the shared
/// <see cref="RenderingLibrary.Graphics.HierarchicalOrderer"/>, which is how it silently diverged
/// from MonoGame's behavior while every MonoGame-side test stayed green (#4144); both the main pass
/// and the render-target bake now go through that shared builder (#4154). Covers the
/// off-screen cull at the clip edge and cull margin, nested-clip intersection, and one pinning
/// test per divergence found auditing against <c>HierarchicalOrderer</c>: draw order relative to
/// an element's own clip, non-<see cref="GraphicalUiElement"/> children, and top-level visibility.
/// </summary>
public class ClipWalkConformanceTests : BaseTestClass
{
    private static int DrawAndCountDrawCalls()
    {
        BeginDrawing();
        GumService.Default.Draw();
        EndDrawing();
        return Renderer.Self.RenderStateChangeStatistics.DrawCallCount;
    }

    // Reads a pixel in top-left-origin draw space. A render texture is stored bottom-up in GL, so
    // LoadImageFromTexture yields an image whose rows are flipped relative to draw space.
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

    // A raw (non-GraphicalUiElement) IRenderableIpso, drawn directly via raylib so its own draw call
    // is counted by BatchDrawCallCounter the same way any renderable's is. Draws a rectangle larger
    // than its own declared bounds by OverdrawPixels on every side, so a clip applied AFTER the draw
    // (the divergence-1 bug) can be told apart from a clip applied BEFORE it.
    private sealed class OverdrawRenderable : InvisibleRenderable
    {
        public Color FillColor { get; set; }
        public int OverdrawPixels { get; set; }

        public override void Render(ISystemManagers managers)
        {
            int x = (int)this.GetAbsoluteLeft() - OverdrawPixels;
            int y = (int)this.GetAbsoluteTop() - OverdrawPixels;
            int width = (int)Width + OverdrawPixels * 2;
            int height = (int)Height + OverdrawPixels * 2;
            Raylib.DrawRectangle(x, y, width, height, FillColor);
        }
    }

    // Runtime wrapper for OverdrawRenderable, following the standard Gum Runtime-wraps-Renderable
    // pattern (e.g. ColoredRectangleRuntime / SolidRectangle) so it can be positioned/sized through
    // normal Gum layout and nested via ContainerRuntime.Children like any other runtime.
    private sealed class OverdrawRuntime : GraphicalUiElement
    {
        private readonly OverdrawRenderable _renderable;

        public OverdrawRuntime() : base(new OverdrawRenderable(), whatContainsThis: null)
        {
            _renderable = (OverdrawRenderable)RenderableComponent;
        }

        public Color FillColor { set => _renderable.FillColor = value; }
        public int OverdrawPixels { set => _renderable.OverdrawPixels = value; }
    }

    [Fact]
    public void Draw_ChildJustInsideCullMargin_IsNotCulled()
    {
        int baseline = DrawAndCountDrawCalls();

        ContainerRuntime clipParent = new();
        clipParent.Y = 200; // away from the camera edge; see OffscreenCullWrappedTextTests remarks
        clipParent.Width = 200;
        clipParent.WidthUnits = DimensionUnitType.Absolute;
        clipParent.Height = 150;
        clipParent.HeightUnits = DimensionUnitType.Absolute;
        clipParent.ClipsChildren = true;

        // Clip bottom is at 200 + 150 = 350. The margin is 15px, so bounds starting up to 365 are
        // kept. This child's top sits 10px past the clip bottom -- inside the margin.
        ColoredRectangleRuntime nearMiss = new();
        nearMiss.Width = 50;
        nearMiss.Height = 50;
        nearMiss.Y = 160;
        clipParent.Children.Add(nearMiss);

        GumService.Default.Root.Children.Add(clipParent);
        GumService.Default.Root.UpdateLayout();

        int withNearMiss = DrawAndCountDrawCalls();

        GumService.Default.Root.Children.Clear();

        (withNearMiss - baseline).ShouldBe(1);
    }

    [Fact]
    public void Draw_ChildStraddlingClipEdge_IsNotCulled()
    {
        int baseline = DrawAndCountDrawCalls();

        ContainerRuntime clipParent = new();
        clipParent.Y = 200;
        clipParent.Width = 200;
        clipParent.WidthUnits = DimensionUnitType.Absolute;
        clipParent.Height = 150;
        clipParent.HeightUnits = DimensionUnitType.Absolute;
        clipParent.ClipsChildren = true;

        // Clip spans absolute Y [200, 350]. This child spans [330, 380] -- half inside, half past
        // the clip edge.
        ColoredRectangleRuntime straddler = new();
        straddler.Width = 50;
        straddler.Height = 50;
        straddler.Y = 130;
        clipParent.Children.Add(straddler);

        GumService.Default.Root.Children.Add(clipParent);
        GumService.Default.Root.UpdateLayout();

        int withStraddler = DrawAndCountDrawCalls();

        GumService.Default.Root.Children.Clear();

        (withStraddler - baseline).ShouldBe(1);
    }

    // Pixel-based (not draw-call-count) because raylib's rlgl batcher coalesces consecutive
    // same-state draws into a single banked segment -- two same-color rectangles drawn back to
    // back would both count as "1" regardless of whether either was culled, hiding a regression.
    [Fact]
    public void Draw_ChildrenScrolledOutsideClip_AreCulledWhileInsideChildrenAreKept()
    {
        ContainerRuntime readableOuter = new();
        readableOuter.X = 0;
        readableOuter.Y = 0;
        readableOuter.Width = 200;
        readableOuter.Height = 200;
        readableOuter.IsRenderTarget = true;

        // Clips to the top half of the render target: [0,200] x [0,100].
        ContainerRuntime clipParent = new();
        clipParent.X = 0;
        clipParent.Y = 0;
        clipParent.Width = 200;
        clipParent.Height = 100;
        clipParent.ClipsChildren = true;

        ColoredRectangleRuntime inside = new();
        inside.Width = 180;
        inside.Height = 80;
        inside.Y = 0;
        inside.Color = new Color((byte)0, (byte)255, (byte)0, (byte)255);
        clipParent.Children.Add(inside);

        // Absolute Y [150,190]: well past the clip bottom (100) plus its 15px margin, but still
        // within the 200-tall readable render target so its pixels are checkable either way.
        ColoredRectangleRuntime outside = new();
        outside.Width = 180;
        outside.Height = 40;
        outside.Y = 150;
        outside.Color = new Color((byte)0, (byte)255, (byte)0, (byte)255);
        clipParent.Children.Add(outside);

        readableOuter.Children.Add(clipParent);
        GumService.Default.Root.Children.Add(readableOuter);
        GumService.Default.Root.UpdateLayout();

        BeginDrawing();
        GumService.Default.Draw();
        EndDrawing();

        RenderTexture2D renderTexture = Renderer.Self.TryGetBakedRenderTargetFor(readableOuter)!.Value;
        Color insidePixel = ReadRenderTargetPixel(renderTexture, 90, 40);
        Color outsidePixel = ReadRenderTargetPixel(renderTexture, 90, 170);

        insidePixel.G.ShouldBeGreaterThan((byte)200);
        outsidePixel.A.ShouldBeLessThan((byte)50);

        GumService.Default.Root.Children.Clear();
    }

    // Divergence 1 (#4155): HierarchicalOrderer emits BeginClip before DrawRenderable, so a
    // clipping element's own draw is clipped by its own bounds. raylib rendered the element and
    // pushed the scissor afterward, so an element that draws outside its own declared bounds (e.g.
    // a border/overflow) leaked past its own clip.
    [Fact]
    public void Draw_ClippingElement_DoesNotDrawOutsideItsOwnClip()
    {
        ContainerRuntime readableOuter = new();
        readableOuter.X = 0;
        readableOuter.Y = 0;
        readableOuter.Width = 100;
        readableOuter.Height = 100;
        readableOuter.IsRenderTarget = true;

        OverdrawRuntime overdraw = new();
        overdraw.X = 25;
        overdraw.Y = 25;
        overdraw.WidthUnits = DimensionUnitType.Absolute;
        overdraw.HeightUnits = DimensionUnitType.Absolute;
        overdraw.Width = 50;
        overdraw.Height = 50;
        overdraw.ClipsChildren = true;
        overdraw.FillColor = new Color((byte)255, (byte)0, (byte)0, (byte)255);
        // Draws [5,5]-[95,95] but its own declared bounds are [25,25]-[75,75].
        overdraw.OverdrawPixels = 20;
        readableOuter.Children.Add(overdraw);

        GumService.Default.Root.Children.Add(readableOuter);
        GumService.Default.Root.UpdateLayout();

        BeginDrawing();
        GumService.Default.Draw();
        EndDrawing();

        RenderTexture2D renderTexture = Renderer.Self.TryGetBakedRenderTargetFor(readableOuter)!.Value;
        Color insideOwnBounds = ReadRenderTargetPixel(renderTexture, 50, 50);
        Color inOverdrawZone = ReadRenderTargetPixel(renderTexture, 10, 50);

        insideOwnBounds.R.ShouldBeGreaterThan((byte)200);
        // Outside the element's own bounds but within its overdraw -- must be clipped away.
        inOverdrawZone.R.ShouldBeLessThan((byte)50);

        GumService.Default.Root.Children.Clear();
    }

    // Divergence 3 (#4155): HierarchicalOrderer skips an invisible renderable and its whole subtree
    // up front. raylib's top-level Render() loop had no Visible check at all (only the IsRenderTarget
    // branch checked it), so an invisible top-level container's own draw was already a no-op
    // (InvisibleRenderable.Render is empty regardless), but the walk still recursed into its VISIBLE
    // children and drew them -- a visibility leak. AddToManagers (rather than Root.Children.Add)
    // places the container directly on the layer as a top-level renderable, exercising Render()'s
    // loop directly rather than a parent's already-Visible-gated child recursion.
    [Fact]
    public void Draw_InvisibleTopLevelContainer_HidesVisibleChildren()
    {
        int baseline = DrawAndCountDrawCalls();

        ContainerRuntime container = new();
        container.Width = 100;
        container.Height = 100;
        container.Visible = false;

        ColoredRectangleRuntime visibleChild = new();
        visibleChild.Width = 50;
        visibleChild.Height = 50;
        container.Children.Add(visibleChild);

        container.AddToManagers();
        container.UpdateLayout();

        int withHiddenContainer = DrawAndCountDrawCalls();

        container.RemoveFromManagers();

        withHiddenContainer.ShouldBe(baseline);
    }

    // Coverage for existing (correct) behavior: raylib's BeginScissorMode replaces rather than
    // intersects, which is why the Submit phase maintains its own _scissorStack. A nested clip
    // narrower than its ancestor must still be bounded by BOTH rectangles' intersection.
    [Fact]
    public void Draw_NestedClipContainers_IntersectRatherThanReplace()
    {
        ContainerRuntime readableOuter = new();
        readableOuter.X = 0;
        readableOuter.Y = 0;
        readableOuter.Width = 100;
        readableOuter.Height = 100;
        readableOuter.IsRenderTarget = true;

        // Clips to the top half: [0,100] x [0,50].
        ContainerRuntime outerClip = new();
        outerClip.X = 0;
        outerClip.Y = 0;
        outerClip.Width = 100;
        outerClip.Height = 50;
        outerClip.ClipsChildren = true;

        // Clips to the left half, taller than outerClip: [0,50] x [0,100].
        ContainerRuntime innerClip = new();
        innerClip.X = 0;
        innerClip.Y = 0;
        innerClip.Width = 50;
        innerClip.Height = 100;
        innerClip.ClipsChildren = true;

        ColoredRectangleRuntime fill = new();
        fill.Width = 100;
        fill.Height = 100;
        fill.Color = new Color((byte)0, (byte)255, (byte)0, (byte)255);
        innerClip.Children.Add(fill);
        outerClip.Children.Add(innerClip);
        readableOuter.Children.Add(outerClip);

        GumService.Default.Root.Children.Add(readableOuter);
        GumService.Default.Root.UpdateLayout();

        BeginDrawing();
        GumService.Default.Draw();
        EndDrawing();

        RenderTexture2D renderTexture = Renderer.Self.TryGetBakedRenderTargetFor(readableOuter)!.Value;
        // Inside both clips' intersection (the top-left quadrant).
        Color intersection = ReadRenderTargetPixel(renderTexture, 25, 25);
        // Inside innerClip's own rect but outside outerClip -- must stay clipped under intersect
        // semantics; a "replace" bug would leave this filled since innerClip's own scissor alone
        // reaches y=100.
        Color belowOuterClip = ReadRenderTargetPixel(renderTexture, 25, 75);

        intersection.G.ShouldBeGreaterThan((byte)200);
        belowOuterClip.A.ShouldBeLessThan((byte)50);

        GumService.Default.Root.Children.Clear();
    }

    // #4154: a container that is both IsRenderTarget and ClipsChildren -- e.g. a scrolling panel
    // rendered to a texture for group opacity. The walk checks IsRenderTarget and
    // composites-then-returns BEFORE ever reaching the ClipsChildren scissor push, so the node's
    // own clip has no effect and its children are never walked outside the bake. Pinned here via a
    // sibling drawn immediately afterward in the SAME bake: if that ordering regressed (the scissor
    // push moved ahead of the composite-and-return, or its pop got skipped on the early-return
    // path), the pushed scissor would leak onto the sibling with nothing to pop it. Draw-call count
    // can't discriminate a scissor leak -- a scissored-away draw is still issued and counted,
    // scissoring is a fragment-level test, not something that suppresses the draw call itself --
    // so this needs pixel readback.
    //
    // This exercises the bake path (BakeRenderTarget builds its subtree through the shared orderer),
    // not the main pass's own IsRenderTarget guard: this harness has no way to read the actual
    // screen framebuffer the main pass draws to, only RenderTexture2D content populated by a bake,
    // and any node nested under an IsRenderTarget ancestor is (by the orderer's own recursion gate)
    // reached via the bake, never via the main pass. Submit's matching guard -- applied
    // uniformly across BeginClip/DrawRenderable/EndClip rather than gated by command kind -- is
    // covered by Draw_TopLevelRenderTargetContainerWithClipsChildren_
    // CompositesOnceWithoutDoubleDrawingChildren below (draw-call count) and by
    // HierarchicalOrdererTests.BuildDrawList_IsRenderTargetNodeWithClipsChildren_
    // StillBracketsButDoesNotRecurse (the orderer contract Submit's guard depends on).
    [Fact]
    public void Draw_NestedRenderTargetContainerWithClipsChildren_DoesNotLeakScissorToFollowingSibling()
    {
        ContainerRuntime readableOuter = new();
        readableOuter.X = 0;
        readableOuter.Y = 0;
        readableOuter.Width = 200;
        readableOuter.Height = 200;
        readableOuter.IsRenderTarget = true;

        ContainerRuntime containerUnderTest = new();
        containerUnderTest.X = 0;
        containerUnderTest.Y = 0;
        containerUnderTest.Width = 40;
        containerUnderTest.Height = 40;
        containerUnderTest.IsRenderTarget = true;
        containerUnderTest.ClipsChildren = true;

        ColoredRectangleRuntime fill = new();
        fill.Width = 40;
        fill.Height = 40;
        fill.Color = new Color((byte)255, (byte)0, (byte)0, (byte)255);
        containerUnderTest.Children.Add(fill);

        // Well outside containerUnderTest's [0,40]x[0,40] rect -- if that rect leaked onto the
        // scissor stack and was never popped, this would be clipped away.
        ColoredRectangleRuntime siblingAfter = new();
        siblingAfter.X = 100;
        siblingAfter.Y = 100;
        siblingAfter.Width = 50;
        siblingAfter.Height = 50;
        siblingAfter.Color = new Color((byte)0, (byte)255, (byte)0, (byte)255);

        readableOuter.Children.Add(containerUnderTest);
        readableOuter.Children.Add(siblingAfter);

        GumService.Default.Root.Children.Add(readableOuter);
        GumService.Default.Root.UpdateLayout();

        BeginDrawing();
        GumService.Default.Draw();
        EndDrawing();

        RenderTexture2D renderTexture = Renderer.Self.TryGetBakedRenderTargetFor(readableOuter)!.Value;
        Color compositedContainer = ReadRenderTargetPixel(renderTexture, 20, 20);
        Color siblingPixel = ReadRenderTargetPixel(renderTexture, 120, 120);

        compositedContainer.R.ShouldBeGreaterThan((byte)200);
        siblingPixel.G.ShouldBeGreaterThan((byte)200);

        GumService.Default.Root.Children.Clear();
    }

    // Divergence 2 (#4155): HierarchicalOrderer recurses into any visible IRenderableIpso child.
    // raylib's child loop tested `child is GraphicalUiElement`, so a raw (non-wrapped) renderable
    // parented directly onto another renderable never drew.
    [Fact]
    public void Draw_NonGraphicalUiElementChild_IsRendered()
    {
        int baseline = DrawAndCountDrawCalls();

        ContainerRuntime container = new();
        container.Width = 100;
        container.Height = 100;

        SolidRectangle rawChild = new();
        rawChild.Width = 50;
        rawChild.Height = 50;
        rawChild.Parent = container;

        container.AddToManagers();
        container.UpdateLayout();

        int withRawChild = DrawAndCountDrawCalls();

        container.RemoveFromManagers();

        (withRawChild - baseline).ShouldBe(1);
    }

    // #4154: raylib's Renderer.Submit (the migrated main-pass walk) special-cases IsRenderTarget
    // uniformly for BeginClip/DrawRenderable/EndClip -- keeping the composite-before-ClipsChildren
    // ordering -- so a TOP-LEVEL container that is both
    // IsRenderTarget and ClipsChildren composites exactly once and never draws its children a
    // second time in the main pass (only once, inside the bake). Unlike the multi-child cull case,
    // draw-call count is reliable here: the bake and the main-pass composite land in different FBO
    // segments (BeginTextureMode/EndTextureMode force a flush before the main pass's BeginMode2D
    // even starts), so a spurious extra main-pass draw of the child cannot coalesce with its bake
    // draw into the same counted slot.
    [Fact]
    public void Draw_TopLevelRenderTargetContainerWithClipsChildren_CompositesOnceWithoutDoubleDrawingChildren()
    {
        int baseline = DrawAndCountDrawCalls();

        ContainerRuntime containerUnderTest = new();
        containerUnderTest.Width = 100;
        containerUnderTest.Height = 100;
        containerUnderTest.IsRenderTarget = true;
        containerUnderTest.ClipsChildren = true;

        ColoredRectangleRuntime child = new();
        child.Width = 50;
        child.Height = 50;
        child.Color = new Color((byte)255, (byte)0, (byte)0, (byte)255);
        containerUnderTest.Children.Add(child);

        GumService.Default.Root.Children.Add(containerUnderTest);
        GumService.Default.Root.UpdateLayout();

        int withContainer = DrawAndCountDrawCalls();

        RenderTexture2D bakedTexture = Renderer.Self.TryGetBakedRenderTargetFor(containerUnderTest)!.Value;
        Color bakedPixel = ReadRenderTargetPixel(bakedTexture, 10, 10);

        // One draw for the child baked into the texture, one for the on-screen composite blit. A
        // regression that also draws the child directly in the main pass would raise this to 3.
        (withContainer - baseline).ShouldBe(2);
        bakedPixel.R.ShouldBeGreaterThan((byte)200);

        GumService.Default.Root.Children.Clear();
    }
}
