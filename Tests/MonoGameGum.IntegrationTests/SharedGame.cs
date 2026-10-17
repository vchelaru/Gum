using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
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
/// <c>MinimalGame</c> that derives from it. Like <see cref="SharedShapeGame"/> but without the shape
/// renderer; a test that draws Apos.Shapes belongs in that collection.
/// </summary>
public sealed class SharedGame : IDisposable
{
    public const string CollectionName = "SharedGame";

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
        public Color ClearColor { get; set; } = Color.CornflowerBlue;

        public HostGame()
        {
            new GraphicsDeviceManager(this)
            {
                // Apos.Shapes and some render-target paths need an SM4 effect the default Reach profile can't load - #4403.
                GraphicsProfile = GraphicsProfile.HiDef,
            };
        }

        protected override void Update(GameTime gameTime) { }

        protected override void Draw(GameTime gameTime) => GraphicsDevice.Clear(ClearColor);
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

    protected SharedGameLease()
    {
        _host = SharedGame.CurrentHost;
        LoaderManager.Self?.DisposeAndClear();
    }

    public Game Game => _host;

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
    }
}
