using Avalonia.Controls;
using Avalonia.Styling;
using Gum.Avalonia.Tests.Harness;

namespace Gum.Avalonia.Tests.Screenshots;

/// <summary>
/// A no-wrap multi-line text box with an overlong line, for a PR's before/after table
/// (<c>Tools/pr-screenshots.ps1 -Filter TextBoxScrollBarScreenshotTests</c>).
/// Uses only API that main already has, so the same file renders the "before" side.
/// </summary>
[Trait("Category", PrScreenshot.Category)]
public class TextBoxScrollBarScreenshotTests
{
    [SkippableFact]
    public void NoWrapTextBox_WithAnOverlongLine() => PrScreenshot.Run(() =>
    {
        TextBox box = new TextBox
        {
            AcceptsReturn = true,
            TextWrapping = global::Avalonia.Media.TextWrapping.NoWrap,
            Text = "Color = Components/Styles.White.FillColor\nFont = Components/Styles.Strong.Font\n" +
                "LineHeightMultiplier = Components/Styles.Strong.LineHeightMultiplier.WithAVeryLongTail",
        };
        using ScreenshotWindow window = PrScreenshot.Show(box, 260, 80, ThemeVariant.Dark);
        window.Save("textbox-horizontal-scrollbar");
    });
}
