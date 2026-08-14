using System;
using System.IO;
using System.Linq;
using Gum;
using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using RenderingLibrary;
using Shouldly;
using SkiaSharp;

namespace SkiaGum.Tests;

public class GumServiceTests
{
    [Fact]
    public void ExportSnapshot_ShouldWriteLoadableProjectFromLiveRoot()
    {
        string tempDirectory = Path.Combine(Path.GetTempPath(), "SkiaExportSnapshotTests_" + Path.GetRandomFileName());
        Directory.CreateDirectory(tempDirectory);
        try
        {
            using SKSurface surface = SKSurface.Create(new SKImageInfo(200, 100));
            GumService.Default.Initialize(surface.Canvas, 200, 100);

            ContainerRuntime panel = new() { Name = "Panel" };
            TextRuntime label = new() { Name = "Label" };
            label.Text = "Hi";
            panel.AddChild(label);
            GumService.Default.Root.AddChild(panel);

            string gumxPath = Path.Combine(tempDirectory, "Live." + GumProjectSave.ProjectExtension);
            GumService.Default.ExportSnapshot(gumxPath);

            GumProjectSave loaded = GumProjectSave.Load(gumxPath, out GumLoadResult loadResult);

            loaded.ShouldNotBeNull();
            loadResult.ErrorMessage.ShouldBeNullOrEmpty();
            loadResult.MissingFiles.ShouldBeEmpty();

            // The screen is named after the file; the live tree is flattened into instances.
            ScreenSave loadedScreen = loaded.Screens.First(s => s.Name == "Live");
            loadedScreen.Instances.Select(i => i.Name).ShouldBe(new[] { "Panel", "Label" }, ignoreOrder: true);
        }
        finally
        {
            try { Directory.Delete(tempDirectory, recursive: true); } catch { /* best-effort */ }
        }
    }

    [Fact]
    public void LoadAnimations_ThrowsException_WhenNoProjectLoaded()
    {
        using SKSurface surface = SKSurface.Create(new SKImageInfo(200, 100));
        GumService.Default.Initialize(surface.Canvas, 200, 100);

#pragma warning disable CS0618 // Type or member is obsolete
        var exception = Should.Throw<InvalidOperationException>(() => GumService.Default.LoadAnimations());
#pragma warning restore CS0618

        exception.Message.ShouldContain("You must first load a project before attempting to load its animations");
    }

    [Fact]
    public void LoadAnimations_LoadsAnimationsFromLoadedProjectDirectory()
    {
        string sourceDirectory = Path.Combine(Path.GetTempPath(), "SkiaLoadAnimationsTests_" + Path.GetRandomFileName());
        Directory.CreateDirectory(sourceDirectory);
        try
        {
            string gumxPath = Path.Combine(sourceDirectory, "Proj.gumx");
            new GumProjectSave().Save(gumxPath, saveElements: false);

            string screensDirectory = Path.Combine(sourceDirectory, "Screens");
            Directory.CreateDirectory(screensDirectory);
            var animation = new Gum.StateAnimation.SaveClasses.ElementAnimationsSave { ElementName = "WRONG" };
            var serializer = ToolsUtilities.FileManager.GetXmlSerializer(typeof(Gum.StateAnimation.SaveClasses.ElementAnimationsSave));
            using (var writer = new StreamWriter(Path.Combine(screensDirectory, "MainScreenAnimations.ganx")))
            {
                serializer.Serialize(writer, animation);
            }

            using SKSurface surface = SKSurface.Create(new SKImageInfo(200, 100));
            GumService.Default.Initialize(surface.Canvas, 200, 100, gumxPath);

#pragma warning disable CS0618 // Type or member is obsolete
            GumService.Default.LoadAnimations();
#pragma warning restore CS0618

            GumProjectSave project = Gum.Managers.ObjectFinder.Self.GumProjectSave!;
            project.ElementAnimations.ShouldHaveSingleItem().ElementName.ShouldBe("MainScreen");
        }
        finally
        {
            // ObjectFinder.Self.GumProjectSave is process-wide static state (no BaseTestClass/Dispose
            // hook in this test project to reset it) -- leaving it set here would fail an unrelated
            // "no project loaded" test in another class.
            Gum.Managers.ObjectFinder.Self.GumProjectSave = null;
            try { Directory.Delete(sourceDirectory, recursive: true); } catch { /* best-effort */ }
        }
    }

    [Fact]
    public void FrameworkElement_AddToRoot_ShouldAddVisualToRoot()
    {
        using SKSurface surface = SKSurface.Create(new SKImageInfo(200, 100));
        GumService.Default.Initialize(surface.Canvas, 200, 100);

        ContainerRuntime visual = new ContainerRuntime();
        FrameworkElement element = new FrameworkElement(visual);
        element.AddToRoot();

        GumService.Default.Root.Children.ShouldContain(visual);
    }

    [Fact]
    public void Initialize_ShouldSetIGumServiceDefault()
    {
        using SKSurface surface = SKSurface.Create(new SKImageInfo(200, 100));
        GumService.Default.Initialize(surface.Canvas, 200, 100);

        IGumService.Default.ShouldNotBeNull();
        IGumService.Default.ShouldBeSameAs(GumService.Default);
    }

    // Pins issue #4452: the render-only Skia GumService.Initialize used to never call
    // FormsUtilities.InitializeDefaults, so a code-only Forms control got no default Visual
    // registered unless a .gumx project happened to be loaded. Initialize now calls it
    // unconditionally, matching every other backend's GumService. Asserted via the
    // DefaultFormsTemplates registration rather than by constructing a Button -- the render-only
    // Skia GumService never assigns FrameworkElement.MainCursor (see GumServiceSkiaBase), which
    // Button construction requires independent of this fix (same reasoning as
    // MenuPasswordBoxTests's PasswordBox case).
    [Fact]
    public void Initialize_WithNoProjectFile_ShouldRegisterCodeOnlyDefaultFormsVisuals()
    {
        using SKSurface surface = SKSurface.Create(new SKImageInfo(200, 100));
        GumService.Default.Initialize(surface.Canvas, 200, 100);

        FrameworkElement.DefaultFormsTemplates.ShouldContainKey(typeof(Button));
    }

    [Fact]
    public void GumService_ShouldDeriveFromGumServiceSkiaBase()
    {
        typeof(GumServiceSkiaBase).IsAssignableFrom(typeof(GumService)).ShouldBeTrue();
    }
}
