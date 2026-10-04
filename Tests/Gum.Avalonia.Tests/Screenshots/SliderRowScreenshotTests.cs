using Avalonia.Styling;
using AvaloniaDataUi.Controls;
using Gum.Avalonia.Tests.DataUi;
using Gum.Avalonia.Tests.Harness;

namespace Gum.Avalonia.Tests.Screenshots;

/// <summary>
/// A Variables tab slider row (Stroke Alpha), for a PR's before/after table
/// (<c>Tools/pr-screenshots.ps1 -Filter SliderRowScreenshotTests</c>).
/// Uses only API that main already has, so the same file renders the "before" side.
/// </summary>
[Trait("Category", PrScreenshot.Category)]
public class SliderRowScreenshotTests
{
    [SkippableFact]
    public void SliderRow_WithMinAndMaxLabels() => PrScreenshot.Run(() =>
    {
        EditorFixture fixture = new EditorFixture { Number = 255 };
        SliderDisplay display = new SliderDisplay { MinValue = 0, MaxValue = 255, InstanceMember = fixture.Member(nameof(EditorFixture.Number)) };
        display.InstanceMember.DisplayName = "Stroke Alpha";

        using ScreenshotWindow window = PrScreenshot.Show(display, 330, 70, ThemeVariant.Dark);
        window.Save("slider-row");
    });
}
