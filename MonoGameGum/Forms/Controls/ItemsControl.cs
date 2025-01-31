﻿using Gum.Wireframe;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

#if FRB
using InteractiveGue = global::Gum.Wireframe.GraphicalUiElement;
namespace FlatRedBall.Forms.Controls;
#else
namespace MonoGameGum.Forms.Controls;
#endif

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

    IList items;
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

                    GraphicalUiElement.IsAllLayoutSuspended = wasSuppressed;
                    if (!wasSuppressed)
                    {
                        Visual.ResumeLayout(recursive: true);
                    }
            }
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

    #endregion

    #region Events

    public event EventHandler ItemClicked;
    public event EventHandler ItemPushed;

    #endregion

    public ItemsControl() : base()
    {
        Items = new ObservableCollection<object>();
    }

    public ItemsControl(InteractiveGue visual) : base(visual)
    {
        Items = new ObservableCollection<object>();
    }

    protected virtual FrameworkElement CreateNewItemFrameworkElement(object o)
    {
        var label = new Label();
        label.Text = o?.ToString();
        label.BindingContext = o;
        return label;
    }

    protected virtual InteractiveGue CreateNewVisual(object vm)
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

    protected override void ReactToVisualChanged()
    {
        base.ReactToVisualChanged();

        if(InnerPanel != null)
        {
            InnerPanel.Children.CollectionChanged += HandleInnerPanelCollectionChanged;
        }
    }

    
    /// <inheritdoc/>
    protected override void ReactToVisualRemoved()
    {
        base.ReactToVisualRemoved();

        if(InnerPanel != null)
        {
            InnerPanel.Children.CollectionChanged -= HandleInnerPanelCollectionChanged;
        }
    }

    private void HandleInnerPanelCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        switch(e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                {

                    int absoluteIndex = e.NewStartingIndex;
                    foreach(var item in e.NewItems)
                    {
                        var asGue = item as InteractiveGue;

                        var newFrameworkItem = asGue?.FormsControlAsObject as FrameworkElement;

                        if(newFrameworkItem != null)
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

                    foreach(var item in e.OldItems)
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
                    if(shouldSuppressLayout)
                    {
                        GraphicalUiElement.IsAllLayoutSuspended = true;
                    }

                    foreach (var item in e.NewItems)
                    {
                        var newItem = item as FrameworkElement ?? CreateNewItemFrameworkElement(item);

                        if (InnerPanel != null)
                        {
                            InnerPanel.Children.Insert(index, newItem.Visual);
                        }


                        newItem.Visual.Parent = InnerPanel;
                        // handled by the panel being updated:
                        //HandleCollectionNewItemCreated(newItem, index);

                        index++;
                    }
                    if(shouldSuppressLayout)
                    {
                        GraphicalUiElement.IsAllLayoutSuspended = wasSuppressed;
                        if (!wasSuppressed)
                        {
                            InnerPanel?.ResumeLayout(recursive:true);
                        }
                    }
                }

                break;
            case NotifyCollectionChangedAction.Move:
                var oldIndex = e.OldStartingIndex;
                var newIndex = e.NewStartingIndex;

                object? itemToMove = default;
                // need to move the item to the new index:
                if(InnerPanel != null)
                {
                    if(oldIndex < InnerPanel.Children.Count)
                    {
                        InnerPanel.Children.Move(oldIndex, newIndex);
                    }
                }

                HandleCollectionItemMoved(e.OldStartingIndex, e.NewStartingIndex);

                break;
            case NotifyCollectionChangedAction.Remove:
                {
                    var index = e.OldStartingIndex;

                    if(InnerPanel != null)
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

    protected virtual void HandleCollectionNewItemCreated(FrameworkElement newItem, int newItemIndex) { }
    protected virtual void HandleCollectionItemRemoved(int indexToRemoveFrom) { }
    protected virtual void HandleCollectionReset() { }
    protected virtual void HandleCollectionReplace(int index) { }
    protected virtual void HandleCollectionItemMoved(int oldIndex, int newIndex) { }

    private void ClearVisualsInternal()
    {
        if(InnerPanel != null)
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
