using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Gum.Avalonia.Shell;
using Gum.Plugins;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Gum.Avalonia.Tests;

/// <summary>The shell's tab regions: tab order and the close button, as in the WPF main panel.</summary>
public class MainPanelTabsTests
{
    private static AvaloniaTabManager CreateTabManager() =>
        ActivatorUtilities.CreateInstance<AvaloniaTabManager>(TestAppBuilder.Services);

    [AvaloniaFact]
    public void BottomTabs_FollowTheWpfOrder_WhateverOrderPluginsAddThem()
    {
        AvaloniaTabManager tabs = CreateTabManager();

        tabs.AddControl(new TextBlock(), "Performance", TabLocation.RightBottom);
        tabs.AddControl(new TextBlock(), "Errors", TabLocation.RightBottom);
        tabs.AddControl(new TextBlock(), "Code", TabLocation.RightBottom);
        tabs.AddControl(new TextBlock(), "Custom Plugin", TabLocation.RightBottom);
        tabs.AddControl(new TextBlock(), "History", TabLocation.RightBottom);

        tabs.RightBottom.Select(tab => tab.Title).ShouldBe(new[] { "Code", "Performance", "History", "Errors", "Custom Plugin" });
    }

    [AvaloniaFact]
    public void ClosableTab_ShowsACloseButton_ThatHidesIt()
    {
        AvaloniaTabManager tabs = CreateTabManager();
        MainPanelView view = new MainPanelView(tabs);
        Window window = new Window { Content = view, Width = 1000, Height = 700 };
        window.Show();
        tabs.AddControl(new TextBlock(), "Variables", TabLocation.RightTop);
        AvaloniaPluginTab closable = (AvaloniaPluginTab)tabs.AddControl(new TextBlock(), "Project Properties", TabLocation.RightTop);
        closable.CanClose = true;
        window.UpdateLayout();

        Button[] closeButtons = view.GetVisualDescendants().OfType<Button>().Where(b => b.Name == MainPanelView.CloseButtonName).ToArray();
        closeButtons.Length.ShouldBe(2);
        Button[] shown = closeButtons.Where(b => b.IsVisible).ToArray();
        shown.Length.ShouldBe(1);

        shown[0].RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

        closable.IsVisible.ShouldBeFalse();
        tabs.RightTop.Select(tab => tab.Title).ShouldBe(new[] { "Variables" });
        window.Close();
    }

    [AvaloniaFact]
    public void SelectingATab_ThroughItsPluginTab_BringsItToTheFront()
    {
        AvaloniaTabManager tabs = CreateTabManager();
        MainPanelView view = new MainPanelView(tabs);
        Window window = new Window { Content = view, Width = 1000, Height = 700 };
        window.Show();
        IPluginTab variables = tabs.AddControl(new TextBlock(), "Variables", TabLocation.CenterBottom);
        IPluginTab properties = tabs.AddControl(new TextBlock(), "Project Properties", TabLocation.CenterBottom);
        Dispatcher.UIThread.RunJobs();
        variables.IsSelected.ShouldBeTrue();

        // What Edit > Properties does after showing its tab.
        properties.IsSelected = true;
        Dispatcher.UIThread.RunJobs();

        TabControl region = view.GetVisualDescendants().OfType<TabControl>().First(tabControl => tabControl.ItemsSource == tabs.CenterBottom);
        region.SelectedItem.ShouldBeSameAs(properties);
        variables.IsSelected.ShouldBeFalse();
        window.Close();
    }
}
