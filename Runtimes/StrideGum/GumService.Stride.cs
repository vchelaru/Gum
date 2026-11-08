// Mirrors Runtimes/SilkNetGum/GumService.Silk.cs: this whole file's content only makes sense when
// this project's STRIDE define is active (always true when compiled here -- see StrideGum.csproj --
// but kept guarded for consistency with the other per-runtime GumService.<Platform>.cs files).
#if STRIDE
#nullable enable

using Gum.Forms;
using Gum.Forms.Controls;
using Gum.Input;
using Gum.Wireframe;
using RenderingLibrary;
using SkiaGum.Helpers;
using SkiaSharp;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Graphics;
using Stride.Input;
using Stride.Rendering;
using Stride.Rendering.Compositing;
using System;
using System.Diagnostics;

namespace Gum;

/// <summary>
/// Stride's GumService, as a subclass of <see cref="GumServiceSkiaBase"/>. Overrides the
/// CreateCursor/CreateKeyboard/ApplyGamePadState capability hooks with real
/// <see cref="Stride.Input.InputManager"/>-backed implementations. Unlike every other Gum runtime,
/// the caller never calls Update/Draw themselves: Stride's rendering is driven by its
/// GraphicsCompositor pipeline rather than a user-owned Draw() method, so <see cref="Initialize"/>
/// registers an internal <see cref="GumSceneRenderer"/> that ticks Update/Draw automatically every
/// frame. The public contract is still just one call -- <c>GumService.Default.Initialize(game)</c> --
/// simpler than the Initialize-then-per-frame-Update/Draw shape elsewhere, not a broken version of
/// it. Mirrors Gum/Runtimes/SilkNetGum/GumService.Silk.cs for the input side.
/// </summary>
public partial class GumService : GumServiceSkiaInteractiveBase, IGumService
{
    private InputManager? _inputManager;

    // Gum renders on the GPU through SkiaGameRendering: _gpuTarget is a SkiaRenderTarget2D from the
    // package matching the running graphics API (D3D11, D3D12 or Vulkan), which Skia draws into
    // directly and which composites itself onto the backbuffer on End. There is no CPU path.
    // GumService owns this state (not GumSceneRenderer) so Initialize can create it synchronously --
    // Root is usable immediately after Initialize returns, matching every other runtime's
    // Initialize-then-build-UI pattern, rather than deferring setup until Stride's own
    // SceneRendererBase.InitializeCore fires.
    // The holder also captures the canvas once via a throwaway Begin/EndWithoutDrawing: the target's
    // underlying SKSurface (and so its Canvas) is allocated once in the target's constructor and does
    // not change between passes.
    private SkiaTargetHolder<IStrideSkiaTarget>? _gpuTarget;

    private readonly Stopwatch _clock = new();

    private partial Cursor CreateHostCursor()
    {
        Cursor cursor = new Cursor();
        if (_inputManager != null)
        {
            cursor.AttachStrideInput(_inputManager.Mouse, _inputManager);
        }
        return cursor;
    }

    // A real desktop Stride window always exposes at least one keyboard.
    private partial Keyboard? CreateHostKeyboard() =>
        _inputManager != null && _inputManager.Keyboards.Count > 0
            ? new Keyboard(_inputManager.Keyboards[0], _inputManager)
            : null;

    /// <inheritdoc/>
    void IGumService.ApplyGamePadState(Gum.Input.GamePad gamepad, int index, double time)
    {
        var strideGamePad = _inputManager != null ? _inputManager.GetGamePadByIndex(index) : null;
        if (strideGamePad != null)
        {
            GamePadDriver.Apply(gamepad, strideGamePad, time);
        }
        else
        {
            gamepad.SetConnected(false);
            gamepad.Activity(time);
        }
    }

    /// <summary>
    /// Initializes Gum for a Stride application, optionally loading a Gum project. Call this once,
    /// after <c>game.AddGraphicsCompositor()</c>/<c>AddCleanUIStage()</c> (or equivalent) has already
    /// run -- a <c>GraphicsCompositor</c> must exist on <c>game.SceneSystem</c> before this call, since
    /// (by default) Initialize registers Gum's own scene renderer into it. <see cref="GumServiceSkiaBase.Root"/>
    /// is usable immediately after this call returns; no further setup call is needed, and you never
    /// call Update/Draw yourself -- Stride drives both automatically every frame from here on.
    /// </summary>
    /// <remarks>
    /// Gum draws on the GPU through SkiaGameRendering, with no CPU fallback.
    /// </remarks>
    /// <exception cref="NotSupportedException">
    /// The game runs on a graphics API that has no SkiaGameRendering package (only Direct3D11,
    /// Direct3D12 and Vulkan do), or on D3D11/D3D12 from the plain net10.0 build of Gum.Stride.
    /// </exception>
    /// <param name="game">The Stride game to initialize Gum into.</param>
    /// <param name="gumProjectFile">An optional .gumx project file to load.</param>
    /// <param name="registerSceneRenderer">
    /// When <see langword="true"/> (the default), Initialize constructs and registers a single
    /// <see cref="GumSceneRenderer"/> for you -- the simple path, and all you need for one UI layer.
    /// Pass <see langword="false"/> to skip that and construct/place <see cref="GumSceneRenderer"/>
    /// instances yourself (e.g. <c>game.AddSceneRenderer(new GumSceneRenderer())</c>, or inserted at a
    /// specific point in the compositor) -- for control over ordering relative to other renderers, or
    /// multiple Gum draw passes. The two are mutually exclusive by construction: Initialize either
    /// registers the renderer for you, or it doesn't -- there's no risk of it applying twice.
    /// </param>
    public void Initialize(Game game, string? gumProjectFile = null, bool registerSceneRenderer = true)
    {
        var backBuffer = game.GraphicsDevice.Presenter.BackBuffer;
        GraphicsDevice graphicsDevice = game.GraphicsDevice;
        _gpuTarget?.Dispose();
        _gpuTarget = new SkiaTargetHolder<IStrideSkiaTarget>(
            (width, height) => StrideGpuTargetFactory.Create(graphicsDevice, width, height),
            canvas => SystemManagers.Default.Canvas = canvas,
            HandleResize);
        _gpuTarget.Create(backBuffer.Width, backBuffer.Height);

        InitializeCore(_gpuTarget.Canvas!, game.Input, _gpuTarget.Width, _gpuTarget.Height, gumProjectFile);

        if (registerSceneRenderer)
        {
            GraphicsCompositor compositor = game.SceneSystem.GraphicsCompositor
                ?? throw new InvalidOperationException(
                    "Gum needs a GraphicsCompositor on game.SceneSystem before Initialize. " +
                    "Set one up first (for example with game.AddGraphicsCompositor()), or pass registerSceneRenderer: false.");
            AddSceneRenderer(compositor, new GumSceneRenderer());
        }

        _clock.Start();
    }

    /// <summary>
    /// Adds <paramref name="renderer"/> after whatever the compositor already draws, so the UI draws
    /// over the scene. Done against Stride directly so Gum.Stride does not depend on
    /// Stride.CommunityToolkit (#6149).
    /// </summary>
    internal static void AddSceneRenderer(GraphicsCompositor compositor, SceneRendererBase renderer)
    {
        if (compositor.Game is SceneRendererCollection collection)
        {
            collection.Children.Add(renderer);
        }
        else
        {
            SceneRendererCollection wrapper = new SceneRendererCollection();
            wrapper.Children.Add(compositor.Game);
            wrapper.Children.Add(renderer);
            compositor.Game = wrapper;
        }
    }

    /// <summary>
    /// The Gum-facing half of <see cref="Initialize(Game, string?, bool)"/>: independent of any live
    /// Stride <see cref="GraphicsDevice"/>/<see cref="Texture"/>/<see cref="GumSceneRenderer"/>, so
    /// StrideGum.Tests can drive Gum through this overload with an in-memory <see cref="SKSurface"/>
    /// the same way SilkNetGum.Tests drives <see cref="GumServiceSkiaBase.Initialize(SKCanvas, int,
    /// int, string?)"/> directly -- no real window/graphics context needed for that half of the
    /// contract. Not part of the public API: production callers always go through <see cref="Initialize(Game, string?, bool)"/>.
    /// </summary>
    internal void InitializeCore(SKCanvas canvas, InputManager inputManager, int width, int height, string? gumProjectFile)
    {
        InitializeWithDevices(canvas, width, height, gumProjectFile, () =>
        {
            _inputManager = inputManager;
            Clipboard = new StrideGumClipboard();
        });
    }

    /// <summary>
    /// Called once per frame by the internal <see cref="GumSceneRenderer"/>: handles backbuffer
    /// resize, ticks Update, and renders Gum into the GPU target, which composites itself into the
    /// frame. Not part of the public API -- see the class remarks for why Update/Draw aren't
    /// separately exposed to Stride callers the way they are on every other Gum runtime.
    /// </summary>
    internal void DrawStrideFrame(RenderDrawContext drawContext)
    {
        var commandList = drawContext.CommandList;
        var backBuffer = commandList.RenderTarget;
        if (_gpuTarget == null) return;

        _gpuTarget.EnsureSize(backBuffer.Width, backBuffer.Height);
        if (_gpuTarget.BeginFrame() == null) return;

        Update(_clock.Elapsed.TotalSeconds);
        Draw();
        // Flushes Skia's GPU work and composites the target onto the backbuffer via its own
        // SpriteBatch.
        _gpuTarget.Target!.End(drawContext.GraphicsContext);
    }

    internal void DisposeStrideResources()
    {
        _gpuTarget?.Dispose();
    }
}

/// <summary>
/// The Stride <see cref="SceneRendererBase"/> that drives <see cref="GumService"/> every frame:
/// ticks Update and renders Gum into its GPU Skia target, which composites into the frame.
/// Stateless -- every instance drives the same <see cref="GumService.Default"/> -- so it's safe to
/// construct as many as you want. By default, <see cref="GumService.Initialize(Game, string?, bool)"/>
/// constructs and registers exactly one of these for you; pass <c>registerSceneRenderer: false</c> to
/// that call instead and construct/place instances yourself for control over ordering or multiple
/// draw passes (e.g. <c>game.AddSceneRenderer(new GumSceneRenderer())</c>).
/// </summary>
public sealed class GumSceneRenderer : SceneRendererBase
{
    protected override void DrawCore(RenderContext context, RenderDrawContext drawContext) =>
        GumService.Default.DrawStrideFrame(drawContext);

    protected override void Destroy()
    {
        GumService.Default.DisposeStrideResources();
        base.Destroy();
    }
}
#endif
