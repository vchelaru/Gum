using System.Collections.Generic;
using System.Linq;
using Gum.GueDeriving;
using Gum.Wireframe;
using Raylib_cs;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using Shouldly;
using static Raylib_cs.Raylib;

namespace RaylibGum.Tests.Rendering;

/// <summary>
/// Comprehensive end-to-end matrix for the raylib draw-call counter. Drives a real render pass for
/// every renderable type and asserts three properties the metric must hold:
/// <list type="bullet">
/// <item>every renderable type renders without crashing and contributes at least one draw call;</item>
/// <item>multiple identical, adjacent items of a type coalesce — the count does not grow with count;</item>
/// <item>things that genuinely break a batch (distinct textures, clip regions, drop shadows) are
/// counted, and a mixed scene reports a stable count frame-over-frame.</item>
/// </list>
/// Counts are compared as whole-scene totals (each <see cref="CountWith"/> call restores the shared
/// harness state), so the assertions are robust to whatever the test root draws as a baseline.
/// </summary>
public class RendererDrawCallMatrixTests : BaseTestClass
{
    /// <summary>
    /// A frame's draw-call total plus a snapshot of how <see cref="BatchDrawCallCounter"/> arrived
    /// at it (issue #4901 — this matrix flaked once on CI with no way to tell, after the fact,
    /// whether the owned batch banked zero real draw calls for a segment that should have had some,
    /// or never banked at all that frame). Surfaced only in assertion failure messages, so a
    /// recurrence carries this detail in the CI log instead of needing to be reproduced separately.
    /// </summary>
    private readonly record struct CountResult(int Count, string Diagnostics);

    private static CountResult DrawAndCount()
    {
        BeginDrawing();
        GumService.Default.Draw();
        EndDrawing();

        RenderStateChangeStatistics stats = Renderer.Self.RenderStateChangeStatistics;
        string diagnostics =
            $"drawCallCount={stats.DrawCallCount} bankSegments=[{string.Join(",", stats.BankSegments)}] counterActive={Renderer.Self.BatchDrawCallCounter.IsActive} {LayerRenderablesSnapshot()}";
        return new CountResult(stats.DrawCallCount, diagnostics);
    }

    /// <summary>
    /// Third diagnostic layer for issue #4901's recurrence: the third occurrence's <c>textureValid=True</c>
    /// ruled out the texture-readiness-race hypothesis (the texture was GPU-ready), yet the segment's
    /// real draw-call count was still identical between the empty-scene baseline and the single-Sprite
    /// draw - so the sprite either never actually reached a draw call, or its draw call silently merged
    /// into an existing one already counted for the baseline (e.g. a stale/leaked renderable from an
    /// earlier test still on the layer). A first version of this snapshot only walked
    /// <see cref="Layer.Renderables"/> itself, which holds only top-level renderables (Root's own
    /// container, plus anything else attached with no parent) - a Sprite added as a child of Root
    /// is reached only via <see cref="IRenderableIpso.Children"/>, so that version could never show
    /// it. This version walks the whole tree and reports the total node count, a per-type breakdown,
    /// and every Sprite node's own visibility/resolved rect - so a recurrence shows whether the
    /// sprite's renderable exists at all, and if so, whether it looked drawable.
    /// </summary>
    private static string LayerRenderablesSnapshot()
    {
        List<IRenderableIpso> allNodes = new();
        void Walk(IRenderableIpso node)
        {
            allNodes.Add(node);
            foreach (IRenderableIpso child in node.Children)
            {
                Walk(child);
            }
        }
        foreach (IRenderableIpso topLevel in Renderer.Self.Layers[0].Renderables)
        {
            Walk(topLevel);
        }

        var typeCounts = allNodes
            .GroupBy(r => r.GetType().Name)
            .Select(g => $"{g.Key}x{g.Count()}");
        var spriteDetails = allNodes
            .Where(r => r.GetType().Name == nameof(global::Gum.Renderables.Sprite))
            .Select(r => $"Sprite(V={r.Visible},X={r.GetAbsoluteX():0.##},Y={r.GetAbsoluteY():0.##},W={r.Width:0.##},H={r.Height:0.##})");
        return $"treeNodeCount={allNodes.Count} treeTypes=[{string.Join(",", typeCounts)}] sprites=[{string.Join(",", spriteDetails)}]";
    }

    /// <summary>
    /// Adds the given items as adjacent children of the test root, renders one frame, returns the
    /// draw-call count (plus a diagnostic snapshot, see <see cref="CountResult"/>), then clears them
    /// so the next call starts from a clean (zero) baseline.
    /// Root children are used (rather than AddToManagers) because <c>BaseTestClass</c> clears them
    /// and they reliably detach — giving deterministic isolation between measurements.
    /// </summary>
    private static CountResult CountWith(params GraphicalUiElement[] items)
    {
        foreach (GraphicalUiElement item in items)
        {
            GumService.Default.Root.Children.Add(item);
        }
        GumService.Default.Root.UpdateLayout();

        CountResult result = DrawAndCount();

        GumService.Default.Root.Children.Clear();
        return result;
    }

    /// <summary>Formats both sides of a count comparison's diagnostics for an assertion failure message.</summary>
    private static string Diagnostics(CountResult lhs, CountResult rhs) =>
        $"lhs: {lhs.Diagnostics} | rhs: {rhs.Diagnostics}";

    /// <summary>
    /// Next diagnostic layer for issue #4901's recurrence: the first occurrence's <c>BankSegments</c>/
    /// <c>IsActive</c> diagnostics (added by #4902) showed the counter armed and banking correctly,
    /// but the single-Sprite/-NineSlice segment recorded zero real draw calls despite drawing the
    /// same texture that later coalesces fine across three draws - narrowing the open hypothesis to
    /// the texture itself not being GPU-ready for that first draw (a llvmpipe-only race between
    /// <see cref="LoadTextureFromImage"/> and the same-frame draw call). <c>IsTextureValid</c> checked
    /// immediately before the draw settles that hypothesis one way or the other on the next occurrence,
    /// instead of leaving it open a third time.
    /// </summary>
    private static string TextureReadiness(Texture2D texture) => $"textureValid={IsTextureValid(texture)}";

    private static Texture2D CreateTexture()
    {
        Image image = GenImageColor(4, 4, Color.White);
        Texture2D texture = LoadTextureFromImage(image);
        UnloadImage(image);
        return texture;
    }

    private static ColoredRectangleRuntime ColoredRect() => new() { Width = 30, Height = 30 };
    private static SpriteRuntime Sprite(Texture2D texture) => new() { Width = 30, Height = 30, Texture = texture };
    private static NineSliceRuntime NineSlice(Texture2D texture) => new() { Width = 30, Height = 30, Texture = texture };
    private static TextRuntime Text(string value = "Hello") => new() { Text = value };
    private static CircleRuntime Circle() => new() { Radius = 15 };
    private static RectangleRuntime RectangleShape() => new() { Width = 30, Height = 30 };
    private static PolygonRuntime Polygon() => new();

    // ---- Every type renders, and multiples of the same type coalesce (no count growth). ----

    [Fact]
    public void ColoredRectangle_DrawsAndMultiplesCoalesce()
    {
        CountResult baseline = CountWith();
        CountResult one = CountWith(ColoredRect());
        CountResult three = CountWith(ColoredRect(), ColoredRect(), ColoredRect());

        one.Count.ShouldBeGreaterThan(baseline.Count, Diagnostics(baseline, one));
        three.Count.ShouldBe(one.Count, Diagnostics(one, three));
    }

    [Fact]
    public void Sprite_DrawsAndMultiplesWithSameTextureCoalesce()
    {
        Texture2D texture = CreateTexture();

        CountResult baseline = CountWith();
        string readinessBeforeOne = TextureReadiness(texture);
        CountResult one = CountWith(Sprite(texture));
        CountResult three = CountWith(Sprite(texture), Sprite(texture), Sprite(texture));

        one.Count.ShouldBeGreaterThan(baseline.Count, $"{Diagnostics(baseline, one)} | {readinessBeforeOne}");
        three.Count.ShouldBe(one.Count, Diagnostics(one, three));

        UnloadTexture(texture);
    }

    [Fact]
    public void NineSlice_DrawsAndMultiplesWithSameTextureCoalesce()
    {
        Texture2D texture = CreateTexture();

        CountResult baseline = CountWith();
        string readinessBeforeOne = TextureReadiness(texture);
        CountResult one = CountWith(NineSlice(texture));
        CountResult three = CountWith(NineSlice(texture), NineSlice(texture), NineSlice(texture));

        one.Count.ShouldBeGreaterThan(baseline.Count, $"{Diagnostics(baseline, one)} | {readinessBeforeOne}");
        three.Count.ShouldBe(one.Count, Diagnostics(one, three));

        UnloadTexture(texture);
    }

    [Fact]
    public void Text_DrawsAndMultiplesWithSameFontCoalesce()
    {
        CountResult baseline = CountWith();
        CountResult one = CountWith(Text());
        CountResult three = CountWith(Text(), Text(), Text());

        one.Count.ShouldBeGreaterThan(baseline.Count, Diagnostics(baseline, one));
        three.Count.ShouldBe(one.Count, Diagnostics(one, three));
    }

    [Fact]
    public void Circle_DrawsAndMultiplesCoalesce()
    {
        CountResult baseline = CountWith();
        CountResult one = CountWith(Circle());
        CountResult three = CountWith(Circle(), Circle(), Circle());

        one.Count.ShouldBeGreaterThan(baseline.Count, Diagnostics(baseline, one));
        three.Count.ShouldBe(one.Count, Diagnostics(one, three));
    }

    [Fact]
    public void RectangleShape_DrawsAndMultiplesCoalesce()
    {
        CountResult baseline = CountWith();
        CountResult one = CountWith(RectangleShape());
        CountResult three = CountWith(RectangleShape(), RectangleShape(), RectangleShape());

        one.Count.ShouldBeGreaterThan(baseline.Count, Diagnostics(baseline, one));
        three.Count.ShouldBe(one.Count, Diagnostics(one, three));
    }

    [Fact]
    public void Polygon_DrawsAndMultiplesCoalesce()
    {
        CountResult baseline = CountWith();
        CountResult one = CountWith(Polygon());
        CountResult three = CountWith(Polygon(), Polygon(), Polygon());

        one.Count.ShouldBeGreaterThan(baseline.Count, Diagnostics(baseline, one));
        three.Count.ShouldBe(one.Count, Diagnostics(one, three));
    }

    // ---- A real batch break (distinct textures) DOES increase the count. ----

    [Fact]
    public void Sprites_WithDistinctTextures_IncreaseCountBeyondSharedTexture()
    {
        Texture2D textureA = CreateTexture();
        Texture2D textureB = CreateTexture();
        Texture2D textureC = CreateTexture();

        CountResult shared = CountWith(Sprite(textureA), Sprite(textureA), Sprite(textureA));
        CountResult distinct = CountWith(Sprite(textureA), Sprite(textureB), Sprite(textureC));

        distinct.Count.ShouldBeGreaterThan(shared.Count, Diagnostics(shared, distinct));

        UnloadTexture(textureA);
        UnloadTexture(textureB);
        UnloadTexture(textureC);
    }

    // ---- Blend mode: a blended sprite wraps its draw in BeginBlendMode/EndBlendMode (both flush);
    //      the draw must still be banked (without routing those flushes it would be lost). Every
    //      Blend value is covered here (not just Additive) because Replace/ReplaceAlpha/
    //      SubtractAlpha/MinAlpha (issue #3470) now route through the separate-factors
    //      BeginBlendMode(Blend) overload instead of the simple BlendMode one, and a regression
    //      that skipped the counted wrapper for that path would only show up on those values. ----

    [Theory]
    [InlineData(global::Gum.RenderingLibrary.Blend.Normal)]
    [InlineData(global::Gum.RenderingLibrary.Blend.Additive)]
    [InlineData(global::Gum.RenderingLibrary.Blend.Replace)]
    [InlineData(global::Gum.RenderingLibrary.Blend.ReplaceAlpha)]
    [InlineData(global::Gum.RenderingLibrary.Blend.SubtractAlpha)]
    [InlineData(global::Gum.RenderingLibrary.Blend.MinAlpha)]
    public void BlendedSprite_RendersAndIsCountedAcrossBlendFlushes(global::Gum.RenderingLibrary.Blend blend)
    {
        Texture2D texture = CreateTexture();

        CountResult baseline = CountWith();
        CountResult blended = CountWith(BlendedSprite(texture, blend));

        blended.Count.ShouldBeGreaterThan(baseline.Count, Diagnostics(baseline, blended));

        UnloadTexture(texture);
    }

    private static SpriteRuntime BlendedSprite(Texture2D texture, global::Gum.RenderingLibrary.Blend blend)
    {
        SpriteRuntime sprite = Sprite(texture);
        sprite.Blend = blend;
        return sprite;
    }

    // ---- Clip regions: a clipped child is still counted across the scissor flushes. ----

    [Fact]
    public void ClippedContainer_CountsChildrenAndIdenticalChildrenCoalesce()
    {
        CountResult baseline = CountWith();
        CountResult oneChild = CountWith(ClippedContainerWith(1));
        CountResult threeChildren = CountWith(ClippedContainerWith(3));

        oneChild.Count.ShouldBeGreaterThan(baseline.Count, Diagnostics(baseline, oneChild));
        threeChildren.Count.ShouldBe(oneChild.Count, Diagnostics(oneChild, threeChildren));
    }

    private static ContainerRuntime ClippedContainerWith(int childCount)
    {
        ContainerRuntime container = new() { Width = 200, Height = 200 };
        container.ClipsChildren = true;
        for (int i = 0; i < childCount; i++)
        {
            container.Children.Add(ColoredRect());
        }
        return container;
    }

    // ---- Drop shadows: the render-target + shader path renders without crashing and is counted. ----

    [Fact]
    public void DropShadowShape_RendersWithoutCrashingAndIsCounted()
    {
        CountResult baseline = CountWith();
        CountResult withShadow = CountWith(ShadowedRectangle());

        withShadow.Count.ShouldBeGreaterThan(baseline.Count, Diagnostics(baseline, withShadow));
    }

    [Fact]
    public void MultipleDropShadowShapes_RenderWithoutCrashing()
    {
        // Each shadow runs its own offscreen render-target + shader passes. The point here is
        // crash-safety of repeated render-target switches inside the owned batch, plus that the
        // count stays finite and sensible.
        CountResult count = CountWith(ShadowedRectangle(), ShadowedRectangle(), ShadowedRectangle());

        count.Count.ShouldBeGreaterThan(0, count.Diagnostics);
    }

    private static RectangleRuntime ShadowedRectangle()
    {
        RectangleRuntime rectangle = new() { Width = 40, Height = 40 };
        rectangle.HasDropshadow = true;
        rectangle.DropshadowBlur = 5;
        rectangle.DropshadowAlpha = 255;
        return rectangle;
    }

    // ---- Mix and match: a rich scene renders without crashing and reports a stable count. ----

    [Fact]
    public void MixedScene_RendersWithoutCrashingAndCountIsStableAcrossFrames()
    {
        Texture2D texture = CreateTexture();

        ContainerRuntime root = new() { Width = 400, Height = 400 };
        root.Children.Add(Sprite(texture));
        root.Children.Add(Sprite(texture));            // adjacent identical sprite (coalesces)
        root.Children.Add(Text("Mixed"));
        root.Children.Add(ColoredRect());
        root.Children.Add(Circle());
        root.Children.Add(RectangleShape());
        root.Children.Add(Polygon());

        ContainerRuntime clip = new() { Width = 100, Height = 100 };
        clip.ClipsChildren = true;
        clip.Children.Add(ColoredRect());
        clip.Children.Add(ColoredRect());
        root.Children.Add(clip);

        root.Children.Add(ShadowedRectangle());

        GumService.Default.Root.Children.Add(root);
        GumService.Default.Root.UpdateLayout();

        CountResult firstFrame = DrawAndCount();
        CountResult secondFrame = DrawAndCount();

        GumService.Default.Root.Children.Clear();
        UnloadTexture(texture);

        firstFrame.Count.ShouldBeGreaterThan(0, firstFrame.Diagnostics);
        // Per-frame Reset means the count must not accumulate frame-over-frame.
        secondFrame.Count.ShouldBe(firstFrame.Count, Diagnostics(firstFrame, secondFrame));
    }

    [Fact]
    public void MixedScene_AddingAnIdenticalAdjacentItem_DoesNotIncreaseCount()
    {
        Texture2D texture = CreateTexture();

        CountResult withOneSprite = CountMixedScene(texture, spriteCount: 1);
        CountResult withThreeSprites = CountMixedScene(texture, spriteCount: 3);

        // The extra sprites are adjacent and share the texture, so they coalesce: the richer scene
        // costs the same number of draw calls as the leaner one.
        withThreeSprites.Count.ShouldBe(withOneSprite.Count, Diagnostics(withOneSprite, withThreeSprites));

        UnloadTexture(texture);
    }

    private static CountResult CountMixedScene(Texture2D texture, int spriteCount)
    {
        ContainerRuntime root = new() { Width = 400, Height = 400 };
        for (int i = 0; i < spriteCount; i++)
        {
            root.Children.Add(Sprite(texture));
        }
        root.Children.Add(Text("Mixed"));
        root.Children.Add(Circle());

        GumService.Default.Root.Children.Add(root);
        GumService.Default.Root.UpdateLayout();

        CountResult result = DrawAndCount();

        GumService.Default.Root.Children.Clear();
        return result;
    }
}
