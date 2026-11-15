using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Gum.Avalonia.Dialogs.Views;
using Gum.Avalonia.Tests.Harness;
using Gum.Services.Dialogs;
using Shouldly;

namespace Gum.Avalonia.Tests;

/// <summary>
/// The choice prompt matches the WPF view: options wrap at its fixed width, and the keyboard
/// picks one without a click (#5558).
/// </summary>
public class ChoiceDialogViewTests
{
    [AvaloniaFact]
    public void LongOption_WrapsAtTheViewWidth_InsteadOfWideningTheWindow()
    {
        string longOption = string.Join(" ", Enumerable.Repeat("Overwrite the existing component and every instance that uses it", 6));
        ChoiceDialogViewModel viewModel = new ChoiceDialogViewModel { Title = "Conflict", Message = "Button already exists." };
        viewModel.SetOptions(new Dictionary<int, string> { [0] = "Skip", [1] = longOption });
        using DialogWindowDriver dialog = new DialogWindowDriver(viewModel);

        ChoiceDialogView view = dialog.Find<ChoiceDialogView>();
        view.Bounds.Width.ShouldBe(450);
        TextBlock option = dialog.Find<TextBlock>(text => text.Text == longOption);
        option.Bounds.Width.ShouldBeLessThanOrEqualTo(450);
        // More than one line: it wrapped rather than being clipped or scrolled sideways.
        option.Bounds.Height.ShouldBeGreaterThan(option.FontSize * 2);
        dialog.Window.Bounds.Width.ShouldBeLessThan(600);
    }

    [AvaloniaFact]
    public void Opening_FocusesTheSelectedOption()
    {
        ChoiceDialogViewModel viewModel = new ChoiceDialogViewModel { Title = "Pick", Message = "Which one?" };
        viewModel.SetOptions(new Dictionary<int, string> { [0] = "First", [1] = "Second" });
        using DialogWindowDriver dialog = new DialogWindowDriver(viewModel);

        ListBoxItem selected = dialog.Find<ListBoxItem>(item => item.IsSelected);
        (selected.Content as string).ShouldBe("First");
        selected.IsFocused.ShouldBeTrue();
    }

    [AvaloniaFact]
    public void DownThenEnter_ConfirmsTheNextOption()
    {
        ChoiceDialogViewModel viewModel = new ChoiceDialogViewModel { Title = "Pick", Message = "Which one?" };
        viewModel.SetOptions(new Dictionary<int, string> { [0] = "First", [1] = "Second" });
        using DialogWindowDriver dialog = new DialogWindowDriver(viewModel);

        dialog.Press(Key.Down, PhysicalKey.ArrowDown);
        dialog.Press(Key.Enter, PhysicalKey.Enter);

        dialog.Result.ShouldBe(true);
        viewModel.SelectedKey.ShouldBe(1);
    }
}
