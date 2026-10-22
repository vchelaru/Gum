using Gum;
using Gum.GueDeriving;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using Shouldly;
using Xunit;

namespace MonoGameGum.IntegrationTests.MonoGameGum.Rendering;

/// <summary>
/// Issue #6461: baking an IsRenderTarget container switches to its render target and back. When the
/// target Gum switches back to (the back buffer, or the user render target bound before the draw) uses
/// RenderTargetUsage.DiscardContents, the graphics device clears it on the switch and everything drawn
/// before Gum in the frame disappears. Gum does not change that setting; it reports it once, through
/// the same channel as its other runtime warnings (PropertyAssignmentError, mirrored to Console.Error).
/// </summary>
[Collection(SharedGame.CollectionName)]
public class RenderTargetDiscardContentsWarningTests : BaseTestClass
{
    private const int Size = 64;

    private static double _hostTime;

    [Fact]
    public void BackBufferDiscardContents_WithRenderTargetContainer_WarnsOnce()
    {
        using MinimalGame game = new();
        game.RunOneFrame();
        game.GraphicsDevice.PresentationParameters.RenderTargetUsage.ShouldBe(RenderTargetUsage.DiscardContents);

        List<string> warnings = DrawCapturingWarnings(game.GraphicsDevice, containerCount: 1, redraws: 2, target: null);

        warnings.Count.ShouldBe(1);
        warnings[0].ShouldContain("IsRenderTarget");
        warnings[0].ShouldContain("RenderTargetUsage.PreserveContents");
        warnings[0].ShouldContain("PreparingDeviceSettings");
    }

    [Fact]
    public void BackBufferPreserveContents_DoesNotWarn()
    {
        using MinimalGame game = new();
        game.RunOneFrame();

        PresentationParameters presentation = game.GraphicsDevice.PresentationParameters;
        RenderTargetUsage original = presentation.RenderTargetUsage;
        presentation.RenderTargetUsage = RenderTargetUsage.PreserveContents;
        try
        {
            DrawCapturingWarnings(game.GraphicsDevice, containerCount: 1, redraws: 1, target: null).ShouldBeEmpty();
        }
        finally
        {
            presentation.RenderTargetUsage = original;
        }
    }

    [Fact]
    public void BackBufferPlatformContents_DoesNotWarn()
    {
        using MinimalGame game = new();
        game.RunOneFrame();

        PresentationParameters presentation = game.GraphicsDevice.PresentationParameters;
        RenderTargetUsage original = presentation.RenderTargetUsage;
        presentation.RenderTargetUsage = RenderTargetUsage.PlatformContents;
        try
        {
            DrawCapturingWarnings(game.GraphicsDevice, containerCount: 1, redraws: 1, target: null).ShouldBeEmpty();
        }
        finally
        {
            presentation.RenderTargetUsage = original;
        }
    }

    [Fact]
    public void UserRenderTargetDiscardContents_WithRenderTargetContainer_WarnsOnce()
    {
        using MinimalGame game = new();
        game.RunOneFrame();

        using RenderTarget2D target = CreateTarget(game.GraphicsDevice, RenderTargetUsage.DiscardContents);

        List<string> warnings = DrawCapturingWarnings(game.GraphicsDevice, containerCount: 1, redraws: 2, target);

        warnings.Count.ShouldBe(1);
        warnings[0].ShouldContain("RenderTargetUsage.PreserveContents");
    }

    [Fact]
    public void UserRenderTargetPreserveContents_DoesNotWarn()
    {
        using MinimalGame game = new();
        game.RunOneFrame();

        using RenderTarget2D target = CreateTarget(game.GraphicsDevice, RenderTargetUsage.PreserveContents);

        DrawCapturingWarnings(game.GraphicsDevice, containerCount: 1, redraws: 1, target).ShouldBeEmpty();
    }

    [Fact]
    public void UserRenderTargetPlatformContents_DoesNotWarn()
    {
        using MinimalGame game = new();
        game.RunOneFrame();

        using RenderTarget2D target = CreateTarget(game.GraphicsDevice, RenderTargetUsage.PlatformContents);

        DrawCapturingWarnings(game.GraphicsDevice, containerCount: 1, redraws: 1, target).ShouldBeEmpty();
    }

    [Fact]
    public void DiscardContents_WithoutRenderTargetContainer_DoesNotWarn()
    {
        using MinimalGame game = new();
        game.RunOneFrame();

        DrawCapturingWarnings(game.GraphicsDevice, containerCount: 0, redraws: 1, target: null).ShouldBeEmpty();
    }

    [Fact]
    public void DiscardContents_WithTwoRenderTargetContainers_WarnsOnce()
    {
        using MinimalGame game = new();
        game.RunOneFrame();

        DrawCapturingWarnings(game.GraphicsDevice, containerCount: 2, redraws: 1, target: null).Count.ShouldBe(1);
    }

    [Fact]
    public void DiscardContents_WarningFires_ThroughConsoleError()
    {
        using MinimalGame game = new();
        game.RunOneFrame();

        TextWriter originalError = Console.Error;
        StringWriter captured = new();
        Console.SetError(captured);
        try
        {
            DrawCapturingWarnings(game.GraphicsDevice, containerCount: 1, redraws: 1, target: null);
        }
        finally
        {
            Console.SetError(originalError);
        }

        captured.ToString().ShouldContain("[Gum] An IsRenderTarget container");
    }

    private static RenderTarget2D CreateTarget(GraphicsDevice device, RenderTargetUsage usage) =>
        new(device, Size * 2, Size * 2, false, SurfaceFormat.Color, DepthFormat.None, 0, usage);

    private static List<string> DrawCapturingWarnings(
        GraphicsDevice device, int containerCount, int redraws, RenderTarget2D? target)
    {
        SystemManagers managers = SystemManagers.Default;
        Renderer renderer = managers.Renderer;

        List<ContainerRuntime> containers = new();
        for (int i = 0; i < containerCount; i++)
        {
            ContainerRuntime container = CreateRenderTargetContainer();
            container.AddToManagers(managers, null);
            container.UpdateLayout();
            containers.Add(container);
        }

        List<string> warnings = new();
        void Handler(string message) => warnings.Add(message);
        CustomSetPropertyOnRenderable.PropertyAssignmentError += Handler;
        try
        {
            for (int i = 0; i < redraws; i++)
            {
                device.SetRenderTarget(target);
                _hostTime += 1.0;
                managers.Activity(_hostTime);
                renderer.Draw(managers);
            }
        }
        finally
        {
            CustomSetPropertyOnRenderable.PropertyAssignmentError -= Handler;
            device.SetRenderTarget(null);
            foreach (ContainerRuntime container in containers)
            {
                container.RemoveFromManagers();
            }
        }

        // Only this feature's message; unrelated runtime errors are not what these tests pin.
        return warnings.Where(message => message.Contains("RenderTargetUsage")).ToList();
    }

    private static ContainerRuntime CreateRenderTargetContainer()
    {
        ContainerRuntime container = new();
        container.Width = Size;
        container.Height = Size;
        container.IsRenderTarget = true;

#pragma warning disable CS0618 // ColoredRectangleRuntime is obsolete; simplest solid fill.
        ColoredRectangleRuntime child = new();
#pragma warning restore CS0618
        child.Width = Size;
        child.Height = Size;
        container.AddChild(child);
        return container;
    }

    /// <summary>Initializes a fresh <see cref="GumService"/> per test, so each test gets a new Renderer.</summary>
    private sealed class MinimalGame : SharedGameLease
    {
        public GumService GumService { get; } = new GumService();

        protected override void OnInitialize() => GumService.Initialize(Game, Gum.Forms.DefaultVisualsVersion.V3);

        protected override void OnDispose()
        {
            if (GumService.IsInitialized)
            {
                GumService.Uninitialize();
            }
        }
    }
}
