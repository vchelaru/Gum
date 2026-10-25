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
        // The spacious row's divider and callout colors depend on the theme, known only once the row is on screen.
        ActualThemeVariantChanged += (_, _) =>
        {
            if (IsSpacious)
            {
                _areFieldsStyled = false;
                ApplyRowStyle();
                Refresh();
            }
        };
        AddSpaciousFieldStyles();
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
        if (e.PropertyName == nameof(WpfDataUi.DataTypes.InstanceMember.FirstGridLength))
        {
            // A page that sizes its label column to the font changes it after the row is built.
            OnInstanceMemberChanged();
            // OnInstanceMemberChanged clears per-row colors, so the spacious look is applied again.
            _areFieldsStyled = false;
            ApplyRowStyle();
        }

        if (e.PropertyName == nameof(WpfDataUi.DataTypes.InstanceMember.Value) ||
            e.PropertyName == nameof(WpfDataUi.DataTypes.InstanceMember.DetailText) ||
            e.PropertyName == nameof(WpfDataUi.DataTypes.InstanceMember.FirstGridLength) ||
            e.PropertyName == nameof(WpfDataUi.DataTypes.InstanceMember.IsSpaciousRow) ||
            e.PropertyName == nameof(WpfDataUi.DataTypes.InstanceMember.WarningText) ||
            e.PropertyName == nameof(WpfDataUi.DataTypes.InstanceMember.ChipText) ||
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

    private bool _isRowStyled;
    private bool _areFieldsStyled;
    private bool _isHintStyled;
    private StackPanel? _hintHost;
    private Border? _callout;
    private TextBlock? _calloutText;

    private const string SpaciousFieldClass = "spaciousField";
    private const string SpaciousLightClass = "spaciousLight";

    // Sunken fields with a hairline border, as in the Settings mock: dark by default, light under the class.
    private void AddSpaciousFieldStyles()
    {
        Styles.Add(CreateFieldStyle<TextBox>(light: false, background: "#171717", border: "#2E2E2E"));
        Styles.Add(CreateFieldStyle<TextBox>(light: true, background: "#FFFFFF", border: "#C8C8C8"));
        Styles.Add(CreateFieldStyle<ComboBox>(light: false, background: "#171717", border: "#2E2E2E"));
        Styles.Add(CreateFieldStyle<ComboBox>(light: true, background: "#FFFFFF", border: "#C8C8C8"));
    }

    private static Style CreateFieldStyle<T>(bool light, string background, string border) where T : TemplatedControl
    {
        Style style = new Style(x => light
            ? x.OfType<T>().Class(SpaciousFieldClass).Class(SpaciousLightClass)
            : x.OfType<T>().Class(SpaciousFieldClass).Not(y => y.Class(SpaciousLightClass)));
        style.Setters.Add(new Setter(TemplatedControl.BackgroundProperty, new SolidColorBrush(Color.Parse(background))));
        style.Setters.Add(new Setter(TemplatedControl.BorderBrushProperty, new SolidColorBrush(Color.Parse(border))));
        style.Setters.Add(new Setter(TemplatedControl.BorderThicknessProperty, new Thickness(1)));
        style.Setters.Add(new Setter(TemplatedControl.CornerRadiusProperty, new CornerRadius(3)));
        style.Setters.Add(new Setter(TemplatedControl.PaddingProperty, new Thickness(8, 4)));
        return style;
    }

    /// <summary>Whether the member opted into the spacious Settings look.</summary>
    protected bool IsSpacious => InstanceMember?.IsSpaciousRow == true;

    /// <inheritdoc/>
    protected override void OnAttachedToVisualTree(global::Avalonia.VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        ApplyRowStyle();
    }

    // The spacious Settings look, for a member that opts in: a divider and padding around the row, taller
    // fields and wider buttons with a 4px gap. Rows that do not opt in are never touched.
    private void ApplyRowStyle()
    {
        if (IsSpacious)
        {
            _isRowStyled = true;
            bool isLight = ActualThemeVariant == Avalonia.Styling.ThemeVariant.Light;
            Padding = new Thickness(10, 8, 10, 2);
            BorderThickness = new Thickness(0, 0, 0, 1);
            BorderBrush = new SolidColorBrush(Color.Parse(isLight ? "#E0E0E0" : "#303030"));
            // Applied once: validation colors set on a field later must not be reset by every refresh.
            foreach (Control control in _areFieldsStyled ? Enumerable.Empty<Control>() : global::Avalonia.LogicalTree.LogicalExtensions.GetLogicalDescendants(this).OfType<Control>())
            {
                if (control is TextBox || control is ComboBox)
                {
                    // A sunken field with a hairline border, as in the Settings mock.
                    control.MinHeight = 26;
                    // Through a style, so the grid's own default-value highlight can still override the color.
                    control.Classes.Add(SpaciousFieldClass);
                    control.Classes.Set(SpaciousLightClass, isLight);
                }
                else if (control is Button button && button.Parent is Grid)
                {
                    button.MinWidth = 28;
                    button.MinHeight = 26;
                    button.VerticalAlignment = global::Avalonia.Layout.VerticalAlignment.Center;
                    button.HorizontalContentAlignment = global::Avalonia.Layout.HorizontalAlignment.Center;
                    button.Margin = new Thickness(4, 0, 0, 0);
                }
            }
            _areFieldsStyled = true;
        }
        else if (_isRowStyled)
        {
            _isRowStyled = false;
            ClearValue(PaddingProperty);
            ClearValue(BorderThicknessProperty);
            ClearValue(BorderBrushProperty);
        }
    }

    // A spacious row's hint is bigger, quieter and lined up with the field, with room above and below; a
    // warning is a card with an amber bar on its left under the hint. Other rows keep the compact hint.
    private void ApplyHintStyle(TextBlock hint)
    {
        if (!IsSpacious)
        {
            if (_isHintStyled)
            {
                _isHintStyled = false;
                SetHintHost(hint, null, 0);
                hint.ClearValue(TextBlock.MarginProperty);
                hint.Opacity = 0.8;
                BindCaptionFontSize(hint);
            }
            return;
        }

        string? warning = InstanceMember!.WarningText;
        bool hasWarning = !string.IsNullOrEmpty(warning);
        double left = InstanceMember.FirstGridLength;
        if (!_isHintStyled)
        {
            _isHintStyled = true;
            hint.Bind(TextBlock.FontSizeProperty, hint.GetResourceObservable("ControlContentThemeFontSize"));
        }
        hint.Opacity = 0.65;
        hint.Margin = new Thickness(0, 3, 4, 7);
        SetHintHost(hint, hasWarning ? warning : null, left);
        if (!hasWarning)
        {
            hint.Margin = new Thickness(left, 3, 4, 7);
        }
    }

    private void SetHintHost(TextBlock hint, string? warning, double left)
    {
        if (warning != null)
        {
            bool isLight = ActualThemeVariant == Avalonia.Styling.ThemeVariant.Light;
            if (_hintHost == null && hint.Parent is Panel panel)
            {
                _hintHost = new StackPanel();
                _calloutText = new TextBlock { TextWrapping = TextWrapping.Wrap };
                _calloutText.Bind(TextBlock.FontSizeProperty, _calloutText.GetResourceObservable("ControlContentThemeFontSize"));
                _callout = new Border
                {
                    BorderThickness = new Thickness(3, 0, 0, 0),
                    Padding = new Thickness(10, 6),
                    Margin = new Thickness(0, 6, 4, 8),
                    Child = _calloutText,
                };
                int index = panel.Children.IndexOf(hint);
                Grid.SetRow(_hintHost, Grid.GetRow(hint));
                Grid.SetColumn(_hintHost, Grid.GetColumn(hint));
                Grid.SetRowSpan(_hintHost, Grid.GetRowSpan(hint));
                Grid.SetColumnSpan(_hintHost, Grid.GetColumnSpan(hint));
                panel.Children.RemoveAt(index);
                _hintHost.Children.Add(hint);
                _hintHost.Children.Add(_callout);
                panel.Children.Insert(index, _hintHost);
            }
            if (_hintHost != null && _callout != null && _calloutText != null)
            {
                _hintHost.Margin = new Thickness(left, 0, 0, 0);
                _calloutText.Text = warning;
                _calloutText.Foreground = new SolidColorBrush(Color.Parse(isLight ? "#5C4000" : "#E8D9B0"));
                _callout.BorderBrush = new SolidColorBrush(Color.Parse(isLight ? "#C77700" : "#E0A63B"));
                _callout.Background = new SolidColorBrush(Color.Parse(isLight ? "#F7EBD0" : "#2E2A1F"));
            }
        }
        else if (_hintHost != null && _hintHost.Parent is Panel hostPanel)
        {
            int index = hostPanel.Children.IndexOf(_hintHost);
            Grid.SetRow(hint, Grid.GetRow(_hintHost));
            Grid.SetColumn(hint, Grid.GetColumn(_hintHost));
            Grid.SetRowSpan(hint, Grid.GetRowSpan(_hintHost));
            Grid.SetColumnSpan(hint, Grid.GetColumnSpan(_hintHost));
            _hintHost.Children.Clear();
            hostPanel.Children.RemoveAt(index);
            hostPanel.Children.Insert(index, hint);
            _hintHost = null;
            _callout = null;
            _calloutText = null;
        }
    }

    /// <summary>Shows the member's detail text in <paramref name="hint"/>, hiding it when empty.</summary>
    protected void RefreshHint(TextBlock hint)
    {
        string? detailText = InstanceMember?.DetailText;
        string? displayed = detailText;
        ApplyRowStyle();
        ApplyHintStyle(hint);
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
