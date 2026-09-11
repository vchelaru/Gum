using Gum.Plugins.PropertiesWindowPlugin;
using System.Windows;
using System.Windows.Controls;

namespace Gum.Gui.Controls;

/// <summary>
/// The WPF Project Properties tab: a property grid over the <see cref="ProjectPropertiesViewModel"/>
/// the shared plugin hands to the tab manager (TabViewRegistry makes it this control's DataContext),
/// driven by the shared <see cref="ProjectPropertiesGridPresenter"/>.
/// </summary>
public partial class ProjectPropertiesControl : UserControl
{
    private readonly ProjectPropertiesGridPresenter _presenter;

    public ProjectPropertiesControl()
    {
        InitializeComponent();
        _presenter = new ProjectPropertiesGridPresenter(DataGrid);
        DataContextChanged += (_, e) => _presenter.Bind(e.NewValue as ProjectPropertiesViewModel);
    }

    private void CancelButtonClicked(object? sender, RoutedEventArgs e)
    {
        _presenter.ViewModel?.RequestClose();
    }
}
