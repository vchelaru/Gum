using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using AvaloniaDataUi.Controls;
using Gum.Avalonia.Themes;
using Shouldly;
using WpfDataUi.Controls;

namespace Gum.Avalonia.Tests;

/// <summary>
/// The WPF tool's chrome metrics on the head's Fluent controls: body-sized text everywhere, compact
/// menus in a Primary outline, thin scroll bars without buttons, and option buttons at the WPF size.
/// </summary>
public class ChromeStylesTests
{
    [AvaloniaFact]
    public void MenuItems_UseTheBaseFont_OnCompactRows_InsideAPrimaryOutline()
    {
        MenuItem child = new MenuItem { Header = "Child", InputGesture = new KeyGesture(Key.S, KeyModifiers.Control) };
        MenuItem top = new MenuItem { Header = "File" };
        top.Items.Add(child);
        Menu menu = new Menu();
        menu.Items.Add(top);
        Window window = new Window { Content = menu, Width = 300, Height = 200 };
        window.Show();
        window.UpdateLayout();

        top.Open();
        window.UpdateLayout();

        child.FontSize.ShouldBe(FrbThemeResources.DefaultBaseFontSize);
        child.Bounds.Height.ShouldBeLessThanOrEqualTo(22);
        Popup popup = top.GetVisualDescendants().OfType<Popup>().Single();
        Border frame = (Border)popup.Child!;
        frame.BorderThickness.ShouldBe(new Thickness(1));
        frame.BorderBrush.ShouldBeSameAs(window.FindResource("Frb.Brushes.Primary"));
        // The WPF SubmenuItemTemplate: headers 28px in behind a reserved icon column, shortcuts
        // 5px after the header and 10px before the edge.
        ContentPresenter header = child.GetVisualDescendants().OfType<ContentPresenter>().First(presenter => presenter.Name == "PART_HeaderPresenter");
        header.Bounds.X.ShouldBe(28);
        TextBlock gesture = child.GetVisualDescendants().OfType<TextBlock>().First(text => text.Name == "PART_InputGestureText");
        gesture.Margin.ShouldBe(new Thickness(5, 0, 10, 0));
        window.Close();
    }

    [AvaloniaFact]
    public void ScrollBars_AreEightWide_WithoutLineButtons()
    {
        StackPanel tall = new StackPanel();
        for (int i = 0; i < 100; i++)
        {
            tall.Children.Add(new TextBlock { Text = "row " + i });
        }
        ScrollViewer scroll = new ScrollViewer { Content = tall };
        Window window = new Window { Content = scroll, Width = 300, Height = 100 };
        window.Show();
        window.UpdateLayout();

        ScrollBar bar = scroll.GetVisualDescendants().OfType<ScrollBar>().Single(b => b.Orientation == Orientation.Vertical);
        bar.Bounds.Width.ShouldBe(8);
        RepeatButton[] lineButtons = bar.GetVisualDescendants().OfType<RepeatButton>()
            .Where(b => b.Name is "PART_LineUpButton" or "PART_LineDownButton").ToArray();
        lineButtons.Length.ShouldBe(2);
        lineButtons.ShouldAllBe(b => !b.IsVisible);
        window.Close();
    }

    [AvaloniaFact]
    public void FluentControls_FollowTheBaseFontSize()
    {
        IResourceDictionary resources = Application.Current!.Resources;
        ComboBox comboBox = new ComboBox { ItemsSource = new[] { "one" }, SelectedIndex = 0 };
        Window window = new Window { Content = comboBox, Width = 300, Height = 100 };
        window.Show();
        window.UpdateLayout();
        try
        {
            comboBox.FontSize.ShouldBe(FrbThemeResources.DefaultBaseFontSize);

            FrbThemeResources.SetBaseFontSize(resources, 16);
            window.UpdateLayout();

            comboBox.FontSize.ShouldBe(16);
        }
        finally
        {
            FrbThemeResources.SetBaseFontSize(resources, FrbThemeResources.DefaultBaseFontSize);
            window.Close();
        }
    }

    [AvaloniaFact]
    public void OptionButtons_AreTheWpfSize_SoLongRowsDoNotWrap()
    {
        ToggleButtonOption[] options = Enumerable.Range(0, 9).Select(i => new ToggleButtonOption("option " + i, i)).ToArray();
        ToggleButtonOptionDisplay display = new ToggleButtonOptionDisplay
        {
            OptionContentFactory = _ => new Border { Width = 28, Height = 28 },
        };
        display.SetOptions(options);
        Window window = new Window { Content = display, Width = 600, Height = 100 };
        window.Show();
        window.UpdateLayout();

        ToggleButton[] buttons = display.GetVisualDescendants().OfType<ToggleButton>().ToArray();
        buttons.Length.ShouldBe(9);
        // 28px icon, 2px padding and a 1px border on each side, no margin: the WPF ToggleButton.
        buttons.ShouldAllBe(b => b.Bounds.Width == 34);
        buttons.Select(b => b.Bounds.Top).Distinct().Count().ShouldBe(1);
        window.Close();
    }

    [AvaloniaFact]
    public void Buttons_DefaultToTheWpfPrimaryLook_AndTheToolClassesOptOut()
    {
        Button plain = new Button { Content = "OK" };
        Button flat = new Button { Content = "x", Classes = { GumChromeStyles.FlatButtonClass } };
        Button icon = new Button { Content = "+", Classes = { GumChromeStyles.IconButtonClass } };
        StackPanel panel = new StackPanel { Children = { plain, flat, icon } };
        Window window = new Window { Content = panel, Width = 300, Height = 200 };
        window.Show();
        window.UpdateLayout();
        try
        {
            plain.Background.ShouldBeSameAs(Application.Current!.Resources["Frb.Brushes.Primary"]);
            plain.Foreground.ShouldBeSameAs(Application.Current.Resources["Frb.Brushes.Primary.Contrast"]);
            plain.FontWeight.ShouldBe(FontWeight.Bold);
            plain.Padding.ShouldBe(new Thickness(4, 2, 4, 3));

            flat.Background.ShouldBe(Brushes.Transparent);
            flat.FontWeight.ShouldBe(FontWeight.Normal);
            icon.Background.ShouldBe(Brushes.Transparent);
            icon.FontWeight.ShouldBe(FontWeight.Normal);
        }
        finally
        {
            window.Close();
        }
    }
}
