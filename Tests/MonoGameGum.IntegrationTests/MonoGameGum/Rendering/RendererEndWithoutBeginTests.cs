using Gum.GueDeriving;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using Shouldly;
using Xunit;

namespace MonoGameGum.IntegrationTests.MonoGameGum.Rendering;

/// <summary>
/// Pins #6952: <see cref="Renderer.End"/> with no matching <see cref="Renderer.Begin"/> throws an
/// <see cref="InvalidOperationException"/> that says so, instead of a LINQ "Sequence contains no elements".
/// </summary>
[Collection(SharedGame.CollectionName)]
public class RendererEndWithoutBeginTests : BaseTestClass
{
    private static RectangleRuntime CreateRectangle()
    {
        RectangleRuntime rectangle = new();
        rectangle.IsFilled = true;
        rectangle.Width = 50;
        rectangle.Height = 50;
        return rectangle;
    }

    [Fact]
    public void End_WithoutBegin_ThrowsInvalidOperationWithClearMessage()
    {
        using MinimalGame game = new();
        game.RunOneFrame();
        Renderer renderer = SystemManagers.Default.Renderer;

        InvalidOperationException exception = Should.Throw<InvalidOperationException>(() => renderer.End());

        exception.Message.ShouldContain("Begin");
        exception.Message.ShouldNotContain("Sequence contains no elements");
    }

    [Fact]
    public void End_CalledTwiceAfterOneBegin_SecondCallThrowsClearMessage()
    {
        using MinimalGame game = new();
        game.RunOneFrame();
        Renderer renderer = SystemManagers.Default.Renderer;

        renderer.Begin();
        renderer.End();

        InvalidOperationException exception = Should.Throw<InvalidOperationException>(() => renderer.End());
        exception.Message.ShouldContain("Begin");
    }

    [Fact]
    public void GumBatchEnd_WithoutBegin_ThrowsClearMessage()
    {
        using MinimalGame game = new();
        game.RunOneFrame();
        GumBatch batch = new(SystemManagers.Default);

        InvalidOperationException exception = Should.Throw<InvalidOperationException>(() => batch.End());

        exception.Message.ShouldContain("Begin");
    }

    [Fact]
    public void NestedBeginEnd_Balanced_DoesNotThrow()
    {
        using MinimalGame game = new();
        game.RunOneFrame();
        Renderer renderer = SystemManagers.Default.Renderer;

        renderer.Begin();
        renderer.Begin();
        renderer.End();
        renderer.End();

        Should.Throw<InvalidOperationException>(() => renderer.End());
    }

    [Fact]
    public void StrayEnd_ThenBalancedFrame_CountsDrawCallsAsCleanFrame()
    {
        using MinimalGame game = new();
        game.RunOneFrame();
        SystemManagers managers = SystemManagers.Default;
        Renderer renderer = managers.Renderer;
        RectangleRuntime rectangle = CreateRectangle();

        managers.Activity(1.0 / 60.0);
        renderer.Begin();
        renderer.Draw(rectangle);
        renderer.End();
        int cleanCount = renderer.RenderStateChangeStatistics.DrawCallCount;
        cleanCount.ShouldBeGreaterThan(0);

        Should.Throw<InvalidOperationException>(() => renderer.End());

        managers.Activity(2.0 / 60.0);
        renderer.Begin();
        renderer.Draw(rectangle);
        renderer.End();

        renderer.RenderStateChangeStatistics.DrawCallCount.ShouldBe(cleanCount);
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
