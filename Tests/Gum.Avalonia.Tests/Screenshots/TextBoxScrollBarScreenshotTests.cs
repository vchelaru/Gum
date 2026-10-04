using Avalonia.Styling;
using AvaloniaDataUi.Controls;
using Gum.Avalonia.Tests.DataUi;
using Gum.Avalonia.Tests.Harness;

namespace Gum.Avalonia.Tests.Screenshots;

/// <summary>
/// The string-list and multi-line editors with an overlong line and an unapplied edit, for a PR's
/// before/after table (<c>Tools/pr-screenshots.ps1 -Filter TextBoxScrollBarScreenshotTests</c>).
/// Uses only API that main already has, so the same file renders the "before" side.
/// </summary>
[Trait("Category", PrScreenshot.Category)]
public class TextBoxScrollBarScreenshotTests
{
    [SkippableFact]
    public void StringListEditor_WithAnOverlongLineAndAnUnappliedEdit() => PrScreenshot.Run(() =>
    {
        EditorFixture fixture = new EditorFixture
        {
            Lines = new List<string>
            {
                "Color = Components/Styles.White.FillColor",
                "Font = Components/Styles.Strong.Font",
                "LineHeightMultiplier = Components/Styles.Strong.LineHeightMultiplier.WithAVeryLongTailThatForcesAScrollBar",
            },
        };
        StringListTextBoxDisplay display = new StringListTextBoxDisplay { InstanceMember = fixture.Member(nameof(EditorFixture.Lines)) };
        display.EditorTextBox.Height = 90;
        display.EditorTextBox.Text += "\nFontSize = Components/Styles.Strong.FontSize";
        using ScreenshotWindow window = PrScreenshot.Show(display, 300, 160, ThemeVariant.Dark);
        window.Save("string-list-editor");
    });

    [SkippableFact]
    public void MultiLineEditor_WithAnUnappliedEdit() => PrScreenshot.Run(() =>
    {
        EditorFixture fixture = new EditorFixture { Text = "Press start\nto play" };
        MultiLineTextBoxDisplay display = new MultiLineTextBoxDisplay { InstanceMember = fixture.Member(nameof(EditorFixture.Text)) };
        display.EditorTextBox.Text += " now";
        using ScreenshotWindow window = PrScreenshot.Show(display, 300, 120, ThemeVariant.Dark);
        window.Save("multi-line-editor");
    });
}
