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
    private static int DrawAndCount()
    {
        BeginDrawing();
        GumService.Default.Draw();
        EndDrawing();
        return Renderer.Self.RenderStateChangeStatistics.DrawCallCount;
    }

    /// <summary>
    /// Adds the given items as adjacent children of the test root, renders one frame, returns the
    /// draw-call count, then clears them so the next call starts from a clean (zero) baseline.
    /// Root children are used (rather than AddToManagers) because <c>BaseTestClass</c> clears them
    /// and they reliably detach — giving deterministic isolation between measurements.
    /// </summary>
    private static int CountWith(params GraphicalUiElement[] items)
    {
        foreach (GraphicalUiElement item in items)
        {
            GumService.Default.Root.Children.Add(item);
        }
        GumService.Default.Root.UpdateLayout();

        int count = DrawAndCount();

        GumService.Default.Root.Children.Clear();
        return count;
    }

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
        int baseline = CountWith();
        int one = CountWith(ColoredRect());
        int three = CountWith(ColoredRect(), ColoredRect(), ColoredRect());

        one.ShouldBeGreaterThan(baseline);
        three.ShouldBe(one);
    }

    [Fact]
    public void Sprite_DrawsAndMultiplesWithSameTextureCoalesce()
    {
        Texture2D texture = CreateTexture();

        int baseline = CountWith();
        int one = CountWith(Sprite(texture));
        int three = CountWith(Sprite(texture), Sprite(texture), Sprite(texture));

        one.ShouldBeGreaterThan(baseline);
        three.ShouldBe(one);

        UnloadTexture(texture);
    }

    [Fact]
    public void NineSlice_DrawsAndMultiplesWithSameTextureCoalesce()
    {
        Texture2D texture = CreateTexture();

        int baseline = CountWith();
        int one = CountWith(NineSlice(texture));
        int three = CountWith(NineSlice(texture), NineSlice(texture), NineSlice(texture));

        one.ShouldBeGreaterThan(baseline);
        three.ShouldBe(one);

        UnloadTexture(texture);
    }

    [Fact]
    public void Text_DrawsAndMultiplesWithSameFontCoalesce()
    {
        int baseline = CountWith();
        int one = CountWith(Text());
        int three = CountWith(Text(), Text(), Text());

        one.ShouldBeGreaterThan(baseline);
        three.ShouldBe(one);
    }

    [Fact]
    public void Circle_DrawsAndMultiplesCoalesce()
    {
        int baseline = CountWith();
        int one = CountWith(Circle());
        int three = CountWith(Circle(), Circle(), Circle());

        one.ShouldBeGreaterThan(baseline);
        three.ShouldBe(one);
    }

    [Fact]
    public void RectangleShape_DrawsAndMultiplesCoalesce()
    {
        int baseline = CountWith();
        int one = CountWith(RectangleShape());
        int three = CountWith(RectangleShape(), RectangleShape(), RectangleShape());

        one.ShouldBeGreaterThan(baseline);
        three.ShouldBe(one);
    }

    [Fact]
    public void Polygon_DrawsAndMultiplesCoalesce()
    {
        int baseline = CountWith();
        int one = CountWith(Polygon());
        int three = CountWith(Polygon(), Polygon(), Polygon());

        one.ShouldBeGreaterThan(baseline);
        three.ShouldBe(one);
    }

    // ---- A real batch break (distinct textures) DOES increase the count. ----

    [Fact]
    public void Sprites_WithDistinctTextures_IncreaseCountBeyondSharedTexture()
    {
        Texture2D textureA = CreateTexture();
        Texture2D textureB = CreateTexture();
        Texture2D textureC = CreateTexture();

        int shared = CountWith(Sprite(textureA), Sprite(textureA), Sprite(textureA));
        int distinct = CountWith(Sprite(textureA), Sprite(textureB), Sprite(textureC));

        distinct.ShouldBeGreaterThan(shared);

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

        int baseline = CountWith();
        int blended = CountWith(BlendedSprite(texture, blend));

        blended.ShouldBeGreaterThan(baseline);

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
        int baseline = CountWith();
        int oneChild = CountWith(ClippedContainerWith(1));
        int threeChildren = CountWith(ClippedContainerWith(3));

        oneChild.ShouldBeGreaterThan(baseline);
        threeChildren.ShouldBe(oneChild);
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
        int baseline = CountWith();
        int withShadow = CountWith(ShadowedRectangle());

        withShadow.ShouldBeGreaterThan(baseline);
    }

    [Fact]
    public void MultipleDropShadowShapes_RenderWithoutCrashing()
    {
        // Each shadow runs its own offscreen render-target + shader passes. The point here is
        // crash-safety of repeated render-target switches inside the owned batch, plus that the
        // count stays finite and sensible.
        int count = CountWith(ShadowedRectangle(), ShadowedRectangle(), ShadowedRectangle());

        count.ShouldBeGreaterThan(0);
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

        int firstFrame = DrawAndCount();
        int secondFrame = DrawAndCount();

        GumService.Default.Root.Children.Clear();
        UnloadTexture(texture);

        firstFrame.ShouldBeGreaterThan(0);
        // Per-frame Reset means the count must not accumulate frame-over-frame.
        secondFrame.ShouldBe(firstFrame);
    }

    [Fact]
    public void MixedScene_AddingAnIdenticalAdjacentItem_DoesNotIncreaseCount()
    {
        Texture2D texture = CreateTexture();

        int withOneSprite = CountMixedScene(texture, spriteCount: 1);
        int withThreeSprites = CountMixedScene(texture, spriteCount: 3);

        // The extra sprites are adjacent and share the texture, so they coalesce: the richer scene
        // costs the same number of draw calls as the leaner one.
        withThreeSprites.ShouldBe(withOneSprite);

        UnloadTexture(texture);
    }

    private static int CountMixedScene(Texture2D texture, int spriteCount)
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

        int count = DrawAndCount();

        GumService.Default.Root.Children.Clear();
        return count;
    }
}
