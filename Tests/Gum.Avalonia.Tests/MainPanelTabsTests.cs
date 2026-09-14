using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using System.Collections.ObjectModel;
using Gum.Avalonia.Shell;
using Gum.Avalonia.Themes;
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
    public void RetitlingATab_ReordersItInPlace_WithoutDuplicatingIt()
    {
        AvaloniaTabManager tabs = CreateTabManager();
        IPluginTab renamed = tabs.AddControl(new TextBlock(), "Zzz", TabLocation.RightBottom);
        tabs.AddControl(new TextBlock(), "History", TabLocation.RightBottom);
        tabs.RightBottom.Select(tab => tab.Title).ShouldBe(new[] { "History", "Zzz" });

        renamed.Title = "Code";
        renamed.Hide();
        renamed.Show();
        Dispatcher.UIThread.RunJobs();

        tabs.RightBottom.Select(tab => tab.Title).ShouldBe(new[] { "Code", "History" });
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
        // Hiding refilters the region on the next dispatcher cycle.
        Dispatcher.UIThread.RunJobs();

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

    [AvaloniaFact]
    public void ShowingATab_KeepsTheTabTheUserSelected()
    {
        AvaloniaTabManager tabs = CreateTabManager();
        MainPanelView view = new MainPanelView(tabs);
        Window window = new Window { Content = view, Width = 1000, Height = 700 };
        window.Show();
        IPluginTab code = tabs.AddControl(new TextBlock(), "Code", TabLocation.RightBottom);
        IPluginTab performance = tabs.AddControl(new TextBlock(), "Performance", TabLocation.RightBottom);
        AvaloniaPluginTab textureCoordinates = (AvaloniaPluginTab)tabs.AddControl(new TextBlock(), "Texture Coordinates", TabLocation.RightBottom);
        textureCoordinates.Hide();
        Dispatcher.UIThread.RunJobs();
        TabControl region = RegionFor(view, tabs.RightBottom);
        region.SelectedItem = performance;
        Dispatcher.UIThread.RunJobs();

        // Selecting a Sprite brings the Texture Coordinates tab back; the user's tab stays in front (#4694).
        textureCoordinates.Show();
        Dispatcher.UIThread.RunJobs();

        performance.IsSelected.ShouldBeTrue();
        region.SelectedItem.ShouldBeSameAs(performance);
        code.IsSelected.ShouldBeFalse();
        tabs.RightBottom.Select(tab => tab.Title).ShouldBe(new[] { "Code", "Performance", "Texture Coordinates" });
        window.Close();
    }

    [AvaloniaFact]
    public void TheCodeTab_IsNotTheDefault_OnceAnotherTabExists()
    {
        // Code generation runs whenever the Code tab has focus, which slows every selection, so it
        // only stays in front when the user put it there (#4694).
        AvaloniaTabManager tabs = CreateTabManager();
        MainPanelView view = new MainPanelView(tabs);
        Window window = new Window { Content = view, Width = 1000, Height = 700 };
        window.Show();
        IPluginTab code = tabs.AddControl(new TextBlock(), "Code", TabLocation.RightBottom);
        Dispatcher.UIThread.RunJobs();
        code.IsSelected.ShouldBeTrue("the only tab");

        IPluginTab performance = tabs.AddControl(new TextBlock(), "Performance", TabLocation.RightBottom);
        Dispatcher.UIThread.RunJobs();

        performance.IsSelected.ShouldBeTrue();
        code.IsSelected.ShouldBeFalse();
        RegionFor(view, tabs.RightBottom).SelectedItem.ShouldBeSameAs(performance);
        window.Close();
    }

    [AvaloniaFact]
    public void HidingTheSelectedTab_SelectsAnotherTab_RatherThanCode()
    {
        AvaloniaTabManager tabs = CreateTabManager();
        MainPanelView view = new MainPanelView(tabs);
        Window window = new Window { Content = view, Width = 1000, Height = 700 };
        window.Show();
        IPluginTab code = tabs.AddControl(new TextBlock(), "Code", TabLocation.RightBottom);
        IPluginTab performance = tabs.AddControl(new TextBlock(), "Performance", TabLocation.RightBottom);
        AvaloniaPluginTab textureCoordinates = (AvaloniaPluginTab)tabs.AddControl(new TextBlock(), "Texture Coordinates", TabLocation.RightBottom);
        Dispatcher.UIThread.RunJobs();
        TabControl region = RegionFor(view, tabs.RightBottom);
        region.SelectedItem = textureCoordinates;
        Dispatcher.UIThread.RunJobs();

        // Selecting a Circle after a Sprite hides the Texture Coordinates tab.
        textureCoordinates.Hide();
        Dispatcher.UIThread.RunJobs();

        textureCoordinates.IsSelected.ShouldBeFalse();
        performance.IsSelected.ShouldBeTrue();
        code.IsSelected.ShouldBeFalse();
        region.SelectedItem.ShouldBeSameAs(performance);
        window.Close();
    }

    [AvaloniaFact]
    public void Regions_HaveADarkTabStrip_AndSplittersAreInvisible()
    {
        // The WPF main panel: the tab strip shows the window background, the content sits on
        // Surface01, and a splitter is an invisible grab area; the gap between regions is the
        // only separator (#4694).
        AvaloniaTabManager tabs = CreateTabManager();
        MainPanelView view = new MainPanelView(tabs);
        Window window = new Window { Content = view, Width = 1000, Height = 700 };
        window.Show();
        tabs.AddControl(new TextBlock(), "Variables", TabLocation.RightTop);
        window.UpdateLayout();

        TabControl region = RegionFor(view, tabs.RightTop);
        region.Background.ShouldBeSameAs(ThemeBrushes.Get(window, "Frb.Brushes.Background", Brushes.Red));
        ContentPresenter content = region.GetVisualDescendants().OfType<ContentPresenter>().First(presenter => presenter.Name == "PART_SelectedContentHost");
        content.Background.ShouldBeSameAs(ThemeBrushes.Get(window, "Frb.Surface01", Brushes.Red));

        GridSplitter[] splitters = view.GetVisualDescendants().OfType<GridSplitter>().ToArray();
        splitters.Length.ShouldBe(4);
        foreach (GridSplitter splitter in splitters)
        {
            Math.Min(splitter.Bounds.Width, splitter.Bounds.Height).ShouldBe(MainPanelView.SplitterThickness);
            splitter.Background.ShouldBeSameAs(Brushes.Transparent);
            splitter.GetVisualDescendants().OfType<Border>()
                .ShouldAllBe(border => border.Background == Brushes.Transparent && border.BorderThickness == default);
        }
        window.Close();
    }

    private static TabControl RegionFor(MainPanelView view, ObservableCollection<AvaloniaPluginTab> source) =>
        view.GetVisualDescendants().OfType<TabControl>().First(tabControl => tabControl.ItemsSource == source);

    [AvaloniaFact]
    public void SelectingATab_ThatHidesItselfOnFocus_DoesNotCrashTheHostingTabControl()
    {
        // CodeOutputPlugin does exactly this: its GotFocus handler hides its own tab when there is
        // nothing to display. That runs synchronously inside the hosting TabControl's own
        // SelectionChanged, so the tab's IsVisible flip must not synchronously clear/rebuild the
        // ObservableCollection that IS that TabControl's ItemsSource, or Avalonia's selection
        // bookkeeping throws ArgumentOutOfRangeException.
        AvaloniaTabManager tabs = CreateTabManager();
        MainPanelView view = new MainPanelView(tabs);
        Window window = new Window { Content = view, Width = 1000, Height = 700 };
        window.Show();
        IPluginTab other = tabs.AddControl(new TextBlock(), "Other", TabLocation.RightBottom);
        AvaloniaPluginTab selfHiding = (AvaloniaPluginTab)tabs.AddControl(new TextBlock(), "Self-Hiding", TabLocation.RightBottom);
        selfHiding.GotFocus += selfHiding.Hide;
        Dispatcher.UIThread.RunJobs();
        TabControl region = view.GetVisualDescendants().OfType<TabControl>().First(tabControl => tabControl.ItemsSource == tabs.RightBottom);

        region.SelectedItem = selfHiding;
        Dispatcher.UIThread.RunJobs();

        selfHiding.IsVisible.ShouldBeFalse();
        tabs.RightBottom.ShouldNotContain(selfHiding);
        window.Close();
    }
}
