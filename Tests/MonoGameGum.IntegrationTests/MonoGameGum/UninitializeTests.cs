using Gum.Forms;
using Gum.GueDeriving;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameAndGum.Renderables;
using RenderingLibrary;
using RenderingLibrary.Content;
using Shouldly;
// MonoGameGum.GueDeriving.ContainerRuntime is obsolete — Gum.GueDeriving.ContainerRuntime
// (above) is the supported type. Drop the legacy using to avoid the CS0104 collision now
// that this file references both namespaces' contents.

namespace MonoGameGum.IntegrationTests.MonoGameGum;

[Collection(SharedGame.CollectionName)]
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
    // Process-wide texture filter
    // -------------------------------------------------------------------------

    [Fact]
    public void Uninitialize_RestoresTheProcessWideTextureFilter()
    {
        using var game = new GameForUninitializeTest();
        game.RunOneFrame();
        // A project with linear filtering sets this for the first service.
        RenderingLibrary.Graphics.Renderer.TextureFilter = TextureFilter.Linear;

        game.GumService.Uninitialize();

        RenderingLibrary.Graphics.Renderer.TextureFilter.ShouldBe(TextureFilter.Point);
    }

    // -------------------------------------------------------------------------
    // Radio button groups
    // -------------------------------------------------------------------------

    [Fact]
    public void Uninitialize_ClearsRadioButtonGroups()
    {
        using var game = new GameForUninitializeTest();
        game.RunOneFrame();
        Gum.Forms.Controls.RadioButton radio = new Gum.Forms.Controls.RadioButton("group");
        game.GumService.Root.AddChild(radio.Visual);
        // The groups are keyed by each button's parent visual, so a group left behind keeps that whole tree alive.
        Gum.Forms.Controls.RadioButton.RadioButtonDictionary.Count.ShouldBeGreaterThan(0);

        game.GumService.Uninitialize();

        Gum.Forms.Controls.RadioButton.RadioButtonDictionary.Count.ShouldBe(0);
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
    // ShapeRenderer (Apos.Shapes) — issue #4416
    // -------------------------------------------------------------------------

    [Fact]
    public void Uninitialize_AllowsShapeRendererReinitialize()
    {
        // Repro from #4416: a host (e.g. FlatRedBall2) that calls ShapeRenderer.Self.Initialize
        // during its own startup, then GumService.Uninitialize(), then re-initializes both. Before
        // the fix, ShapeRenderer.Self.IsInitialized stayed true across Uninitialize, so the second
        // ShapeRenderer.Self.Initialize() threw "ShapeRenderer is already initialized".
        using var reinitGame = new GameForShapeRendererReinitializeTest();

        Should.NotThrow(() => reinitGame.RunOneFrame());

        ShapeRenderer.Self.IsInitialized.ShouldBeTrue();
    }

    // -------------------------------------------------------------------------
    // Multiple GumService instances (#6066)
    // -------------------------------------------------------------------------

    [Fact]
    public void Uninitialize_OfNonCurrentService_LeavesLiveServiceDefaultsIntact()
    {
        using TwoServiceGame game = new TwoServiceGame();
        game.RunOneFrame();

        // The first Initialize made ServiceA the current service; a second Initialize leaves it alone.
        game.ServiceB.Uninitialize();

        IGumService.Default.ShouldBeSameAs(game.ServiceA);
        SystemManagers.Default.ShouldBeSameAs(game.ServiceA.SystemManagers);
        ISystemManagers.Default.ShouldBeSameAs(game.ServiceA.SystemManagers);
    }

    [Fact]
    public void Uninitialize_OfCurrentService_DoesNotLeaveDefaultsPointingAtDeadService()
    {
        using TwoServiceGame game = new TwoServiceGame();
        game.RunOneFrame();

        SystemManagers deadManagers = game.ServiceA.SystemManagers;
        game.ServiceA.Uninitialize();

        IGumService.Default.ShouldNotBeSameAs(game.ServiceA);
        SystemManagers.Default.ShouldNotBeSameAs(deadManagers);
        ISystemManagers.Default.ShouldNotBeSameAs(deadManagers);
    }

    [Fact]
    public void Uninitialize_OfCurrentServiceWithoutOwnManagers_ClearsDefaultManagersAssignedByHand()
    {
        // Hosts such as the raylib tests call InitializeForTesting and assign SystemManagers.Default by
        // hand, so the service owns no managers of its own. As the current default service it must
        // still clear them, otherwise the next Initialize reuses stale managers.
        using TwoServiceGame game = new TwoServiceGame();
        game.RunOneFrame();
        game.ServiceB.Uninitialize();
        game.ServiceA.Uninitialize();
        SystemManagers handAssigned = new SystemManagers();
        SystemManagers.Default = handAssigned;
        ISystemManagers.Default = handAssigned;
        GumService service = new GumService();
        service.InitializeForTesting();
        IGumService.Default.ShouldBeSameAs(service);

        service.Uninitialize();

        SystemManagers.Default.ShouldBeNull();
        ISystemManagers.Default.ShouldBeNull();
    }

    [Fact]
    public void Uninitialize_OfNonDefaultService_KeepsStaticGumServiceDefault()
    {
        using TwoServiceGame game = new TwoServiceGame();
        game.RunOneFrame();

        game.ServiceB.Uninitialize();

        GumService.Default.ShouldBeSameAs(game.ServiceA);
    }

    // -------------------------------------------------------------------------
    // Nested Game classes
    // -------------------------------------------------------------------------

    private sealed class TwoServiceGame : SharedGameLease
    {
        public GumService ServiceA { get; }
        public GumService ServiceB { get; }

        public TwoServiceGame()
        {
            // ServiceA is the static GumService.Default so its lifetime can be checked too.
            ServiceA = GumService.Default;
            ServiceB = new GumService();
        }

        protected override void OnInitialize()
        {
            ServiceA.Initialize(Game, DefaultVisualsVersion.V3);
            ServiceB.Initialize(Game, DefaultVisualsVersion.V3);
        }

        protected override void OnDispose()
        {
            if (ServiceB.IsInitialized)
                ServiceB.Uninitialize();
            if (ServiceA.IsInitialized)
                ServiceA.Uninitialize();
        }
    }

    private sealed class GameForUninitializeTest : SharedGameLease
    {
        public GumService GumService { get; } = new GumService();

        protected override void OnInitialize() => GumService.Initialize(Game, DefaultVisualsVersion.V3);

        protected override void OnDispose()
        {
            // The test may already have called Uninitialize.
            if (GumService.IsInitialized)
                GumService.Uninitialize();
        }
    }

    /// <summary>
    /// Calls Uninitialize inside Initialize, then re-initializes — used to verify
    /// that Uninitialize leaves the system in a state where Initialize can run again.
    /// </summary>
    private sealed class GameForReinitializeTest : SharedGameLease
    {
        public GumService GumService { get; } = new GumService();

        protected override void OnInitialize()
        {
            GumService.Initialize(Game, DefaultVisualsVersion.V3);
            GumService.Uninitialize();

            // Second initialization on a fresh instance.
            GumService.Initialize(Game, DefaultVisualsVersion.V3);
        }

        protected override void OnDispose()
        {
            if (GumService.IsInitialized)
                GumService.Uninitialize();
        }
    }

    /// <summary>
    /// Calls ShapeRenderer.Self.Initialize alongside GumService, then Uninitialize, then
    /// re-initializes both — used to verify that GumService.Uninitialize() resets ShapeRenderer.Self
    /// so a host-driven second Initialize (#4416) does not throw.
    /// </summary>
    private sealed class GameForShapeRendererReinitializeTest : SharedGameLease
    {
        public GumService GumService { get; } = new GumService();

        protected override void OnInitialize()
        {
            GumService.Initialize(Game, DefaultVisualsVersion.V3);
            ShapeRenderer.Self.Initialize(GraphicsDevice, Content);

            GumService.Uninitialize();

            // Second initialization on a fresh instance.
            GumService.Initialize(Game, DefaultVisualsVersion.V3);
            ShapeRenderer.Self.Initialize(GraphicsDevice, Content);
        }

        protected override void OnDispose()
        {
            if (GumService.IsInitialized)
            {
                GumService.Uninitialize();
            }
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
