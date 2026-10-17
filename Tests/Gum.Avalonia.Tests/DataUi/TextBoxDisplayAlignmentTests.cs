using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using AvaloniaDataUi;
using AvaloniaDataUi.Controls;
using Shouldly;
using WpfDataUi.DataTypes;

namespace Gum.Avalonia.Tests.DataUi;

/// <summary>A text-box row's label and its field text sit on the same line.</summary>
public class TextBoxDisplayAlignmentTests
{
    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void LabelText_IsVerticallyCenteredOnTheFieldText(bool isCompact)
    {
        EditorFixture fixture = new EditorFixture();
        InstanceMember member = new InstanceMember(nameof(EditorFixture.Number), fixture);
        SingleDataUiContainer container = new SingleDataUiContainer(new DataUiGrid()) { IsCompact = isCompact, DataContext = member };
        Window window = new Window { Content = container, Width = 400, Height = 200 };
        window.Show();
        window.UpdateLayout();

        TextBlock label = container.GetVisualDescendantsOfType<TextBlock>().First(text => text.Text == nameof(EditorFixture.Number));
        TextBox textBox = ((TextBoxDisplay)container.Displayer!).TextBox;
        TextPresenter fieldText = textBox.GetVisualDescendantsOfType<TextPresenter>().First();

        double labelTextCenter = label.TranslatePoint(
            new Point(0, label.Padding.Top + (label.Bounds.Height - label.Padding.Top - label.Padding.Bottom) / 2), window)!.Value.Y;
        double fieldTextCenter = fieldText.TranslatePoint(new Point(0, fieldText.Bounds.Height / 2), window)!.Value.Y;
        // Within a pixel: layout rounds to whole pixels and font metrics differ a little per platform.
        labelTextCenter.ShouldBe(fieldTextCenter, 1.0);
        window.Close();
    }
}
