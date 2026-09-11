using System;
using System.Globalization;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Gum.Avalonia.Controls;
using GumFormsPlugin.ViewModels;

namespace Gum.Avalonia.Plugins.PluginDialogs;

/// <summary>The Add Forms dialog: the theme picker and whether to include the demo screen. Twin of the WPF <c>AddFormsWindow</c>.</summary>
public sealed class AddFormsView : StackPanel
{
    /// <summary>Builds the view.</summary>
    public AddFormsView()
    {
        MaxWidth = 420;

        ThemeSelectionView themeSelection = new ThemeSelectionView { Margin = new Thickness(0, 0, 0, 8) };
        themeSelection.Bind(DataContextProperty, new Binding(nameof(AddFormsViewModel.ThemeSelection)));
        Children.Add(themeSelection);

        CheckBox includeDemo = new CheckBox { Content = "Include DemoScreenGum" };
        includeDemo.Bind(ToggleButton.IsCheckedProperty, new Binding(nameof(AddFormsViewModel.IsIncludeDemoScreenGum)) { Mode = BindingMode.TwoWay });
        Children.Add(includeDemo);
    }
}

