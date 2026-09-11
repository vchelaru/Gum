using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Layout;
using Avalonia.Media;
using Gum.Services.Dialogs;

namespace Gum.Avalonia.Dialogs.Views;

/// <summary>
/// The delete confirmation: the message, then every check box and pick-one group plugins added. The
/// window title comes from <see cref="DeleteOptionsDialogViewModel.Title"/>. Twin of the WPF
/// <c>DeleteOptionsWindow</c>.
/// </summary>
public sealed class DeleteOptionsDialogView : StackPanel
{
    /// <summary>Builds the view.</summary>
    public DeleteOptionsDialogView()
    {
        // Sized by the message, as the WPF window is.
        Spacing = 8;
        MaxWidth = 600;

        TextBlock message = new TextBlock { TextWrapping = TextWrapping.Wrap };
        message.Bind(TextBlock.TextProperty, new Binding(nameof(DeleteOptionsDialogViewModel.Message)));
        Children.Add(message);

        ItemsControl checkBoxes = new ItemsControl
        {
            ItemTemplate = new FuncDataTemplate<DeleteOptionCheckboxViewModel>((_, _) => CreateOption(groupName: null)),
        };
        checkBoxes.Bind(ItemsControl.ItemsSourceProperty, new Binding(nameof(DeleteOptionsDialogViewModel.CheckBoxes)));
        // Hidden when empty, so the stack's spacing is not spent on it (the WPF window gives an
        // empty option list no room).
        checkBoxes.Bind(IsVisibleProperty, new Binding($"{nameof(DeleteOptionsDialogViewModel.CheckBoxes)}.Count") { Converter = HasItems });
        Children.Add(checkBoxes);

        ItemsControl choices = new ItemsControl
        {
            ItemTemplate = new FuncDataTemplate<DeleteOptionChoiceViewModel>((_, _) => CreateChoiceGroup()),
        };
        choices.Bind(ItemsControl.ItemsSourceProperty, new Binding(nameof(DeleteOptionsDialogViewModel.Choices)));
        choices.Bind(IsVisibleProperty, new Binding($"{nameof(DeleteOptionsDialogViewModel.Choices)}.Count") { Converter = HasItems });
        Children.Add(choices);
    }

    private static readonly IValueConverter HasItems = new FuncValueConverter<int, bool>(count => count > 0);

    private static Control CreateChoiceGroup()
    {
        // One radio group per choice, so two groups never uncheck each other.
        string groupName = "DeleteChoice" + Guid.NewGuid().ToString("N");
        StackPanel group = new StackPanel { Spacing = 2, Margin = new Thickness(0, 4, 0, 0) };

        TextBlock header = new TextBlock { FontWeight = FontWeight.SemiBold };
        header.Bind(TextBlock.TextProperty, new Binding(nameof(DeleteOptionChoiceViewModel.Header)));
        group.Children.Add(header);

        ItemsControl options = new ItemsControl
        {
            Margin = new Thickness(8, 0, 0, 0),
            ItemTemplate = new FuncDataTemplate<DeleteOptionCheckboxViewModel>((_, _) => CreateOption(groupName)),
        };
        options.Bind(ItemsControl.ItemsSourceProperty, new Binding(nameof(DeleteOptionChoiceViewModel.Options)));
        group.Children.Add(options);
        return group;
    }

    private static Control CreateOption(string? groupName)
    {
        ToggleButton option = groupName == null ? new CheckBox() : new RadioButton { GroupName = groupName };
        option.HorizontalAlignment = HorizontalAlignment.Left;
        TextBlock label = new TextBlock { TextWrapping = TextWrapping.Wrap };
        label.Bind(TextBlock.TextProperty, new Binding(nameof(DeleteOptionCheckboxViewModel.Label)));
        option.Content = label;
        option.Bind(ToggleButton.IsCheckedProperty, new Binding(nameof(DeleteOptionCheckboxViewModel.IsChecked)) { Mode = BindingMode.TwoWay });
        return option;
    }
}
