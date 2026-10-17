using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Avalonia.Styling;
using WpfDataUi;
using WpfDataUi.DataTypes;

namespace AvaloniaDataUi.Controls;

/// <summary>
/// Shared plumbing for the Avalonia displayers: tracks the <see cref="InstanceMember"/>, refreshes
/// when its value or detail text changes, keeps the row's content disabled for read-only members, and
/// builds the right-click menu from <see cref="IDataUiExtensionMethods.GetContextMenuEntries"/>.
/// </summary>
public abstract class DataUiDisplayBase : UserControl, IDataUi
{
    private InstanceMember? _instanceMember;

    protected DataUiDisplayBase()
    {
        // The menu belongs to the whole row, as in the WPF grid. Transparent rather than unset, so a
        // right click on the row's empty space reaches it too: a panel or label without a background
        // hit-tests only where something is drawn. Text boxes give up their own edit flyout, which
        // would otherwise open in the row menu's place.
        Background = Brushes.Transparent;
        DataUiContextMenus.Attach(this, this);
        Styles.Add(new Style(x => x.OfType<TextBox>()) { Setters = { new Setter(ContextFlyoutProperty, null) } });
        AddHandler(PointerMovedEvent, HandleHintPointerMoved);
        AddHandler(PointerPressedEvent, HandleHintPointerPressed);
    }

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

    /// <summary>
    /// Whether the row is one cell of a shared row (see <see cref="DataUiGrid.RowPairs"/>), so a
    /// displayer that can gives its label only the width its text needs. Set before the member is.
    /// </summary>
    public bool IsCompact { get; set; }

    /// <summary>
    /// The least width a compact row needs to show its label and a usable field; a shared row stacks
    /// its cells when it cannot give both this much. Zero when the displayer does not say.
    /// </summary>
    public virtual double CompactMinimumWidth => 0;

    /// <inheritdoc/>
    public abstract void Refresh(bool forceRefreshEvenIfFocused = false);

    /// <inheritdoc/>
    public abstract ApplyValueResult TryGetValueOnUi(out object? result);

    /// <inheritdoc/>
    public abstract ApplyValueResult TrySetValueOnUi(object? value);

    /// <summary>Called when a different member is assigned, before the refresh; clear per-member state here.</summary>
    protected virtual void OnInstanceMemberChanged()
    {
    }

    /// <summary>Refreshes on value and detail-text changes.</summary>
    protected virtual void HandleMemberPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(WpfDataUi.DataTypes.InstanceMember.Value) ||
            e.PropertyName == nameof(WpfDataUi.DataTypes.InstanceMember.DetailText) ||
            e.PropertyName == nameof(WpfDataUi.DataTypes.InstanceMember.DetailLink) ||
            e.PropertyName == nameof(WpfDataUi.DataTypes.InstanceMember.DetailAction))
        {
            Refresh();
        }
    }

    /// <summary>Disables the row's content for a read-only member.</summary>
    protected void RefreshIsEnabled()
    {
        SetIsEditable(InstanceMember?.IsReadOnly != true);
    }

    /// <summary>
    /// Enables or disables the row's content. The control itself stays enabled because it owns the
    /// right-click menu, which a disabled control never opens (#4956).
    /// </summary>
    protected void SetIsEditable(bool isEditable)
    {
        if (Content is Control content)
        {
            content.IsEnabled = isEditable;
        }
    }

    /// <summary>
    /// Creates the small wrapping text under a row that shows <see cref="InstanceMember.DetailText"/>,
    /// including its <see cref="InstanceMember.DetailLink"/> as a clickable span.
    /// </summary>
    protected static TextBlock CreateHintTextBlock()
    {
        TextBlock hint = new TextBlock
        {
            Opacity = 0.8,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(8, 0, 4, 4),
            IsVisible = false,
        };
        BindCaptionFontSize(hint);
        return hint;
    }

    private static readonly Cursor HandCursor = new Cursor(StandardCursorType.Hand);

    /// <summary>
    /// Draws the icon a <see cref="DetailTextLink.IconName"/> names (such as <c>Delete</c>), or returns null
    /// for a name it does not know, in which case the link's text is shown. The head sets this, since the
    /// icons belong to the tool and this library has no icon set of its own.
    /// </summary>
    public static Func<string, Control?>? DetailIconFactory { get; set; }

    /// <summary>
    /// Sizes an icon placed in the hint's text to that text's font size, so it follows the tool's zoom,
    /// and sits it a little low in its line: centered on the line it reads high against the lettering.
    /// </summary>
    private sealed class InlineIconHost : Decorator
    {
        private const double DropFraction = 0.25;

        static InlineIconHost()
        {
            AffectsMeasure<InlineIconHost>(TextElement.FontSizeProperty);
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            double size = TextElement.GetFontSize(this);
            // The icon is drawn at its own size inside a Viewbox, which scales it to the text.
            Child?.Measure(new Size(size, size));
            return new Size(size, size * (1 + DropFraction));
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            double size = TextElement.GetFontSize(this);
            Child?.Arrange(new Rect(0, size * DropFraction, size, size));
            return finalSize;
        }
    }

    /// <summary>Where a link sits inside a hint's displayed text; <paramref name="Icon"/> replaces its text when set.</summary>
    private sealed record HintLink(int Start, int Length, DetailTextLink Link, Control? Icon = null);

    private const string ActionSeparator = " · ";

    // An inline control takes one position in the text layout, like this placeholder character.
    private const string InlineControlPlaceholder = "￼";

    private TextBlock? _linkHint;
    private List<HintLink> _hintLinks = new List<HintLink>();

    /// <summary>
    /// The link under the pointer, if any. Clicks are handled on this control, not on the hint: a
    /// read-only row disables its content (the hint included), and a disabled control gets no pointer
    /// events, so they go to this enabled control.
    /// </summary>
    private HintLink? GetLinkUnder(PointerEventArgs e)
    {
        if (_linkHint == null || _hintLinks.Count == 0)
        {
            return null;
        }

        TextHitTestResult hit = _linkHint.TextLayout.HitTestPoint(e.GetPosition(_linkHint));
        // The character hit, not TextPosition: TextPosition counts the trailing half of a character as the next one.
        int index = hit.CharacterHit.FirstCharacterIndex;
        return hit.IsInside
            ? _hintLinks.FirstOrDefault(span => index >= span.Start && index < span.Start + span.Length)
            : null;
    }

    private void HandleHintPointerMoved(object? sender, PointerEventArgs e)
    {
        HintLink? span = GetLinkUnder(e);
        Cursor = span != null ? HandCursor : null;
        ToolTip.SetTip(this, span?.Link.ToolTip);
    }

    private void HandleHintPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed && GetLinkUnder(e) is { } span)
        {
            e.Handled = true;
            span.Link.Navigate();
        }
    }

    /// <summary>
    /// Sizes <paramref name="textBlock"/> from the tool's caption font size, which follows the
    /// user's UI font size. A fixed size would stay put when the user zooms the UI. The key is
    /// <c>FrbThemeResources.CaptionFontSizeKey</c>; with no such resource (a bare test window) the
    /// text keeps the inherited size.
    /// </summary>
    protected static void BindCaptionFontSize(TextBlock textBlock)
    {
        textBlock.Bind(TextBlock.FontSizeProperty, textBlock.GetResourceObservable("Frb.FontSize.Caption"));
    }

    /// <summary>Shows the member's detail text in <paramref name="hint"/>, hiding it when empty.</summary>
    protected void RefreshHint(TextBlock hint)
    {
        string? detailText = InstanceMember?.DetailText;
        string? displayed = detailText;
        List<HintLink> links = new List<HintLink>();

        if (detailText != null)
        {
            // The action (such as Unlink) closes the first line of the text, after the source it acts on.
            DetailTextLink? action = InstanceMember?.DetailAction;
            if (action != null)
            {
                int firstLineEnd = detailText.IndexOf('\n');
                firstLineEnd = firstLineEnd < 0 ? detailText.Length : firstLineEnd;
                Control? icon = action.IconName == null ? null : DetailIconFactory?.Invoke(action.IconName);
                string actionText = icon != null ? InlineControlPlaceholder : action.Text;
                string separator = icon != null ? " " : ActionSeparator;
                displayed = detailText.Insert(firstLineEnd, separator + actionText);
                links.Add(new HintLink(firstLineEnd + separator.Length, actionText.Length, action, icon));
            }

            DetailTextLink? source = InstanceMember?.DetailLink;
            int sourceStart = source == null ? -1 : displayed!.IndexOf(source.Text, StringComparison.Ordinal);
            if (source != null && sourceStart >= 0)
            {
                links.Add(new HintLink(sourceStart, source.Text.Length, source));
            }
        }
        links.Sort((a, b) => a.Start.CompareTo(b.Start));

        hint.Inlines?.Clear();
        _hintLinks = links;
        if (links.Count == 0)
        {
            _linkHint = null;
            Cursor = null;
            ToolTip.SetTip(this, null);
            hint.Text = displayed;
        }
        else
        {
            _linkHint = hint;
            hint.Inlines ??= new InlineCollection();
            int position = 0;
            foreach (HintLink span in links)
            {
                hint.Inlines.Add(new Run(displayed!.Substring(position, span.Start - position)));
                if (span.Icon != null)
                {
                    // The inline's visual parent chain does not inherit the hint's font size, so follow it explicitly.
                    InlineIconHost iconHost = new InlineIconHost { Child = new Viewbox { Child = span.Icon } };
                    iconHost.Bind(TextElement.FontSizeProperty, hint.GetObservable(TextBlock.FontSizeProperty));
                    hint.Inlines.Add(new InlineUIContainer(iconHost) { BaselineAlignment = BaselineAlignment.Center });
                }
                else
                {
                    Run linkRun = new Run(span.Link.Text) { TextDecorations = TextDecorations.Underline };
                    linkRun.Bind(TextElement.ForegroundProperty, hint.GetResourceObservable("Frb.Brushes.Link"));
                    hint.Inlines.Add(linkRun);
                }
                position = span.Start + span.Length;
            }
            hint.Inlines.Add(new Run(displayed!.Substring(position)));
        }
        hint.IsVisible = !string.IsNullOrEmpty(displayed);
    }

}

/// <summary>Builds the displayers' and category headers' right-click menus.</summary>
public static class DataUiContextMenus
{
    /// <summary>
    /// Gives <paramref name="target"/> a menu of <paramref name="dataUi"/>'s entries, rebuilt when it
    /// opens so it always reflects the current member.
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
                MenuItem item = new MenuItem { Header = entry.Header, IsEnabled = entry.IsEnabled };
                item.Click += (_, _) => entry.Execute();
                menu.Items.Add(item);
            }
            e.Cancel = entries.Count == 0;
        };
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
