using Gum.Automation;
using Gum.Forms;
using Gum.Forms.Controls;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Automation;

/// <summary>
/// <see cref="GumAutomationHost"/> run the way a game runs it: this test thread is the game loop and
/// calls <see cref="GumAutomationHost.BeforeUpdate"/> then Gum's Update each frame, while the script
/// plays on the host's worker thread.
/// </summary>
public class GumAutomationHostTests : BaseTestClass
{
    private readonly ICursor? _originalCursor = FormsUtilities.Cursor;
    private readonly IInputReceiverKeyboard? _originalKeyboard = FormsUtilities.Keyboard;

    public override void Dispose()
    {
        if (_originalCursor != null)
        {
            FormsUtilities.SetCursor(_originalCursor);
        }
        if (_originalKeyboard != null)
        {
            FormsUtilities.SetKeyboard(_originalKeyboard);
        }
        base.Dispose();
    }

    private sealed class FakePlacer : IWindowPlacer
    {
        public IntPtr MainWindow { get; set; } = (IntPtr)100;
        public bool Foreground { get; set; } = true;
        public IntPtr Below { get; set; } = (IntPtr)200;
        public List<string> Calls { get; } = new();

        public bool IsForeground(IntPtr window) { Calls.Add("IsForeground"); return Foreground; }
        public IntPtr WindowBelow(IntPtr window) { Calls.Add("WindowBelow"); return Below; }
        public void MoveOffScreenToBottom(IntPtr window) => Calls.Add("Move");
        public void Focus(IntPtr window) => Calls.Add($"Focus:{window}");
    }

    private static GumScriptResult RunGameLoop(GumAutomationHost host, int maxFrames = 500)
    {
        TimeSpan step = TimeSpan.FromTicks(TimeSpan.TicksPerSecond / 60);
        TimeSpan total = TimeSpan.Zero;
        for (int frame = 0; frame < maxFrames && !host.IsFinished; frame++)
        {
            host.BeforeUpdate();
            total += step;
            Gum.GumService.Default.Update(new GameTime(total, step));
        }
        return host.Result.ShouldNotBeNull("the script did not finish within the frame budget");
    }

    [Fact]
    public void BeforeUpdate_EachFrame_PlaysTheScriptThroughTheGameLoopAndParksTheWindow()
    {
        Button button = new Button { Name = "OkButton" };
        button.AddToRoot();
        int clicks = 0;
        button.Click += (_, _) => clicks++;
        FakePlacer placer = new FakePlacer();
        GumScriptResult? finished = null;
        GumAutomationHost host = new GumAutomationHost("""
            {"cmd":"click","target":"OkButton"}
            {"cmd":"click","target":"OkButton"}
            {"cmd":"assert","target":"OkButton","property":"Name","equals":"OkButton"}
            """,
            () => GumDriver.RootsOf(Gum.GumService.Default), game: null,
            new AutomationWindowParker(placer), result => finished = result);

        GumScriptResult result = RunGameLoop(host);

        result.Passed.ShouldBeTrue(result.ToString());
        finished.ShouldBeSameAs(result);
        clicks.ShouldBe(2);
        // Two clicks are six frames, all run by the game loop.
        host.Driver.FrameCount.ShouldBe(6);
        placer.Calls.ShouldContain("Move");
        placer.Calls.ShouldContain("Focus:200");
    }

    [Fact]
    public void RunOnGameThread_RunsBetweenFramesAndReportsFailures()
    {
        GumAutomationHost host = new GumAutomationHost("""
            {"cmd":"step"}
            {"cmd":"screenshot","path":"out.png"}
            """,
            () => GumDriver.RootsOf(Gum.GumService.Default), game: null, parker: null, _ => { });
        int gameThread = Environment.CurrentManagedThreadId;
        int? ranOn = null;
        host.Driver.ScreenshotWriter = path => host.RunOnGameThread(() =>
        {
            ranOn = Environment.CurrentManagedThreadId;
            throw new InvalidOperationException("no device");
        });

        GumScriptResult result = RunGameLoop(host);

        ranOn.ShouldBe(gameThread);
        LineResult failure = result.FirstFailure.ShouldNotBeNull();
        failure.LineNumber.ShouldBe(2);
        failure.Error.ShouldNotBeNull().ShouldContain("no device");
    }
}
