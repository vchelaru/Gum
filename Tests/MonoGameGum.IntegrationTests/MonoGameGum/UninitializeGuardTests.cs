using Gum.Forms;
using Gum.Forms.Controls;
using Gum.Managers;
using Gum.Wireframe;
using RenderingLibrary;
using Shouldly;
using System;
using System.IO;

namespace MonoGameGum.IntegrationTests.MonoGameGum;

// The enclosing MonoGameGum namespace holds an obsolete GumService shim; bind to the real type.
using GumService = global::Gum.GumService;

/// <summary>
/// Uninitialize on a service that is not initialized has nothing of its own to tear down, so it must
/// not reset the process-wide state a different, live service depends on (#6066).
/// </summary>
public class UninitializeGuardTests : BaseTestClass
{
    [Fact]
    public void Uninitialize_BeforeInitialize_LeavesAnotherLiveServiceIntact()
    {
        using ServiceTestGame game = new();
        game.Start();
        GumService live = game.CreateService();
        live.Initialize(game, DefaultVisualsVersion.V3);
        SystemManagers liveManagers = live.SystemManagers;
        InteractiveGue popupRoot = FrameworkElement.PopupRoot;
        ObjectFinder.Self.GumProjectSave = new Gum.DataTypes.GumProjectSave();
        GumService neverInitialized = new();

        neverInitialized.Uninitialize();

        IGumService.Default.ShouldBeSameAs(live);
        SystemManagers.Default.ShouldBeSameAs(liveManagers);
        FrameworkElement.PopupRoot.ShouldBeSameAs(popupRoot,
            "a service that was never initialized has nothing to tear down");
        ObjectFinder.Self.GumProjectSave.ShouldNotBeNull();
    }

    [Fact]
    public void Uninitialize_SecondCall_DoesNotTearDownAServiceInitializedInBetween()
    {
        using ServiceTestGame game = new();
        game.Start();
        GumService first = game.CreateService();
        first.Initialize(game, DefaultVisualsVersion.V3);
        first.Uninitialize();
        GumService second = game.CreateService();
        second.Initialize(game, DefaultVisualsVersion.V3);
        InteractiveGue popupRoot = FrameworkElement.PopupRoot;

        first.Uninitialize();

        IGumService.Default.ShouldBeSameAs(second);
        SystemManagers.Default.ShouldBeSameAs(second.SystemManagers);
        FrameworkElement.PopupRoot.ShouldBeSameAs(popupRoot);
        FrameworkElement.MainCursor.ShouldNotBeNull();
        GraphicalUiElement.AddRenderableToManagers.ShouldNotBeNull();
    }
}
