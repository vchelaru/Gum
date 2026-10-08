using Avalonia.Controls;
using Avalonia.Styling;
using AvaloniaDataUi.Controls;
using Gum.Avalonia.Plugins.VariableGrid;
using Gum.Avalonia.Tests.DataUi;
using Gum.Avalonia.Tests.Harness;
using WpfDataUi.DataTypes;

namespace Gum.Avalonia.Tests.Screenshots;

/// <summary>
/// The Variables tab's Alpha slider, Color and nullable (Min Height) rows with the panel dragged
/// narrow, for a PR's before/after table (<c>Tools/pr-screenshots.ps1 -Filter NarrowRowsScreenshotTests</c>).
/// Uses only API that main already has, so the same file renders the "before" side.
/// </summary>
[Trait("Category", PrScreenshot.Category)]
public class NarrowRowsScreenshotTests
{
    [SkippableFact]
    public void Rows_AtWidth220() => PrScreenshot.Run(() => Capture(220));

    [SkippableFact]
    public void Rows_AtWidth180() => PrScreenshot.Run(() => Capture(180));

    [SkippableFact]
    public void Rows_AtWidth150() => PrScreenshot.Run(() => Capture(150));

    [SkippableFact]
    public void Rows_AtWidth120() => PrScreenshot.Run(() => Capture(120));

    private static void Capture(int width)
    {
        EditorFixture fixture = new EditorFixture { Number = 255, MaybeNumber = null };
        GumEditorFixture gumFixture = new GumEditorFixture { Color = System.Drawing.Color.FromArgb(255, 255, 255, 255) };

        SliderDisplay alpha = new SliderDisplay { MinValue = 0, MaxValue = 255, DecimalPointsFromSlider = 0 };
        alpha.InstanceMember = Named(fixture.Member(nameof(EditorFixture.Number)), "Alpha");
        ColorDisplay color = new ColorDisplay { InstanceMember = Named(gumFixture.Member(nameof(GumEditorFixture.Color)), "Color") };
        TextBoxDisplay minHeight = new TextBoxDisplay { InstanceMember = Named(fixture.Member(nameof(EditorFixture.MaybeNumber)), "Min Height") };

        StackPanel rows = new StackPanel();
        rows.Children.Add(alpha);
        rows.Children.Add(color);
        rows.Children.Add(minHeight);

        using ScreenshotWindow window = PrScreenshot.Show(rows, width, 130, ThemeVariant.Dark);
        window.Save($"narrow-rows-{width}");
    }

    private static InstanceMember Named(InstanceMember member, string displayName)
    {
        member.DisplayName = displayName;
        return member;
    }
}
