using System.Collections.Generic;
using System.Linq;
using Gum.GueDeriving;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary;
using RenderingLibrary.Content;
using RenderingLibrary.Graphics;
using Shouldly;
using Xunit;

namespace MonoGameGum.IntegrationTests.MonoGameGum.Rendering;

/// <summary>
/// Pins that <see cref="DrawStateSummary"/> and <see cref="RenderStateChangeStatistics"/> describe the
/// real frame (#6841). Ground truth is <c>GraphicsDevice.Metrics.DrawCount</c>: sprites that alternate
/// between two textures cost exactly one draw call each, so the summary's texture sets must match.
/// </summary>
[Collection(SharedGame.CollectionName)]
public class DrawStateCounterTests : BaseTestClass
{
    private static double _hostTime;

    private static void AdvanceHostFrame()
    {
        _hostTime += 1.0 / 60.0;
        SystemManagers.Default.Activity(_hostTime);
    }

    private static Texture2D CreateTexture(GraphicsDevice device, Color color)
    {
        Texture2D texture = new(device, 1, 1);
        texture.SetData(new[] { color });
        return texture;
    }

    private static void AddAlternating(Texture2D a, Texture2D b, Layer? layer, int count)
    {
        for (int i = 0; i < count; i++)
        {
            SpriteRuntime sprite = new();
            sprite.Texture = i % 2 == 0 ? a : b;
            sprite.Width = 10;
            sprite.Height = 10;
            sprite.Y = i * 10;
            sprite.AddToManagers(SystemManagers.Default, layer);
            sprite.UpdateLayout();
        }
    }

    private static ContainerRuntime CreateContainerWithSprites(Texture2D a, Texture2D b, Layer? layer, int spriteCount, bool clips, bool renderTarget)
    {
        ContainerRuntime container = new();
        container.Width = 64;
        container.Height = 64;
        container.ClipsChildren = clips;
        container.IsRenderTarget = renderTarget;
        for (int i = 0; i < spriteCount; i++)
        {
            SpriteRuntime sprite = new();
            sprite.Texture = i % 2 == 0 ? a : b;
            sprite.Width = 10;
            sprite.Height = 10;
            sprite.Y = i * 10;
            container.AddChild(sprite);
        }
        container.AddToManagers(SystemManagers.Default, layer);
        container.UpdateLayout();
        return container;
    }

    private static ContainerRuntime AddClipContainer(Texture2D a, Texture2D b, Layer? layer, int spriteCount) =>
        CreateContainerWithSprites(a, b, layer, spriteCount, clips: true, renderTarget: false);

    private static ContainerRuntime AddRenderTargetContainer(Texture2D a, Texture2D b, Layer? layer, int spriteCount) =>
        CreateContainerWithSprites(a, b, layer, spriteCount, clips: false, renderTarget: true);

    private static void ShouldHaveDistinctChangeRecords(Renderer renderer)
    {
        List<StateChangeInfoList> lists = new();
        foreach (BeginParameters state in renderer.SpriteRenderer.LastFrameDrawStates)
        {
            lists.Contains(state.ChangeRecord).ShouldBeFalse(
                "two draw-state records share one change list, so their texture sets are counted twice");
            lists.Add(state.ChangeRecord);
        }
    }

    private static long MeasureDrawCalls(GraphicsDevice device, System.Action draw)
    {
        long before = device.Metrics.DrawCount;
        draw();
        return device.Metrics.DrawCount - before;
    }

    private static SpriteRuntime[] CreateAlternatingSprites(Texture2D a, Texture2D b, int count)
    {
        SpriteRuntime[] sprites = new SpriteRuntime[count];
        for (int i = 0; i < count; i++)
        {
            sprites[i] = new SpriteRuntime { Texture = i % 2 == 0 ? a : b, Width = 10, Height = 10 };
        }
        return sprites;
    }

    [Fact]
    public void TextureSetCount_TwoLayers_MatchesRealTextureSwitches()
    {
        using MinimalGame game = new();
        game.RunOneFrame();
        Renderer renderer = SystemManagers.Default.Renderer;
        using Texture2D a = CreateTexture(game.GraphicsDevice, Color.Red);
        using Texture2D b = CreateTexture(game.GraphicsDevice, Color.Blue);
        Layer second = renderer.AddLayer();
        AddAlternating(a, b, renderer.MainLayer, 4);
        AddAlternating(a, b, second, 4);

        renderer.Draw(SystemManagers.Default);

        renderer.RenderStateChangeStatistics.DrawCallCount.ShouldBe(8);
        renderer.GetDrawStateSummary().TextureSetCount.ShouldBe(8);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(5)]
    public void TextureSetCount_NLayers_EqualsRealDrawCalls(int layerCount)
    {
        using MinimalGame game = new();
        game.RunOneFrame();
        Renderer renderer = SystemManagers.Default.Renderer;
        using Texture2D a = CreateTexture(game.GraphicsDevice, Color.Red);
        using Texture2D b = CreateTexture(game.GraphicsDevice, Color.Blue);
        AddAlternating(a, b, renderer.MainLayer, 3);
        int expected = 3;
        for (int i = 1; i < layerCount; i++)
        {
            AddAlternating(a, b, renderer.AddLayer(), 3 + i);
            expected += 3 + i;
        }

        long measured = MeasureDrawCalls(game.GraphicsDevice, () => renderer.Draw(SystemManagers.Default));

        measured.ShouldBe(expected);
        renderer.RenderStateChangeStatistics.DrawCallCount.ShouldBe(expected);
        renderer.GetDrawStateSummary().TextureSetCount.ShouldBe(expected);
    }

    [Fact]
    public void LastFrameDrawStates_TwoLayers_NeverShareChangeLists()
    {
        using MinimalGame game = new();
        game.RunOneFrame();
        Renderer renderer = SystemManagers.Default.Renderer;
        using Texture2D a = CreateTexture(game.GraphicsDevice, Color.Red);
        using Texture2D b = CreateTexture(game.GraphicsDevice, Color.Blue);
        AddClipContainer(a, b, renderer.MainLayer, 4);
        AddClipContainer(a, b, renderer.AddLayer(), 4);

        renderer.Draw(SystemManagers.Default);

        ShouldHaveDistinctChangeRecords(renderer);
    }

    [Fact]
    public void Summary_TwoLayers_IsSumOfTheLayersAlone()
    {
        using MinimalGame game = new();
        game.RunOneFrame();
        Renderer renderer = SystemManagers.Default.Renderer;
        using Texture2D a = CreateTexture(game.GraphicsDevice, Color.Red);
        using Texture2D b = CreateTexture(game.GraphicsDevice, Color.Blue);

        AddClipContainer(a, b, renderer.MainLayer, 4);
        renderer.Draw(SystemManagers.Default);
        DrawStateSummary one = renderer.GetDrawStateSummary();

        AddClipContainer(a, b, renderer.AddLayer(), 4);
        renderer.Draw(SystemManagers.Default);
        DrawStateSummary two = renderer.GetDrawStateSummary();

        two.TextureSetCount.ShouldBe(one.TextureSetCount * 2);
        two.ClipChangeBeginCount.ShouldBe(one.ClipChangeBeginCount * 2);
    }

    [Fact]
    public void Summary_BeginCountEqualsRecordedDrawStates()
    {
        using MinimalGame game = new();
        game.RunOneFrame();
        Renderer renderer = SystemManagers.Default.Renderer;
        using Texture2D a = CreateTexture(game.GraphicsDevice, Color.Red);
        using Texture2D b = CreateTexture(game.GraphicsDevice, Color.Blue);
        AddClipContainer(a, b, renderer.MainLayer, 4);
        AddClipContainer(a, b, renderer.AddLayer(), 4);

        renderer.Draw(SystemManagers.Default);

        DrawStateSummary summary = renderer.GetDrawStateSummary();
        summary.BeginCount.ShouldBe(renderer.SpriteRenderer.LastFrameDrawStates.Count());
        (summary.InitialBeginCount + summary.ClipChangeBeginCount + summary.StateChangeBeginCount).ShouldBe(summary.BeginCount);
    }

    [Fact]
    public void Summary_SameSceneDrawnRepeatedly_IsStable()
    {
        using MinimalGame game = new();
        game.RunOneFrame();
        Renderer renderer = SystemManagers.Default.Renderer;
        using Texture2D a = CreateTexture(game.GraphicsDevice, Color.Red);
        using Texture2D b = CreateTexture(game.GraphicsDevice, Color.Blue);
        AddClipContainer(a, b, renderer.MainLayer, 4);
        AddClipContainer(a, b, renderer.AddLayer(), 4);

        renderer.Draw(SystemManagers.Default);
        renderer.Draw(SystemManagers.Default);
        DrawStateSummary first = renderer.GetDrawStateSummary();
        for (int i = 0; i < 4; i++)
        {
            renderer.Draw(SystemManagers.Default);
        }
        DrawStateSummary last = renderer.GetDrawStateSummary();

        last.BeginCount.ShouldBe(first.BeginCount);
        last.TextureSetCount.ShouldBe(first.TextureSetCount);
        last.ClipChangeBeginCount.ShouldBe(first.ClipChangeBeginCount);
    }

    [Fact]
    public void Summary_RemovingContent_DropsCounts()
    {
        using MinimalGame game = new();
        game.RunOneFrame();
        Renderer renderer = SystemManagers.Default.Renderer;
        using Texture2D a = CreateTexture(game.GraphicsDevice, Color.Red);
        using Texture2D b = CreateTexture(game.GraphicsDevice, Color.Blue);
        AddAlternating(a, b, renderer.MainLayer, 4);
        ContainerRuntime removable = AddClipContainer(a, b, renderer.AddLayer(), 4);
        renderer.Draw(SystemManagers.Default);
        renderer.GetDrawStateSummary().TextureSetCount.ShouldBe(8);

        removable.RemoveFromManagers();
        renderer.Draw(SystemManagers.Default);

        renderer.GetDrawStateSummary().TextureSetCount.ShouldBe(4);
    }

    [Fact]
    public void DrawCallCount_RenderTargetBake_CountsEachDrawCallOnce()
    {
        using MinimalGame game = new();
        game.RunOneFrame();
        Renderer renderer = SystemManagers.Default.Renderer;
        using Texture2D a = CreateTexture(game.GraphicsDevice, Color.Red);
        using Texture2D b = CreateTexture(game.GraphicsDevice, Color.Blue);
        AddRenderTargetContainer(a, b, renderer.MainLayer, 4);
        AddAlternating(a, b, renderer.MainLayer, 4);
        renderer.Draw(SystemManagers.Default); // warm up the target cache

        long measured = MeasureDrawCalls(game.GraphicsDevice, () => renderer.Draw(SystemManagers.Default));

        measured.ShouldBeGreaterThan(8);
        renderer.RenderStateChangeStatistics.DrawCallCount.ShouldBe((int)measured);
    }

    [Fact]
    public void DrawCallCount_RenderTargetBakeOnSecondLayer_CountsEachDrawCallOnce()
    {
        using MinimalGame game = new();
        game.RunOneFrame();
        Renderer renderer = SystemManagers.Default.Renderer;
        using Texture2D a = CreateTexture(game.GraphicsDevice, Color.Red);
        using Texture2D b = CreateTexture(game.GraphicsDevice, Color.Blue);
        AddAlternating(a, b, renderer.MainLayer, 4);
        AddRenderTargetContainer(a, b, renderer.AddLayer(), 4);
        renderer.Draw(SystemManagers.Default);

        long measured = MeasureDrawCalls(game.GraphicsDevice, () => renderer.Draw(SystemManagers.Default));

        renderer.RenderStateChangeStatistics.DrawCallCount.ShouldBe((int)measured);
    }

    [Fact]
    public void Summary_RenderTargetBake_KeepsTheLayerBegins()
    {
        using MinimalGame game = new();
        game.RunOneFrame();
        Renderer renderer = SystemManagers.Default.Renderer;
        using Texture2D a = CreateTexture(game.GraphicsDevice, Color.Red);
        using Texture2D b = CreateTexture(game.GraphicsDevice, Color.Blue);
        AddAlternating(a, b, renderer.MainLayer, 4);
        renderer.Draw(SystemManagers.Default);
        int withoutTarget = renderer.GetDrawStateSummary().TextureSetCount;

        AddRenderTargetContainer(a, b, renderer.AddLayer(), 4);
        renderer.Draw(SystemManagers.Default);

        // The bake's own 4 sets are real texture sets on top of the first layer's.
        renderer.GetDrawStateSummary().TextureSetCount.ShouldBeGreaterThanOrEqualTo(withoutTarget + 4);
    }

    [Fact]
    public void PerLayerDraw_SameHostFrame_AccumulatesAcrossLayers()
    {
        using MinimalGame game = new();
        game.RunOneFrame();
        SystemManagers managers = SystemManagers.Default;
        Renderer renderer = managers.Renderer;
        using Texture2D a = CreateTexture(game.GraphicsDevice, Color.Red);
        using Texture2D b = CreateTexture(game.GraphicsDevice, Color.Blue);
        Layer second = renderer.AddLayer();
        AddAlternating(a, b, renderer.MainLayer, 4);
        AddAlternating(a, b, second, 4);
        AdvanceHostFrame();

        renderer.Draw(managers, renderer.MainLayer);
        renderer.Draw(managers, second);
        renderer.ForceEnd();

        renderer.GetDrawStateSummary().TextureSetCount.ShouldBe(8);
        ShouldHaveDistinctChangeRecords(renderer);
    }

    [Fact]
    public void PerLayerDraw_NextHostFrame_DescribesOnlyThatFrame()
    {
        using MinimalGame game = new();
        game.RunOneFrame();
        SystemManagers managers = SystemManagers.Default;
        Renderer renderer = managers.Renderer;
        using Texture2D a = CreateTexture(game.GraphicsDevice, Color.Red);
        using Texture2D b = CreateTexture(game.GraphicsDevice, Color.Blue);
        Layer second = renderer.AddLayer();
        AddAlternating(a, b, renderer.MainLayer, 4);
        AddAlternating(a, b, second, 4);

        for (int frame = 0; frame < 6; frame++)
        {
            AdvanceHostFrame();
            renderer.Draw(managers, renderer.MainLayer);
            renderer.Draw(managers, second);
            renderer.ForceEnd();
        }

        renderer.GetDrawStateSummary().TextureSetCount.ShouldBe(8);
        renderer.SpriteRenderer.LastFrameDrawStates.Count().ShouldBeLessThan(10);
    }

    [Fact]
    public void PerLayerDraw_BakeFirstAppearsOnSecondLayer_KeepsFirstLayersCounts()
    {
        using MinimalGame game = new();
        game.RunOneFrame();
        SystemManagers managers = SystemManagers.Default;
        Renderer renderer = managers.Renderer;
        using Texture2D a = CreateTexture(game.GraphicsDevice, Color.Red);
        using Texture2D b = CreateTexture(game.GraphicsDevice, Color.Blue);
        Layer second = renderer.AddLayer();
        AddAlternating(a, b, renderer.MainLayer, 4);
        AdvanceHostFrame();
        renderer.Draw(managers, renderer.MainLayer);

        // Content that appears after the first layer was drawn in the same host frame.
        AddAlternating(a, b, second, 4);
        AddRenderTargetContainer(a, b, second, 2);
        renderer.Draw(managers, second);
        renderer.ForceEnd();

        // First layer 4 + second layer 4 + the target's bake (2) and composite (1).
        renderer.GetDrawStateSummary().TextureSetCount.ShouldBeGreaterThanOrEqualTo(8 + 2);
    }

    [Fact]
    public void ImmediateMode_ThreeCyclesInOneHostFrame_CountEveryTextureSetOnce()
    {
        using MinimalGame game = new();
        game.RunOneFrame();
        Renderer renderer = SystemManagers.Default.Renderer;
        using Texture2D a = CreateTexture(game.GraphicsDevice, Color.Red);
        using Texture2D b = CreateTexture(game.GraphicsDevice, Color.Blue);
        SpriteRuntime[] sprites = CreateAlternatingSprites(a, b, 4);
        AdvanceHostFrame();

        for (int cycle = 0; cycle < 3; cycle++)
        {
            renderer.Begin();
            foreach (SpriteRuntime sprite in sprites)
            {
                renderer.Draw(sprite);
            }
            renderer.End();
        }

        renderer.GetDrawStateSummary().TextureSetCount.ShouldBe(12);
        renderer.RenderStateChangeStatistics.DrawCallCount.ShouldBe(12);
        ShouldHaveDistinctChangeRecords(renderer);
    }

    [Fact]
    public void ImmediateMode_NextHostFrame_DescribesOnlyThatFrame()
    {
        using MinimalGame game = new();
        game.RunOneFrame();
        Renderer renderer = SystemManagers.Default.Renderer;
        using Texture2D a = CreateTexture(game.GraphicsDevice, Color.Red);
        using Texture2D b = CreateTexture(game.GraphicsDevice, Color.Blue);
        SpriteRuntime[] sprites = CreateAlternatingSprites(a, b, 4);

        for (int frame = 0; frame < 5; frame++)
        {
            AdvanceHostFrame();
            for (int cycle = 0; cycle < 2; cycle++)
            {
                renderer.Begin();
                foreach (SpriteRuntime sprite in sprites)
                {
                    renderer.Draw(sprite);
                }
                renderer.End();
            }
        }

        renderer.GetDrawStateSummary().TextureSetCount.ShouldBe(8);
    }

    [Fact]
    public void ImmediateMode_BakeBeforeBegin_CountsEachDrawCallOnce()
    {
        using MinimalGame game = new();
        game.RunOneFrame();
        Renderer renderer = SystemManagers.Default.Renderer;
        using Texture2D a = CreateTexture(game.GraphicsDevice, Color.Red);
        using Texture2D b = CreateTexture(game.GraphicsDevice, Color.Blue);
        ContainerRuntime target = AddRenderTargetContainer(a, b, null, 4);
        IRenderableIpso root = (IRenderableIpso)target.RenderableComponent!;
        AdvanceHostFrame();

        long measured = MeasureDrawCalls(game.GraphicsDevice, () =>
        {
            renderer.PreRenderRoots(new[] { root });
            renderer.Begin();
            renderer.Draw(root);
            renderer.End();
        });

        renderer.RenderStateChangeStatistics.DrawCallCount.ShouldBe((int)measured);
    }

    [Fact]
    public void DrawTwiceWithoutActivity_DoesNotAccumulateTextureSets()
    {
        using MinimalGame game = new();
        game.RunOneFrame();
        Renderer renderer = SystemManagers.Default.Renderer;
        using Texture2D a = CreateTexture(game.GraphicsDevice, Color.Red);
        using Texture2D b = CreateTexture(game.GraphicsDevice, Color.Blue);
        AddAlternating(a, b, renderer.MainLayer, 4);
        AddAlternating(a, b, renderer.AddLayer(), 4);

        renderer.Draw(SystemManagers.Default);
        renderer.Draw(SystemManagers.Default);

        renderer.GetDrawStateSummary().TextureSetCount.ShouldBe(8);
    }

    [Fact]
    public void ShapeBatchBeginCount_ResetsEachFrame_WithRenderTargetBake()
    {
        using MinimalGame game = new();
        game.RunOneFrame();
        Renderer renderer = SystemManagers.Default.Renderer;
        using Texture2D a = CreateTexture(game.GraphicsDevice, Color.Red);
        using Texture2D b = CreateTexture(game.GraphicsDevice, Color.Blue);
        AddRenderTargetContainer(a, b, renderer.MainLayer, 2);

        renderer.RenderStateChangeStatistics.RecordShapeBatchBegin();
        renderer.Draw(SystemManagers.Default);

        renderer.GetDrawStateSummary().ShapeBatchBeginCount.ShouldBe(0);
    }

    [Fact]
    public void ImmediateMode_NestedBeginEnd_CountsEachDrawCallOnce()
    {
        using MinimalGame game = new();
        game.RunOneFrame();
        Renderer renderer = SystemManagers.Default.Renderer;
        using Texture2D a = CreateTexture(game.GraphicsDevice, Color.Red);
        using Texture2D b = CreateTexture(game.GraphicsDevice, Color.Blue);
        SpriteRuntime[] sprites = CreateAlternatingSprites(a, b, 2);
        AdvanceHostFrame();

        long measured = MeasureDrawCalls(game.GraphicsDevice, () =>
        {
            renderer.Begin();
            renderer.Draw(sprites[0]);
            renderer.Begin();
            renderer.Draw(sprites[1]);
            renderer.End();
            renderer.Draw(sprites[0]);
            renderer.End();
        });

        renderer.RenderStateChangeStatistics.DrawCallCount.ShouldBe((int)measured);
    }

    [Fact]
    public void ImmediateMode_NeverAdvancingHostFrame_KeepsRecordsBounded()
    {
        using MinimalGame game = new();
        game.RunOneFrame();
        Renderer renderer = SystemManagers.Default.Renderer;
        using Texture2D a = CreateTexture(game.GraphicsDevice, Color.Red);
        SpriteRuntime sprite = new() { Texture = a, Width = 10, Height = 10 };
        AdvanceHostFrame();

        for (int i = 0; i < 40000; i++)
        {
            renderer.Begin();
            renderer.Draw(sprite);
            renderer.End();
        }

        renderer.SpriteRenderer.LastFrameDrawStates.Count().ShouldBeLessThan(70000);
    }

    [Fact]
    public void PerLayerDraw_NeverAdvancingHostFrame_KeepsRecordsBounded()
    {
        using MinimalGame game = new();
        game.RunOneFrame();
        SystemManagers managers = SystemManagers.Default;
        Renderer renderer = managers.Renderer;
        using Texture2D a = CreateTexture(game.GraphicsDevice, Color.Red);
        using Texture2D b = CreateTexture(game.GraphicsDevice, Color.Blue);
        AddClipContainer(a, b, renderer.MainLayer, 2);
        AdvanceHostFrame();

        for (int i = 0; i < 20000; i++)
        {
            renderer.Draw(managers, renderer.MainLayer);
        }

        renderer.SpriteRenderer.LastFrameDrawStates.Count().ShouldBeLessThan(70000);
    }

    [Fact]
    public void ImmediateMode_LargeSingleFrame_ReportsEveryBegin()
    {
        using MinimalGame game = new();
        game.RunOneFrame();
        Renderer renderer = SystemManagers.Default.Renderer;
        using Texture2D a = CreateTexture(game.GraphicsDevice, Color.Red);
        SpriteRuntime sprite = new() { Texture = a, Width = 10, Height = 10 };
        AdvanceHostFrame();

        for (int i = 0; i < 5000; i++)
        {
            renderer.Begin();
            renderer.Draw(sprite);
            renderer.End();
        }

        renderer.GetDrawStateSummary().TextureSetCount.ShouldBe(5000);
    }

    [Fact]
    public void ImmediateMode_EndWithoutBegin_DoesNotThrow()
    {
        using MinimalGame game = new();
        game.RunOneFrame();
        Renderer renderer = SystemManagers.Default.Renderer;
        AdvanceHostFrame();

        Should.NotThrow(() => renderer.End());
    }

    private sealed class MinimalGame : SharedGameLease
    {
        protected override void OnInitialize() => global::Gum.GumService.Default.Initialize(Game, global::Gum.Forms.DefaultVisualsVersion.V3);

        protected override void OnDispose()
        {
            if (global::Gum.GumService.Default.IsInitialized)
            {
                global::Gum.GumService.Default.Uninitialize();
            }
        }
    }
}
