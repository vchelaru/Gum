using Gum.Forms;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameAndGum.Renderables;
using RenderingLibrary.Content;
using Xunit;

namespace MonoGameGum.IntegrationTests;

/// <summary>
/// Puts every shape test class in one xunit collection so they share <see cref="SharedShapeGame"/>.
/// Collections run one at a time, so this Game never overlaps another test's Game.
/// </summary>
[CollectionDefinition(SharedShapeGame.CollectionName)]
public class SharedShapeGameCollection : ICollectionFixture<SharedShapeGame>
{
}

/// <summary>
/// One <see cref="Game"/> and GraphicsDevice for all the Apos.Shapes tests (#6078). On CI's software
/// GL (Mesa llvmpipe) the first <c>ShapeBatch</c> on a device costs about 2.4 s while it prepares
/// the shape shader, and later ones on the same device cost about 2 ms, so a device per test paid
/// that on every test. Each test takes a <see cref="ShapeGameLease"/> instead, which initializes
/// and uninitializes <see cref="Gum.GumService.Default"/> and the <see cref="ShapeRenderer"/>
/// per test, exactly as the per-test Game did.
/// </summary>
public sealed class SharedShapeGame : IDisposable
{
    public const string CollectionName = "SharedShapeGame";

    private static SharedShapeGame? _current;

    private readonly HostGame _game;

    public SharedShapeGame()
    {
        LoaderManager.Self?.DisposeAndClear();
        _game = new HostGame();
        _game.RunOneFrame();
        _current = this;
    }

    internal Game Game => _game;

    /// <summary>Starts a test's Gum and shape session on the shared device.</summary>
    public static ShapeGameLease Acquire()
    {
        SharedShapeGame current = _current
            ?? throw new InvalidOperationException(
                $"No shared shape Game. Mark the test class with [Collection({nameof(SharedShapeGame)}.{nameof(CollectionName)})].");
        return new ShapeGameLease(current._game);
    }

    public void Dispose()
    {
        _current = null;
        LoaderManager.Self?.DisposeAndClear();
        _game.Dispose();
    }

    private sealed class HostGame : Game
    {
        public HostGame()
        {
            new GraphicsDeviceManager(this)
            {
                // Apos.Shapes uses an SM4 effect the default Reach profile can't load - #4403.
                GraphicsProfile = GraphicsProfile.HiDef,
            };
        }

        protected override void Update(GameTime gameTime) { }

        protected override void Draw(GameTime gameTime) => GraphicsDevice.Clear(Color.Black);
    }
}

/// <summary>
/// One test's use of the <see cref="SharedShapeGame"/> device: Gum and the shape renderer are
/// initialized on construction and uninitialized on dispose.
/// </summary>
public sealed class ShapeGameLease : IDisposable
{
    private readonly Game _game;

    internal ShapeGameLease(Game game)
    {
        _game = game;
        LoaderManager.Self?.DisposeAndClear();
        Gum.GumService.Default.Initialize(game, DefaultVisualsVersion.V3);
        ShapeRenderer.Self.Initialize(game.GraphicsDevice, game.Content);
    }

    public GraphicsDevice GraphicsDevice => _game.GraphicsDevice;

    public void Dispose()
    {
        if (Gum.GumService.Default.IsInitialized)
        {
            Gum.GumService.Default.Uninitialize();
        }
        LoaderManager.Self?.DisposeAndClear();

        // The next test gets the device as the last one left it, so put back the state a fresh device has.
        GraphicsDevice device = _game.GraphicsDevice;
        device.SetRenderTarget(null);
        device.BlendState = BlendState.Opaque;
        device.DepthStencilState = DepthStencilState.Default;
        device.RasterizerState = RasterizerState.CullCounterClockwise;
        device.ScissorRectangle = device.Viewport.Bounds;
    }
}
