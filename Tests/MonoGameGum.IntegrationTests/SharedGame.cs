using Gum.Forms;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using Gum.Renderables;
using RenderingLibrary.Content;
using Xunit;

namespace MonoGameGum.IntegrationTests;

/// <summary>
/// Puts the rendering, font and performance test classes in one xunit collection so they share
/// <see cref="SharedGame"/>. Collections run one at a time, so this Game never overlaps another
/// test's Game.
/// </summary>
[CollectionDefinition(SharedGame.CollectionName)]
public class SharedGameCollection : ICollectionFixture<SharedGame>
{
}

/// <summary>
/// One <see cref="Game"/> and GraphicsDevice for every test that needs a device but is not testing
/// device or Game creation itself (#6092). On CI's software GL (Mesa llvmpipe) a new device costs
/// about 0.8 s to create and the first draw on it about 3 s more, and the process's native memory
/// grew by roughly 35 MB per Game until it passed 7 GB, which slowed later tests from about 0.3 s
/// to about 2 s each. Each test takes a <see cref="SharedGameLease"/> instead, through a nested
/// <c>MinimalGame</c> that derives from it. A test that draws Apos.Shapes takes a
/// <see cref="ShapeGameLease"/> and one that runs two themed services takes a
/// <see cref="MultiServiceGameLease"/>; both are leases on this same device, so the whole assembly
/// creates one <see cref="Game"/> (it used to create one per lease type, and one per test before that).
/// </summary>
public sealed class SharedGame : IDisposable
{
    public const string CollectionName = "SharedGame";

    /// <summary>The back buffer size <c>GraphicsDeviceManager</c> starts with.</summary>
    public const int DefaultWidth = 800;
    public const int DefaultHeight = 480;

    private static SharedGame? _current;

    private readonly HostGame _game;

    public SharedGame()
    {
        LoaderManager.Self?.DisposeAndClear();
        _game = new HostGame();
        _game.RunOneFrame();
        _current = this;
    }

    internal static HostGame CurrentHost => _current?._game
        ?? throw new InvalidOperationException(
            $"No shared Game. Mark the test class with [Collection({nameof(SharedGame)}.{nameof(CollectionName)})].");

    public void Dispose()
    {
        _current = null;
        LoaderManager.Self?.DisposeAndClear();
        _game.Dispose();
    }

    internal sealed class HostGame : Game
    {
        private readonly GraphicsDeviceManager _graphics;
        private BasicEffect? _cacheInvalidatorEffect;

        public Color ClearColor { get; set; } = Color.CornflowerBlue;

        /// <summary>How many frames have been presented; lets a test see that a lease flushed pending GL deletions.</summary>
        public int FramesDrawn { get; private set; }

        public HostGame()
        {
            _graphics = new GraphicsDeviceManager(this)
            {
                // Apos.Shapes and some render-target paths need an SM4 effect the default Reach profile can't load - #4403.
                GraphicsProfile = GraphicsProfile.HiDef,
            };
        }

        /// <summary>Resizes the back buffer for a test that is laid out for a different canvas; a no-op at the same size.</summary>
        public void EnsureBackBufferSize(int width, int height)
        {
            if (_graphics.PreferredBackBufferWidth == width && _graphics.PreferredBackBufferHeight == height)
            {
                return;
            }
            _graphics.PreferredBackBufferWidth = width;
            _graphics.PreferredBackBufferHeight = height;
            _graphics.ApplyChanges();
        }

        /// <summary>
        /// Makes MonoGame's DesktopGL layer forget which vertex buffer it last pointed the vertex attributes
        /// at (#6107). It forgets whenever a draw goes through <c>VertexDeclaration.Apply</c>, which is what
        /// <c>DrawUserPrimitives</c> does, so a draw of one degenerate triangle does it without reaching into
        /// MonoGame's internals. Nothing is shaded: all three vertices are the same point.
        /// </summary>
        public void InvalidateVertexAttributeCache()
        {
            _cacheInvalidatorEffect ??= new BasicEffect(GraphicsDevice);
            _cacheInvalidatorEffect.CurrentTechnique.Passes[0].Apply();
            GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, new VertexPositionColor[3], 0, 1);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _cacheInvalidatorEffect?.Dispose();
                _cacheInvalidatorEffect = null;
            }
            base.Dispose(disposing);
        }

        protected override void Update(GameTime gameTime) { }

        protected override void Draw(GameTime gameTime)
        {
            FramesDrawn++;
            GraphicsDevice.Clear(ClearColor);
        }
    }
}

/// <summary>
/// One test's use of the <see cref="SharedGame"/> device. It stands in for the per-test
/// <c>Game</c> subclass the tests used to declare: <see cref="OnInitialize"/> runs on the first
/// <see cref="RunOneFrame"/>, where <c>Game.Initialize</c> used to run, and <see cref="OnDispose"/> runs
/// when the test disposes it, where <c>Game.Dispose</c> used to run.
/// </summary>
public abstract class SharedGameLease : IDisposable
{
    private readonly SharedGame.HostGame _host;
    private bool _isInitialized;
    private bool _isDisposed;

    /// <param name="backBufferWidth">Back buffer width the test is laid out for; the device is resized if it differs.</param>
    /// <param name="backBufferHeight">Back buffer height the test is laid out for; the device is resized if it differs.</param>
    protected SharedGameLease(
        int backBufferWidth = SharedGame.DefaultWidth,
        int backBufferHeight = SharedGame.DefaultHeight)
    {
        _host = SharedGame.CurrentHost;
        _host.EnsureBackBufferSize(backBufferWidth, backBufferHeight);
        LoaderManager.Self?.DisposeAndClear();
    }

    public Game Game => _host;

    /// <summary>Lets a test pass the lease wherever the per-test Game it replaced was passed.</summary>
    public static implicit operator Game(SharedGameLease lease) => lease._host;

    public GraphicsDevice GraphicsDevice => _host.GraphicsDevice;

    public ContentManager Content => _host.Content;

    public GameServiceContainer Services => _host.Services;

    /// <summary>The color the back buffer is cleared to each frame.</summary>
    protected virtual Color ClearColor => Color.CornflowerBlue;

    /// <summary>Runs once, on the first <see cref="RunOneFrame"/>; set up Gum here.</summary>
    protected virtual void OnInitialize()
    {
    }

    /// <summary>Runs when the test disposes the lease; tear down whatever <see cref="OnInitialize"/> set up.</summary>
    protected virtual void OnDispose()
    {
    }

    public void RunOneFrame()
    {
        _host.ClearColor = ClearColor;
        if (!_isInitialized)
        {
            _isInitialized = true;
            OnInitialize();
        }
        _host.RunOneFrame();
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }
        _isDisposed = true;

        OnDispose();
        LoaderManager.Self?.DisposeAndClear();

        // The next test gets the device as the last one left it, so put back the state a fresh device has.
        GraphicsDevice device = _host.GraphicsDevice;
        device.SetRenderTarget(null);
        PresentationParameters presentation = device.PresentationParameters;
        device.Viewport = new Viewport(0, 0, presentation.BackBufferWidth, presentation.BackBufferHeight);
        device.BlendState = BlendState.Opaque;
        device.DepthStencilState = DepthStencilState.Default;
        device.RasterizerState = RasterizerState.CullCounterClockwise;
        device.ScissorRectangle = device.Viewport.Bounds;

        // MonoGame deletes a disposed GL object two Presents after the dispose (the Present after it moves the
        // pending deletes to a second list and the one after frees them), so a test that never draws a frame
        // (the themed multi-service tests) keeps every texture it disposed until the process ends: about
        // 20 MB of GPU or software-GL memory per test. Two frames here lets the deletions run.
        //
        // The frames are only safe after InvalidateVertexAttributeCache (#6107). The last vertex buffer drawn
        // (Apos.Shapes') is freed by the second frame, and the next one created gets the same GL name back.
        // MonoGame skips re-pointing the vertex attributes for a buffer whose name matches the cached one, so
        // the draw reads from nothing and the process dies with an access violation in native code.
        _host.InvalidateVertexAttributeCache();
        _host.RunOneFrame();
        _host.RunOneFrame();
    }
}

/// <summary>
/// A test that draws Apos.Shapes (#6078). Gum and the shape renderer are initialized on construction and
/// uninitialized on dispose, as the per-test Game did. On CI's software GL (Mesa llvmpipe) the first
/// <c>ShapeBatch</c> on a device costs about 2.4 s while it prepares the shape shader and later ones about
/// 2 ms, which is why these tests share a device at all.
/// </summary>
public sealed class ShapeGameLease : SharedGameLease
{
    public ShapeGameLease()
    {
        Gum.GumService.Default.Initialize(Game, DefaultVisualsVersion.V3);
        ShapeRenderer.Self.Initialize(GraphicsDevice, Content);
    }

    protected override Color ClearColor => Color.Black;

    protected override void OnDispose()
    {
        if (Gum.GumService.Default.IsInitialized)
        {
            Gum.GumService.Default.Uninitialize();
        }
    }
}

/// <summary>
/// A test that runs two themed GumServices (#6085). It builds and tears down its own services on the
/// shared device. The themed demo screens are laid out for 1024x768, so the back buffer is that size for
/// the test and goes back to the default for the next one.
/// </summary>
public sealed class MultiServiceGameLease : SharedGameLease
{
    public const int Width = 1024;
    public const int Height = 768;

    public MultiServiceGameLease()
        : base(Width, Height)
    {
    }
}
