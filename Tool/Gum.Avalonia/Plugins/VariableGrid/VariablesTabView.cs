using Gum.Avalonia.Shell;
using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.ComponentModel.Composition;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using AvaloniaDataUi;
using Gum.Commands;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Plugins.BaseClasses;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Gum.Plugins.VariableGrid;
using Gum.ToolStates;
using Gum.ViewModels;
using WpfDataUi;
using FluentIcons.Avalonia;
using Gum.Avalonia.Themes;

namespace Gum.Avalonia.Plugins.VariableGrid;

/// <summary>
/// The Avalonia Variables tab over <see cref="MainControlViewModel"/>: the state banner, errors and
/// category notice, the filter box (Ctrl+E focuses it, Escape clears it), the variables grid, the
/// behavior grid and behavior-variable list, and the Add Variable button. Twin of the WPF
/// <c>MainPropertyGrid</c>.
/// </summary>
public sealed class VariablesTabView : DockPanel, IVariablesTabView
{
    private readonly DataUiGrid _variablesGrid;
    private readonly DataUiGrid _behaviorGrid;
    private readonly TextBox _filterTextBox;
    private readonly ListBox _behaviorVariables;
    private MainControlViewModel? _viewModel;

    /// <summary>Builds the view; its grids use <paramref name="displayers"/>.</summary>
    public VariablesTabView(DisplayerRegistry displayers)
    {
        _variablesGrid = new DataUiGrid(displayers);
        _behaviorGrid = new DataUiGrid(displayers);
        // The WPF Variables grid's rows: separators and the set-value marker, no stripes.
        _variablesGrid.RowDecorator = VariableGridRows.Frame;
        _variablesGrid.AlternatesRowBackgrounds = false;

        Border stateBanner = new Border { Padding = new Thickness(6, 3) };
        stateBanner.Bind(Border.BackgroundProperty, new Binding(nameof(MainControlViewModel.StateBackground)) { Converter = DrawingColorToBrushConverter.Instance });
        stateBanner.Bind(IsVisibleProperty, new Binding(nameof(MainControlViewModel.HasStateInformation)));
        TextBlock stateText = new TextBlock { Foreground = Brushes.Black };
        stateText.Bind(TextBlock.TextProperty, new Binding(nameof(MainControlViewModel.StateInformation)));
        stateBanner.Child = stateText;

        TextBlock errors = new TextBlock { Foreground = Brushes.OrangeRed, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(4, 2) };
        errors.Bind(TextBlock.TextProperty, new Binding(nameof(MainControlViewModel.ErrorInformation)));
        errors.Bind(IsVisibleProperty, new Binding(nameof(MainControlViewModel.HasErrors)));

        TextBlock notification = new TextBlock { TextWrapping = TextWrapping.Wrap, Margin = new Thickness(5) };
        notification.Bind(TextBlock.TextProperty, new Binding(nameof(MainControlViewModel.CategoryNotification)));
        notification.Bind(IsVisibleProperty, new Binding(nameof(MainControlViewModel.HasCategoryNotification)));

        _filterTextBox = new TextBox { Watermark = "Filter variables", Padding = new Thickness(4, 2, 44, 2) };
        _filterTextBox.Bind(TextBox.TextProperty, new Binding(nameof(MainControlViewModel.VariableFilterText)) { Mode = BindingMode.TwoWay });
        ToolTip.SetTip(_filterTextBox, "Show only variables whose name contains this text. Press Ctrl+E to focus, Escape to clear.");
        _filterTextBox.AddHandler(KeyDownEvent, HandleFilterKeyDown, RoutingStrategies.Tunnel);
        TextBlock shortcutHint = new TextBlock
        {
            Text = "Ctrl+E",
            Opacity = 0.6,
            IsHitTestVisible = false,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 6, 0),
        };
        shortcutHint.Bind(IsVisibleProperty, new Binding(nameof(MainControlViewModel.IsFilterWatermarkVisible)));
        Button clearFilter = new Button
        {
            Content = GumFluentIcons.Create(FluentIcons.Common.Icon.Dismiss, 12),
            Padding = new Thickness(4, 0),
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center,
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Classes = { GumChromeStyles.IconButtonClass },
        };
        ToolTip.SetTip(clearFilter, "Clear filter");
        clearFilter.Bind(IsVisibleProperty, new Binding(nameof(MainControlViewModel.IsFilterWatermarkVisible)) { Converter = BoolConverters.Not });
        clearFilter.Click += (_, _) =>
        {
            ClearFilter();
            // Focus stays in the box so a different filter can be typed straight away.
            _filterTextBox.Focus();
        };
        // As in the WPF tab: a search icon, then the box with its hint and clear button in one cell.
        Grid filterBox = new Grid();
        filterBox.Children.Add(_filterTextBox);
        filterBox.Children.Add(shortcutHint);
        filterBox.Children.Add(clearFilter);
        Grid.SetColumn(filterBox, 1);
        Grid filterRow = new Grid { Margin = new Thickness(2, 2, 2, 4), ColumnDefinitions = new ColumnDefinitions("Auto,*") };
        FluentIcon searchIcon = GumFluentIcons.Create(FluentIcons.Common.Icon.Search, 16);
        searchIcon.Margin = new Thickness(2, 0, 4, 0);
        searchIcon.VerticalAlignment = VerticalAlignment.Center;
        filterRow.Children.Add(searchIcon);
        filterRow.Children.Add(filterBox);
        filterRow.Bind(IsVisibleProperty, new Binding(nameof(MainControlViewModel.ShowVariableGrid)));

        // The WPF tab's full-width icon button, its text larger than the body text.
        Binding largeText = new Binding(nameof(Window.FontSize))
        {
            RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor) { AncestorType = typeof(Window) },
            Converter = GumChromeStyles.ScaleFontSize(1.5),
        };
        FluentIcon addIcon = GumFluentIcons.Create(FluentIcons.Common.Icon.Add, largeText);
        addIcon.VerticalAlignment = VerticalAlignment.Center;
        TextBlock addText = new TextBlock { Text = "Add Variable", VerticalAlignment = VerticalAlignment.Center };
        addText.Bind(TextBlock.FontSizeProperty, largeText);
        StackPanel addContent = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 2 };
        addContent.Children.Add(addIcon);
        addContent.Children.Add(addText);
        Button addVariable = new Button
        {
            Content = addContent,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 2, 0, 0),
        };
        addVariable.Classes.Add(GumChromeStyles.IconButtonClass);
        AddVariableButton = addVariable;
        addVariable.Bind(IsVisibleProperty, new Binding(nameof(MainControlViewModel.IsAddVariableButtonVisible)));
        addVariable.Click += (_, _) => AddVariableClicked?.Invoke(this, EventArgs.Empty);

        _variablesGrid.Bind(IsVisibleProperty, new Binding(nameof(MainControlViewModel.ShowVariableGrid)));

        _behaviorVariables = new ListBox
        {
            ItemTemplate = new FuncDataTemplate<VariableSave>((variable, _) =>
                new TextBlock { Text = variable == null ? string.Empty : $"{variable.Name} ({variable.Type})" }),
        };
        _behaviorVariables.Bind(ItemsControl.ItemsSourceProperty, new Binding(nameof(MainControlViewModel.BehaviorVariables)));
        _behaviorVariables.Bind(SelectingItemsControl.SelectedItemProperty, new Binding(nameof(MainControlViewModel.SelectedBehaviorVariable)) { Mode = BindingMode.TwoWay });
        _behaviorVariables.SelectionChanged += (_, _) => SelectedBehaviorVariableChanged?.Invoke(this, EventArgs.Empty);
        AttachBehaviorVariablesMenu();

        DockPanel behaviorPanel = new DockPanel();
        SetDock(_behaviorGrid, Dock.Top);
        behaviorPanel.Children.Add(_behaviorGrid);
        behaviorPanel.Children.Add(_behaviorVariables);
        behaviorPanel.Bind(IsVisibleProperty, new Binding(nameof(MainControlViewModel.ShowBehaviorUi)));

        Grid center = new Grid();
        center.Children.Add(_variablesGrid);
        center.Children.Add(behaviorPanel);

        foreach (Control top in new Control[] { stateBanner, errors, notification, filterRow })
        {
            SetDock(top, Dock.Top);
            Children.Add(top);
        }
        SetDock(addVariable, Dock.Bottom);
        Children.Add(addVariable);
        Children.Add(center);

        DataContextChanged += (_, _) => _viewModel = DataContext as MainControlViewModel;
    }

    /// <inheritdoc/>
    public event EventHandler? AddVariableClicked;

    /// <inheritdoc/>
    public event EventHandler? SelectedBehaviorVariableChanged;

    /// <inheritdoc/>
    object IVariablesTabView.Control => this;

    /// <inheritdoc/>
    public IDataUiGrid VariablesGrid => _variablesGrid;

    /// <inheritdoc/>
    public IDataUiGrid BehaviorGrid => _behaviorGrid;

    /// <summary>The filter box, for tests.</summary>
    internal TextBox FilterTextBox => _filterTextBox;

    /// <summary>The Add Variable button, for tests.</summary>
    internal Button AddVariableButton { get; }

    /// <inheritdoc/>
    public void FocusVariableFilter()
    {
        // Posted: the caller may have just brought a hidden tab forward, and an unlaid-out box refuses focus.
        Dispatcher.UIThread.Post(() =>
        {
            _filterTextBox.Focus();
            _filterTextBox.SelectAll();
        }, DispatcherPriority.Loaded);
    }

    private void HandleFilterKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Escape)
        {
            return;
        }

        ClearFilter();
        _variablesGrid.Focus();
        e.Handled = true;
    }

    private void ClearFilter()
    {
        if (_viewModel != null)
        {
            _viewModel.VariableFilterText = string.Empty;
        }
    }

    private void AttachBehaviorVariablesMenu()
    {
        ContextMenu menu = new ContextMenu();
        menu.Opening += (_, e) =>
        {
            menu.Items.Clear();
            if (_viewModel != null)
            {
                foreach (ContextMenuItemViewModel item in _viewModel.BehaviorVariablesContextMenuItems)
                {
                    MenuItem menuItem = new MenuItem { Header = item.Text };
                    ContextMenuItemViewModel captured = item;
                    menuItem.Click += (_, _) => MenuItemActions.InvokeAfterClose(() => captured.Action?.Invoke());
                    menu.Items.Add(menuItem);
                }
            }
            e.Cancel = menu.Items.Count == 0;
        };
        _behaviorVariables.ContextMenu = menu;
    }
}

/// <summary>A <see cref="System.Drawing.Color"/> as an Avalonia brush.</summary>
public sealed class DrawingColorToBrushConverter : IValueConverter
{
    /// <summary>Shared instance.</summary>
    public static readonly DrawingColorToBrushConverter Instance = new DrawingColorToBrushConverter();

    /// <inheritdoc/>
    public object? Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture) =>
        value is System.Drawing.Color color ? new SolidColorBrush(Color.FromArgb(color.A, color.R, color.G, color.B)) : null;

    /// <inheritdoc/>
    public object ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>The Avalonia head's export of <see cref="VariableGridPluginBase"/>.</summary>
[Export(typeof(PluginBase))]
public class MainVariableGridPlugin : VariableGridPluginBase
{
    /// <summary>Creates the plugin.</summary>
    [ImportingConstructor]
    public MainVariableGridPlugin(ISelectedState selectedState, PropertyGridManager propertyGridManager, IVariableReferenceLogic variableReferenceLogic)
        : base(selectedState, propertyGridManager, variableReferenceLogic)
    {
    }
}

/// <summary>The Avalonia head's export of <see cref="ExclusionsPluginBase"/>.</summary>
[Export(typeof(PluginBase))]
public class ExclusionsPlugin : ExclusionsPluginBase
{
    /// <summary>Creates the plugin.</summary>
    [ImportingConstructor]
    public ExclusionsPlugin(ISelectedState selectedState, IGuiCommands guiCommands)
        : base(selectedState, guiCommands)
    {
    }
}
