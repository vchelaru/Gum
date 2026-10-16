using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;
using GumRuntime;
using Microsoft.Xna.Framework;
using Shouldly;
using System;
using System.IO;
using System.Linq;
using Xunit;

namespace MonoGameGum.IntegrationTests.MonoGameGum;

/// <summary>
/// Lifecycle edges of a service's GumContext (#6066 prototype): a service that is not initialized must not
/// touch the state of one that is, and a failed Initialize must leave nothing live.
/// </summary>
public class GumServiceContextLifecycleTests : BaseTestClass
{
    private readonly string _root;

    public GumServiceContextLifecycleTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "GumContextLifecycle_" + Guid.NewGuid().ToString("N"));
    }

    public override void Dispose()
    {
        base.Dispose();
        try { Directory.Delete(_root, recursive: true); } catch { /* best effort */ }
    }

    private string WriteProject(string name)
    {
        GumProjectFolder folder = new GumProjectFolder(Path.Combine(_root, name));
        folder.Add(new GumElement("MainScreen", isScreen: true));
        return folder.Write();
    }

    private static CollisionGame CreateGame()
    {
        CollisionGame game = new CollisionGame();
        game.RunOneFrame();
        return game;
    }

    private static GumProjectSave? ProjectOf(Gum.GumService service)
    {
        global::RenderingLibrary.IGumService.Default = service;
        {
            return ObjectFinder.Self.GumProjectSave;
        }
    }

    #region Uninitialize is a no-op for a service without a context

    [Fact]
    public void Uninitialize_CalledTwiceOnSecondService_ShouldNotUnloadFirstServicesProject()
    {
        using CollisionGame game = CreateGame();
        game.InitializeA(WriteProject("A"));
        game.InitializeB(WriteProject("B"));
        GumProjectSave? projectA = ProjectOf(game.ServiceA);
        projectA.ShouldNotBeNull("setup");

        game.ServiceB.Uninitialize();
        game.ServiceB.Uninitialize();

        ProjectOf(game.ServiceA).ShouldBeSameAs(projectA, "B's second Uninitialize ran against the first service's state");
    }

    [Fact]
    public void Uninitialize_OnNeverInitializedService_ShouldNotUnloadFirstServicesProject()
    {
        using CollisionGame game = CreateGame();
        game.InitializeA(WriteProject("A"));
        GumProjectSave? projectA = ProjectOf(game.ServiceA);
        projectA.ShouldNotBeNull("setup");

        new Gum.GumService().Uninitialize();

        ProjectOf(game.ServiceA).ShouldBeSameAs(projectA);
    }

    #endregion

    #region A failed Initialize leaves nothing behind

    [Fact]
    public void Initialize_WithMissingProject_ShouldNotLeaveALiveContext()
    {
        using CollisionGame game = CreateGame();
        string missing = Path.Combine(_root, "Missing", "Project.gumx");

        Should.Throw<Exception>(() => game.ServiceA.Initialize(game, missing));

        game.ServiceA.IsInitialized.ShouldBeFalse();
        game.ServiceA.Context.ShouldBeNull();
        Gum.GumContext.Current.ShouldBeNull("the failed service must not stay live");
    }

    [Fact]
    public void Initialize_WithMissingProjectOnSecondService_ShouldLeaveFirstServiceAsHome()
    {
        using CollisionGame game = CreateGame();
        game.InitializeA(WriteProject("A"));
        Gum.GumContext? homeBefore = Gum.GumContext.Current;
        string missing = Path.Combine(_root, "Missing", "Project.gumx");

        Should.Throw<Exception>(() => game.ServiceB.Initialize(game, missing));

        Gum.GumContext.Current.ShouldBeSameAs(homeBefore);
        game.ServiceB.IsInitialized.ShouldBeFalse();
    }

    [Fact]
    public void Initialize_AfterAFailedInitialize_ShouldSucceed()
    {
        using CollisionGame game = CreateGame();
        string missing = Path.Combine(_root, "Missing", "Project.gumx");
        Should.Throw<Exception>(() => game.ServiceA.Initialize(game, missing));

        GumProjectSave? project = game.ServiceA.Initialize(game, WriteProject("A"));

        project.ShouldNotBeNull();
        game.ServiceA.IsInitialized.ShouldBeTrue();
        Gum.GumContext.Current.ShouldBeSameAs(game.ServiceA.Context);
    }

    #endregion

    #region The Forms refresh hooks survive an Initialize, Uninitialize, Initialize cycle

    [Fact]
    public void Initialize_AfterAnUninitializeCycle_ShouldReinstallTheFormsRefreshHooks()
    {
        using CollisionGame game = CreateGame();
        game.InitializeA(WriteProject("A"));
        game.ServiceA.Uninitialize();

        game.ServiceA.Initialize(game, WriteProject("A2"));

        GraphicalUiElement.SaveFormsRuntimePropertiesAction.ShouldNotBeNull();
        GraphicalUiElement.UpdateFormsStateAction.ShouldNotBeNull();
    }

    #endregion

    #region Uninitialize only touches the thread synchronization context when it is its own

    [Fact]
    public void Uninitialize_ShouldClearTheThreadsSynchronizationContext_WhenItIsThisServicesOwn()
    {
        System.Threading.SynchronizationContext? original = System.Threading.SynchronizationContext.Current;
        try
        {
            using CollisionGame game = CreateGame();
            game.InitializeA(WriteProject("A"));
            game.ServiceA.UseSingleThreadedAsync();
            System.Threading.SynchronizationContext.Current.ShouldBeSameAs(game.ServiceA.SynchronizationContext);

            game.ServiceA.Uninitialize();

            System.Threading.SynchronizationContext.Current.ShouldBeNull();
        }
        finally
        {
            System.Threading.SynchronizationContext.SetSynchronizationContext(original);
        }
    }

    [Fact]
    public void Uninitialize_ShouldLeaveAnotherSynchronizationContextAlone()
    {
        System.Threading.SynchronizationContext? original = System.Threading.SynchronizationContext.Current;
        try
        {
            using CollisionGame game = CreateGame();
            game.InitializeA(WriteProject("A"));
            game.InitializeB(WriteProject("B"));
            game.ServiceA.UseSingleThreadedAsync();
            game.ServiceB.UseSingleThreadedAsync();
            System.Threading.SynchronizationContext.Current.ShouldBeSameAs(game.ServiceB.SynchronizationContext);

            game.ServiceA.Uninitialize();

            System.Threading.SynchronizationContext.Current.ShouldBeSameAs(game.ServiceB.SynchronizationContext,
                "A's Uninitialize must not touch the context that is B's");
        }
        finally
        {
            System.Threading.SynchronizationContext.SetSynchronizationContext(original);
        }
    }

    [Fact]
    public void Uninitialize_ShouldLeaveACustomSynchronizationContextAlone()
    {
        System.Threading.SynchronizationContext? original = System.Threading.SynchronizationContext.Current;
        try
        {
            using CollisionGame game = CreateGame();
            game.InitializeA(WriteProject("A"));
            game.ServiceA.UseSingleThreadedAsync();
            System.Threading.SynchronizationContext custom = new System.Threading.SynchronizationContext();
            System.Threading.SynchronizationContext.SetSynchronizationContext(custom);

            game.ServiceA.Uninitialize();

            System.Threading.SynchronizationContext.Current.ShouldBeSameAs(custom);
        }
        finally
        {
            System.Threading.SynchronizationContext.SetSynchronizationContext(original);
        }
    }

    [Fact]
    public void UseSingleThreadedAsync_AfterAnUninitializeCycle_ShouldInstallAFreshContext()
    {
        System.Threading.SynchronizationContext? original = System.Threading.SynchronizationContext.Current;
        try
        {
            using CollisionGame game = CreateGame();
            game.InitializeA(WriteProject("A"));
            game.ServiceA.UseSingleThreadedAsync();
            Gum.Async.SingleThreadSynchronizationContext first = game.ServiceA.SynchronizationContext!;
            game.ServiceA.Uninitialize();
            game.ServiceA.Initialize(game, WriteProject("A2"));

            game.ServiceA.UseSingleThreadedAsync();

            game.ServiceA.SynchronizationContext.ShouldNotBeNull();
            game.ServiceA.SynchronizationContext.ShouldNotBeSameAs(first);
            System.Threading.SynchronizationContext.Current.ShouldBeSameAs(game.ServiceA.SynchronizationContext);
        }
        finally
        {
            System.Threading.SynchronizationContext.SetSynchronizationContext(original);
        }
    }

    #endregion

    #region A service without a context does not answer with another service's state

    [Fact]
    public void InstanceProperties_OnServiceWithoutContext_ShouldNotReturnTheFirstServicesState()
    {
        using CollisionGame game = CreateGame();
        game.InitializeA(WriteProject("A"));
        Gum.GumService notInitialized = new Gum.GumService();

        notInitialized.Cursor.ShouldBeNull();
        notInitialized.Keyboard.ShouldBeNull();
        notInitialized.PopupRoot.ShouldBeNull();
        notInitialized.ModalRoot.ShouldBeNull();
        notInitialized.Gamepads.ShouldBeEmpty();
        notInitialized.ContentLoader.ShouldBeNull();
    }

    [Fact]
    public void InstanceProperties_AfterUninitialize_ShouldNotReturnTheFirstServicesState()
    {
        using CollisionGame game = CreateGame();
        game.InitializeA(WriteProject("A"));
        game.InitializeB(WriteProject("B"));
        game.ServiceB.Uninitialize();

        game.ServiceB.Cursor.ShouldBeNull();
        game.ServiceB.PopupRoot.ShouldBeNull();
        game.ServiceB.ModalRoot.ShouldBeNull();
        game.ServiceA.PopupRoot.ShouldNotBeNull("the first service is unaffected");
    }

    #endregion

    #region Detached elements remember the service that built them

    [Fact]
    public void OwnerOf_ForDetachedElementBuiltInsideItsOwnServicesScope_ShouldBeThatService()
    {
        using CollisionGame game = CreateGame();
        game.InitializeA(WriteProject("A"));
        GumProjectSave projectB = game.InitializeB(WriteProject("B"));
        ScreenSave screen = projectB.Screens.First(item => item.Name == "MainScreen");

        GraphicalUiElement detached;
        global::RenderingLibrary.IGumService.Default = game.ServiceB;
        {
            detached = screen.ToGraphicalUiElement(game.ServiceB.SystemManagers, addToManagers: false);
        }

        Gum.GumContext.OwnerOf(detached).ShouldBeSameAs(game.ServiceB.Context);
    }

    #endregion
}
