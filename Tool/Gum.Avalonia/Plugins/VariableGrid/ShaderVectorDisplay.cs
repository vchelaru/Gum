using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using AvaloniaDataUi.Controls;
using Gum.Plugins.InternalPlugins.VariableGrid;
using WpfDataUi;
using WpfDataUi.DataTypes;

namespace Gum.Avalonia.Plugins.VariableGrid;

/// <summary>
/// The editor of a float vector shader parameter (<see cref="GumDisplayers.ShaderVector"/>): the parameter's name,
/// then its X, Y, Z and W fields on one line, each with its letter to the left as Width and Height have. Every field
/// is the ordinary editor of one component variable; the row's set marker and right-click menu belong to the whole
/// parameter. When the panel is too narrow for all fields on a line they wrap to the next one, so none is clipped.
/// </summary>
public class ShaderVectorDisplay : DataUiDisplayBase
{
    private readonly Grid _grid;
    private readonly TextBlock _label;
    private readonly FieldFlowPanel _fields;
    private readonly List<TextBoxDisplay> _cells = new List<TextBoxDisplay>();

    /// <summary>Builds the editor.</summary>
    public ShaderVectorDisplay()
    {
        _label = new TextBlock
        {
            VerticalAlignment = VerticalAlignment.Top,
            TextWrapping = global::Avalonia.Media.TextWrapping.Wrap,
            Padding = new Thickness(4, 4),
        };
        _fields = new FieldFlowPanel();
        _grid = new Grid { ColumnDefinitions = new ColumnDefinitions("100,*") };
        DataUiRowLayout.ConfigureLabelColumn(_grid.ColumnDefinitions[0], 100);
        DataUiRowLayout.ConfigureValueColumn(_grid.ColumnDefinitions[1], 0);
        Grid.SetColumn(_fields, 1);
        _grid.Children.Add(_label);
        _grid.Children.Add(_fields);
        Content = _grid;
    }

    /// <summary>The field of each component, in X, Y, Z, W order, for tests.</summary>
    internal IReadOnlyList<TextBoxDisplay> Cells => _cells;

    /// <inheritdoc/>
    protected override void OnInstanceMemberChanged()
    {
        _fields.Children.Clear();
        _cells.Clear();

        DataUiRowLayout.ConfigureLabelColumn(_grid.ColumnDefinitions[0], InstanceMember?.FirstGridLength ?? 100);
        if (InstanceMember is not CompositeInstanceMember composite)
        {
            return;
        }

        foreach (InstanceMember component in composite.ChannelMembers)
        {
            // Compact: the letter takes only its own width, and the field the rest of the cell.
            TextBoxDisplay cell = new TextBoxDisplay { IsCompact = true };
            cell.InstanceMember = component;
            // A right click on any field is the whole parameter's menu, not one component's.
            DataUiContextMenus.Attach(cell, this);
            _fields.Children.Add(cell);
            _cells.Add(cell);
        }
    }

    /// <inheritdoc/>
    protected override Size MeasureOverride(Size availableSize)
    {
        // Like every row, the label column gives way first: the fields keep one line while the label can still shrink
        // for them, and only then wrap (see FieldFlowPanel).
        double onOneLine = _cells.Sum(cell => cell.CompactMinimumWidth);
        double largest = _cells.Select(cell => cell.CompactMinimumWidth).DefaultIfEmpty(0).Max();
        _label.Measure(Size.Infinity);
        double labelNeed = Math.Min(_label.DesiredSize.Width, 100);
        double room = double.IsInfinity(availableSize.Width) ? onOneLine : availableSize.Width - labelNeed;
        _grid.ColumnDefinitions[1].MinWidth = Math.Min(onOneLine, Math.Max(largest, room));
        return base.MeasureOverride(availableSize);
    }

    /// <inheritdoc/>
    public override void Refresh(bool forceRefreshEvenIfFocused = false)
    {
        if (InstanceMember == null)
        {
            return;
        }

        _label.Text = InstanceMember.DisplayName;
        foreach (TextBoxDisplay cell in _cells)
        {
            cell.Refresh(forceRefreshEvenIfFocused);
        }
    }

    /// <inheritdoc/>
    public override ApplyValueResult TryGetValueOnUi(out object? result)
    {
        // The value lives in the component fields; the row itself has none to read or write.
        result = _cells.Select(cell => cell.TryGetValueOnUi(out object? value) == ApplyValueResult.Success ? value : null).ToArray();
        return ApplyValueResult.Success;
    }

    /// <inheritdoc/>
    public override ApplyValueResult TrySetValueOnUi(object? value) => ApplyValueResult.Success;

    /// <summary>
    /// Lays the fields out in equal columns, as many per line as the width gives each its letter and a usable field:
    /// all of them when they fit, otherwise two lines (a float4 never wraps as three and one).
    /// </summary>
    private sealed class FieldFlowPanel : Panel
    {
        private int _columns = 1;

        private double LargestMinimum() => Children.OfType<TextBoxDisplay>().Select(cell => cell.CompactMinimumWidth).DefaultIfEmpty(0).Max();

        /// <inheritdoc/>
        protected override Size MeasureOverride(Size availableSize)
        {
            int count = Children.Count;
            if (count == 0)
            {
                return default;
            }

            double minimum = LargestMinimum();
            double total = Children.OfType<TextBoxDisplay>().Sum(cell => cell.CompactMinimumWidth);
            _columns = double.IsInfinity(availableSize.Width) || minimum <= 0 || availableSize.Width >= total - 0.5
                ? count
                : Math.Clamp((int)(availableSize.Width / minimum), 1, count);
            if (count == 4 && _columns == 3)
            {
                _columns = 2;
            }

            double cellWidth = double.IsInfinity(availableSize.Width) ? double.PositiveInfinity : availableSize.Width / _columns;
            double height = 0;
            double lineHeight = 0;
            for (int i = 0; i < count; i++)
            {
                Children[i].Measure(new Size(cellWidth, double.PositiveInfinity));
                lineHeight = Math.Max(lineHeight, Children[i].DesiredSize.Height);
                if ((i + 1) % _columns == 0 || i == count - 1)
                {
                    height += lineHeight;
                    lineHeight = 0;
                }
            }

            double width = double.IsInfinity(availableSize.Width) ? Children.Sum(child => child.DesiredSize.Width) : availableSize.Width;
            return new Size(width, height);
        }

        /// <inheritdoc/>
        protected override Size ArrangeOverride(Size finalSize)
        {
            int count = Children.Count;
            double cellWidth = finalSize.Width / Math.Max(_columns, 1);
            double top = 0;
            for (int start = 0; start < count; start += _columns)
            {
                int end = Math.Min(start + _columns, count);
                double lineHeight = 0;
                for (int i = start; i < end; i++)
                {
                    lineHeight = Math.Max(lineHeight, Children[i].DesiredSize.Height);
                }
                for (int i = start; i < end; i++)
                {
                    Children[i].Arrange(new Rect((i - start) * cellWidth, top, cellWidth, lineHeight));
                }
                top += lineHeight;
            }
            return finalSize;
        }
    }
}
