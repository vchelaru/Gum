using System.Collections.Generic;
using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using WpfDataUi;
using WpfDataUi.DataTypes;

namespace AvaloniaDataUi.Controls;

/// <summary>
/// Shared plumbing for the Avalonia displayers: tracks the <see cref="InstanceMember"/>, refreshes
/// when its value or detail text changes, keeps the control disabled for read-only members, and
/// builds the right-click menu from <see cref="IDataUiExtensionMethods.GetContextMenuEntries"/>.
/// </summary>
public abstract class DataUiDisplayBase : UserControl, IDataUi
{
    private InstanceMember? _instanceMember;

    /// <inheritdoc/>
    public InstanceMember? InstanceMember
    {
        get => _instanceMember;
        set
        {
            bool changed = _instanceMember != value;
            if (_instanceMember != null && changed)
            {
                _instanceMember.PropertyChanged -= HandleMemberPropertyChanged;
            }
            _instanceMember = value;
            if (_instanceMember != null && changed)
            {
                _instanceMember.PropertyChanged += HandleMemberPropertyChanged;
            }

            if (changed)
            {
                OnInstanceMemberChanged();
            }

            if (_instanceMember != null)
            {
                Refresh();
            }
        }
    }

    /// <inheritdoc/>
    public bool SuppressSettingProperty { get; set; }

    /// <inheritdoc/>
    public abstract void Refresh(bool forceRefreshEvenIfFocused = false);

    /// <inheritdoc/>
    public abstract ApplyValueResult TryGetValueOnUi(out object? result);

    /// <inheritdoc/>
    public abstract ApplyValueResult TrySetValueOnUi(object value);

    /// <summary>Called when a different member is assigned, before the refresh; clear per-member state here.</summary>
    protected virtual void OnInstanceMemberChanged()
    {
    }

    /// <summary>Refreshes on value and detail-text changes.</summary>
    protected virtual void HandleMemberPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(WpfDataUi.DataTypes.InstanceMember.Value) ||
            e.PropertyName == nameof(WpfDataUi.DataTypes.InstanceMember.DetailText))
        {
            Refresh();
        }
    }

    /// <summary>Disables the control for a read-only member.</summary>
    protected void RefreshIsEnabled()
    {
        IsEnabled = InstanceMember?.IsReadOnly != true;
    }

    /// <summary>Creates the small wrapping text under a row that shows <see cref="InstanceMember.DetailText"/>.</summary>
    protected static TextBlock CreateHintTextBlock()
    {
        return new TextBlock
        {
            FontSize = 11,
            Opacity = 0.8,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(8, 0, 4, 4),
            IsVisible = false,
        };
    }

    /// <summary>Shows the member's detail text in <paramref name="hint"/>, hiding it when empty.</summary>
    protected void RefreshHint(TextBlock hint)
    {
        string? detailText = InstanceMember?.DetailText;
        hint.Text = detailText;
        hint.IsVisible = !string.IsNullOrEmpty(detailText);
    }

    /// <summary>Gives <paramref name="target"/> this displayer's right-click menu, rebuilt each time it opens.</summary>
    protected void AttachContextMenu(Control target)
    {
        DataUiContextMenus.Attach(target, this);
    }
}

/// <summary>Builds the displayers' and category headers' right-click menus.</summary>
public static class DataUiContextMenus
{
    /// <summary>
    /// Gives <paramref name="target"/> a menu of <paramref name="dataUi"/>'s entries, rebuilt when it
    /// opens so it always reflects the current member. Replaces a text box's own edit flyout, as the
    /// WPF grid does.
    /// </summary>
    public static void Attach(Control target, IDataUi dataUi)
    {
        ContextMenu menu = new ContextMenu();
        menu.Opening += (_, e) =>
        {
            menu.Items.Clear();
            List<DataUiContextMenuEntry> entries = dataUi.GetContextMenuEntries();
            foreach (DataUiContextMenuEntry entry in entries)
            {
                MenuItem item = new MenuItem { Header = entry.Header };
                item.Click += (_, _) => entry.Execute();
                menu.Items.Add(item);
            }
            e.Cancel = entries.Count == 0;
        };
        target.ContextFlyout = null;
        target.ContextMenu = menu;
    }

    /// <summary>
    /// Gives a category header a menu of its <see cref="MemberCategory.ContextMenuItems"/>, evaluating
    /// each item's CanExecute when the menu opens; a category with no items gets no menu.
    /// </summary>
    public static void AttachCategoryMenu(Control target, MemberCategory category)
    {
        ContextMenu menu = new ContextMenu();
        menu.Opening += (_, e) =>
        {
            menu.Items.Clear();
            foreach (MemberCategoryContextMenuItem categoryItem in category.ContextMenuItems)
            {
                MenuItem item = new MenuItem
                {
                    Header = categoryItem.Header,
                    IsEnabled = categoryItem.CanExecute(null),
                };
                item.Click += (_, _) => categoryItem.Execute(null);
                menu.Items.Add(item);
            }
            e.Cancel = category.ContextMenuItems.Count == 0;
        };
        target.ContextMenu = menu;
    }
}

/// <summary>Tints displayer fields by <see cref="DataUiValueState"/>.</summary>
public static class DataUiValueStateBrushes
{
    /// <summary>Background of a field whose value is the default.</summary>
    public static readonly IBrush DefaultValueBackground = new SolidColorBrush(Color.FromArgb(128, 180, 255, 180));

    /// <summary>Background of a field whose multi-selection disagrees.</summary>
    public static readonly IBrush IndeterminateValueBackground = new SolidColorBrush(Colors.LightGray);

    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Control, System.Action> PendingUntilInTree = new();

    /// <summary>
    /// Sets <paramref name="target"/>'s background for <paramref name="state"/>; a custom value uses the
    /// theme's, and so does every value under a grid with <see cref="DataUiGrid.OverridesIsDefaultStylingProperty"/> set.
    /// </summary>
    public static void ApplyBackground(TemplatedControl target, DataUiValueState state)
    {
        // The grid's OverridesIsDefaultStyling is inherited, so it can only be read once the field is in the tree.
        if (DeferUntilInTree(target, () => ApplyBackground(target, state)))
        {
            return;
        }
        if (DataUiGrid.GetOverridesIsDefaultStyling(target))
        {
            target.ClearValue(TemplatedControl.BackgroundProperty);
            return;
        }

        switch (state)
        {
            case DataUiValueState.Default:
                target.Background = DefaultValueBackground;
                break;
            case DataUiValueState.Indeterminate:
                target.Background = IndeterminateValueBackground;
                break;
            default:
                target.ClearValue(TemplatedControl.BackgroundProperty);
                break;
        }
    }

    /// <summary>
    /// When <paramref name="control"/> is not in the visual tree yet, runs <paramref name="apply"/> once
    /// it is (the latest one, if called again before then) and returns true; otherwise returns false.
    /// </summary>
    internal static bool DeferUntilInTree(Control control, System.Action apply)
    {
        if (Avalonia.VisualTree.VisualExtensions.GetVisualRoot(control) != null)
        {
            return false;
        }

        bool waiting = PendingUntilInTree.TryGetValue(control, out _);
        PendingUntilInTree.AddOrUpdate(control, apply);
        if (!waiting)
        {
            control.AttachedToVisualTree += HandleAttachedToVisualTree;
        }
        return true;
    }

    private static void HandleAttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
    {
        Control control = (Control)sender!;
        control.AttachedToVisualTree -= HandleAttachedToVisualTree;
        if (PendingUntilInTree.TryGetValue(control, out System.Action? apply))
        {
            PendingUntilInTree.Remove(control);
            apply();
        }
    }
}
