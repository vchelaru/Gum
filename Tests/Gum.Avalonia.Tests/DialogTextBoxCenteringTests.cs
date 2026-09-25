using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.VisualTree;
using Gum.Avalonia.Dialogs.Views;
using Gum.Avalonia.Themes;
using Shouldly;

namespace Gum.Avalonia.Tests;

/// <summary>macOS text boxes: single-line text sits in the vertical middle of the box (#5013).</summary>
public class DialogTextBoxCenteringTests
{
    [AvaloniaFact]
    public void MacOSStyles_CenterSingleLineText_AndLeaveMultiLineAtTop()
    {
        GetUserStringDialogView view = new GetUserStringDialogView();
        TextBox multiLine = new TextBox { AcceptsReturn = true, Height = 80 };
        StackPanel panel = new StackPanel { Children = { view, multiLine } };
        Window window = new Window { Content = panel, Width = 400, Height = 300 };
        window.Styles.Add(GumChromeStyles.CreateMacOS());
        TextBox singleLine = view.GetVisualDescendants().OfType<TextBox>().FirstOrDefault()
            ?? throw new InvalidOperationException("The dialog has no text box.");
        singleLine.Text = "Name";
        window.Show();
        window.UpdateLayout();

        singleLine = view.GetVisualDescendants().OfType<TextBox>().First();
        TextPresenter presenter = singleLine.GetVisualDescendants().OfType<TextPresenter>().Single();
        double textTop = presenter.TranslatePoint(new Point(0, 0), singleLine)!.Value.Y;
        double textMiddle = textTop + presenter.TextLayout.Height / 2;
        textMiddle.ShouldBe(singleLine.Bounds.Height / 2, 0.5);
        multiLine.VerticalContentAlignment.ShouldNotBe(VerticalAlignment.Center);
        window.Close();
    }
}
