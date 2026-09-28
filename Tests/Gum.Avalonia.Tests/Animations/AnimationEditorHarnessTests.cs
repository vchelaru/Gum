using Avalonia.Headless.XUnit;
using Shouldly;

namespace Gum.Avalonia.Tests.Animations;

/// <summary>The harness's own waits: which clock bounds them.</summary>
public class AnimationEditorHarnessTests
{
    [AvaloniaFact]
    public void WaitUntil_WorkOnAnotherThread_WaitsForItOnTheWallClock()
    {
        // A project load deserializes on the thread pool, so no amount of playback ticks finishes
        // it; a wait bounded by the playback clock gave up within milliseconds (#5402).
        using AnimationEditorHarness editor = new AnimationEditorHarness();
        Task work = Task.Run(() => Thread.Sleep(300));

        editor.WaitUntil(() => work.IsCompleted, TimeSpan.FromSeconds(30), "the background work");

        work.IsCompleted.ShouldBeTrue();
    }
}
