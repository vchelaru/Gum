using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WpfDataUi.DataTypes;
using WpfDataUi.EventArguments;

namespace WpfDataUi;

/// <summary>
/// The WPF view of a <see cref="DataUiGridModel"/>: an <see cref="ItemsControl"/> of categories
/// whose rows are <see cref="SingleDataUiContainer"/>s. All category, filter, and multi-select
/// logic lives in the model.
/// </summary>
public class DataUiGrid : ItemsControl, INotifyPropertyChanged, IDataUiGrid
{
    #region Fields

    private readonly DataUiGridModel _model;

    #endregion

    #region Dependency Properties

    public static readonly DependencyProperty InstanceProperty =
        DependencyProperty.Register(
            nameof(Instance),
            typeof(object),
            typeof(DataUiGrid),
            new PropertyMetadata(null, HandleInstanceChanged));

    public bool IsAutoPopulateCategoriesEnabled
    {
        get => _model.IsAutoPopulateCategoriesEnabled;
        set => _model.IsAutoPopulateCategoriesEnabled = value;
    }

    private static void HandleInstanceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var grid = (DataUiGrid)d;
        grid._model.Instance = e.NewValue;
    }

    /// <inheritdoc cref="DataUiGridModel.Instance"/>
    public object? Instance
    {
        get => GetValue(InstanceProperty);
        set => SetValue(InstanceProperty, value);
    }

    public static readonly DependencyProperty OrientationProperty =
        DependencyProperty.Register(
            nameof(Orientation),
            typeof(Orientation),
            typeof(DataUiGrid),
            new PropertyMetadata(Orientation.Vertical));

    public Orientation Orientation
    {
        get => (Orientation)GetValue(OrientationProperty);
        set => SetValue(OrientationProperty, value);
    }



    public static void SetOverridesIsDefaultStyling(DependencyObject element, bool value)
    {
        element.SetValue(OverridesIsDefaultStylingProperty, value);
    }
    public static bool GetOverridesIsDefaultStyling(DependencyObject element)
    {
        return (bool)element.GetValue(OverridesIsDefaultStylingProperty);
    }

    // When set, rows show the per-row "is edited" icon instead of displayer-painted default backgrounds.
    public static readonly DependencyProperty OverridesIsDefaultStylingProperty =
        DependencyProperty.RegisterAttached("OverridesIsDefaultStyling", typeof(bool), typeof(DataUiGrid), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.Inherits));

    public static readonly DependencyProperty InstanceMemberItemTemplateProperty = DependencyProperty.Register(
        nameof(InstanceMemberItemTemplate), typeof(DataTemplate), typeof(DataUiGrid), new PropertyMetadata(default(DataTemplate?)));

    public DataTemplate? InstanceMemberItemTemplate
    {
        get { return (DataTemplate?)GetValue(InstanceMemberItemTemplateProperty); }
        set { SetValue(InstanceMemberItemTemplateProperty, value); }
    }

    #endregion

    #region Properties

    /// <summary>The model this grid renders.</summary>
    public DataUiGridModel Model => _model;

    public ObservableCollection<Type> TypesToIgnore => _model.TypesToIgnore;
    public ObservableCollection<string> MembersToIgnore => _model.MembersToIgnore;
    public BulkObservableCollection<MemberCategory> Categories => _model.Categories;

    #endregion

    #region Events

    public event Action<string, BeforePropertyChangedArgs>? BeforePropertyChange
    {
        add => _model.BeforePropertyChange += value;
        remove => _model.BeforePropertyChange -= value;
    }

    /// <summary>
    /// Raised whenever an instance member is set by the UI, such as the user typing a value in a text box.
    /// </summary>
    public event Action<string, PropertyChangedArgs>? PropertyChange
    {
        add => _model.PropertyChange += value;
        remove => _model.PropertyChange -= value;
    }

#pragma warning disable CS0067 // Required by INotifyPropertyChanged; reserved for derived classes.
    public event PropertyChangedEventHandler? PropertyChanged;
#pragma warning restore CS0067

    #endregion

    #region Constructor

    static DataUiGrid()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(DataUiGrid),
            new FrameworkPropertyMetadata(typeof(DataUiGrid)));
    }

    public DataUiGrid()
    {
        _model = new DataUiGridModel();
        ItemsSource = _model.Categories;
    }

    #endregion

    #region Methods

    /// <inheritdoc cref="DataUiGridModel.SetCategories"/>
    public void SetCategories(IList<MemberCategory> newCategories) => _model.SetCategories(newCategories);

    /// <inheritdoc cref="DataUiGridModel.ApplyMemberFilter"/>
    public void ApplyMemberFilter(Func<InstanceMember, bool>? isMatch) => _model.ApplyMemberFilter(isMatch);

    public void Apply(TypeMemberDisplayProperties properties) => _model.Apply(properties);

    public void IgnoreAllMembers() => _model.IgnoreAllMembers();

    public bool TryGetInstanceMember(string name, out InstanceMember? member, out MemberCategory? category) =>
        _model.TryGetInstanceMember(name, out member, out category);

    public InstanceMember? GetInstanceMember(string memberName) => _model.GetInstanceMember(memberName);

    public void MoveMemberToCategory(string memberName, string categoryName) =>
        _model.MoveMemberToCategory(memberName, categoryName);

    public void InsertSpacesInCamelCaseMemberNames() => _model.InsertSpacesInCamelCaseMemberNames();

    public void SetMultipleCategoryLists(List<List<MemberCategory>> listOfCategoryLists) =>
        _model.SetMultipleCategoryLists(listOfCategoryLists);

    public void Refresh()
    {
        for (int i = 0; i < Items.Count; i++)
        {
            var uiElement =
                ItemContainerGenerator.ContainerFromIndex(i);

            bool handledByRefresh = false;

            if (uiElement is ContentPresenter contentPresenter && VisualTreeHelper.GetChildrenCount(contentPresenter) > 0 && VisualTreeHelper.GetChild(contentPresenter, 0) is Expander expander)
            {
                var itemsInExpander = expander.Content as ItemsControl;

                if (itemsInExpander != null)
                {
                    for (int j = 0; j < itemsInExpander.Items.Count; j++)
                    {
                        var innerUiElement =
                            itemsInExpander.ItemContainerGenerator.ContainerFromIndex(j) as ContentPresenter;

                        if (innerUiElement != null && VisualTreeHelper.GetChildrenCount(innerUiElement) > 0 && VisualTreeHelper.GetChild(innerUiElement, 0) is SingleDataUiContainer singleDataUiContainer)
                        {
                            (singleDataUiContainer.UserControl as IDataUi)?.Refresh();
                            handledByRefresh = true;
                        }
                    }
                }
            }

            if (!handledByRefresh && Items[i] is MemberCategory memberCategory)
            {
                foreach (var instanceMember in memberCategory.Members)
                {
                    instanceMember.SimulateValueChanged();
                }
            }
        }
    }

    #endregion
}
