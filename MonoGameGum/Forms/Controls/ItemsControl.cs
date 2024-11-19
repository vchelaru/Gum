using Gum.Wireframe;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace MonoGameGum.Forms.Controls;

public class ItemsControl : ScrollViewer
{
    #region Fields/Properties

    protected Type ItemGumType { get; set; }

    Type itemFormsType = typeof(ListBoxItem);


    // There can be a logical conflict when dealing with list items.
    // When creating a Gum list item, the Gum object may specify a Forms
    // type. But the list can also specify a forms type. So which do we use?
    // We'll use the list item forms type unless the list box has its value set
    // explicitly. then we'll go to the list box type. This eventually should get
    // marked as obsolete and we should instead go to a VM solution.
    bool isItemTypeSetExplicitly = false;
    protected Type ItemFormsType
    {
        get => itemFormsType;
        set
        {
            if (value != itemFormsType)
            {
                isItemTypeSetExplicitly = true;
                itemFormsType = value;
            }
        }
    }

    IList items;
    public IList Items
    {
        get => items;
        set
        {
            if (items != value)
            {
                if (items != null)
                {
                    ClearVisualsInternal();
                }

                if (items is INotifyCollectionChanged notifyCollectionChanged)
                {
                    notifyCollectionChanged.CollectionChanged -= HandleCollectionChanged;
                }
                items = value;
                if (items is INotifyCollectionChanged newNotifyCollectionChanged)
                {
                    newNotifyCollectionChanged.CollectionChanged += HandleCollectionChanged;
                }

                if (items?.Count > 0)
                {
                    // refresh!
                    var args = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add,
                        items, startingIndex: 0);
                    HandleCollectionChanged(this, args);
                }
            }
        }
    }

    protected List<ListBoxItem> ListBoxItemsInternal = new List<ListBoxItem>();

    ReadOnlyCollection<ListBoxItem> listBoxItemsReadOnly;
    public ReadOnlyCollection<ListBoxItem> ListBoxItems
    {
        get
        {
            if (listBoxItemsReadOnly == null)
            {
                listBoxItemsReadOnly = new ReadOnlyCollection<ListBoxItem>(ListBoxItemsInternal);
            }
            return listBoxItemsReadOnly;
        }
    }

    public FrameworkElementTemplate FrameworkElementTemplate { get; set; }

    VisualTemplate visualTemplate;
    public VisualTemplate VisualTemplate
    {
        get => visualTemplate;
        set
        {
            if (value != visualTemplate)
            {
                visualTemplate = value;

                if (items != null)
                {
                    ClearVisualsInternal();

                    if (items.Count > 0)
                    {
                        // refresh!
                        var args = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, items, startingIndex: 0);
                        HandleCollectionChanged(this, args);
                    }
                }
            }
        }
    }

    public event EventHandler<NotifyCollectionChangedEventArgs> ItemsCollectionChanged;

    #endregion

    #region Events

    public event EventHandler ItemClicked;

    #endregion

    public ItemsControl() : base()
    {
        Items = new ObservableCollection<object>();
    }

    public ItemsControl(InteractiveGue visual) : base(visual)
    {
        Items = new ObservableCollection<object>();
    }

    private ListBoxItem CreateNewListItemVisual(object o)
    {
        ListBoxItem item;
        if (o is ListBoxItem)
        {
            // the user provided a list box item, so just use that directly instead of creating a new one
            item = o as ListBoxItem;
        }
        else
        {
            var visual = CreateNewVisual(o);

            item = CreateNewListBoxItem(visual);

            item.UpdateToObject(o);

            item.BindingContext = o;

        }
        // If the iuser added a ListBoxItem as a parameter,
        // let's hope the item doesn't already have this event - if the user recycles them that could be a problem...
        item.Selected += HandleItemSelected;
        item.GotFocus += HandleItemFocused;
        item.Clicked += HandleListBoxItemClicked;

        return item;
    }

    private ListBoxItem CreateNewListBoxItem(InteractiveGue visual)
    {
        if (FrameworkElementTemplate != null)
        {
            var item = FrameworkElementTemplate.CreateContent();
            if (item != null && item is ListBoxItem == false)
            {
                throw new InvalidOperationException($"Could not create an item of type {item.GetType()} because it must inherit from ListBoxItem.");
            }
            return item as ListBoxItem;
        }
        else
        {
#if DEBUG
            if (ItemFormsType == null)
            {
                throw new Exception($"This {GetType().Name} named {this.Name} does not have a ItemFormsType specified. " +
                    "This property must be set before adding any items");
            }
#endif
            var listBoxFormsConstructor = ItemFormsType.GetConstructor(new Type[] { typeof(InteractiveGue) });

            if (listBoxFormsConstructor == null)
            {
                string message =
                    $"Could not find a constructor for {ItemFormsType} which takes a single InteractiveGue argument. " +
                    $"If you defined {ItemFormsType} without specifying a constructor, you need to add a constructor which takes a GraphicalUiElement and calls the base constructor.";
                throw new Exception(message);
            }

            ListBoxItem item;
            if (visual.FormsControlAsObject is ListBoxItem asListBoxItem && !isItemTypeSetExplicitly)
            {
                item = asListBoxItem;
            }
            else
            {
                item = listBoxFormsConstructor.Invoke(new object[] { visual }) as ListBoxItem;
            }

            return item;
        }
    }

    private InteractiveGue CreateNewVisual(object vm)
    {
        if (VisualTemplate != null)
        {
            return VisualTemplate.CreateContent(vm) as InteractiveGue;
        }
        else
        {
            var listBoxItemGumType = ItemGumType;

            if (listBoxItemGumType == null && DefaultFormsComponents.ContainsKey(typeof(ListBoxItem)))
            {
                listBoxItemGumType = DefaultFormsComponents[typeof(ListBoxItem)];
            }
#if DEBUG
            if (listBoxItemGumType == null)
            {
                throw new Exception($"This {GetType().Name} named {this.Name} does not have a ItemGumType specified, nor does the DefaultFormsComponents have an entry for ListBoxItem. " +
                    "This property must be set before adding any items");
            }
#endif
            // vic says - this uses reflection, could be made faster, somehow...

            var gumConstructor = listBoxItemGumType.GetConstructor(new[] { typeof(bool), typeof(bool) });
            var visual = gumConstructor.Invoke(new object[] { true, true }) as InteractiveGue;
            return visual;
        }
    }

    #region Event Handler methods

    protected virtual void HandleCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                {
                    int index = e.NewStartingIndex;
                    foreach (var item in e.NewItems)
                    {
                        var newItem = CreateNewListItemVisual(item);

                        InnerPanel.Children.Insert(index, newItem.Visual);

                        newItem.Visual.Parent = base.InnerPanel;

                        ListBoxItemsInternal.Insert(index, newItem);
                        index++;
                    }
                }

                break;
            case NotifyCollectionChangedAction.Remove:
                {
                    var index = e.OldStartingIndex;

                    var listItem = InnerPanel.Children[index];
                    ListBoxItemsInternal.RemoveAt(index);
                    listItem.Parent = null;
                }
                break;
            case NotifyCollectionChangedAction.Reset:
                ClearVisualsInternal();
                break;
            case NotifyCollectionChangedAction.Replace:
                {
                    var index = e.NewStartingIndex;
                    var listItem = InnerPanel.Children[index];

                    ListBoxItemsInternal[e.NewStartingIndex].UpdateToObject(Items[index]);
                }

                break;
        }

        ItemsCollectionChanged?.Invoke(sender, e);
    }

    private void ClearVisualsInternal()
    {
        for (int i = InnerPanel.Children.Count - 1; i > -1; i--)
        {
            InnerPanel.Children[i].Parent = null;
        }
        ListBoxItemsInternal.Clear();
    }

    private void HandleItemSelected(object sender, EventArgs e)
    {
        OnItemSelected(sender, new SelectionChangedEventArgs());

    }

    private void HandleItemFocused(object sender, EventArgs e)
    {
        OnitemFocused(sender, EventArgs.Empty);
    }


    private void HandleListBoxItemClicked(object sender, EventArgs e)
    {
        OnItemClicked(sender, EventArgs.Empty);
    }

    protected virtual void OnItemSelected(object sender, SelectionChangedEventArgs args)
    {
        for (int i = 0; i < ListBoxItemsInternal.Count; i++)
        {
            var listBoxItem = ListBoxItemsInternal[i];
            if (listBoxItem != sender && listBoxItem.IsSelected)
            {
                var deselectedItem = listBoxItem.BindingContext ?? listBoxItem;
                args.RemovedItems.Add(deselectedItem);
                listBoxItem.IsSelected = false;
            }
        }
    }

    protected virtual void OnitemFocused(object sender, EventArgs args)
    {
        for (int i = 0; i < ListBoxItemsInternal.Count; i++)
        {
            var listBoxItem = ListBoxItemsInternal[i];
            if (listBoxItem != sender && listBoxItem.IsFocused)
            {
                listBoxItem.IsFocused = false;
            }
        }
    }

    protected void OnItemClicked(object sender, EventArgs args)
    {
        ItemClicked?.Invoke(sender, args);
    }

    #endregion

    #region Update To

#if FRB
    protected override void HandleVisualBindingContextChanged(object sender, BindingContextChangedEventArgs args)
    {
        if(args.OldBindingContext != null && BindingContext == null)
        {
            // user removed the binding context, usually this happens when the object is removed
            if(vmPropsToUiProps.ContainsValue(nameof(Items)))
            {
                // null out the items!
                this.Items = null;
            }
        }
        base.HandleVisualBindingContextChanged(sender, args);
    }
#endif

    #endregion

}
