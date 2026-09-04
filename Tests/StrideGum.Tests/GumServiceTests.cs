using Gum;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using RenderingLibrary;
using Shouldly;

namespace StrideGum.Tests;

/// <summary>
/// Integration tests for the StrideGum <see cref="GumService"/>: it wires itself as the runtime
/// default and registers the V3 Forms defaults on Skia, so Forms controls construct with a valid
/// Visual. Relies on the assembly bootstrap having run <c>InitializeCore</c> with a raster canvas
/// (the Gum-facing half of <c>Initialize(Game, ...)</c> that needs no live Stride GraphicsDevice --
/// see TestAssemblyInitialize).
/// </summary>
public class GumServiceTests : BaseTestClass
{
    [Fact]
    public void Bootstrap_SetsIGumServiceDefault_ToStrideService()
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
    public void Clipboard_IsAssigned_AfterInitialize()
    {
        GumService.Default.Clipboard.ShouldNotBeNull();
    }

    [Fact]
    public void Cursor_IsCreatedByStrideService()
    {
        // The service's CreateCursor override ran during InitializeCore, so FormsUtilities.Cursor (and
        // the typed GumService.Default.Cursor) is a Stride-backed Gum.Input.Cursor.
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
    public void Update_WithDeviceLessKeyboard_DoesNotThrow()
    {
        // The bootstrap initialized with a bare InputManager that has no keyboard devices.
        // CreateKeyboard must return an inert device-less Keyboard (not null) so
        // FormsUtilities.Update, which ticks keyboard.Activity() unconditionally, does not NRE on the
        // first frame.
        GumService.Default.Keyboard.ShouldNotBeNull();
        Should.NotThrow(() => GumService.Default.Update(0));
    }
}
