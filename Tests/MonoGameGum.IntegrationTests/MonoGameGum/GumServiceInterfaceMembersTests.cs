using Gum.Forms;
using Gum.Forms.Controls;
using Gum.Wireframe;
using RenderingLibrary;
using Shouldly;
using System;
using System.Collections.Generic;

namespace MonoGameGum.IntegrationTests.MonoGameGum;

// The enclosing MonoGameGum namespace holds an obsolete GumService shim; bind to the real type.
using GumService = global::Gum.GumService;

/// <summary>
/// Members of <see cref="IGumService"/> that act on a fully initialized service (#6325): the headless
/// cases are in MonoGameGum.Tests (<c>IGumServiceParityTests</c>).
/// </summary>
[Collection(SharedGame.CollectionName)]
public class GumServiceInterfaceMembersTests : BaseTestClass
{
    [Fact]
    public void Uninitialize_ThroughTheInterface_TearsTheServiceDown()
    {
        using ServiceTestGame game = new();
        game.Start();
        GumService service = game.CreateService();
        service.Initialize(game, DefaultVisualsVersion.V3);

        ((IGumService)service).Uninitialize();

        service.IsInitialized.ShouldBeFalse();
        ((IGumService)service).Cursor.ShouldBeNull();
    }

    [Fact]
    public void Update_WithTotalSeconds_ThroughTheInterface_PumpsTheInstalledCursor()
    {
        using ServiceTestGame game = new();
        game.Start();
        GumService service = game.CreateService();
        service.Initialize(game, DefaultVisualsVersion.V3);
        Moq.Mock<Gum.Wireframe.ICursor> cursor = new();
        FormsUtilities.SetCursor(cursor.Object);

        ((IGumService)service).Update(3.0);

        cursor.Verify(c => c.Activity(3.0), Moq.Times.Once());
        service.GameTime.ShouldNotBeNull().TotalGameTime.ShouldBe(TimeSpan.FromSeconds(3));
        ((IGumService)service).GameTime.ShouldBe(3f);
        service.Uninitialize();
    }

    [Fact]
    public void PopupRootAndModalRoot_ThroughTheInterface_AreTheServicesRoots()
    {
        using ServiceTestGame game = new();
        game.Start();
        GumService service = game.CreateService();
        service.Initialize(game, DefaultVisualsVersion.V3);

        ((IGumService)service).PopupRoot.ShouldBeSameAs(service.PopupRoot);
        ((IGumService)service).ModalRoot.ShouldBeSameAs(service.ModalRoot);
        ((IGumService)service).PopupRoot.ShouldBeSameAs(FrameworkElement.PopupRoot);
        service.Uninitialize();
    }

    [Fact]
    public void Update_WithTotalSeconds_AfterUninitializeAndInitialize_AdvancesByZeroOnTheFirstFrame()
    {
        using ServiceTestGame game = new();
        game.Start();
        GumService service = game.CreateService();
        service.Initialize(game, DefaultVisualsVersion.V3);
        ((IGumService)service).Update(100.0);
        service.Uninitialize();
        service.Initialize(game, DefaultVisualsVersion.V3);
        ElapsedRecorder recorder = new ElapsedRecorder();
        service.Root.Children.Add(new InteractiveGue { FormsControlAsObject = recorder });

        ((IGumService)service).Update(1.0);

        recorder.Seconds.ShouldBe(new[] { 0.0 });
        service.Uninitialize();
    }

    [Fact]
    public void GameTime_AfterUninitialize_IsNull()
    {
        using ServiceTestGame game = new();
        game.Start();
        GumService service = game.CreateService();
        service.Initialize(game, DefaultVisualsVersion.V3);
        ((IGumService)service).Update(5.0);

        service.Uninitialize();

        service.GameTime.ShouldBeNull();
        ((IGumService)service).GameTime.ShouldBeNull();
    }

    private class ElapsedRecorder : IUpdateEveryFrame
    {
        public List<double> Seconds { get; } = new List<double>();

        public void Activity(double secondDifference) => Seconds.Add(secondDifference);
    }
}
