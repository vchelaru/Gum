using Gum;
using RenderingLibrary;
using Shouldly;

namespace SilkNetGum.Tests;

/// <summary>
/// Integration tests for the SilkNetGum <see cref="GumService"/>. The cases every Skia host shares live
/// in <see cref="SkiaHostTests.GumServiceContractTestsBase"/>; this keeps the Silk-specific ones and
/// is run by <c>GumServiceContractTests</c>. Relies on the assembly bootstrap having run <c>Initialize</c> with a
/// raster canvas.
/// </summary>
public class GumServiceTests : BaseTestClass
{
    [Fact]
    public void Clipboard_IsAssigned_AfterInitialize()
    {
        // Regression test for #3651: GumService.Default's lazy ctor runs AssignClipboard() before
        // Initialize assigns _inputContext, so the ctor-time call always no-oped. Initialize must
        // re-run AssignClipboard() once _inputContext is set, or Clipboard silently stays null and
        // TextBox copy/paste never reaches the OS clipboard.
        GumService.Default.Clipboard.ShouldNotBeNull();
    }

    [Fact]
    public void Cursor_IsCreatedBySilkService()
    {
        // The service's CreateCursor override ran during Initialize, so FormsUtilities.Cursor (and the
        // typed GumService.Default.Cursor) is a Silk-backed Gum.Input.Cursor.
        GumService.Default.Cursor.ShouldNotBeNull();
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
