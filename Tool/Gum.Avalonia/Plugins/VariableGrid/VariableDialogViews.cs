using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Media;
using Gum.Dialogs;
using Gum.Plugins.InternalPlugins.VariableGrid.ViewModels;

namespace Gum.Avalonia.Plugins.VariableGrid;

/// <summary>The Add Variable / Edit Variable dialog: the type list and the name. Twin of the WPF <c>AddVariableWindow</c>.</summary>
public sealed class AddVariableView : StackPanel
{
    /// <summary>Builds the view.</summary>
    public AddVariableView()
    {
        Width = 450;
        Spacing = 4;
        Margin = new Thickness(10);

        Children.Add(new TextBlock { Text = "Type:" });
        ListBox types = new ListBox { MinHeight = 150, MaxHeight = 300 };
        types.Bind(ItemsControl.ItemsSourceProperty, new Binding(nameof(AddVariableViewModel.AvailableTypes)));
        types.Bind(SelectingItemsControl.SelectedItemProperty, new Binding(nameof(AddVariableViewModel.SelectedItem)) { Mode = BindingMode.TwoWay });
        types.Bind(IsEnabledProperty, new Binding(nameof(AddVariableViewModel.IsTypeChangeUiEnabled)));
        Children.Add(types);

        TextBlock typeChangeMessage = new TextBlock { TextWrapping = TextWrapping.Wrap };
        typeChangeMessage.Bind(TextBlock.TextProperty, new Binding(nameof(AddVariableViewModel.TypeChangeMessage)));
        Children.Add(typeChangeMessage);

        Children.Add(new TextBlock { Text = "Name:", Margin = new Thickness(0, 5, 0, 0) });
        TextBox name = new TextBox();
        name.Bind(TextBox.TextProperty, new Binding(nameof(AddVariableViewModel.EnteredName)) { Mode = BindingMode.TwoWay });
        Children.Add(name);

        TextBlock detail = new TextBlock { TextWrapping = TextWrapping.Wrap };
        detail.Bind(TextBlock.TextProperty, new Binding(nameof(AddVariableViewModel.DetailText)));
        Children.Add(detail);

        AttachedToVisualTree += (_, _) => name.Focus();
    }
}

