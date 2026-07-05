using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary.Content;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace MonoGameGum.IntegrationTests.MonoGameGum;

public class GumServiceUnitTests : BaseTestClass
{
    [Fact]
    public void Game_ThrowsException_WhenNotInitialized()
    {
        var gumService = new GumService();

        var exception = Should.Throw<InvalidOperationException>(() => gumService.Game);

        exception.Message.ShouldContain("GumService has not been initialized");
        exception.Message.ShouldContain("Call GumService.Initialize() first");
    }

    [Fact]
    public void Game_DoesNotThrow_WhenInitialized()
    {
        using var game = new GameForInitializationTest();
        game.RunOneFrame();

        var gumService = game.GumService;

        // Should not throw
        var gameInstance = gumService.Game;

        gameInstance.ShouldNotBeNull();
        gameInstance.ShouldBeSameAs(game);
    }

    [Fact]
    public void Initialize_ShouldWork()
    {
        using var game = new Game1();
        game.RunOneFrame();
    }

    [Fact]
    public void Initialize_ThenDispose_UninitializesGumServiceDefault()
    {
        using (Game1 game = new Game1())
        {
            game.RunOneFrame();
            GumService.Default.IsInitialized.ShouldBeTrue();
        }

        // The integration suite runs sequentially in a single process with non-deterministic
        // test ordering (xUnit re-orders test cases between runs). If Game1 leaves
        // GumService.Default initialized, whatever Game-based test xUnit schedules next throws
        // "Initialize has already been called once" from its own GumService.Default.Initialize
        // — an order-dependent flake that surfaced in the allocation/perf tests. Game1's
        // teardown must uninitialize so every test starts from a clean, deterministic state.
        GumService.Default.IsInitialized.ShouldBeFalse();
    }

    [Fact]
    public void SystemManagers_ThrowsException_WhenNotInitialized()
    {
        var gumService = new GumService();

        var exception = Should.Throw<InvalidOperationException>(() => gumService.SystemManagers);

        exception.Message.ShouldContain("GumService has not been initialized");
        exception.Message.ShouldContain("Call GumService.Initialize() first");
    }

    [Fact]
    public void SystemManagers_DoesNotThrow_WhenInitialized()
    {
        using var game = new GameForInitializationTest();
        game.RunOneFrame();

        var gumService = game.GumService;

        // Should not throw
        var systemManagers = gumService.SystemManagers;

        systemManagers.ShouldNotBeNull();
    }

    [Fact]
    public void LoadAnimations_ThrowsException_WhenNoProjectLoaded()
    {
        using var game = new GameForInitializationTest();
        game.RunOneFrame();

        var gumService = game.GumService;

#pragma warning disable CS0618 // Type or member is obsolete
        var exception = Should.Throw<InvalidOperationException>(() => gumService.LoadAnimations());
#pragma warning restore CS0618 // Type or member is obsolete

        exception.Message.ShouldContain("You must first load a project before attempting to load its animations");
        exception.Message.ShouldContain("Did you call GumUI.Initialize with a valid .gumx first?");
    }

    #region Test Classes

    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        GumService GumUI => GumService.Default;
        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            base.Initialize();
            GumUI.Initialize(this, Gum.Forms.DefaultVisualsVersion.V2);
        }

        protected override void Update(GameTime gameTime)
        {
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);
        }

        protected override void Dispose(bool disposing)
        {
            // Game1 initializes GumService.Default, so it must uninitialize it on teardown
            // (before base.Dispose disposes the GraphicsDevice that Uninitialize releases
            // GPU resources through). Without this, GumService.Default stays initialized and
            // leaks into the next test in this sequential single-process suite, whose own
            // GumService.Default.Initialize would then throw "already been called once".
            if (GumUI.IsInitialized)
            {
                GumUI.Uninitialize();
            }
            LoaderManager.Self?.DisposeAndClear();
            base.Dispose(disposing);
        }
    }

    public class GameForInitializationTest : Game
    {
        private GraphicsDeviceManager _graphics;
        public GumService GumService { get; private set; }

        public GameForInitializationTest()
        {
            LoaderManager.Self?.DisposeAndClear();
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
            GumService = new GumService();
        }

        protected override void Initialize()
        {
            base.Initialize();
            GumService.Initialize(this, Gum.Forms.DefaultVisualsVersion.V2);
        }

        protected override void Update(GameTime gameTime)
        {
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);
        }

        protected override void Dispose(bool disposing)
        {
            LoaderManager.Self?.DisposeAndClear();
            base.Dispose(disposing);
        }
    }
    #endregion
}
