using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using EditorTabPlugin_XNA.ViewModels;
using Gum.DataTypes;

namespace Gum.Avalonia.Plugins.EditorTab;

/// <summary>
/// The orange "Snap to Grid: X uses non-pixel units..." bar above the editor canvas. Each offending
/// instance name is a link that selects that instance (issue #5703).
/// </summary>
public class GridSnapWarningBar : Border
{
    private readonly WrapPanel _panel = new();
    private EditorViewModel? _viewModel;

    public GridSnapWarningBar()
    {
        Background = Brushes.Orange;
        Padding = new Thickness(6, 3);
        Child = _panel;
        DataContextChanged += (_, _) => Attach(DataContext as EditorViewModel);
    }

    private void Attach(EditorViewModel? viewModel)
    {
        if (_viewModel != null)
        {
            _viewModel.PropertyChanged -= HandlePropertyChanged;
        }
        _viewModel = viewModel;
        if (_viewModel != null)
        {
            _viewModel.PropertyChanged += HandlePropertyChanged;
        }
        Rebuild();
    }

    private void HandlePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(EditorViewModel.GridSnapWarningText)
            or nameof(EditorViewModel.GridSnapWarningOffenders)
            or nameof(EditorViewModel.HasGridSnapWarning))
        {
            Rebuild();
        }
    }

    private void Rebuild()
    {
        _panel.Children.Clear();
        IsVisible = _viewModel?.HasGridSnapWarning == true;
        if (!IsVisible)
        {
            return;
        }

        var offenders = _viewModel!.GridSnapWarningOffenders;
        if (offenders.Count == 0)
        {
            _panel.Children.Add(Label(_viewModel.GridSnapWarningText ?? string.Empty));
            return;
        }

        _panel.Children.Add(Label("Snap to Grid: "));
        for (int i = 0; i < offenders.Count; i++)
        {
            InstanceSave offender = offenders[i];
            _panel.Children.Add(Link(offender));
            if (i < offenders.Count - 1)
            {
                _panel.Children.Add(Label(", "));
            }
        }
        _panel.Children.Add(Label(offenders.Count == 1
            ? " uses non-pixel units and won't fully snap"
            : " use non-pixel units and won't fully snap"));
    }

    private static TextBlock Label(string text) => new() { Text = text, Foreground = Brushes.Black };

    private TextBlock Link(InstanceSave instance)
    {
        TextBlock link = new()
        {
            Text = instance.Name,
            Foreground = Brushes.Black,
            TextDecorations = TextDecorations.Underline,
            Cursor = new Cursor(StandardCursorType.Hand),
        };
        link.PointerPressed += (_, e) =>
        {
            _viewModel?.SelectGridSnapOffender(instance);
            e.Handled = true;
        };
        return link;
    }
}
