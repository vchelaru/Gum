using System;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace WpfDataUi.DataTypes;

/// <summary>
/// A named, collapsible group of <see cref="InstanceMember"/> rows in a data grid. Framework-neutral:
/// colors are <see cref="System.Drawing.Color"/> and visibility is a <c>bool</c>, which each head's
/// template converts to its own brush and visibility types.
/// </summary>
public class MemberCategory : INotifyPropertyChanged
{
    #region Properties

    public string Name { get; set; }

    /// <summary>Optional tint for the category header; null uses the theme's default header.</summary>
    public System.Drawing.Color? HeaderColor { get; set; } = null;

    /// <summary>
    /// Whether the category has any rows to show. A category emptied by a filter or by
    /// delegate-driven visibility hides its header through this.
    /// </summary>
    public bool IsVisible => Members.Count > 0;

    public bool HideHeader
    {
        get;
        set;
    }

    public int FontSize
    {
        get;
        set;
    }

    double categoryBorderThickness = 1;
    public double CategoryBorderThickness
    {
        get => categoryBorderThickness;
        set
        {
            if (categoryBorderThickness != value)
            {
                categoryBorderThickness = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CategoryBorderThickness)));

            }
        }
    }

    public ObservableCollection<InstanceMember> Members
    {
        get;
        private set;
    }

    /// <summary>
    /// Right-click menu entries for the category header. Empty by default; a consumer adds items to
    /// offer category-wide actions such as copying every value in the category.
    /// </summary>
    public ObservableCollection<MemberCategoryContextMenuItem> ContextMenuItems
    {
        get;
        private set;
    }

    double? width;
    public double? Width
    {
        get => width;
        set
        {
            if(width != value)
            {
                width = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Width)));
            }
        }
    }

    bool isExpanded = true;
    public bool IsExpanded
    {
        get => isExpanded;
        set
        {
            if (isExpanded != value)
            {
                isExpanded = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsExpanded)));
            }
        }
    }



    #endregion

    #region Events

    public event PropertyChangedEventHandler? PropertyChanged;

    public event Action<InstanceMember>? MemberValueChangedByUi;

    #endregion

    #region Methods

    public MemberCategory()
    {
        Name = "";

        HideHeader = false;

        Members = new ObservableCollection<InstanceMember>();

        ContextMenuItems = new ObservableCollection<MemberCategoryContextMenuItem>();

        Members.CollectionChanged += HandleMembersChanged;
    }

    public MemberCategory(string name) : this()
    {
        Name = name;
    }

    void HandleMembersChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        NotifyPropertyChanged(nameof(IsVisible));

        bool isAddOrReplace =
            e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add ||
            e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Replace;

        if (!isAddOrReplace || e.NewItems == null)
        {
            return;
        }

        foreach (InstanceMember newItem in e.NewItems)
        {
            newItem.Category = this;
        }
    }

    void NotifyPropertyChanged(string propertyName)
    {
        if(PropertyChanged != null)
        {
            PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    internal void HandleValueSetByUi(InstanceMember instanceMember)
    {
        MemberValueChangedByUi?.Invoke(instanceMember);
    }


    public override string ToString()
    {
        return Name + " (" + Members.Count + ")";
    }

    #endregion
}
