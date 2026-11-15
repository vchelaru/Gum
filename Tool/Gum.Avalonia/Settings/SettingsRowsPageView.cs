using Avalonia.Controls;
using AvaloniaDataUi;
using Avalonia;
using Avalonia.VisualTree;
using Avalonia.Media;
using Avalonia.Reactive;
using Avalonia.Threading;
using Gum.Avalonia.Plugins.VariableGrid;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Gum.SettingsWindow;
using System.Linq;
using System;
using WpfDataUi.DataTypes;

namespace Gum.Avalonia.Settings;

/// <summary>
/// A Settings page built from rows (see <see cref="SettingsRows"/>): a property grid over the sections of
/// an <see cref="IRowsSettingsPage"/>, rebuilt when the page asks. It gives every such page the same
/// look: section bars spaced as groups, and a label column that grows with the zoom so labels stay on one line.
/// The page is the view's DataContext, or set <see cref="Rows"/> directly.
/// </summary>
public class SettingsRowsPageView : DockPanel
{
    private const double DefaultFontSize = 12;
    private double _fontSize = DefaultFontSize;
    private IRowsSettingsPage? _rows;
    private bool _isListening;

    /// <summary>
    /// The Settings window's search text, set on the window and inherited by the page views inside it. A
    /// page view marks the rows whose label or hint contains it; nothing is hidden.
    /// </summary>
    public static readonly AttachedProperty<string?> SearchTextProperty =
        AvaloniaProperty.RegisterAttached<SettingsRowsPageView, Control, string?>("SearchText", null, inherits: true);

    /// <summary>Gets the search text in effect for <paramref name="control"/>.</summary>
    public static string? GetSearchText(Control control) => control.GetValue(SearchTextProperty);

    /// <summary>Sets the search text for <paramref name="control"/> and the page views under it.</summary>
    public static void SetSearchText(Control control, string? value) => control.SetValue(SearchTextProperty, value);

    /// <summary>Builds the view.</summary>
    public SettingsRowsPageView()
    {
        Grid = new DataUiGrid { HideLoneCategoryHeader = true, AlternatesRowBackgrounds = false };
        // The color row kind (SettingsRows.Color) uses the tool's own color editor, as the Variables tab does.
        Grid.Displayers.Register(typeof(GumDisplayers.Color), typeof(ColorDisplay));
        Children.Add(Grid);
        this.GetObservable(SearchTextProperty).Subscribe(new AnonymousObserver<string?>(_ => MarkSearchMatches()));

        DataContextChanged += (_, _) => HandleDataContextChanged();

        // The label column is sized for the default font; a larger font (zoom) widens it in proportion
        // so labels stay on one line.
        this.GetResourceObservable("ControlContentThemeFontSize").Subscribe(new AnonymousObserver<object?>(size =>
        {
            _fontSize = size is double fontSize && fontSize > 0 ? fontSize : DefaultFontSize;
            ScaleLabelColumns();
        }));
        this.GetObservable(BoundsProperty).Subscribe(new AnonymousObserver<Rect>(_ => ScaleLabelColumns()));
    }

    /// <summary>The property grid.</summary>
    public DataUiGrid Grid { get; }

    /// <summary>The page whose sections the grid shows, or null for none.</summary>
    protected IRowsSettingsPage? Rows
    {
        get => _rows;
        set
        {
            StopListening();
            _rows = value;
            if (this.IsAttachedToVisualTree())
            {
                Listen();
                Rebuild();
            }
        }
    }

    /// <summary>Called when the DataContext changes; the default shows it as the page when it is one.</summary>
    protected virtual void HandleDataContextChanged() => Rows = DataContext as IRowsSettingsPage;

    // The page lives as long as the plugin, while a view lives as long as its Settings window, so the
    // view listens only while it is on screen. A listener left behind by a closed window would keep
    // rebuilding that window's rows on every later change.
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        Listen();
        Rebuild();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        StopListening();
    }

    private void Listen()
    {
        if (_rows != null && !_isListening)
        {
            _rows.Reloaded += Rebuild;
            _isListening = true;
        }
    }

    private void StopListening()
    {
        if (_rows != null && _isListening)
        {
            _rows.Reloaded -= Rebuild;
            _isListening = false;
        }
    }

    private void Rebuild()
    {
        if (_rows != null)
        {
            Grid.SetCategories(_rows.BuildCategories());
            MarkSearchMatches();
            ScaleLabelColumns();
            Dispatcher.UIThread.Post(SpaceSectionBars);
        }
    }

    private void MarkSearchMatches() =>
        SettingsRows.MarkMatches(Grid.Categories, GetSearchText(this) ?? string.Empty);

    // The section bars sit closer together in the shared grid; on these pages each one starts a new group.
    private void SpaceSectionBars()
    {
        bool isFirst = true;
        foreach (DataUiCategoryView view in Grid.GetVisualDescendants().OfType<DataUiCategoryView>())
        {
            view.Margin = new Thickness(0, isFirst ? 4 : 14, 0, 2);
            view.Header.Padding = new Thickness(0, 2);
            foreach (TextBlock name in view.Header.GetVisualDescendants().OfType<TextBlock>())
            {
                name.FontWeight = FontWeight.SemiBold;
            }
            isFirst = false;
        }
    }

    private void ScaleLabelColumns()
    {
        double width = Math.Round(SettingsRows.LabelColumnWidth * _fontSize / DefaultFontSize);
        // At a large zoom the label column must not take the page: keep room for the field.
        if (Bounds.Width > 0)
        {
            width = Math.Min(width, Math.Max(SettingsRows.LabelColumnWidth, Math.Round(Bounds.Width * 0.38)));
        }
        if (Grid.Categories.SelectMany(category => category.Members).FirstOrDefault()?.FirstGridLength == width)
        {
            return;
        }
        foreach (MemberCategory category in Grid.Categories)
        {
            foreach (InstanceMember member in category.Members)
            {
                member.FirstGridLength = width;
            }
        }
    }
}
