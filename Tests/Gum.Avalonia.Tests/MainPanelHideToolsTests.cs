using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Gum.Avalonia.Shell;
using Gum.Plugins;
using Gum.Plugins.InternalPlugins.HideShowTools;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Gum.Avalonia.Tests;

/// <summary>
/// View > Hide Tools collapses the tree, middle column, bottom-right panel and their splitters so
/// the canvas fills the window; Show Tools puts back the sizes they had (#7355). Each test hosts its
/// own panel in its own window and closes it.
/// </summary>
public class MainPanelHideToolsTests : IDisposable
{
    private readonly AvaloniaTabManager _tabs =
        ActivatorUtilities.CreateInstance<AvaloniaTabManager>(TestAppBuilder.Services);
    private readonly MainPanelView _view;
    private readonly Window _window;
    private readonly TextBlock _left = new TextBlock { Text = "Tree" };
    private readonly TextBlock _center = new TextBlock { Text = "Variables" };
    private readonly TextBlock _canvas = new TextBlock { Text = "Editor" };
    private readonly TextBlock _bottomRight = new TextBlock { Text = "Output" };

    public MainPanelHideToolsTests()
    {
        _view = new MainPanelView(_tabs);
        _window = new Window { Content = _view, Width = 1000, Height = 700 };
        _window.Show();
        _tabs.AddControl(_left, "Project", TabLocation.Left);
        _tabs.AddControl(_center, "Variables", TabLocation.CenterTop);
        _tabs.AddControl(_canvas, "Editor", TabLocation.RightTop);
        _tabs.AddControl(_bottomRight, "Errors", TabLocation.RightBottom);
        Settle();
    }

    public void Dispose() => _window.Close();

    private void Settle()
    {
        Dispatcher.UIThread.RunJobs();
        _window.UpdateLayout();
    }

    private void Toggle()
    {
        new HideShowToolsLogic(_tabs).ToggleToolsVisibility();
        Settle();
    }

    [AvaloniaFact]
    public void HideTools_CollapsesEveryToolPanel_AndLetsTheCanvasFill()
    {
        _left.Bounds.Width.ShouldBeGreaterThan(0);
        _bottomRight.Bounds.Height.ShouldBeGreaterThan(0);
        double canvasWidthBefore = _canvas.Bounds.Width;
        double heightBefore = _canvas.Bounds.Height;

        Toggle();

        _left.IsEffectivelyVisible.ShouldBeFalse();
        _center.IsEffectivelyVisible.ShouldBeFalse();
        _bottomRight.IsEffectivelyVisible.ShouldBeFalse();
        _canvas.IsEffectivelyVisible.ShouldBeTrue();
        _canvas.Bounds.Width.ShouldBeGreaterThan(canvasWidthBefore);
        _canvas.Bounds.Width.ShouldBe(_view.Bounds.Width);
        _canvas.Bounds.Height.ShouldBeGreaterThan(heightBefore);
    }

    [AvaloniaFact]
    public void ShowTools_RestoresTheSizesTheUserHadDragged()
    {
        _view.ColumnDefinitions[0].Width = new GridLength(333);
        _view.ColumnDefinitions[2].Width = new GridLength(222);
        Settle();

        Toggle();
        Toggle();

        _left.IsEffectivelyVisible.ShouldBeTrue();
        _view.ColumnDefinitions[0].ActualWidth.ShouldBe(333);
        _view.ColumnDefinitions[2].ActualWidth.ShouldBe(222);
        _bottomRight.Bounds.Height.ShouldBeGreaterThan(0);
    }

    [AvaloniaFact]
    public void HiddenTools_DoNotOverwriteTheSizesSavedOnExit()
    {
        _view.ColumnDefinitions[0].Width = new GridLength(333);
        Settle();
        double bottom = _tabs.BottomRightHeight;

        Toggle();

        _tabs.LeftColumnWidth.ShouldBe(333);
        _tabs.BottomRightHeight.ShouldBe(bottom);
    }

    [AvaloniaFact]
    public void ResizingTheWindowWhileHidden_KeepsTheCanvasFilling_AndShowStillRestores()
    {
        _view.ColumnDefinitions[0].Width = new GridLength(333);
        Settle();
        Toggle();

        _window.Width = 1400;
        Settle();
        _canvas.Bounds.Width.ShouldBe(_view.Bounds.Width);
        _canvas.Bounds.Width.ShouldBe(1400);

        Toggle();
        _view.ColumnDefinitions[0].ActualWidth.ShouldBe(333);
    }

    [AvaloniaFact]
    public void TabAddedWhileHidden_StaysCollapsed()
    {
        Toggle();
        TextBlock late = new TextBlock();
        _tabs.Select((AvaloniaPluginTab)_tabs.AddControl(late, "Late", TabLocation.Left));
        Settle();

        late.Bounds.Width.ShouldBe(0);

        late.IsEffectivelyVisible.ShouldBeFalse();
    }

    [AvaloniaFact]
    public void SelectedToolTab_StaysSelected_AcrossHideAndShow()
    {
        TextBlock secondContent = new TextBlock();
        AvaloniaPluginTab second = (AvaloniaPluginTab)_tabs.AddControl(secondContent, "Styles", TabLocation.CenterTop);
        _tabs.Select(second);
        Settle();

        Toggle();
        Toggle();

        second.IsSelected.ShouldBeTrue();
        secondContent.IsEffectivelyVisible.ShouldBeTrue();
    }

    [AvaloniaFact]
    public void ToggleTwice_LeavesTheLayoutAsItWas()
    {
        double left = _left.Bounds.Width;
        double canvas = _canvas.Bounds.Width;

        Toggle();
        Toggle();

        _left.Bounds.Width.ShouldBe(left);
        _canvas.Bounds.Width.ShouldBe(canvas);
        _tabs.IsToolsVisible.ShouldBeTrue();
    }
}
