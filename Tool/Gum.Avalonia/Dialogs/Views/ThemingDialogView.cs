using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Layout;
using Gum.Avalonia.Converters;
using Gum.Dialogs;
using Gum.Avalonia.Themes;

namespace Gum.Avalonia.Dialogs.Views;

/// <summary>
/// Theme mode plus the accent and editor colors, each with a reset-to-default button shown only when
/// the color is set explicitly. Twin of the WPF <c>ThemingDialogView</c>; Avalonia's own
/// <see cref="ColorPicker"/> replaces PixiEditor's.
/// </summary>
public sealed class ThemingDialogView : Grid
{
    /// <summary>Builds the view.</summary>
    public ThemingDialogView()
    {
        MinWidth = 360;
        DialogWindow.SetDialogTitle(this, "Theming");
        ColumnDefinitions = new ColumnDefinitions("Auto,*,40");
        RowDefinitions = new RowDefinitions("Auto,Auto,Auto,Auto,Auto,Auto,Auto,Auto");

        AddLabel("Theme Mode", 0);
        ComboBox mode = new ComboBox { HorizontalAlignment = HorizontalAlignment.Stretch, Margin = new Thickness(0, 2) };
        mode.Bind(ItemsControl.ItemsSourceProperty, new Binding(nameof(ThemingDialogViewModel.ThemeModes)));
        mode.Bind(SelectingItemsControl.SelectedItemProperty, new Binding(nameof(ThemingDialogViewModel.Mode)) { Mode = BindingMode.TwoWay });
        SetRow(mode, 0);
        SetColumn(mode, 1);
        SetColumnSpan(mode, 2);
        Children.Add(mode);

        AddColorRow("Accent Color", 1, nameof(ThemingDialogViewModel.AccentColor), nameof(ThemingDialogViewModel.HasExplicitAccentColor));
        AddColorRow("Editor Background", 2, nameof(ThemingDialogViewModel.CheckerAColor), nameof(ThemingDialogViewModel.HasExplicitCheckerAColor));
        AddColorRow("Checker Color", 3, nameof(ThemingDialogViewModel.CheckerBColor), nameof(ThemingDialogViewModel.HasExplicitCheckerBColor));
        AddColorRow("Outline Color", 4, nameof(ThemingDialogViewModel.OutlineColor), nameof(ThemingDialogViewModel.HasExplicitOutlineColor));
        AddColorRow("Guide Line Color", 5, nameof(ThemingDialogViewModel.GuideLineColor), nameof(ThemingDialogViewModel.HasExplicitGuideLineColor));
        AddColorRow("Guide Text Color", 6, nameof(ThemingDialogViewModel.GuideTextColor), nameof(ThemingDialogViewModel.HasExplicitGuideTextColor));

        Button resetAll = new Button { Content = "Reset All", Margin = new Thickness(0, 16, 0, 0), CommandParameter = null };
        resetAll.Bind(Button.CommandProperty, new Binding(nameof(ThemingDialogViewModel.ResetCommand)));
        SetRow(resetAll, 7);
        Children.Add(resetAll);
    }

    private void AddLabel(string text, int row)
    {
        TextBlock label = new TextBlock { Text = text, MinWidth = 64, Margin = new Thickness(0, 0, 12, 0), VerticalAlignment = VerticalAlignment.Center };
        SetRow(label, row);
        Children.Add(label);
    }

    private void AddColorRow(string label, int row, string colorProperty, string hasExplicitProperty)
    {
        AddLabel(label, row);

        ColorPicker picker = new ColorPicker { HorizontalAlignment = HorizontalAlignment.Stretch, Margin = new Thickness(0, 2) };
        picker.Bind(ColorView.ColorProperty, new Binding(colorProperty) { Mode = BindingMode.TwoWay, Converter = DrawingColorToAvaloniaColorConverter.Instance });
        SetRow(picker, row);
        SetColumn(picker, 1);
        Children.Add(picker);

        Button reset = new Button { Classes = { GumChromeStyles.FlatButtonClass }, Content = "↺", Margin = new Thickness(8, 0, 0, 0), CommandParameter = colorProperty, VerticalAlignment = VerticalAlignment.Center };
        ToolTip.SetTip(reset, "Reset to Default");
        reset.Bind(Button.CommandProperty, new Binding(nameof(ThemingDialogViewModel.ResetCommand)));
        reset.Bind(IsVisibleProperty, new Binding(hasExplicitProperty));
        SetRow(reset, row);
        SetColumn(reset, 2);
        Children.Add(reset);
    }
}
