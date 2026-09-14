using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Layout;
using FluentIcons.Avalonia;
using Gum.Avalonia.Themes;
using WpfDataUi.DataTypes;

namespace Gum.Avalonia.Plugins.VariableGrid;

/// <summary>
/// The Variables tab's row frame, as the WPF head's <c>InstanceMember.DefaultColumnTemplate</c>: a
/// separator under each row, and a narrow column that shows the BoxEdit icon when the row's value is
/// set rather than inherited. The icon is the WPF grid's cue for a set value, in place of a
/// default-value tint.
/// </summary>
internal static class VariableGridRows
{
    private static readonly IValueConverter DefaultToOpacity = new FuncValueConverter<bool, double>(isDefault => isDefault ? 0 : 1);

    // The WPF IconInline size: 1.25 times the base font.
    private static readonly IValueConverter IconSize = GumChromeStyles.ScaleFontSize(1.25);

    /// <summary>Frames <paramref name="editor"/>, a row of the grid whose DataContext is its member.</summary>
    public static Control Frame(Control editor)
    {
        FluentIcon setMarker = GumFluentIcons.Create(FluentIcons.Common.Icon.BoxEdit, new Binding(nameof(Window.FontSize))
        {
            RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor) { AncestorType = typeof(Window) },
            Converter = IconSize,
        });
        setMarker.VerticalAlignment = VerticalAlignment.Center;
        // Hidden rather than collapsed, so the column keeps its width on default rows.
        setMarker.Bind(Visual.OpacityProperty, new Binding(nameof(InstanceMember.IsDefault)) { Converter = DefaultToOpacity });

        Border markerCell = new Border
        {
            Margin = new Thickness(0, -1),
            Padding = new Thickness(2, 0, 4, 0),
            BorderThickness = new Thickness(0, 0, 1, 0),
            Child = setMarker,
        }.WithThemeResource(Border.BorderBrushProperty, "Frb.Brushes.Contrast.Subtle");

        Border editorCell = new Border { Margin = new Thickness(0, 0, 6, 0), Child = editor };
        Grid.SetColumn(editorCell, 1);

        Grid columns = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*") };
        columns.Children.Add(markerCell);
        columns.Children.Add(editorCell);

        return new Border
        {
            // A pixel above and below the 22px field: the WPF row height.
            Padding = new Thickness(0, 1),
            BorderThickness = new Thickness(0, 0, 0, 1),
            Child = columns,
        }.WithThemeResource(Border.BorderBrushProperty, "Frb.Brushes.Contrast.Subtle");
    }
}
