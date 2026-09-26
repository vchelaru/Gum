using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Gum.Avalonia.Plugins.TreeView;
using Shouldly;

namespace Gum.Avalonia.Tests;

/// <summary>The Project panel's search box.</summary>
public class ProjectSearchBoxTests
{
    [AvaloniaFact]
    public void ClearButton_ShowsOnlyWithText_AndClickingItClearsTheSearch()
    {
        AvaloniaElementTreeView view = new AvaloniaElementTreeView();
        Window window = new Window { Content = (Control)view.Content, Width = 300, Height = 600 };
        window.Show();
        string? lastSearch = "unset";
        view.SearchTextChanged += text => lastSearch = text;
        TextBox search = view.SearchRow.Children.OfType<TextBox>().Single();
        Button clear = view.SearchClearButton;
        clear.IsVisible.ShouldBeFalse();

        search.Text = "Button";
        Dispatcher.UIThread.RunJobs();
        clear.IsVisible.ShouldBeTrue();

        clear.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Dispatcher.UIThread.RunJobs();

        search.Text.ShouldBeNullOrEmpty();
        lastSearch.ShouldBeNullOrEmpty();
        clear.IsVisible.ShouldBeFalse();
        window.Close();
    }
}
