using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace Gum.Behaviors;

public static class GridDefinitionSync
{
    public static readonly DependencyProperty EnableProperty =
        DependencyProperty.RegisterAttached(
            "Enable",
            typeof(bool),
            typeof(GridDefinitionSync),
            new PropertyMetadata(false, OnEnableChanged));

    public static void SetEnable(DependencyObject obj, bool value) => obj.SetValue(EnableProperty, value);
    public static bool GetEnable(DependencyObject obj) => (bool)obj.GetValue(EnableProperty);

    private static readonly DependencyPropertyDescriptor ColWidthDPD =
        DependencyPropertyDescriptor.FromProperty(ColumnDefinition.WidthProperty, typeof(ColumnDefinition));

    private static readonly DependencyPropertyDescriptor RowHeightDPD =
        DependencyPropertyDescriptor.FromProperty(RowDefinition.HeightProperty, typeof(RowDefinition));

    private static void OnEnableChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not Grid grid) return;

        if ((bool)e.NewValue)
        {
            // watch existing defs
            HookAll(grid);
        }
    }

    private static void HookAll(Grid grid)
    {
        HookColumns(grid);
        HookRows(grid);
    }

    private static void HookColumns(Grid grid)
    {
        foreach (var col in grid.ColumnDefinitions)
        {
            // avoid double-hook
            ColWidthDPD.RemoveValueChanged(col, OnColumnWidthChanged);
            ColWidthDPD.AddValueChanged(col, OnColumnWidthChanged);
        }
    }

    private static void HookRows(Grid grid)
    {
        foreach (var row in grid.RowDefinitions)
        {
            RowHeightDPD.RemoveValueChanged(row, OnRowHeightChanged);
            RowHeightDPD.AddValueChanged(row, OnRowHeightChanged);
        }
    }

    private static void OnColumnWidthChanged(object? sender, EventArgs e)
    {
        if (sender is ColumnDefinition col)
        {
            // If there is a TwoWay binding, push the current GridLength (including Star/Auto)
            BindingOperations.GetBindingExpression(col, ColumnDefinition.WidthProperty)?.UpdateSource();
        }
    }

    private static void OnRowHeightChanged(object? sender, EventArgs e)
    {
        if (sender is RowDefinition row)
        {
            BindingOperations.GetBindingExpression(row, RowDefinition.HeightProperty)?.UpdateSource();
        }
    }
}