using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Gum.Avalonia.Dialogs;
using Gum.Dialogs;
using Gum.Services.Dialogs;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using StateAnimationPlugin.ViewModels;

namespace Gum.Avalonia.Tests;

/// <summary>Where keyboard focus lands when a dialog opens, and what a text dialog shows as it opens.</summary>
public class DialogFocusTests
{
    private static IServiceProvider Services => TestAppBuilder.Services;

    [AvaloniaFact]
    public void TextInputDialog_FocusesItsTextBoxOnceOpen()
    {
        AddCategoryDialogViewModel viewModel = ActivatorUtilities.CreateInstance<AddCategoryDialogViewModel>(Services);
        Control view = Services.GetRequiredService<DialogViewRegistry>().CreateView(viewModel);
        DialogWindow window = new DialogWindow(viewModel, view);

        window.Show();
        Dispatcher.UIThread.RunJobs();

        TextBox textBox = window.GetVisualDescendants().OfType<TextBox>().First();
        textBox.IsFocused.ShouldBeTrue();
        window.Close();
    }

    [AvaloniaFact]
    public void AddAnimationDialog_FocusesAndSelectsItsNameOnceOpen()
    {
        // The name box took focus before the window opened, and the affirmative-button fallback
        // then took it away (#5039).
        AddAnimationDialogViewModel viewModel = new AddAnimationDialogViewModel { Name = "Anim1" };
        AssertTextBoxFocusedAndSelectedOnceOpen(viewModel, "Anim1");
    }

    [AvaloniaFact]
    public void ExposeColorDialog_FocusesAndSelectsItsBaseNameOnceOpen()
    {
        ExposeColorDialogViewModel viewModel = new ExposeColorDialogViewModel("Base", new[] { "Red" }, _ => null);
        AssertTextBoxFocusedAndSelectedOnceOpen(viewModel, "Base");
    }

    private static void AssertTextBoxFocusedAndSelectedOnceOpen(DialogViewModel viewModel, string expectedText)
    {
        Control view = Services.GetRequiredService<DialogViewRegistry>().CreateView(viewModel);
        DialogWindow window = new DialogWindow(viewModel, view);

        window.Show();
        Dispatcher.UIThread.RunJobs();

        TextBox textBox = window.GetVisualDescendants().OfType<TextBox>().First();
        textBox.IsFocused.ShouldBeTrue();
        textBox.SelectedText.ShouldBe(expectedText);
        window.Close();
    }

    [AvaloniaFact]
    public void MessageDialog_FocusesTheAffirmativeButtonOnceOpen_SoEnterConfirmsWithNoPriorClick()
    {
        // A view with no text box or list to claim focus (e.g. a Yes/No confirmation) previously left
        // the window with no keyboard focus at all, so Enter/the default button never fired (#4810).
        MessageDialogViewModel viewModel = new MessageDialogViewModel { Title = "Delete?", Message = "Are you sure?", NegativeText = "No" };
        Control view = Services.GetRequiredService<DialogViewRegistry>().CreateView(viewModel);
        DialogWindow window = new DialogWindow(viewModel, view);

        window.Show();
        Dispatcher.UIThread.RunJobs();

        Button affirmative = window.GetVisualDescendants().OfType<Button>().First(button => button.Name == DialogWindow.AffirmativeButtonName);
        affirmative.IsFocused.ShouldBeTrue();
        window.Close();
    }

    [AvaloniaFact]
    public void TextInputDialog_ValidatesAsItOpens_SoAnEmptyValueShowsTheErrorWithOkDisabled()
    {
        // As the WPF view does on load: the user sees why OK is disabled before typing anything.
        GetUserStringDialogViewModel viewModel = new GetUserStringDialogViewModel();
        Control view = Services.GetRequiredService<DialogViewRegistry>().CreateView(viewModel);
        DialogWindow window = new DialogWindow(viewModel, view);

        window.Show();
        Dispatcher.UIThread.RunJobs();

        viewModel.Error.ShouldBe("Cannot be empty.");
        TextBlock error = window.GetVisualDescendants().OfType<TextBlock>().First(text => text.Text == "Cannot be empty.");
        error.IsVisible.ShouldBeTrue();
        // The WPF view shows it in the caption size.
        error.FontSize.ShouldBe(Themes.FrbThemeResources.DefaultBaseFontSize * 0.85, 0.01);
        Button ok = window.GetVisualDescendants().OfType<Button>().First(button => button.Name == DialogWindow.AffirmativeButtonName);
        // A command's CanExecute disables an Avalonia button effectively, not through IsEnabled.
        ok.IsEffectivelyEnabled.ShouldBeFalse();
        window.Close();
    }
}
