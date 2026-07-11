using Gum;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using RenderingLibrary;
using Shouldly;

namespace SilkNetGum.Tests;

/// <summary>
/// Integration tests for the SilkNetGum <see cref="GumService"/>: it wires itself as the runtime
/// default and registers the V3 Forms defaults on Skia, so Forms controls construct with a valid
/// Visual and a Silk-backed cursor is created. Relies on the assembly bootstrap having run
/// <c>Initialize</c> with a raster canvas.
/// </summary>
public class GumServiceTests : BaseTestClass
{
    [Fact]
    public void Bootstrap_SetsIGumServiceDefault_ToSilkNetService()
    {
        IGumService.Default.ShouldNotBeNull();
        IGumService.Default.ShouldBeSameAs(GumService.Default);
        GumService.Default.Root.ShouldNotBeNull();
    }

    [Fact]
    public void Button_ConstructsWithVisual_OnSkia()
    {
        // Proves the whole Forms-on-Skia path wired by this runtime: FormsUtilities.InitializeDefaults
        // registered the V3 ButtonVisual, so the parameterless control has a non-null Visual.
        Button button = new Button();

        button.Visual.ShouldNotBeNull();
    }

    [Fact]
    public void Cursor_IsCreatedBySilkService()
    {
        // The service's CreateCursor override ran during Initialize, so FormsUtilities.Cursor (and the
        // typed GumService.Default.Cursor) is a Silk-backed Gum.Input.Cursor.
        GumService.Default.Cursor.ShouldNotBeNull();
    }

    [Fact]
    public void FrameworkElement_AddToRoot_AddsVisualToRoot()
    {
        ContainerRuntime visual = new ContainerRuntime();
        FrameworkElement element = new FrameworkElement(visual);

        element.AddToRoot();

        GumService.Default.Root.Children.ShouldContain(visual);
    }

    [Fact]
    public void Update_WithDeviceLessInputContext_DoesNotThrow()
    {
        // The bootstrap initialized with an input context that has no keyboard devices. CreateKeyboard
        // must return an inert device-less Keyboard (not null) so FormsUtilities.Update, which ticks
        // keyboard.Activity() unconditionally, does not NRE on the first frame.
        GumService.Default.Keyboard.ShouldNotBeNull();
        Should.NotThrow(() => GumService.Default.Update(0));
    }
}
