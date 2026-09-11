using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using AvaloniaDataUi.Controls;
using WpfDataUi.DataTypes;

namespace AvaloniaDataUi;

/// <summary>
/// One category of a <see cref="DataUiGrid"/>, laid out like the WPF tool's category expander: a
/// header strip with a chevron and the category name, painted with the category's
/// <see cref="MemberCategory.HeaderColor"/> (or the grid's <see cref="DataUiGrid.CategoryHeaderBackground"/>),
/// over its rows. Clicking the header expands or collapses the rows.
/// </summary>
public sealed class DataUiCategoryView : StackPanel
{
    /// <summary>The class on the rows' items control, which the grid's alternating-row styles target.</summary>
    public const string RowsClass = "dataUiRows";

    private static readonly Geometry ChevronGeometry = Geometry.Parse("M 0,0 L 4,4 L 0,8");

    // A null grid brush leaves the header text on its inherited brush rather than hiding it.
    private static readonly IValueConverter NullToUnset =
        new FuncValueConverter<IBrush?, object?>(brush => brush ?? AvaloniaProperty.UnsetValue);

    // A transparent rather than null header background, so the whole strip takes clicks.
    private static readonly IValueConverter NullToTransparent =
        new FuncValueConverter<IBrush?, IBrush>(brush => brush ?? Brushes.Transparent);

    private static readonly IValueConverter ExpandedToAngle =
        new FuncValueConverter<bool, double>(expanded => expanded ? 90 : 0);

    /// <summary>Builds the view of <paramref name="category"/>, whose rows <paramref name="grid"/> hosts.</summary>
    public DataUiCategoryView(DataUiGrid grid, MemberCategory category)
    {
        Margin = new Thickness(0, 0, 0, 2);
        this.Bind(IsVisibleProperty, new Binding(nameof(MemberCategory.IsVisible)));

        TextBlock name = new TextBlock
        {
            FontWeight = FontWeight.Medium,
            Margin = new Thickness(0, 4),
            VerticalAlignment = VerticalAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis,
        };
        name.Bind(TextBlock.TextProperty, new Binding(nameof(MemberCategory.Name)));
        name.Bind(TextBlock.ForegroundProperty, new Binding(nameof(DataUiGrid.CategoryHeaderForeground)) { Source = grid, Converter = NullToUnset });

        RotateTransform rotation = new RotateTransform();
        rotation.Bind(RotateTransform.AngleProperty, new Binding(nameof(MemberCategory.IsExpanded)) { Source = category, Converter = ExpandedToAngle });
        Path chevron = new Path
        {
            Data = ChevronGeometry,
            StrokeThickness = 1.5,
            Width = 9,
            Height = 9,
            Stretch = Stretch.Uniform,
            Margin = new Thickness(6, 0, 8, 0),
            VerticalAlignment = VerticalAlignment.Center,
            RenderTransform = rotation,
        };
        chevron.Bind(Shape.StrokeProperty, name.GetObservable(TextBlock.ForegroundProperty));

        Grid headerRow = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*") };
        headerRow.Children.Add(chevron);
        Grid.SetColumn(name, 1);
        headerRow.Children.Add(name);

        Header = new Border { Child = headerRow, Padding = new Thickness(0, 3) };
        if (category.HeaderColor is System.Drawing.Color headerColor)
        {
            Header.Background = new SolidColorBrush(Color.FromArgb(headerColor.A, headerColor.R, headerColor.G, headerColor.B));
        }
        else
        {
            Header.Bind(Border.BackgroundProperty, new Binding(nameof(DataUiGrid.CategoryHeaderBackground)) { Source = grid, Converter = NullToTransparent });
        }
        // Every click toggles, as the WPF header's toggle button does; Tapped skips the second click
        // of a double click.
        Header.PointerReleased += (_, e) =>
        {
            if (e.InitialPressMouseButton == MouseButton.Left &&
                new Rect(Header.Bounds.Size).Contains(e.GetPosition(Header)))
            {
                category.IsExpanded = !category.IsExpanded;
            }
        };
        DataUiContextMenus.AttachCategoryMenu(Header, category);
        Children.Add(Header);

        Rows = new ItemsControl
        {
            ItemTemplate = new FuncDataTemplate<InstanceMember>((_, _) =>
            {
                SingleDataUiContainer editor = new SingleDataUiContainer(grid);
                return grid.RowDecorator?.Invoke(editor) ?? editor;
            }),
        };
        Rows.Classes.Add(RowsClass);
        Rows.Bind(ItemsControl.ItemsSourceProperty, new Binding(nameof(MemberCategory.Members)));
        Rows.Bind(IsVisibleProperty, new Binding(nameof(MemberCategory.IsExpanded)));
        Children.Add(Rows);
    }

    /// <summary>The header strip: chevron and name. Clicking it toggles the rows.</summary>
    public Border Header { get; }

    /// <summary>The category's rows.</summary>
    public ItemsControl Rows { get; }
}
