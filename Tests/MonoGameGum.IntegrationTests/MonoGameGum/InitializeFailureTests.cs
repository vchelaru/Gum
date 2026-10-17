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
/// A failed Initialize (for example a missing project file) must roll back, so the service is not left
/// half-initialized and the caller can fix the problem and call Initialize again (#6066).
/// </summary>
[Collection(SharedGame.CollectionName)]
public class InitializeFailureTests : BaseTestClass
{
    [Fact]
    public void Initialize_WithMissingProject_ThrowsAndAllowsRetry()
    {
        using ServiceTestGame game = new();
        game.Start();
        GumService service = game.CreateService();
        string missing = Path.Combine(Path.GetTempPath(), "GumLifecycle_" + Guid.NewGuid().ToString("N"), "Missing.gumx");

        Should.Throw<Exception>(() => service.Initialize(game, missing));

        service.IsInitialized.ShouldBeFalse("a failed Initialize should not leave the service half-initialized");
        Should.NotThrow(() => service.Initialize(game, DefaultVisualsVersion.V3));
        service.IsInitialized.ShouldBeTrue();
        SystemManagers.Default.ShouldBeSameAs(service.SystemManagers);
    }

    [Fact]
    public void Initialize_WithMissingProject_DoesNotLeaveDefaultsPointingAtFailedService()
    {
        using ServiceTestGame game = new();
        game.Start();
        GumService service = game.CreateService();
        string missing = Path.Combine(Path.GetTempPath(), "GumLifecycle_" + Guid.NewGuid().ToString("N"), "Missing.gumx");

        Should.Throw<Exception>(() => service.Initialize(game, missing));

        IGumService.Default.ShouldNotBeSameAs(service);
        SystemManagers.Default.ShouldBeNull();
        FrameworkElement.PopupRoot.ShouldBeNull();
    }
}
