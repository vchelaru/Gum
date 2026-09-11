using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using AvaloniaDataUi;
using Gum.Plugins.PropertiesWindowPlugin;

namespace Gum.Avalonia.Panels;

/// <summary>
/// The Project Properties tab: a property grid over the <see cref="ProjectPropertiesViewModel"/>
/// the shared plugin hands to the tab manager, laid out by the shared
/// <see cref="ProjectPropertiesGridPresenter"/> exactly as the WPF tab, with a Close button.
/// </summary>
public sealed class ProjectPropertiesView : DockPanel
{
    private readonly ProjectPropertiesGridPresenter _presenter;

    /// <summary>Builds the view.</summary>
    public ProjectPropertiesView()
    {
        Grid = new DataUiGrid();
        _presenter = new ProjectPropertiesGridPresenter(Grid);

        Button close = new Button { Content = "Close", HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(4) };
        close.Click += (_, _) => _presenter.ViewModel?.RequestClose();
        SetDock(close, Dock.Bottom);
        Children.Add(close);
        Children.Add(Grid);

        DataContextChanged += (_, _) => _presenter.Bind(DataContext as ProjectPropertiesViewModel);
    }

    /// <summary>The property grid.</summary>
    public DataUiGrid Grid { get; }
}
