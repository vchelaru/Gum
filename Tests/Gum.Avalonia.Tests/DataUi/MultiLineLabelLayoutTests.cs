using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using AvaloniaDataUi.Controls;
using Shouldly;
using WpfDataUi.DataTypes;

namespace Gum.Avalonia.Tests.DataUi;

/// <summary>A multi-line editor's label sits on its own row, so it wraps only when the row is genuinely too narrow.</summary>
public class MultiLineLabelLayoutTests
{
    [AvaloniaFact]
    public void Label_WithRoomOnItsRow_StaysOnOneLine()
    {
        EditorFixture fixture = new EditorFixture();
        InstanceMember member = fixture.Member(nameof(EditorFixture.Text));
        member.DisplayName = "Variable References";
        MultiLineTextBoxDisplay display = new MultiLineTextBoxDisplay { InstanceMember = member };
        Window window = new Window { Content = display, Width = 400, Height = 300 };
        window.Show();
        window.UpdateLayout();

        TextBlock label = display.GetVisualDescendants().OfType<TextBlock>().First(text => text.Text == "Variable References");
        label.Measure(Size.Infinity);
        double oneLineHeight = label.DesiredSize.Height;

        label.Bounds.Height.ShouldBe(oneLineHeight, 0.5);
        window.Close();
    }
}
