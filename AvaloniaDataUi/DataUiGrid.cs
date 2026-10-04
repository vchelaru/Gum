using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using AvaloniaDataUi.Controls;
using WpfDataUi;
using WpfDataUi.DataTypes;
using WpfDataUi.EventArguments;

namespace AvaloniaDataUi;

/// <summary>
/// The Avalonia view of a <see cref="DataUiGridModel"/>: a scrolling list of collapsible categories
/// whose rows are <see cref="SingleDataUiContainer"/>s. All category, filter, and multi-select logic
/// lives in the model; the editors for each row come from <see cref="Displayers"/>.
/// </summary>
public class DataUiGrid : UserControl, IDataUiGrid
{
    private readonly DataUiGridModel _model;
    private readonly HashSet<SingleDataUiContainer> _liveContainers;
    private readonly Style[] _rowStripes;
    private bool _alternatesRowBackgrounds = true;
    private readonly ItemsControl _categories;
    private readonly ScrollViewer _scrollViewer;
    private SingleDataUiContainer? _anchorRow;
    private double _anchorViewportTop;

    /// <summary>Creates a grid that uses the standard editors.</summary>
    public DataUiGrid() : this(CreateStandardRegistry())
    {
    }

    /// <summary>Creates a grid whose rows resolve displayers through <paramref name="displayers"/>.</summary>
    public DataUiGrid(DisplayerRegistry displayers)
    {
        _model = new DataUiGridModel();
        _liveContainers = new HashSet<SingleDataUiContainer>();
        Displayers = displayers;

        ItemsControl categories = new ItemsControl
        {
            ItemsSource = _model.Categories,
            ItemTemplate = new FuncDataTemplate<MemberCategory>((category, _) => new DataUiCategoryView(this, category)),
        };

        _categories = categories;
        _scrollViewer = new ScrollViewer
        {
            Content = categories,
            HorizontalScrollBarVisibility = global::Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = global::Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
        };
        _scrollViewer.ScrollChanged += HandleScrollChanged;
        Content = _scrollViewer;

        // Rows alternate a faint dark stripe, as in the WPF grid: 10% black, then 5%.
        _rowStripes = new[] { RowStripe(offset: 1, opacity: 0.10), RowStripe(offset: 0, opacity: 0.05) };
        Styles.AddRange(_rowStripes);
    }

    /// <summary>Defines the <see cref="CategoryHeaderBackground"/> property.</summary>
    public static readonly StyledProperty<IBrush?> CategoryHeaderBackgroundProperty =
        AvaloniaProperty.Register<DataUiGrid, IBrush?>(nameof(CategoryHeaderBackground));

    /// <summary>Defines the <see cref="CategoryHeaderForeground"/> property.</summary>
    public static readonly StyledProperty<IBrush?> CategoryHeaderForegroundProperty =
        AvaloniaProperty.Register<DataUiGrid, IBrush?>(nameof(CategoryHeaderForeground));

    /// <summary>
    /// The header strip of a category with no <see cref="MemberCategory.HeaderColor"/>; transparent
    /// when unset.
    /// </summary>
    public IBrush? CategoryHeaderBackground
    {
        get => GetValue(CategoryHeaderBackgroundProperty);
        set => SetValue(CategoryHeaderBackgroundProperty, value);
    }

    /// <summary>The category names' text brush; the inherited foreground when unset.</summary>
    public IBrush? CategoryHeaderForeground
    {
        get => GetValue(CategoryHeaderForegroundProperty);
        set => SetValue(CategoryHeaderForegroundProperty, value);
    }

    /// <summary>
    /// Defines the inherited OverridesIsDefaultStyling attached property. When true, fields keep the
    /// theme's look whatever their value state, with no default or indeterminate tint: the WPF grid's
    /// property of the same name, which the Gum tool's grids set.
    /// </summary>
    public static readonly AttachedProperty<bool> OverridesIsDefaultStylingProperty =
        AvaloniaProperty.RegisterAttached<DataUiGrid, Control, bool>("OverridesIsDefaultStyling", inherits: true);

    /// <summary>Gets whether default-value tinting is turned off where <paramref name="element"/> sits.</summary>
    public static bool GetOverridesIsDefaultStyling(Control element) => element.GetValue(OverridesIsDefaultStylingProperty);

    /// <summary>Turns default-value tinting off (or back on) for <paramref name="element"/> and its descendants.</summary>
    public static void SetOverridesIsDefaultStyling(Control element, bool value) => element.SetValue(OverridesIsDefaultStylingProperty, value);

    /// <summary>
    /// Wraps each row's editor host before it is shown, for a grid that frames its rows (the Gum
    /// Variables tab adds separators and a set-value marker). Null shows the host as it is.
    /// </summary>
    public Func<Control, Control>? RowDecorator { get; set; }

    /// <summary>Whether rows alternate a faint dark stripe; on by default, as in the WPF grid.</summary>
    public bool AlternatesRowBackgrounds
    {
        get => _alternatesRowBackgrounds;
        set
        {
            if (_alternatesRowBackgrounds == value)
            {
                return;
            }
            _alternatesRowBackgrounds = value;
            foreach (Style stripe in _rowStripes)
            {
                if (value)
                {
                    Styles.Add(stripe);
                }
                else
                {
                    Styles.Remove(stripe);
                }
            }
        }
    }

    private static Style RowStripe(int offset, double opacity) =>
        new Style(selector => selector.OfType<ItemsControl>().Class(DataUiCategoryView.RowsClass)
            .Child().OfType<ContentPresenter>().NthChild(2, offset))
        {
            Setters = { new Setter(ContentPresenter.BackgroundProperty, new SolidColorBrush(Colors.Black, opacity)) },
        };

    /// <summary>Maps displayer keys to controls for this grid's rows.</summary>
    public DisplayerRegistry Displayers { get; set; }

    /// <summary>The model this grid renders.</summary>
    public DataUiGridModel Model => _model;

    /// <inheritdoc/>
    public object? Instance
    {
        get => _model.Instance;
        set => _model.Instance = value;
    }

    /// <inheritdoc/>
    public BulkObservableCollection<MemberCategory> Categories => _model.Categories;

    /// <inheritdoc cref="DataUiGridModel.TypesToIgnore"/>
    public ObservableCollection<Type> TypesToIgnore => _model.TypesToIgnore;

    /// <inheritdoc cref="DataUiGridModel.MembersToIgnore"/>
    public ObservableCollection<string> MembersToIgnore => _model.MembersToIgnore;

    /// <inheritdoc/>
    public event Action<string, PropertyChangedArgs>? PropertyChange
    {
        add => _model.PropertyChange += value;
        remove => _model.PropertyChange -= value;
    }

    /// <summary>Raised before a member is set by the UI.</summary>
    public event Action<string, BeforePropertyChangedArgs>? BeforePropertyChange
    {
        add => _model.BeforePropertyChange += value;
        remove => _model.BeforePropertyChange -= value;
    }

    /// <summary>A registry with every standard editor registered.</summary>
    public static DisplayerRegistry CreateStandardRegistry()
    {
        DisplayerRegistry registry = new DisplayerRegistry();
        registry.Register(typeof(StandardDisplayers.TextBox), typeof(TextBoxDisplay));
        registry.Register(typeof(StandardDisplayers.MultiLineTextBox), typeof(MultiLineTextBoxDisplay));
        registry.Register(typeof(StandardDisplayers.CheckBox), typeof(CheckBoxDisplay));
        registry.Register(typeof(StandardDisplayers.NullableBool), typeof(NullableBoolDisplay));
        registry.Register(typeof(StandardDisplayers.ComboBox), typeof(ComboBoxDisplay));
        registry.Register(typeof(StandardDisplayers.ListBox), typeof(ListBoxDisplay));
        registry.Register(typeof(StandardDisplayers.Slider), typeof(SliderDisplay));
        registry.Register(typeof(StandardDisplayers.AngleSelector), typeof(AngleSelectorDisplay));
        registry.Register(typeof(StandardDisplayers.FileSelection), typeof(FileSelectionDisplay));
        registry.Register(typeof(StandardDisplayers.MultiFile), typeof(MultiFileDisplay));
        registry.Register(typeof(StandardDisplayers.StringList), typeof(StringListTextBoxDisplay));
        return registry;
    }

    /// <inheritdoc/>
    public void SetCategories(IList<MemberCategory> newCategories) => _model.SetCategories(newCategories);

    /// <inheritdoc/>
    public void SetMultipleCategoryLists(List<List<MemberCategory>> listOfCategoryLists) =>
        _model.SetMultipleCategoryLists(listOfCategoryLists);

    /// <inheritdoc/>
    public void ApplyMemberFilter(Func<InstanceMember, bool>? isMatch) => _model.ApplyMemberFilter(isMatch);

    /// <inheritdoc/>
    public InstanceMember? GetInstanceMember(string memberName) => _model.GetInstanceMember(memberName);

    /// <inheritdoc/>
    public void InsertSpacesInCamelCaseMemberNames() => _model.InsertSpacesInCamelCaseMemberNames();

    /// <inheritdoc/>
    public void Refresh()
    {
        HashSet<InstanceMember> refreshed = new HashSet<InstanceMember>();
        foreach (SingleDataUiContainer container in _liveContainers.ToList())
        {
            if (container.Displayer is IDataUi dataUi && container.Member != null)
            {
                dataUi.Refresh();
                // The displayer's own Refresh() already re-read the value, but a row-frame decoration
                // bound directly to the member (e.g. the "not default" marker icon) isn't part of the
                // displayer and needs its own notification.
                container.Member.NotifyIsDefaultChanged();
                refreshed.Add(container.Member);
            }
        }

        // Rows without a live editor still raise a change so they read the value when shown.
        foreach (MemberCategory category in _model.Categories)
        {
            foreach (InstanceMember member in category.Members)
            {
                if (!refreshed.Contains(member))
                {
                    member.SimulateValueChanged();
                }
            }
        }
    }

    // Wrapping rows change height when the grid's width changes, which would push the content under
    // the viewport around. The first visible row is remembered while the user scrolls, and when the
    // extent changes without a scroll the offset is shifted so that row stays where it was.
    private void HandleScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        bool extentChangedWithoutScroll = e.ExtentDelta.Y != 0 && e.OffsetDelta.Y == 0;
        if (extentChangedWithoutScroll
            && _anchorRow != null
            && _anchorRow.IsVisible
            && _anchorRow.GetVisualRoot() != null
            && _anchorRow.TranslatePoint(new Point(0, 0), _categories) is Point anchorTop)
        {
            double desiredOffset = anchorTop.Y - _anchorViewportTop;
            double maxOffset = Math.Max(0, _scrollViewer.Extent.Height - _scrollViewer.Viewport.Height);
            double clamped = Math.Clamp(desiredOffset, 0, maxOffset);
            if (Math.Abs(clamped - _scrollViewer.Offset.Y) > 0.5)
            {
                _scrollViewer.Offset = new Vector(_scrollViewer.Offset.X, clamped);
                return;
            }
        }

        CaptureAnchor();
    }

    private void CaptureAnchor()
    {
        double offsetY = _scrollViewer.Offset.Y;
        SingleDataUiContainer? best = null;
        double bestTop = double.MaxValue;
        foreach (SingleDataUiContainer container in _liveContainers)
        {
            if (!container.IsVisible || container.GetVisualRoot() == null)
            {
                continue;
            }
            Point? top = container.TranslatePoint(new Point(0, 0), _categories);
            if (top is Point point && point.Y + container.Bounds.Height > offsetY && point.Y < bestTop)
            {
                best = container;
                bestTop = point.Y;
            }
        }
        _anchorRow = best;
        _anchorViewportTop = bestTop - offsetY;
    }

    internal void RegisterContainer(SingleDataUiContainer container) => _liveContainers.Add(container);

    internal void UnregisterContainer(SingleDataUiContainer container) => _liveContainers.Remove(container);

    /// <summary>The live row hosts, for tests.</summary>
    internal IReadOnlyCollection<SingleDataUiContainer> LiveContainers => _liveContainers;
}
