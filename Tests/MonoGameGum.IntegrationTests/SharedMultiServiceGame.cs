using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary.Content;
using Xunit;

namespace MonoGameGum.IntegrationTests;

/// <summary>
/// Puts the two-themed-service test classes (input and themes) in one xunit collection so they share
/// <see cref="SharedMultiServiceGame"/>. Collections run one at a time, so this Game never overlaps another
/// test's Game.
/// </summary>
[CollectionDefinition(SharedMultiServiceGame.CollectionName)]
public class SharedMultiServiceGameCollection : ICollectionFixture<SharedMultiServiceGame>
{
}

/// <summary>
/// One <see cref="Game"/> and GraphicsDevice for the tests that run two themed GumServices (#6085). On CI's
/// software GL (Mesa llvmpipe) a new device costs about 0.8 s to create, about 2.3 s for the first
/// <c>ShapeRenderer.Initialize</c> and about 3 s more for the first draw, so a device per test spent 3 to 7 s
/// per test before it did anything. Each test takes a <see cref="MultiServiceGameLease"/> instead and builds
/// its own services on the shared device. Like <see cref="SharedShapeGame"/> but 1024x768, which is the
/// canvas size the themed demo screens are laid out for.
/// </summary>
public sealed class SharedMultiServiceGame : IDisposable
{
    public const string CollectionName = "SharedMultiServiceGame";

    public const int Width = 1024;
    public const int Height = 768;

    private static SharedMultiServiceGame? _current;

    private readonly HostGame _game;

    public SharedMultiServiceGame()
    {
        LoaderManager.Self?.DisposeAndClear();
        _game = new HostGame();
        _game.RunOneFrame();
        _current = this;
    }

    /// <summary>Starts a test's use of the shared device.</summary>
    public static MultiServiceGameLease Acquire()
    {
        SharedMultiServiceGame current = _current
            ?? throw new InvalidOperationException(
                $"No shared multi-service Game. Mark the test class with [Collection({nameof(SharedMultiServiceGame)}.{nameof(CollectionName)})].");
        return new MultiServiceGameLease(current._game);
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
                // Apos.Shapes (the themes' rectangles and circles) needs an SM4 effect - #4403.
                GraphicsProfile = GraphicsProfile.HiDef,
                PreferredBackBufferWidth = Width,
                PreferredBackBufferHeight = Height,
            };
        }

        protected override void Update(GameTime gameTime) { }

        protected override void Draw(GameTime gameTime) => GraphicsDevice.Clear(Color.CornflowerBlue);
    }
}

/// <summary>
/// One test's use of the <see cref="SharedMultiServiceGame"/> device. The test builds and tears down its own
/// services; disposing the lease puts back the device state a fresh device has.
/// </summary>
public sealed class MultiServiceGameLease : IDisposable
{
    internal MultiServiceGameLease(Game game)
    {
        Game = game;
        LoaderManager.Self?.DisposeAndClear();
    }

    public Game Game { get; }

    public GraphicsDevice GraphicsDevice => Game.GraphicsDevice;

    public void Dispose()
    {
        // The next test gets the device as the last one left it, so put back the state a fresh device has.
        GraphicsDevice device = GraphicsDevice;
        device.SetRenderTarget(null);
        device.BlendState = BlendState.Opaque;
        device.DepthStencilState = DepthStencilState.Default;
        device.RasterizerState = RasterizerState.CullCounterClockwise;
        device.ScissorRectangle = device.Viewport.Bounds;
    }
}
