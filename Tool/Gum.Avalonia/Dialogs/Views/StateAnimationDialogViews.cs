using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Media;
using Gum.Avalonia.Converters;
using StateAnimationPlugin.ViewModels;

namespace Gum.Avalonia.Dialogs.Views;

/// <summary>Name and loop option for a new animation. Twin of the WPF <c>AddAnimationDialogView</c>.</summary>
public sealed class AddAnimationDialogView : StackPanel
{
    /// <summary>Builds the view.</summary>
    public AddAnimationDialogView()
    {
        Width = 300;
        MinHeight = 120;
        DialogWindow.SetDialogTitle(this, "New Animation");

        Children.Add(new TextBlock { Text = "Animation name:", Margin = new Thickness(0, 0, 0, 4) });

        TextBox name = new TextBox { Margin = new Thickness(0, 0, 0, 8) };
        name.Bind(TextBox.TextProperty, new Binding(nameof(AddAnimationDialogViewModel.Name)) { Mode = BindingMode.TwoWay });
        Children.Add(name);

        TextBlock validation = new TextBlock { Margin = new Thickness(0, 0, 0, 8), Foreground = Brushes.OrangeRed, TextWrapping = TextWrapping.Wrap };
        validation.Bind(TextBlock.TextProperty, new Binding(nameof(AddAnimationDialogViewModel.ValidationMessage)));
        validation.Bind(IsVisibleProperty, new Binding(nameof(AddAnimationDialogViewModel.ValidationMessage)) { Converter = NotNullConverter.Instance });
        Children.Add(validation);

        CheckBox loops = new CheckBox { Content = "Loop animation" };
        loops.Bind(ToggleButton.IsCheckedProperty, new Binding(nameof(AddAnimationDialogViewModel.Loops)) { Mode = BindingMode.TwoWay });
        Children.Add(loops);

        DialogViewHelpers.FocusAndSelectAllWhenShown(this, name);
    }
}

/// <summary>Picks the state a new keyframe uses. Twin of the WPF <c>AddStateKeyFrameDialogView</c>.</summary>
public sealed class AddStateKeyframeDialogView : DockPanel
{
    /// <summary>Builds the view.</summary>
    public AddStateKeyframeDialogView()
    {
        Width = 350;
        DialogWindow.SetDialogTitle(this, "Add State Keyframe");

        TextBlock prompt = new TextBlock { Text = "Select a state", Margin = new Thickness(0, 0, 0, 8) };
        SetDock(prompt, Dock.Top);
        Children.Add(prompt);

        ListBox states = new ListBox { MaxHeight = 360 };
        states.Bind(ItemsControl.ItemsSourceProperty, new Binding(nameof(AddStateKeyframeDialog.States)));
        states.Bind(SelectingItemsControl.SelectedItemProperty, new Binding(nameof(AddStateKeyframeDialog.SelectedState)) { Mode = BindingMode.TwoWay });
        DialogViewHelpers.AffirmOnDoubleTap(states);
        Children.Add(states);
    }
}

/// <summary>
/// Picks an animation, from this element or another, to add as a sub-animation keyframe. Twin of
/// the WPF <c>SubAnimationSelectionWindow</c>.
/// </summary>
public sealed class SubAnimationSelectionDialogView : Grid
{
    /// <summary>Builds the view.</summary>
    public SubAnimationSelectionDialogView()
    {
        Width = 500;
        Height = 300;
        DialogWindow.SetDialogTitle(this, "Add Sub-Animation");
        ColumnDefinitions = new ColumnDefinitions("*,*");

        ListBox containers = new ListBox { Margin = new Thickness(0, 0, 4, 0), ItemTemplate = DialogViewHelpers.TextTemplate(nameof(AnimationContainerViewModel.Name)) };
        containers.Bind(ItemsControl.ItemsSourceProperty, new Binding(nameof(SubAnimationSelectionDialogViewModel.AnimationContainers)));
        containers.Bind(SelectingItemsControl.SelectedItemProperty, new Binding(nameof(SubAnimationSelectionDialogViewModel.SelectedContainer)) { Mode = BindingMode.TwoWay });
        Children.Add(containers);

        ListBox animations = new ListBox { ItemTemplate = DialogViewHelpers.TextTemplate(nameof(AnimationViewModel.Name)) };
        animations.Bind(ItemsControl.ItemsSourceProperty, new Binding(nameof(SubAnimationSelectionDialogViewModel.Animations)));
        animations.Bind(SelectingItemsControl.SelectedItemProperty, new Binding(nameof(SubAnimationSelectionDialogViewModel.SelectedAnimation)) { Mode = BindingMode.TwoWay });
        DialogViewHelpers.AffirmOnDoubleTap(animations);
        SetColumn(animations, 1);
        Children.Add(animations);
    }
}
