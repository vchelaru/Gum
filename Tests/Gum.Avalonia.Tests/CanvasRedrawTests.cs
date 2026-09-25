using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Gum.Avalonia.Canvas;
using Gum.Plugins;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Gum.Avalonia.Tests;

/// <summary>
/// On-demand canvas rendering (#4989): the canvases draw only after something could have changed
/// what they show, or while something animates, and sit idle otherwise.
/// </summary>
public class CanvasRedrawTests
{
    private sealed class ManualTimeProvider : TimeProvider
    {
        private long _timestamp;

        public override long TimestampFrequency => TimeSpan.TicksPerSecond;

        public override long GetTimestamp() => _timestamp;

        public void Advance(TimeSpan amount) => _timestamp += amount.Ticks;
    }

    [Fact]
    public void Scheduler_NeedsRedraw_OnlyUntilTheSettlePeriodAfterARequestEnds()
    {
        ManualTimeProvider time = new ManualTimeProvider();
        CanvasRedrawScheduler scheduler = new CanvasRedrawScheduler(time);

        scheduler.IsRedrawNeeded.ShouldBeFalse();

        scheduler.RequestRedraw();
        scheduler.IsRedrawNeeded.ShouldBeTrue();

        time.Advance(CanvasRedrawScheduler.SettleDuration - TimeSpan.FromMilliseconds(1));
        scheduler.IsRedrawNeeded.ShouldBeTrue();

        time.Advance(TimeSpan.FromMilliseconds(2));
        scheduler.IsRedrawNeeded.ShouldBeFalse();
    }

    [Fact]
    public void Scheduler_NeedsRedraw_WhileAContinuousSourceIsChanging()
    {
        CanvasRedrawScheduler scheduler = new CanvasRedrawScheduler(new ManualTimeProvider());
        bool isPlaying = true;
        scheduler.AddContinuousRedrawSource(() => isPlaying);

        scheduler.IsRedrawNeeded.ShouldBeTrue();

        isPlaying = false;
        scheduler.IsRedrawNeeded.ShouldBeFalse();
    }

    [Fact]
    public void FrameGate_DrawsTheFirstFrame_ThenOnlyWhenSomethingChanged()
    {
        ManualTimeProvider time = new ManualTimeProvider();
        CanvasRedrawScheduler scheduler = new CanvasRedrawScheduler(time);
        CanvasFrameGate gate = new CanvasFrameGate(scheduler);

        gate.ShouldDraw(100, 50, hasRenderError: false).ShouldBeTrue("the first frame");
        gate.MarkDrawn(100, 50);
        gate.ShouldDraw(100, 50, hasRenderError: false).ShouldBeFalse("idle");

        gate.ShouldDraw(120, 50, hasRenderError: false).ShouldBeTrue("the surface was resized");
        gate.MarkDrawn(120, 50);

        gate.ShouldDraw(120, 50, hasRenderError: true).ShouldBeTrue("a failed frame retries");

        gate.MarkSkipped();
        gate.ShouldDraw(120, 50, hasRenderError: false).ShouldBeTrue("changes may have landed while hidden");
        gate.MarkDrawn(120, 50);

        scheduler.RequestRedraw();
        gate.ShouldDraw(120, 50, hasRenderError: false).ShouldBeTrue("a redraw was requested");
        time.Advance(CanvasRedrawScheduler.SettleDuration + TimeSpan.FromMilliseconds(1));
        gate.ShouldDraw(120, 50, hasRenderError: false).ShouldBeFalse("the request settled");
    }

    [AvaloniaFact]
    public void InputHook_RequestsARedraw_ForInputInAnyWindow()
    {
        CanvasRedrawScheduler scheduler = new CanvasRedrawScheduler(new ManualTimeProvider());
        int requestCount = 0;
        scheduler.RedrawRequested += () => requestCount++;
        using IDisposable hook = CanvasInputRedrawHook.Install(scheduler);
        Window dialog = new Window { Content = new TextBox() };
        dialog.Show();

        dialog.KeyPressQwerty(PhysicalKey.A, RawInputModifiers.None);
        requestCount.ShouldBeGreaterThan(0, "key input");

        requestCount = 0;
        dialog.MouseMove(new global::Avalonia.Point(5, 5));
        requestCount.ShouldBeGreaterThan(0, "pointer input");

        dialog.Close();
    }

    [AvaloniaFact]
    public void EditorTab_RequestsARedraw_WhenTheWireframeRefreshes()
    {
        ToolStartup.EnsureInitialized();
        ICanvasRedrawScheduler scheduler = TestAppBuilder.Services.GetRequiredService<ICanvasRedrawScheduler>();
        int requestCount = 0;
        Action countRequest = () => requestCount++;
        scheduler.RedrawRequested += countRequest;

        try
        {
            TestAppBuilder.Services.GetRequiredService<PluginManager>().WireframeRefreshed();
        }
        finally
        {
            scheduler.RedrawRequested -= countRequest;
        }

        requestCount.ShouldBeGreaterThan(0, "a project load or a file changed on disk refreshes the wireframe with no input behind it");
    }

    [AvaloniaFact]
    public void InputHook_StopsRequesting_OnceDisposed()
    {
        CanvasRedrawScheduler scheduler = new CanvasRedrawScheduler(new ManualTimeProvider());
        int requestCount = 0;
        scheduler.RedrawRequested += () => requestCount++;
        IDisposable hook = CanvasInputRedrawHook.Install(scheduler);
        hook.Dispose();
        Window window = new Window { Content = new TextBox() };
        window.Show();

        window.KeyPressQwerty(PhysicalKey.A, RawInputModifiers.None);

        requestCount.ShouldBe(0);
        window.Close();
    }
}
