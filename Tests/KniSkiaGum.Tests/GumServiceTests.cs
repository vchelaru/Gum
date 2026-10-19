using Gum;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using Moq;
using RenderingLibrary;
using Shouldly;

namespace KniSkiaGum.Tests;

/// <summary>
/// Integration tests for the KNI <see cref="GumService"/>: it wires itself as the runtime default
/// and registers the V3 Forms defaults on Skia, so Forms controls construct with a valid Visual and
/// KNI-backed input is created. Relies on the assembly bootstrap having run Initialize with a raster
/// canvas.
/// </summary>
public class GumServiceTests : BaseTestClass
{
    [Fact]
    public void Bootstrap_SetsIGumServiceDefault_ToTheKniService()
    {
        IGumService.Default.ShouldNotBeNull();
        IGumService.Default.ShouldBeSameAs(GumService.Default);
        GumService.Default.Root.ShouldNotBeNull();
    }

    [Fact]
    public void Button_ConstructsWithVisual_OnSkia()
    {
        Button button = new Button();

        button.Visual.ShouldNotBeNull();
    }

    [Fact]
    public void Clipboard_IsAssigned_AfterInitialize()
    {
        GumService.Default.Clipboard.ShouldNotBeNull();
    }

    [Fact]
    public void CursorAndKeyboard_AreCreatedByTheKniService()
    {
        GumService.Default.Cursor.ShouldNotBeNull();
        GumService.Default.Keyboard.ShouldNotBeNull();
    }

    [Fact]
    public void Update_WithoutAWindow_DoesNotThrow()
    {
        // The keyboard is device-less here (no window, so no typed text); FormsUtilities.Update ticks
        // it unconditionally, so it must still be a working keyboard rather than null.
        Should.NotThrow(() => GumService.Default.Update(0));
    }

    [Fact]
    public void Update_PumpsFormsInput_ThroughTheCursor()
    {
        Mock<Gum.Wireframe.ICursor> cursor = new Mock<Gum.Wireframe.ICursor>();
        Gum.Forms.FormsUtilities.SetCursor(cursor.Object);

        GumService.Default.Update(5);

        cursor.Verify(c => c.Activity(5), Times.Once());
    }

    [Fact]
    public void FrameworkElement_AddToRoot_AddsVisualToRoot()
    {
        ContainerRuntime visual = new ContainerRuntime();
        FrameworkElement element = new FrameworkElement(visual);

        element.AddToRoot();

        GumService.Default.Root.Children.ShouldContain(visual);
    }
}
