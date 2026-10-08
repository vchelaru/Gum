using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using AvaloniaDataUi.Controls;
using Gum.Avalonia.Plugins.VariableGrid;
using Shouldly;
using WpfDataUi.DataTypes;

namespace Gum.Avalonia.Tests.DataUi;

/// <summary>
/// A row's controls keep their own space when the Variables panel is dragged narrow (#5883): the
/// label gives way first, and below the row's floor the row clips instead of piling controls on
/// top of each other.
/// </summary>
public class NarrowRowLayoutTests
{
    // The narrowest the rows are asked to lay out cleanly; the slider row needs the most room.
    private const double NarrowestWidth = 150;

    [AvaloniaFact]
    public void SliderRow_KeepsSliderTextBoxAndCaptionsApart_WhenNarrow()
    {
        foreach (double width in new[] { 220, 180, NarrowestWidth })
        {
            EditorFixture fixture = new EditorFixture { Number = 255 };
            SliderDisplay display = new SliderDisplay { MinValue = 0, MaxValue = 255, InstanceMember = fixture.Member(nameof(EditorFixture.Number)) };
            using HostedRow host = new HostedRow(display, width);

            Rect slider = host.RectOf(display.Slider);
            Rect textBox = host.RectOf(display.TextBox);
            TextBlock minCaption = host.CaptionNamed(display, "0");
            TextBlock maxCaption = host.CaptionNamed(display, "255");
            // Both captions share one cell, so compare the extent of their text, not the cell.
            Rect min = new Rect(host.RectOf(minCaption).TopLeft, minCaption.DesiredSize);
            Rect maxCell = host.RectOf(maxCaption);
            Rect max = new Rect(new Point(maxCell.Right - maxCaption.DesiredSize.Width, maxCell.Top), maxCaption.DesiredSize);

            string at = $"at width {width}";
            slider.Right.ShouldBeLessThanOrEqualTo(textBox.Left + 0.5, at);
            slider.Width.ShouldBeGreaterThan(30, at);
            min.Width.ShouldBeGreaterThan(0, at);
            max.Width.ShouldBeGreaterThan(0, at);
            min.Right.ShouldBeLessThanOrEqualTo(max.Left + 0.5, at);
            min.Left.ShouldBeGreaterThanOrEqualTo(slider.Left - 0.5, at);
            max.Right.ShouldBeLessThanOrEqualTo(slider.Right + 0.5, at);
            textBox.Right.ShouldBeLessThanOrEqualTo(width + 0.5, at);
        }
    }

    [AvaloniaFact]
    public void ColorRow_KeepsItsSwatchUsable_WhenNarrow()
    {
        foreach (double width in new[] { 220, 180, NarrowestWidth })
        {
            GumEditorFixture fixture = new GumEditorFixture();
            ColorDisplay display = new ColorDisplay { InstanceMember = fixture.Member(nameof(GumEditorFixture.Color)) };
            using HostedRow host = new HostedRow(display, width);

            Rect swatch = host.RectOf(display.Swatch);
            Rect hex = host.RectOf(display.HexTextBox);

            string at = $"at width {width}";
            swatch.Width.ShouldBeGreaterThanOrEqualTo(30, at);
            swatch.Right.ShouldBeLessThanOrEqualTo(hex.Left, at);
            hex.Right.ShouldBeLessThanOrEqualTo(width + 0.5, at);
        }
    }

    [AvaloniaFact]
    public void NullableRow_KeepsTheTextBoxClearOfIsNull_WhenNarrow()
    {
        foreach (double width in new[] { 220, 180, NarrowestWidth })
        {
            EditorFixture fixture = new EditorFixture { MaybeNumber = null };
            TextBoxDisplay display = new TextBoxDisplay { InstanceMember = fixture.Member(nameof(EditorFixture.MaybeNumber)) };
            using HostedRow host = new HostedRow(display, width);

            Rect textBox = host.RectOf(display.GetVisualDescendants().OfType<TextBox>().First());
            Rect isNull = host.RectOf(display.GetVisualDescendants().OfType<CheckBox>().First());

            string at = $"at width {width}";
            textBox.Width.ShouldBeGreaterThan(30, at);
            textBox.Right.ShouldBeLessThanOrEqualTo(isNull.Left + 0.5, at);
            isNull.Right.ShouldBeLessThanOrEqualTo(width + 0.5, at);
        }
    }

    [AvaloniaFact]
    public void RowsBelowTheirFloor_ClipInsteadOfOverlapping()
    {
        EditorFixture fixture = new EditorFixture { Number = 255 };
        SliderDisplay display = new SliderDisplay { MinValue = 0, MaxValue = 255, InstanceMember = fixture.Member(nameof(EditorFixture.Number)) };
        using HostedRow host = new HostedRow(display, 100);

        // Too narrow for the whole row: the slider still ends where the text box begins.
        host.RectOf(display.Slider).Right.ShouldBeLessThanOrEqualTo(host.RectOf(display.TextBox).Left + 0.5);
    }

    /// <summary>A displayer in a window of a given width, with positions read in window coordinates.</summary>
    private sealed class HostedRow : IDisposable
    {
        private readonly Window _window;

        public HostedRow(Control row, double width)
        {
            _window = new Window { Content = row, Width = width, Height = 120 };
            _window.Show();
            _window.UpdateLayout();
        }

        public Rect RectOf(Control control)
        {
            Point topLeft = control.TranslatePoint(new Point(0, 0), _window)!.Value;
            return new Rect(topLeft, control.Bounds.Size);
        }

        public TextBlock CaptionNamed(Control row, string text) =>
            row.GetVisualDescendants().OfType<TextBlock>().First(block => block.Text == text && !block.IsHitTestVisible);

        public void Dispose() => _window.Close();
    }
}
