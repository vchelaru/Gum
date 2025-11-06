using Gum.Wireframe;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

#if FRB
using InteractiveGue = global::Gum.Wireframe.GraphicalUiElement;
namespace FlatRedBall.Forms.Controls;
#endif

#if !FRB
namespace Gum.Forms.Controls;

#endif

public class ItemsControl : ScrollViewer
{
    #region Fields/Properties

    protected Type ItemGumType { get; set; }

    Type itemFormsType = typeof(ListBoxItem);

    public virtual string DisplayMemberPath
    {
        get;
        set;
    }

    // There can be a logical conflict when dealing with list items.
    // When creating a Gum list item, the Gum object may specify a Forms
    // type. But the list can also specify a forms type. So which do we use?
    // We'll use the list item forms type unless the list box has its value set
    // explicitly. then we'll go to the list box type. This eventually should get
    // marked as obsolete and we should instead go to a VM solution.
    protected bool isItemTypeSetExplicitly = false;
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

    IList items = new ObservableCollection<object>();
    /// <summary>
    /// The items contained by this ItemsControl. This can contain regular
    /// data such as strings, instances of ViewModels, or FrameworkElement instances
    /// such as ListBoxItem instances.
    /// 
    /// Typically the Items is assigned or bound to an ObservableCollection. In this case
    /// the ItemsControl automatically creates FrameworkElements (such as ListBoxItem instances)
    /// in response to items being added or removed from the Items list.
    /// </summary>
    /// <remarks>
    /// The Items list is not guaranteed to reflect the items contained in the ItemsControl
    /// if Visual elements have been added directly to the InnerPanel. This can happen if the
    /// Visual's InnerPanel's Children are directly added to, or if an instance of an ItemsControl,
    /// such as a ListBox, is loaded from the Gum tool pre-filled with ListBoxItems. 
    /// 
    /// If items are not added directly to the InnerPanel, then the Items list and the internal InnerPanel
    /// Children will remain in sync since the ItemsControl automatically creates FrameworkElement instances
    /// in response to the Items.
    /// </remarks>
    public IList Items
    {
        get => items;
        set
        {
            if (items != value)
            {
                var wasSuppressed = GraphicalUiElement.IsAllLayoutSuspended;
                GraphicalUiElement.IsAllLayoutSuspended = true;

                if (items != null)
                {
                    ClearVisualsInternal();
                    HandleItemsCollectionChanged(this,
                        new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
                }

                if (items is INotifyCollectionChanged notifyCollectionChanged)
                {
                    notifyCollectionChanged.CollectionChanged -= HandleItemsCollectionChanged;
                }
                items = value;
                ForceUpdateToItems();

                GraphicalUiElement.IsAllLayoutSuspended = wasSuppressed;
                if (!wasSuppressed)
                {
                    Visual.ResumeLayout(recursive: true);
                }
            }
        }
    }

    private void ForceUpdateToItems()
    {
        if (items is INotifyCollectionChanged newNotifyCollectionChanged)
        {
            newNotifyCollectionChanged.CollectionChanged += HandleItemsCollectionChanged;
        }

        if (items?.Count > 0)
        {
            // refresh!
            var args = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add,
                items, startingIndex: 0);
            HandleItemsCollectionChanged(this, args);
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
                        HandleItemsCollectionChanged(this, args);
                    }
                }
            }
        }
    }

    public event EventHandler<NotifyCollectionChangedEventArgs> ItemsCollectionChanged;

    public Orientation? Orientation
    {
        get
        {
            if(InnerPanel?.ChildrenLayout == global::Gum.Managers.ChildrenLayout.TopToBottomStack)
            {
                return Controls.Orientation.Vertical;
            }
            else if(InnerPanel?.ChildrenLayout == global::Gum.Managers.ChildrenLayout.LeftToRightStack)
            {
                return Controls.Orientation.Horizontal;
            }
            else
            {
                return null;
            }
        }
        set
        {
            if(value.HasValue && InnerPanel != null)
            {
                if(value == Controls.Orientation.Horizontal)
                {
                    InnerPanel.ChildrenLayout =
                        global::Gum.Managers.ChildrenLayout.LeftToRightStack;
                }
                else
                {
                    InnerPanel.ChildrenLayout =
                        global::Gum.Managers.ChildrenLayout.TopToBottomStack;
                }
            }
        }
    }

    #endregion

    #region Events

    public event EventHandler ItemClicked;
    public event EventHandler ItemPushed;

    #endregion

    #region Constructor/Initialize

    public ItemsControl() : base()
    {
        ForceUpdateToItems();
    }

    public ItemsControl(InteractiveGue visual) : base(visual)
    {
        ForceUpdateToItems();
    }

    protected override void ReactToVisualChanged()
    {
        base.ReactToVisualChanged();

        if (InnerPanel != null)
        {
            InnerPanel.Children.CollectionChanged += HandleInnerPanelCollectionChanged;
        }
    }

    #endregion

    #region Create New Item

    protected virtual FrameworkElement CreateNewItemFrameworkElement(object o)
    {
        if(FrameworkElementTemplate != null)
        {
            var frameworkElement = FrameworkElementTemplate.CreateContent();
            frameworkElement.BindingContext = o;
            return frameworkElement;
        }
        else
        {

            var label = new Label();
            label.Text = o?.ToString() ?? string.Empty;
            label.BindingContext = o;
            return label;
        }
    }

    protected virtual InteractiveGue CreateNewVisual(object vm)
    {
        if (VisualTemplate != null || DefaultFormsTemplates.ContainsKey(typeof(ListBoxItem)))
        {
            var template = VisualTemplate ?? DefaultFormsTemplates[typeof(ListBoxItem)];
            var toReturn = template.CreateContent(vm);

            if (toReturn != null && toReturn is not InteractiveGue)
            {
                throw new InvalidOperationException(
                    "The visual template for this ListBox returned an instance which did not inherit from InteractiveGue. This is a requirement.");
            }


            return toReturn as InteractiveGue;
        }
        else
        {
            var listBoxItemGumType = ItemGumType;

#pragma warning disable CS0618 // we need this to support old projects
            if (listBoxItemGumType == null && DefaultFormsComponents.ContainsKey(typeof(ListBoxItem)))
            {
                listBoxItemGumType = DefaultFormsComponents[typeof(ListBoxItem)];
            }
#pragma warning restore CS0618 // Type or member is obsolete
#if FULL_DIAGNOSTICS
            if (listBoxItemGumType == null)
            {
                throw new Exception($"This {GetType().Name} named {this.Name} does not have a ItemGumType specified, " +
                    $"nor does the DefaultFormsTemplates have an entry for ListBoxItem. " +
                    "This property must be set before adding any items");
            }
#endif
            // vic says - this uses reflection, could be made faster, somehow...

            var gumConstructor = listBoxItemGumType.GetConstructor(new[] { typeof(bool), typeof(bool) });
            var visual = gumConstructor.Invoke(new object[] { true, true }) as InteractiveGue;
            return visual;
        }
    }

    #endregion

    /// <inheritdoc/>
    protected override void ReactToVisualRemoved()
    {
        base.ReactToVisualRemoved();

        if (InnerPanel != null)
        {
            InnerPanel.Children.CollectionChanged -= HandleInnerPanelCollectionChanged;
        }
    }

    private void HandleInnerPanelCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                {

                    int absoluteIndex = e.NewStartingIndex;
                    foreach (var item in e.NewItems)
                    {
                        var asGue = item as InteractiveGue;

                        var newFrameworkItem = asGue?.FormsControlAsObject as FrameworkElement;

                        if (newFrameworkItem != null)
                        {
                            HandleCollectionNewItemCreated(newFrameworkItem, absoluteIndex);
                        }
                        absoluteIndex++;
                    }
                }
                break;
            case NotifyCollectionChangedAction.Move:
                // todo - we need to raise an event here when items get moved
                // https://github.com/vchelaru/Gum/issues/557
                break;
            case NotifyCollectionChangedAction.Remove:
                {
                    int absoluteIndex = e.OldStartingIndex;

                    foreach (var item in e.OldItems)
                    {
                        var asGue = item as InteractiveGue;
                        var newFrameworkItem = asGue?.FormsControlAsObject as FrameworkElement;
                        if (newFrameworkItem != null)
                        {
                            HandleCollectionItemRemoved(absoluteIndex);
                        }

                        absoluteIndex++;
                    }
                }
                break;
            case NotifyCollectionChangedAction.Replace:
                var index = e.NewStartingIndex;

                //var listItem = InnerPanel.Children[index];
                HandleCollectionReplace(index);
                break;
            case NotifyCollectionChangedAction.Reset:
                HandleCollectionReset();

                break;
        }
    }

    #region Event Handler methods

    protected virtual void HandleItemsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                {
                    int index = e.NewStartingIndex;

                    var shouldSuppressLayout = e.NewItems.Count > 0 && InnerPanel != null;
                    var wasSuppressed = GraphicalUiElement.IsAllLayoutSuspended;
                    if (shouldSuppressLayout)
                    {
                        GraphicalUiElement.IsAllLayoutSuspended = true;
                    }

                    foreach (var item in e.NewItems)
                    {
                        GraphicalUiElement? newVisual = null;
                        if (item is FrameworkElement existingItemFrameworkElement)
                        {
                            newVisual = existingItemFrameworkElement.Visual;
                        }
                        else if(item is GraphicalUiElement existingItemGue)
                        {
                            newVisual = existingItemGue;
                        }
                        else if(this.VisualTemplate != null)
                        {
                            // August 19, 2025
                            // Not sure if this
                            // should set createFormsInternally
                            // to true. ItemsControls probably want
                            // this true, but ListBoxes probably don't
                            // since they want to force a ListBoxItem. For
                            // now let's make it false, and revisit later.
                            newVisual = VisualTemplate.CreateContent(item, createFormsInternally:false);

                            // the visual template should respect the item (BindingContext), but just in case it doesn't:
                            if(newVisual is InteractiveGue interactivegue)
                            {
                                interactivegue.BindingContext = item;
                            }

                            HandleCreatedItemVisual(newVisual, item);
                        }
                        else
                        {
                            newVisual = CreateNewItemFrameworkElement(item).Visual;

                        }

                        InnerPanel?.Children.Insert(index, newVisual);

                        newVisual.Parent = InnerPanel;
                        // handled by the panel being updated:
                        //HandleCollectionNewItemCreated(newItem, index);

                        index++;
                    }
                    if (shouldSuppressLayout)
                    {
                        GraphicalUiElement.IsAllLayoutSuspended = wasSuppressed;
                        if (!wasSuppressed)
                        {
                            InnerPanel?.ResumeLayout(recursive: true);
                        }
                    }
                }

                break;
            case NotifyCollectionChangedAction.Move:
                var oldIndex = e.OldStartingIndex;
                var newIndex = e.NewStartingIndex;

                object? itemToMove = default;
                // need to move the item to the new index:
                if (InnerPanel != null)
                {
                    if (oldIndex < InnerPanel.Children.Count)
                    {
                        InnerPanel.Children.Move(oldIndex, newIndex);
                    }
                }

                HandleCollectionItemMoved(e.OldStartingIndex, e.NewStartingIndex);

                break;
            case NotifyCollectionChangedAction.Remove:
                {
                    var index = e.OldStartingIndex;

                    if (InnerPanel != null)
                    {
                        var listItem = InnerPanel.Children[index];
                        listItem.Parent = null;
                    }

                    //HandleCollectionItemRemoved(index);
                }
                break;
            case NotifyCollectionChangedAction.Replace:
                {
                    //var index = e.NewStartingIndex;

                    //var listItem = InnerPanel.Children[index];
                    //HandleCollectionReplace(index);

                }
                break;
            case NotifyCollectionChangedAction.Reset:
                ClearVisualsInternal();
                break;
        }

        ItemsCollectionChanged?.Invoke(sender, e);
    }

    protected virtual void HandleCreatedItemVisual(GraphicalUiElement newVisual, object item)
    {

    }

    protected virtual void HandleCollectionNewItemCreated(FrameworkElement newItem, int newItemIndex) { }
    protected virtual void HandleCollectionItemRemoved(int indexToRemoveFrom) { }
    protected virtual void HandleCollectionReset() { }
    protected virtual void HandleCollectionReplace(int index) { }
    protected virtual void HandleCollectionItemMoved(int oldIndex, int newIndex) { }

    private void ClearVisualsInternal()
    {
        if (InnerPanel != null)
        {
            InnerPanel.Children.Clear();

            for (int i = InnerPanel.Children.Count - 1; i > -1; i--)
            {
                InnerPanel.Children[i].Parent = null;
            }
        }
    }


    protected void OnItemClicked(object sender, EventArgs args)
    {
        ItemClicked?.Invoke(sender, args);
    }

    protected void OnItemPushed(object sender, EventArgs args)
    {
        ItemPushed?.Invoke(sender, args);
    }

    #endregion

    #region Update To

#if FRB
    protected override void OnBindingContextChanged(object sender, BindingContextChangedEventArgs args)
    {
        if (IsDataBound(nameof(Items)) && 
            args is { OldBindingContext: not null, NewBindingContext: null })
        {
            this.Items = null;
        }
    }
#endif
    #endregion
}
