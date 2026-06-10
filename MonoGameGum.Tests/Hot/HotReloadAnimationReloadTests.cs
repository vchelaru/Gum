using System;
using System.IO;
using System.Xml.Serialization;
using Gum.DataTypes;
using Gum.Managers;
using Gum.StateAnimation.SaveClasses;
using Gum.Wireframe;
using Shouldly;
using ToolsUtilities;
using Xunit;

namespace MonoGameGum.Tests.Hot;

/// <summary>
/// Drives the animation side of hot reload through <see cref="GumHotReloadManager.PerformReload"/>
/// directly (bypassing the FileSystemWatcher + debounce). Reload reads animations from the live
/// <em>source</em> directory by enumerating its <c>*Animations.ganx</c> files through a
/// <see cref="Gum.Bundle.LooseFileGumFileProvider"/> — this replaced the old per-element
/// <c>FileManager.FileExists</c> probe + <c>RelativeDirectory</c> swap. Empty roots keep the
/// structural <see cref="GumHotReloadManager.ApplyDiff"/> a no-op so the test isolates the
/// animation-loading path.
/// </summary>
public class HotReloadAnimationReloadTests : BaseTestClass
{
    [Fact]
    public void PerformReload_loads_animations_from_the_source_directory()
    {
        string sourceDirectory = Path.Combine(
            Path.GetTempPath(), "HotReloadAnimationReloadTests_" + Path.GetRandomFileName());
        Directory.CreateDirectory(sourceDirectory);

        try
        {
            // A minimal real project on disk is enough — the reloaded project only needs to load;
            // the animation comes from the sibling .ganx the Gum tool would have just saved.
            string gumxPath = Path.Combine(sourceDirectory, "Proj.gumx");
            new GumProjectSave().Save(gumxPath, saveElements: false);

            string screensDirectory = Path.Combine(sourceDirectory, "Screens");
            Directory.CreateDirectory(screensDirectory);
            File.WriteAllBytes(Path.Combine(screensDirectory, "MainScreenAnimations.ganx"), Ganx());

            GumHotReloadManager manager = new GumHotReloadManager();
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
