using Gum.Automation;
using Gum.Forms;
using Gum.Forms.Controls;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using RenderingLibrary;
using Shouldly;
using System;
using System.IO;
using System.Runtime.CompilerServices;
using Xunit;

namespace MonoGameGum.IntegrationTests.MonoGameGum;

/// <summary>
/// <c>GumService.Uninitialize</c> releases the input state the service owns (the pushed visual, queued
/// next-push and next-click actions, the last event roots, the hovered tooltip host and the scratch lists
/// the input walk fills) and leaves another live service's input working (#8473). Released means
/// unreachable: each test holds a <see cref="WeakReference"/> to the old visual and checks that a
/// collection frees it.
/// </summary>
[Collection(SharedGame.CollectionName)]
public class GumServiceUninitializeInputStateTests : BaseTestClass
{
    private readonly string _root;
    private double _seconds;
    private TimeSpan _initialShowDelayBefore;

    public GumServiceUninitializeInputStateTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "GumUninitInput_" + Guid.NewGuid().ToString("N"));
        _initialShowDelayBefore = ToolTipService.InitialShowDelay;
    }

    public override void Dispose()
    {
        ToolTipService.InitialShowDelay = _initialShowDelayBefore;
        base.Dispose();
        try { Directory.Delete(_root, recursive: true); } catch { /* best effort */ }
    }

    #region Helpers

    private string WriteProject(string name)
    {
        GumProjectFolder folder = new GumProjectFolder(Path.Combine(_root, name));
        folder.Add(new GumElement("MainScreen", isScreen: true));
        return folder.Write();
    }

    private sealed class Rig : IDisposable
    {
        public CollisionGame Game { get; } = new CollisionGame();
        public ScriptedCursor? CursorA { get; set; }
        public ScriptedCursor? CursorB { get; set; }
        public void Dispose()
        {
            ToolTipService.ResetForTesting();
            Game.Dispose();
        }
    }

    private Rig CreateRig(bool twoServices)
    {
        Rig rig = new Rig();
        rig.Game.RunOneFrame();
        IGumService.Default = null;
        rig.Game.InitializeA(WriteProject("A"));
        rig.CursorA = InstallCursor(rig.Game.ServiceA);
        if (twoServices)
        {
            rig.Game.InitializeB(WriteProject("B"));
            rig.CursorB = InstallCursor(rig.Game.ServiceB);
        }
        return rig;
    }

    private static ScriptedCursor InstallCursor(Gum.GumService service)
    {
        IGumService.Default = service;
        ScriptedCursor cursor = new ScriptedCursor();
        cursor.Place(-50, -50, leftDown: false);
        FormsUtilities.SetCursor(cursor);
        return cursor;
    }

    private void Tick(Gum.GumService service, double seconds = 1.0 / 60.0)
    {
        _seconds += seconds;
        IGumService.Default = service;
        service.Update(new GameTime(TimeSpan.FromSeconds(_seconds), TimeSpan.FromSeconds(seconds)));
    }

    private void Click(Gum.GumService service, ScriptedCursor cursor, int x, int y)
    {
        for (int frame = 0; frame < 3; frame++)
        {
            cursor.Place(x, y, leftDown: frame == 1);
            Tick(service);
        }
    }

    // Builds the control in its own frame so no local in the test keeps it alive.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference AddControl<T>(Gum.GumService service, Action<T>? configure = null) where T : FrameworkElement, new()
    {
        IGumService.Default = service;
        T control = new T();
        control.Visual.X = 10;
        control.Visual.Y = 10;
        configure?.Invoke(control);
        service.Root.Children.Add(control.Visual);
        return new WeakReference(control.Visual);
    }

    private static bool IsCollected(WeakReference reference)
    {
        for (int i = 0; i < 3 && reference.IsAlive; i++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }
        return !reference.IsAlive;
    }

    private const int OverX = 20;
    private const int OverY = 20;

    #endregion

    #region Pushed visual

    [Fact]
    public void Uninitialize_FirstServiceWithItsButtonHeld_ReleasesTheButtonAndTheSecondServiceStillClicks()
    {
        using Rig rig = CreateRig(twoServices: true);
        int clicksB = 0;
        WeakReference buttonA = AddControl<Button>(rig.Game.ServiceA);
        AddControl<Button>(rig.Game.ServiceB, b => b.Click += (_, _) => clicksB++);
        rig.CursorA!.Place(OverX, OverY, leftDown: false);
        Tick(rig.Game.ServiceA);
        rig.CursorA.Place(OverX, OverY, leftDown: true);
        Tick(rig.Game.ServiceA);

        rig.Game.ServiceA.Uninitialize();
        rig.CursorA = null;

        Click(rig.Game.ServiceB, rig.CursorB!, OverX, OverY);
        clicksB.ShouldBe(1);
        IsCollected(buttonA).ShouldBeTrue("the first service's held button is still reachable after Uninitialize");
    }

    [Fact]
    public void Uninitialize_SecondServiceWithItsButtonHeld_ReleasesTheButtonAndTheFirstServiceStillClicks()
    {
        using Rig rig = CreateRig(twoServices: true);
        int clicksA = 0;
        AddControl<Button>(rig.Game.ServiceA, b => b.Click += (_, _) => clicksA++);
        WeakReference buttonB = AddControl<Button>(rig.Game.ServiceB);
        rig.CursorB!.Place(OverX, OverY, leftDown: false);
        Tick(rig.Game.ServiceB);
        rig.CursorB.Place(OverX, OverY, leftDown: true);
        Tick(rig.Game.ServiceB);

        rig.Game.ServiceB.Uninitialize();
        rig.CursorB = null;

        Click(rig.Game.ServiceA, rig.CursorA!, OverX, OverY);
        clicksA.ShouldBe(1);
        IsCollected(buttonB).ShouldBeTrue("the second service's held button is still reachable after Uninitialize");
    }

    [Fact]
    public void Uninitialize_ThenInitializeAgain_WithButtonHeld_ReleasesTheOldButton()
    {
        using Rig rig = CreateRig(twoServices: false);
        WeakReference button = AddControl<Button>(rig.Game.ServiceA);
        rig.CursorA!.Place(OverX, OverY, leftDown: false);
        Tick(rig.Game.ServiceA);
        rig.CursorA.Place(OverX, OverY, leftDown: true);
        Tick(rig.Game.ServiceA);

        rig.Game.ServiceA.Uninitialize();
        rig.CursorA = null;
        rig.Game.InitializeA(WriteProject("A2"));
        rig.CursorA = InstallCursor(rig.Game.ServiceA);
        Tick(rig.Game.ServiceA);

        InteractiveGue.LastVisualPushed.ShouldBeNull();
        IsCollected(button).ShouldBeTrue("the held button survives Uninitialize and a new Initialize");
    }

    #endregion

    [Fact]
    public void ProbeTemp_NoInput()
    {
        using Rig rig = CreateRig(twoServices: false);
        WeakReference button = AddControl<Button>(rig.Game.ServiceA);
        Tick(rig.Game.ServiceA);
        rig.Game.ServiceA.Uninitialize();
        rig.CursorA = null;
        IsCollected(button).ShouldBeTrue("probe");
    }

    #region Next push and click actions

    [Fact]
    public void Uninitialize_ThenInitializeAgain_DoesNotRunTheOldServicesQueuedClickAndPushActions()
    {
        using Rig rig = CreateRig(twoServices: false);
        int ran = 0;
        IGumService.Default = rig.Game.ServiceA;
        InteractiveGue.AddNextClickAction(() => ran++);
        InteractiveGue.AddNextPushAction(() => ran++);

        rig.Game.ServiceA.Uninitialize();
        rig.Game.InitializeA(WriteProject("A2"));
        rig.CursorA = InstallCursor(rig.Game.ServiceA);
        Tick(rig.Game.ServiceA);
        Click(rig.Game.ServiceA, rig.CursorA, OverX, OverY);
        Click(rig.Game.ServiceA, rig.CursorA, OverX, OverY);

        ran.ShouldBe(0, "an action queued before Uninitialize ran on the next service's click");
    }

    [Fact]
    public void Uninitialize_FirstServiceWithQueuedActions_ReleasesThemAndTheSecondServiceStillRunsItsOwn()
    {
        using Rig rig = CreateRig(twoServices: true);
        int ranB = 0;
        WeakReference ownerA = QueueActionsHoldingAControl(rig.Game.ServiceA);
        IGumService.Default = rig.Game.ServiceB;
        InteractiveGue.AddNextClickAction(() => ranB++);
        Tick(rig.Game.ServiceB);

        rig.Game.ServiceA.Uninitialize();
        Click(rig.Game.ServiceB, rig.CursorB!, OverX, OverY);

        ranB.ShouldBe(1);
        IsCollected(ownerA).ShouldBeTrue("the first service's queued actions keep its control alive after Uninitialize");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference QueueActionsHoldingAControl(Gum.GumService service)
    {
        IGumService.Default = service;
        Button button = new Button();
        InteractiveGue.AddNextClickAction(() => button.Text = "clicked");
        InteractiveGue.AddNextPushAction(() => button.Text = "pushed");
        return new WeakReference(button.Visual);
    }

    #endregion

    #region Last event roots and walk scratch lists

    [Fact]
    public void LastEventRoots_AfterTheLastUpdatedServiceUninitializes_ReportTheRemainingServicesRoots()
    {
        using Rig rig = CreateRig(twoServices: true);
        Tick(rig.Game.ServiceB);
        Tick(rig.Game.ServiceA);
        GraphicalUiElement rootA = rig.Game.ServiceA.Root;

        rig.Game.ServiceA.Uninitialize();

        IGumService.Default = rig.Game.ServiceB;
        FormsUtilities.LastEventRoots.ShouldNotContain(rootA);
        FormsUtilities.LastEventRoots.ShouldContain(rig.Game.ServiceB.Root);
    }

    [Fact]
    public void LastEventRoots_AfterUninitialize_AreEmpty()
    {
        using Rig rig = CreateRig(twoServices: false);
        Tick(rig.Game.ServiceA);
        FormsUtilities.LastEventRoots.ShouldNotBeEmpty();

        rig.Game.ServiceA.Uninitialize();

        FormsUtilities.LastEventRoots.ShouldBeEmpty();
    }

    [Fact]
    public void Uninitialize_ReleasesThePopupAndModalRootsTheLastUpdateWalked()
    {
        using Rig rig = CreateRig(twoServices: false);
        Tick(rig.Game.ServiceA);
        WeakReference[] roots = CaptureFormsRoots(rig.Game.ServiceA);

        rig.Game.ServiceA.Uninitialize();
        rig.CursorA = null;

        IsCollected(roots[0]).ShouldBeTrue("the popup root is still reachable after Uninitialize");
        IsCollected(roots[1]).ShouldBeTrue("the modal root is still reachable after Uninitialize");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference[] CaptureFormsRoots(Gum.GumService service) =>
        new[] { new WeakReference(service.PopupRoot), new WeakReference(service.ModalRoot) };

    [Fact]
    public void Uninitialize_FirstServiceWithAFocusedTextBox_ReleasesItWhileTheSecondServiceKeepsUpdating()
    {
        using Rig rig = CreateRig(twoServices: true);
        WeakReference textBoxA = AddControl<TextBox>(rig.Game.ServiceA);
        Click(rig.Game.ServiceA, rig.CursorA!, OverX, OverY);
        IGumService.Default = rig.Game.ServiceA;
        InteractiveGue.CurrentInputReceiver.ShouldNotBeNull();
        Tick(rig.Game.ServiceA);

        rig.Game.ServiceA.Uninitialize();
        rig.CursorA = null;
        Tick(rig.Game.ServiceB);

        IsCollected(textBoxA).ShouldBeTrue("the first service's focused TextBox is still reachable after Uninitialize");
    }

    #endregion

    #region Tooltip hover

    [Fact]
    public void Uninitialize_WithATooltipOpen_ReleasesTheHoveredButton()
    {
        using Rig rig = CreateRig(twoServices: false);
        ToolTipService.InitialShowDelay = TimeSpan.Zero;
        WeakReference button = AddControl<Button>(rig.Game.ServiceA, b => b.ToolTip = "tip");
        rig.CursorA!.Place(OverX, OverY, leftDown: false);
        Tick(rig.Game.ServiceA);
        Tick(rig.Game.ServiceA);

        rig.Game.ServiceA.Uninitialize();
        rig.CursorA = null;

        IsCollected(button).ShouldBeTrue("the hovered tooltip host is still reachable after Uninitialize");
    }

    [Fact]
    public void Update_OnAnotherService_LeavesThisServicesOpenTooltipOpen()
    {
        using Rig rig = CreateRig(twoServices: true);
        ToolTipService.InitialShowDelay = TimeSpan.Zero;
        Tooltip? tooltipA = null;
        AddControl<Button>(rig.Game.ServiceA, b => { b.ToolTip = "tip"; tooltipA = ToolTipService.GetTooltip(b); });
        rig.CursorA!.Place(OverX, OverY, leftDown: false);
        Tick(rig.Game.ServiceA);
        Tick(rig.Game.ServiceA);
        tooltipA.ShouldNotBeNull().IsOpen.ShouldBeTrue();

        Tick(rig.Game.ServiceB);

        tooltipA.IsOpen.ShouldBeTrue("updating the second service closed the first service's tooltip");
    }

    #endregion
}
