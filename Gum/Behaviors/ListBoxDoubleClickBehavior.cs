using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Gum.Behaviors;
public static class ListBoxDoubleClick
{
    // ============ ICommand version ============
    public static readonly DependencyProperty CommandProperty =
        DependencyProperty.RegisterAttached(
            "Command", typeof(ICommand), typeof(ListBoxDoubleClick),
            new PropertyMetadata(null, OnCommandChanged));

    public static void SetCommand(DependencyObject d, ICommand value) => d.SetValue(CommandProperty, value);
    public static ICommand GetCommand(DependencyObject d) => (ICommand)d.GetValue(CommandProperty);

    // Optional: if set, this is used; otherwise we pass the clicked item
    public static readonly DependencyProperty CommandParameterProperty =
        DependencyProperty.RegisterAttached(
            "CommandParameter", typeof(object), typeof(ListBoxDoubleClick),
            new PropertyMetadata(null));

    public static void SetCommandParameter(DependencyObject d, object value) => d.SetValue(CommandParameterProperty, value);
    public static object GetCommandParameter(DependencyObject d) => d.GetValue(CommandParameterProperty);

    // Optional: pass SelectedItems (as a copy) instead of the clicked item
    public static readonly DependencyProperty UseSelectedItemsProperty =
        DependencyProperty.RegisterAttached(
            "UseSelectedItems", typeof(bool), typeof(ListBoxDoubleClick),
            new PropertyMetadata(false));

    public static void SetUseSelectedItems(DependencyObject d, bool value) => d.SetValue(UseSelectedItemsProperty, value);
    public static bool GetUseSelectedItems(DependencyObject d) => (bool)d.GetValue(UseSelectedItemsProperty);

    // Optional: mark the routed event handled to stop bubbling
    public static readonly DependencyProperty HandleEventProperty =
        DependencyProperty.RegisterAttached(
            "HandleEvent", typeof(bool), typeof(ListBoxDoubleClick),
            new PropertyMetadata(true));

    public static void SetHandleEvent(DependencyObject d, bool value) => d.SetValue(HandleEventProperty, value);
    public static bool GetHandleEvent(DependencyObject d) => (bool)d.GetValue(HandleEventProperty);

    private static void OnCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ListBox lb)
        {
            if (e.OldValue == null && e.NewValue != null)
                lb.MouseDoubleClick += OnMouseDoubleClick;
            else if (e.OldValue != null && e.NewValue == null)
                lb.MouseDoubleClick -= OnMouseDoubleClick;
        }
    }

    private static void OnMouseDoubleClick(object? sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Left) return;

        var lb = (ListBox)sender;

        // Map original source -> ListBoxItem
        var lbi = ItemsControl.ContainerFromElement(lb, (DependencyObject)e.OriginalSource) as ListBoxItem;
        if (lbi is null) return;

        // Prefer ICommand if present
        var cmd = GetCommand(lb);
        if (cmd is not null)
        {
            object param = GetCommandParameter(lb)
                           ?? (GetUseSelectedItems(lb)
                                   ? lb.SelectedItems.Cast<object>().ToList()
                                   : lbi.DataContext);

            if (cmd.CanExecute(param))
            {
                cmd.Execute(param);
                if (GetHandleEvent(lb)) e.Handled = true;
            }
        }

        // Also raise the attached routed event (for code-behind handlers)
        var args = new ItemDoubleClickEventArgs(ItemDoubleClickEvent, lb, lbi.DataContext);
        lb.RaiseEvent(args);
        if (args.Handled) e.Handled = true;
    }

    // ============ Attached routed event version ============
    public static readonly RoutedEvent ItemDoubleClickEvent =
        EventManager.RegisterRoutedEvent(
            "ItemDoubleClick",
            RoutingStrategy.Bubble,
            typeof(RoutedEventHandler),
            typeof(ListBoxDoubleClick));

    // XAML: local:ListBoxDoubleClick.ItemDoubleClick="OnItemDoubleClick"
    public static void AddItemDoubleClickHandler(DependencyObject d, RoutedEventHandler handler)
    {
        if (d is UIElement el)
        {
            el.AddHandler(ItemDoubleClickEvent, handler);
            // Ensure the ListBox hooks the mouse event
            if (d is ListBox lb && GetCommand(lb) is null)
                lb.MouseDoubleClick += OnMouseDoubleClick;
        }
    }

    public static void RemoveItemDoubleClickHandler(DependencyObject d, RoutedEventHandler handler)
    {
        if (d is UIElement el)
            el.RemoveHandler(ItemDoubleClickEvent, handler);
    }

    // Custom args to expose the clicked item
    public sealed class ItemDoubleClickEventArgs : RoutedEventArgs
    {
        public object? ClickedItem { get; }
        public ItemDoubleClickEventArgs(RoutedEvent routedEvent, object source, object? clickedItem)
            : base(routedEvent, source) => ClickedItem = clickedItem;
    }
}

