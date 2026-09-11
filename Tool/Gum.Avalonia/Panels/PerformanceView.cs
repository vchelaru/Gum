using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Media;
using PerformanceMeasurementPlugin.ViewModels;

namespace Gum.Avalonia.Panels;

/// <summary>
/// The Performance tab: the sibling render order, off-screen culling, and the last frame's
/// render-state-change counts, bound to <see cref="PerformanceViewModel"/>. Twin of the WPF
/// <c>PerformanceView</c>.
/// </summary>
public sealed class PerformanceView : StackPanel
{
    /// <summary>Builds the view.</summary>
    public PerformanceView()
    {
        Margin = new Thickness(4);
        Spacing = 2;

        Children.Add(Heading("Sibling render order:"));
        Children.Add(Option(new RadioButton { GroupName = "RenderOrder", Content = "Depth-first (hierarchical)" }, nameof(PerformanceViewModel.RenderDepthFirst)));
        Children.Add(Option(new RadioButton { GroupName = "RenderOrder", Content = "Sort by batch" }, nameof(PerformanceViewModel.SortByBatchKey)));

        TextBlock culling = Heading("Off-screen culling:");
        culling.Margin = new Thickness(0, 6, 0, 0);
        Children.Add(culling);
        Children.Add(Option(new CheckBox { Content = "Cull content outside clip regions" }, nameof(PerformanceViewModel.CullOffscreenWhenClipped)));

        Children.Add(new Separator { Margin = new Thickness(0, 6) });

        Grid counts = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*"), RowDefinitions = new RowDefinitions("Auto,Auto,Auto") };
        AddCount(counts, 0, "SpriteBatch begins:", nameof(PerformanceViewModel.SpriteBatchBeginCount), bold: false);
        AddCount(counts, 1, "Apos.Shapes begins:", nameof(PerformanceViewModel.ShapeBatchBeginCount), bold: false);
        AddCount(counts, 2, "Total render-state changes:", nameof(PerformanceViewModel.TotalRenderStateChanges), bold: true);
        Children.Add(counts);
    }

    private static TextBlock Heading(string text) => new TextBlock { Text = text, FontWeight = FontWeight.Bold };

    private static ToggleButton Option(ToggleButton toggle, string property)
    {
        toggle.Margin = new Thickness(8, 2, 0, 2);
        toggle.Bind(ToggleButton.IsCheckedProperty, new Binding(property) { Mode = BindingMode.TwoWay });
        return toggle;
    }

    private static void AddCount(Grid grid, int row, string label, string property, bool bold)
    {
        FontWeight weight = bold ? FontWeight.Bold : FontWeight.Normal;
        TextBlock name = new TextBlock { Text = label, FontWeight = weight, Margin = new Thickness(0, 2, 8, 2) };
        Grid.SetRow(name, row);
        grid.Children.Add(name);

        TextBlock value = new TextBlock { FontWeight = weight, Margin = new Thickness(0, 2) };
        value.Bind(TextBlock.TextProperty, new Binding(property));
        Grid.SetRow(value, row);
        Grid.SetColumn(value, 1);
        grid.Children.Add(value);
    }
}
