using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Layout;
using Avalonia.Media;
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
            ItemTemplate = new FuncDataTemplate<MemberCategory>((category, _) => CreateCategoryView(category)),
        };

        Content = new ScrollViewer
        {
            Content = categories,
            HorizontalScrollBarVisibility = global::Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = global::Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
        };
    }

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
        registry.Register(typeof(StandardDisplayers.Slider), typeof(SliderDisplay));
        registry.Register(typeof(StandardDisplayers.PlusMinus), typeof(PlusMinusTextBox));
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

    private Control CreateCategoryView(MemberCategory category)
    {
        TextBlock headerText = new TextBlock
        {
            FontWeight = FontWeight.Medium,
            VerticalAlignment = VerticalAlignment.Center,
        };
        headerText.Bind(TextBlock.TextProperty, new Binding(nameof(MemberCategory.Name)));

        Border header = new Border
        {
            Background = Brushes.Transparent,
            Padding = new Thickness(0, 2),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Child = headerText,
        };
        DataUiContextMenus.AttachCategoryMenu(header, category);

        ItemsControl rows = new ItemsControl
        {
            ItemTemplate = new FuncDataTemplate<InstanceMember>((_, _) => new SingleDataUiContainer(this)),
        };
        rows.Bind(ItemsControl.ItemsSourceProperty, new Binding(nameof(MemberCategory.Members)));

        Expander expander = new Expander
        {
            Header = header,
            Content = rows,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            Margin = new Thickness(0, 0, 0, 2),
        };
        expander.Bind(Expander.IsExpandedProperty, new Binding(nameof(MemberCategory.IsExpanded)) { Mode = BindingMode.TwoWay });
        expander.Bind(IsVisibleProperty, new Binding(nameof(MemberCategory.IsVisible)));
        if (category.HeaderColor is System.Drawing.Color headerColor)
        {
            expander.Background = new SolidColorBrush(Color.FromArgb(headerColor.A, headerColor.R, headerColor.G, headerColor.B));
        }
        return expander;
    }
}
