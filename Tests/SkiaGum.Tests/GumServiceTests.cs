using Gum;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using RenderingLibrary;
using Shouldly;
using SkiaSharp;

namespace SkiaGum.Tests;

public class GumServiceTests
{
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
