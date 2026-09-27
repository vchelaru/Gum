using Gum;
using Gum.DataTypes;
using Gum.Wireframe;
using Shouldly;
using System;
using System.IO;
using Xunit;

namespace MonoGameGum.Tests.Hot;

/// <summary>
/// Pins the 200 ms hot-reload debounce with an injected clock. A file change arrives on the
/// watcher's thread while the game thread runs <see cref="GumHotReloadManager.Update"/>, so the
/// change must never be visible as pending with an older change's timestamp (#5277).
/// </summary>
public class GumHotReloadDebounceTests : BaseTestClass
{
    [Fact]
    public void Update_WaitsTheFullDebounce_EvenWhenItRunsWhileAChangeIsBeingRecorded()
    {
        string sourceDirectory = Path.Combine(
            Path.GetTempPath(), "GumHotReloadDebounceTests_" + Path.GetRandomFileName());
        Directory.CreateDirectory(sourceDirectory);

        try
        {
            string gumxPath = Path.Combine(sourceDirectory, "Proj.gumx");
            new GumProjectSave().Save(gumxPath, saveElements: false);

            DateTime now = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            Action? duringNextClockRead = null;
            int reloadCount = 0;
            GumHotReloadManager manager = null!;
            manager = new GumHotReloadManager(
                _ => reloadCount++,
                GumAnimationLoader.LoadAnimationsFromProvider,
                _ => { },
                () =>
                {
                    Action? action = duringNextClockRead;
                    duringNextClockRead = null;
                    action?.Invoke();
                    return now;
                });
            manager.Start(gumxPath);
            manager.Stop();

            manager.NotifyFileChanged(gumxPath);
            now = now.AddMilliseconds(300);
            manager.Update(Array.Empty<GraphicalUiElement>());
            reloadCount.ShouldBe(1);

            // The game thread's Update runs in the middle of recording the next change.
            duringNextClockRead = () => manager.Update(Array.Empty<GraphicalUiElement>());
            manager.NotifyFileChanged(gumxPath);
            reloadCount.ShouldBe(1, "the second change is 0 ms old, so the debounce has not passed");

            now = now.AddMilliseconds(199);
            manager.Update(Array.Empty<GraphicalUiElement>());
            reloadCount.ShouldBe(1);

            now = now.AddMilliseconds(1);
            manager.Update(Array.Empty<GraphicalUiElement>());
            reloadCount.ShouldBe(2);
        }
        finally
        {
            try { Directory.Delete(sourceDirectory, recursive: true); } catch { /* best-effort */ }
        }
    }
}
