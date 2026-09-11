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

        Content = new ScrollViewer
        {
            Content = categories,
            HorizontalScrollBarVisibility = global::Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = global::Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
        };

        // Rows alternate a faint dark stripe, as in the WPF grid: 10% black, then 5%.
        Styles.Add(RowStripe(offset: 1, opacity: 0.10));
        Styles.Add(RowStripe(offset: 0, opacity: 0.05));
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
        registry.Register(typeof(StandardDisplayers.EditableComboBox), typeof(EditableComboBoxDisplay));
        registry.Register(typeof(StandardDisplayers.ListBox), typeof(ListBoxDisplay));
        registry.Register(typeof(StandardDisplayers.Slider), typeof(SliderDisplay));
        registry.Register(typeof(StandardDisplayers.PlusMinus), typeof(PlusMinusTextBox));
        registry.Register(typeof(StandardDisplayers.AngleSelector), typeof(AngleSelectorDisplay));
        registry.Register(typeof(StandardDisplayers.FileSelection), typeof(FileSelectionDisplay));
        registry.Register(typeof(StandardDisplayers.MultiFile), typeof(MultiFileDisplay));
        registry.Register(typeof(StandardDisplayers.StringList), typeof(StringListTextBoxDisplay));
        registry.Register(typeof(StandardDisplayers.InlineChannels), typeof(InlineChannelsDisplay));
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

    internal void RegisterContainer(SingleDataUiContainer container) => _liveContainers.Add(container);

    internal void UnregisterContainer(SingleDataUiContainer container) => _liveContainers.Remove(container);

    /// <summary>The live row hosts, for tests.</summary>
    internal IReadOnlyCollection<SingleDataUiContainer> LiveContainers => _liveContainers;
}
