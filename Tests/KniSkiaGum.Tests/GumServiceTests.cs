using Gum;
using RenderingLibrary;
using Shouldly;

namespace KniSkiaGum.Tests;

/// <summary>
/// Integration tests for the KNI <see cref="GumService"/>. The cases every Skia host shares live in
/// <see cref="SkiaHostTests.GumServiceContractTestsBase"/>; this keeps the KNI-specific ones and
/// is run by <c>GumServiceContractTests</c>. Relies on the assembly bootstrap having run Initialize with a raster
/// canvas.
/// </summary>
public class GumServiceTests : BaseTestClass
{
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
}
