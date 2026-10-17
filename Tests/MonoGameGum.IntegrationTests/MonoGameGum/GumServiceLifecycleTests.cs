using Gum.Forms;
using Gum.Managers;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using RenderingLibrary;
using Shouldly;
using System;
using System.IO;
using ToolsUtilities;

namespace MonoGameGum.IntegrationTests.MonoGameGum;

// The enclosing MonoGameGum namespace holds an obsolete GumService shim; bind to the real type.
using GumService = global::Gum.GumService;

/// <summary>
/// Edge cases in the <see cref="GumService"/> lifecycle (#6066): calls out of order, repeated calls,
/// a failed Initialize followed by a retry, services created with their own SystemManagers, and a
/// Game that goes away without Uninitialize.
/// </summary>
[Collection(SharedGame.CollectionName)]
public class GumServiceLifecycleTests : BaseTestClass
{
    private static readonly GameTime FrameTime = new(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1.0 / 60.0));

    // -------------------------------------------------------------------------
    // Uninitialize ordering
    // -------------------------------------------------------------------------

    [Fact]
    public void Uninitialize_BeforeInitialize_DoesNotThrow()
    {
        GumService service = new();

        Should.NotThrow(() => service.Uninitialize());

        service.IsInitialized.ShouldBeFalse();
    }

    [Fact]
    public void Uninitialize_Twice_DoesNotThrow()
    {
        using ServiceTestGame game = new();
        game.Start();
        GumService service = game.CreateService();
        service.Initialize(game, DefaultVisualsVersion.V3);
        service.Uninitialize();

        Should.NotThrow(() => service.Uninitialize());

        service.IsInitialized.ShouldBeFalse();
    }

    // -------------------------------------------------------------------------
    // Initialize ordering
    // -------------------------------------------------------------------------

    [Fact]
    public void Initialize_Twice_ThrowsAndLeavesFirstInitializationIntact()
    {
        using ServiceTestGame game = new();
        game.Start();
        GumService service = game.CreateService();
        service.Initialize(game, DefaultVisualsVersion.V3);
        SystemManagers managers = service.SystemManagers;

        Should.Throw<InvalidOperationException>(() => service.Initialize(game, DefaultVisualsVersion.V3));

        service.IsInitialized.ShouldBeTrue();
        service.SystemManagers.ShouldBeSameAs(managers);
        SystemManagers.Default.ShouldBeSameAs(managers);
    }

    [Fact]
    public void Initialize_AfterUninitialize_Succeeds()
    {
        using ServiceTestGame game = new();
        game.Start();
        GumService service = game.CreateService();
        service.Initialize(game, DefaultVisualsVersion.V3);
        service.Uninitialize();

        service.Initialize(game, DefaultVisualsVersion.V3);

        service.IsInitialized.ShouldBeTrue();
        SystemManagers.Default.ShouldBeSameAs(service.SystemManagers);
        IGumService.Default.ShouldBeSameAs(service);
    }

    // -------------------------------------------------------------------------
    // External SystemManagers
    // -------------------------------------------------------------------------

    [Fact]
    public void Initialize_WithExternalSystemManagers_DoesNotChangeSystemManagersDefault()
    {
        using ServiceTestGame game = new();
        game.Start();
        GumService hostService = game.CreateService();
        hostService.Initialize(game, DefaultVisualsVersion.V3);
        SystemManagers hostManagers = hostService.SystemManagers;
        GumService external = game.CreateService();
        SystemManagers externalManagers = new();

        external.Initialize(game, externalManagers);

        external.SystemManagers.ShouldBeSameAs(externalManagers);
        SystemManagers.Default.ShouldBeSameAs(hostManagers);
        ISystemManagers.Default.ShouldBeSameAs(hostManagers);
    }

    [Fact]
    public void Initialize_WithExternalSystemManagers_WhenNoDefaultExists_AdoptsThemSoTheServiceWorks()
    {
        // Cursor, ScrollViewer, ListBox and others read SystemManagers.Default; a lone service that is
        // handed its own managers must not be left with a null default and a NullReferenceException
        // in Update.
        using ServiceTestGame game = new();
        game.Start();
        GumService service = game.CreateService();
        SystemManagers externalManagers = new();

        service.Initialize(game, externalManagers);

        SystemManagers.Default.ShouldBeSameAs(externalManagers);
        ISystemManagers.Default.ShouldBeSameAs(externalManagers);
        Should.NotThrow(() => service.Update(FrameTime));
        Should.NotThrow(() => service.Draw());

        service.Uninitialize();

        SystemManagers.Default.ShouldBeNull();
        ISystemManagers.Default.ShouldBeNull();
    }

    [Fact]
    public void Uninitialize_OfServiceWithExternalSystemManagers_LeavesSystemManagersDefaultAlone()
    {
        using ServiceTestGame game = new();
        game.Start();
        GumService hostService = game.CreateService();
        hostService.Initialize(game, DefaultVisualsVersion.V3);
        SystemManagers hostManagers = hostService.SystemManagers;
        GumService external = game.CreateService();
        external.Initialize(game, new SystemManagers());

        external.Uninitialize();

        SystemManagers.Default.ShouldBeSameAs(hostManagers);
        ISystemManagers.Default.ShouldBeSameAs(hostManagers);
        hostService.IsInitialized.ShouldBeTrue();
    }

    [Fact]
    public void Uninitialize_OfServiceWithExternalSystemManagers_KeepsHostSetDefaultOfThoseManagers()
    {
        // A host (for example an embedding engine) that hands in its own managers and also publishes
        // them as the default owns that default; the service must clear it only because it is
        // pointing at this service's managers, which is the same object here.
        using ServiceTestGame game = new();
        game.Start();
        GumService external = game.CreateService();
        SystemManagers externalManagers = new();
        SystemManagers.Default = externalManagers;
        ISystemManagers.Default = externalManagers;
        external.Initialize(game, externalManagers);

        external.Uninitialize();

        SystemManagers.Default.ShouldBeNull();
    }

    // -------------------------------------------------------------------------
    // GumService.Default
    // -------------------------------------------------------------------------

    [Fact]
    public void GumServiceDefault_AfterEveryServiceIsUninitialized_IsAFreshUninitializedService()
    {
        using ServiceTestGame game = new();
        game.Start();
        GumService first = GumService.Default;
        first.Initialize(game, DefaultVisualsVersion.V3);
        first.Uninitialize();

        GumService lazy = GumService.Default;

        lazy.ShouldNotBeSameAs(first);
        lazy.IsInitialized.ShouldBeFalse();
        Should.Throw<InvalidOperationException>(() => _ = lazy.SystemManagers)
            .Message.ShouldContain("not been initialized");
        lazy.Root.ShouldNotBeNull();
    }

    [Fact]
    public void GumServiceDefault_AfterLazyRecreate_CanBeInitializedAgain()
    {
        using ServiceTestGame game = new();
        game.Start();
        GumService.Default.Initialize(game, DefaultVisualsVersion.V3);
        GumService.Default.Uninitialize();

        GumService.Default.Initialize(game, DefaultVisualsVersion.V3);

        GumService.Default.IsInitialized.ShouldBeTrue();
        IGumService.Default.ShouldBeSameAs(GumService.Default);
        GumService.Default.Uninitialize();
    }

    [Fact]
    public void NewGumService_DoesNotReplaceGumServiceDefault()
    {
        using ServiceTestGame game = new();
        game.Start();
        GumService defaultService = GumService.Default;
        GumService second = new();
        game.Track(second);

        second.Initialize(game, DefaultVisualsVersion.V3);

        GumService.Default.ShouldBeSameAs(defaultService);
        IGumService.Default.ShouldBeSameAs(second);
        SystemManagers.Default.ShouldBeSameAs(second.SystemManagers);
    }

    // -------------------------------------------------------------------------
    // Use after Uninitialize
    // -------------------------------------------------------------------------

    [Fact]
    public void Draw_AfterUninitialize_ThrowsInvalidOperationException()
    {
        using ServiceTestGame game = new();
        game.Start();
        GumService service = game.CreateService();
        service.Initialize(game, DefaultVisualsVersion.V3);
        service.Uninitialize();

        Should.Throw<InvalidOperationException>(() => service.Draw())
            .Message.ShouldContain("not been initialized");
    }

    [Fact]
    public void Update_AfterUninitialize_ThrowsInvalidOperationException()
    {
        using ServiceTestGame game = new();
        game.Start();
        GumService service = game.CreateService();
        service.Initialize(game, DefaultVisualsVersion.V3);
        service.Uninitialize();

        Should.Throw<InvalidOperationException>(() => service.Update(FrameTime));
    }

    // -------------------------------------------------------------------------
    // Game goes away
    // -------------------------------------------------------------------------

    [Fact]
    public void Uninitialize_AfterGameDisposed_DoesNotThrowAndResetsService()
    {
        GumService service = new();
        ServiceTestGame game = new();
        game.Start();
        service.Initialize(game, DefaultVisualsVersion.V3);
        service.Root.AddChild(new ContainerRuntime());
        game.Dispose();

        Should.NotThrow(() => service.Uninitialize());

        service.IsInitialized.ShouldBeFalse();
        SystemManagers.Default.ShouldBeNull();
    }

    [Fact]
    public void Initialize_OnNewGame_AfterPreviousGameDisposedWithoutUninitialize_Throws()
    {
        // Pins the current contract: Gum cannot see the Game go away, so a service that was never
        // uninitialized still reports IsInitialized and refuses a second Initialize.
        GumService service = new();
        ServiceTestGame first = new();
        first.Start();
        service.Initialize(first, DefaultVisualsVersion.V3);
        first.Dispose();
        service.IsInitialized.ShouldBeTrue();

        using ServiceTestGame second = new();
        second.Start();
        second.Track(service);

        Should.Throw<InvalidOperationException>(() => service.Initialize(second, DefaultVisualsVersion.V3));
    }
}
