using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media;

namespace WpfDataUi.DataTypes;

public class MemberCategory : INotifyPropertyChanged
{
    #region Properties

    public string Name { get; set; }

    public System.Windows.Media.Brush? HeaderColor { get; set; } = null;

    public Visibility Visibility
    {
        get
        {
            if (Members.Count == 0)
            {
                return System.Windows.Visibility.Collapsed;
            }
            else
            {
                return System.Windows.Visibility.Visible;

            }
        }
    }

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
        NotifyPropertyChanged("Visibility");

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
