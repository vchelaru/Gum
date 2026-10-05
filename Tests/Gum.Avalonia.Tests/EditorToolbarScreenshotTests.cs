using Avalonia.Styling;
using Gum.Avalonia.Tests.Harness;
using Gum.Avalonia.Plugins.EditorTab;
using Xunit;

namespace Gum.Avalonia.Tests;

[Trait("Category", PrScreenshot.Category)]
public class EditorToolbarScreenshotTests
{
    [SkippableFact]
    public void NarrowToolbar() => PrScreenshot.Run(() =>
    {
        using ScreenshotWindow window = PrScreenshot.Show(new EditorToolbar(), 560, 44, ThemeVariant.Dark);
        window.Save("editor-toolbar-narrow");
    });

    [SkippableFact]
    public void GridSizeToolbar() => PrScreenshot.Run(() =>
    {
        using ScreenshotWindow window = PrScreenshot.Show(new EditorToolbar(), 1000, 44, ThemeVariant.Dark);
        window.Save("editor-toolbar-grid-size");
    });
}
