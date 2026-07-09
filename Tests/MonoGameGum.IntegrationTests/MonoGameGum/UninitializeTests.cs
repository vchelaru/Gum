using Gum.Forms;
using Gum.GueDeriving;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary;
using RenderingLibrary.Content;
using Shouldly;
// MonoGameGum.GueDeriving.ContainerRuntime is obsolete — Gum.GueDeriving.ContainerRuntime
// (above) is the supported type. Drop the legacy using to avoid the CS0104 collision now
// that this file references both namespaces' contents.

namespace MonoGameGum.IntegrationTests.MonoGameGum;

public class UninitializeTests : BaseTestClass
{
    // -------------------------------------------------------------------------
    // LoaderManager content cache
    // -------------------------------------------------------------------------

    [Fact]
    public void Uninitialize_ClearsLoaderManagerCache()
    {
        using var game = new GameForUninitializeTest();
        game.RunOneFrame();

        // Initialize loads embedded fonts and textures into LoaderManager.
        LoaderManager.Self.CachedDisposables.Count.ShouldBeGreaterThan(0);

        game.GumService.Uninitialize();

        LoaderManager.Self.CachedDisposables.Count.ShouldBe(0);
    }

    [Fact]
    public void Uninitialize_DisposesLoadedContent()
    {
        using var game = new GameForUninitializeTest();
        game.RunOneFrame();

        // Snapshot the disposables before teardown.
        var disposables = LoaderManager.Self.CachedDisposables.Values.ToList();
        disposables.Count.ShouldBeGreaterThan(0);

        game.GumService.Uninitialize();

        // Every disposable registered by Initialize should now be disposed.
        // BitmapFont and Texture2D both implement IDisposable and set IsDisposed = true.
        foreach (var disposable in disposables)
        {
            if (disposable is Texture2D texture)
                texture.IsDisposed.ShouldBeTrue();
        }
    }

    // -------------------------------------------------------------------------
    // GumService lifecycle
    // -------------------------------------------------------------------------

    [Fact]
    public void Uninitialize_SetsIsInitializedToFalse()
    {
        using var game = new GameForUninitializeTest();
        game.RunOneFrame();
        game.GumService.IsInitialized.ShouldBeTrue();

        game.GumService.Uninitialize();

        game.GumService.IsInitialized.ShouldBeFalse();
    }

    [Fact]
    public void Uninitialize_NullsSystemManagersDefault()
    {
        using var game = new GameForUninitializeTest();
        game.RunOneFrame();
        SystemManagers.Default.ShouldNotBeNull();

        game.GumService.Uninitialize();

        SystemManagers.Default.ShouldBeNull();
    }

    [Fact]
    public void Uninitialize_ClearsRootChildren()
    {
        using var game = new GameForUninitializeTest();
        game.RunOneFrame();

        var child = new ContainerRuntime();
        game.GumService.Root.Children.Add(child);
        game.GumService.Root.Children.ShouldContain(child);

        game.GumService.Uninitialize();

        game.GumService.Root.Children.Count.ShouldBe(0);
    }

    [Fact]
    public void Uninitialize_DoesNotClearOrDetachAReassignedHostRoot()
    {
        using var game = new GameForUninitializeTest();
        game.RunOneFrame();

        // Mirrors how an embedding host (e.g. FlatRedBall2) reassigns Root to its own
        // container, attached via AttachManagersOnly rather than AddToManagers since the
        // host draws it through its own render pass.
        var hostRoot = new ContainerRuntime();
        hostRoot.AttachManagersOnly(game.GumService.SystemManagers);
        var hostChild = new ContainerRuntime();
        hostRoot.Children.Add(hostChild);

        game.GumService.Root = hostRoot;

        game.GumService.Uninitialize();

        hostRoot.Children.ShouldContain(hostChild,
            "because Uninitialize must not clear a host-reassigned Root's own children");
        hostRoot.Managers.ShouldNotBeNull(
            "because Uninitialize must not detach a host-reassigned Root's EffectiveManagers resolution");
    }

    [Fact]
    public void Uninitialize_ClearsRenderableRegistry()
    {
        using GameForUninitializeTest game = new GameForUninitializeTest();
        game.RunOneFrame();

        RenderableRegistry.RegisterFactory<IRegistryCapability>(() => new RegistryCapability());
        RenderableRegistry.GetFactory<IRegistryCapability>().ShouldNotBeNull();

        game.GumService.Uninitialize();

        RenderableRegistry.GetFactory<IRegistryCapability>().ShouldBeNull();
    }

    [Fact]
    public void Uninitialize_AllowsReinitialize()
    {
        using var reinitGame = new GameForReinitializeTest();
        reinitGame.RunOneFrame();

        // Should not throw on re-initialize.
        reinitGame.GumService.IsInitialized.ShouldBeTrue();
        LoaderManager.Self.CachedDisposables.Count.ShouldBeGreaterThan(0);
    }

    // -------------------------------------------------------------------------
    // Nested Game classes
    // -------------------------------------------------------------------------

    private class GameForUninitializeTest : Game
    {
        private readonly GraphicsDeviceManager _graphics;
        public GumService GumService { get; }

        public GameForUninitializeTest()
        {
            LoaderManager.Self?.DisposeAndClear();
            _graphics = new GraphicsDeviceManager(this);
            GumService = new GumService();
        }

        protected override void Initialize()
        {
            base.Initialize();
            GumService.Initialize(this, DefaultVisualsVersion.V2);
        }

        protected override void Draw(GameTime gameTime) =>
            GraphicsDevice.Clear(Color.CornflowerBlue);

        protected override void Dispose(bool disposing)
        {
            // Uninitialize may have already been called by the test; guard against double-dispose.
            if (GumService.IsInitialized)
                LoaderManager.Self?.DisposeAndClear();
            base.Dispose(disposing);
        }
    }

    /// <summary>
    /// Calls Uninitialize inside Initialize, then re-initializes — used to verify
    /// that Uninitialize leaves the system in a state where Initialize can run again.
    /// </summary>
    private class GameForReinitializeTest : Game
    {
        private readonly GraphicsDeviceManager _graphics;
        public GumService GumService { get; }

        public GameForReinitializeTest()
        {
            LoaderManager.Self?.DisposeAndClear();
            _graphics = new GraphicsDeviceManager(this);
            GumService = new GumService();
        }

        protected override void Initialize()
        {
            base.Initialize();
            GumService.Initialize(this, DefaultVisualsVersion.V2);
            GumService.Uninitialize();

            // Second initialization on a fresh instance.
            GumService.Initialize(this, DefaultVisualsVersion.V2);
        }

        protected override void Draw(GameTime gameTime) =>
            GraphicsDevice.Clear(Color.CornflowerBlue);

        protected override void Dispose(bool disposing)
        {
            LoaderManager.Self?.DisposeAndClear();
            base.Dispose(disposing);
        }
    }

    // Sentinel capability for Uninitialize_ClearsRenderableRegistry — intentionally
    // unrelated to any real renderable contract; RenderableRegistry keys by type only.
    private interface IRegistryCapability
    {
    }

    private sealed class RegistryCapability : IRegistryCapability
    {
    }
}
