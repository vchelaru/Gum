using System;
using System.IO;
using System.Xml.Serialization;
using Gum;
using Gum.DataTypes;
using Gum.Managers;
using Gum.StateAnimation.SaveClasses;
using Gum.Wireframe;
using RenderingLibrary.Content;
using Shouldly;
using ToolsUtilities;
using Xunit;

namespace SkiaGum.Tests.Hot;

/// <summary>
/// Drives the animation side of hot reload through <see cref="GumHotReloadManager.PerformReload"/>
/// directly (bypassing the FileSystemWatcher + debounce), the same way the MonoGame/Raylib
/// equivalents do. Pins that <see cref="GumServiceSkiaBase.EnableHotReload"/> wires the real
/// <see cref="GumAnimationLoader.LoadAnimationsFromProvider"/> rather than the pre-#4460 no-op
/// (issue #4460).
/// </summary>
public class HotReloadAnimationReloadTests
{
    [Fact]
    public void PerformReload_loads_animations_from_the_source_directory()
    {
        string sourceDirectory = Path.Combine(
            Path.GetTempPath(), "SkiaHotReloadAnimationReloadTests_" + Path.GetRandomFileName());
        Directory.CreateDirectory(sourceDirectory);

        try
        {
            string gumxPath = Path.Combine(sourceDirectory, "Proj.gumx");
            new GumProjectSave().Save(gumxPath, saveElements: false);

            string screensDirectory = Path.Combine(sourceDirectory, "Screens");
            Directory.CreateDirectory(screensDirectory);
            File.WriteAllBytes(Path.Combine(screensDirectory, "MainScreenAnimations.ganx"), Ganx());

            GumHotReloadManager manager = new GumHotReloadManager(
                applyProjectTextureFilter: _ => { },
                loadAnimationsFromProvider: GumAnimationLoader.LoadAnimationsFromProvider,
                disposeCachedAsset: path => LoaderManager.Self.Dispose(path));
            manager.Start(gumxPath);
            // Release the OS watcher immediately; Start has already recorded the source path.
            manager.Stop();

            manager.PerformReload(Array.Empty<GraphicalUiElement>());

            GumProjectSave reloaded = ObjectFinder.Self.GumProjectSave!;
            reloaded.ShouldNotBeNull();
            ElementAnimationsSave animation = reloaded.ElementAnimations.ShouldHaveSingleItem();
            animation.ElementName.ShouldBe("MainScreen");
        }
        finally
        {
            // ObjectFinder.Self.GumProjectSave is process-wide static state (no BaseTestClass/Dispose
            // hook in this test project to reset it) — leaving it set here would fail an unrelated
            // "no project loaded" test in another class, since SkiaGum.Tests disables parallelization
            // but doesn't isolate static state between test classes.
            ObjectFinder.Self.GumProjectSave = null;
            try { Directory.Delete(sourceDirectory, recursive: true); } catch { /* best-effort */ }
        }
    }

    private static byte[] Ganx()
    {
        ElementAnimationsSave save = new ElementAnimationsSave();
        save.ElementName = "WRONG"; // loader must overwrite from the file path
        XmlSerializer serializer = FileManager.GetXmlSerializer(typeof(ElementAnimationsSave));
        using MemoryStream memoryStream = new MemoryStream();
        serializer.Serialize(memoryStream, save);
        return memoryStream.ToArray();
    }
}
