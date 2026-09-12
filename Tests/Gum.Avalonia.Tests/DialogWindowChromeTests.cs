using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Gum.Avalonia.Dialogs;
using Gum.Services.Dialogs;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Gum.Avalonia.Tests;

/// <summary>
/// The dialog window draws the WPF <c>DialogWindow</c> chrome: no system decorations, the title as a
/// bold caption inside the rounded frame, and the affirmative and negative buttons in the tool's
/// primary button style.
/// </summary>
public class DialogWindowChromeTests
{
    [AvaloniaFact]
    public void Escape_ClosesAnOkOnlyDialog_AsTheWpfWindowDoes()
    {
        // The WPF window runs the negative command on Escape whether or not a Cancel button is shown.
        MessageDialogViewModel viewModel = new MessageDialogViewModel { Title = "About", Message = "Gum" };
        viewModel.AffirmativeText = "OK";
        viewModel.NegativeText = null;
        Control view = TestAppBuilder.Services.GetRequiredService<DialogViewRegistry>().CreateView(viewModel);
        DialogWindow window = new DialogWindow(viewModel, view);
        window.Show();
        Dispatcher.UIThread.RunJobs();

        window.RaiseEvent(new global::Avalonia.Input.KeyEventArgs { RoutedEvent = global::Avalonia.Input.InputElement.KeyDownEvent, Key = global::Avalonia.Input.Key.Escape, Source = window });
        Dispatcher.UIThread.RunJobs();

        window.Result.ShouldBe(false);
        window.IsVisible.ShouldBeFalse();
    }

    [AvaloniaFact]
    public void DialogWindow_DrawsTheWpfChrome_WithCaptionAndPrimaryButtons()
    {
        MessageDialogViewModel viewModel = new MessageDialogViewModel { Title = "Delete Instance?", Message = "Are you sure?" };
        viewModel.AffirmativeText = "Yes";
        viewModel.NegativeText = "No";
        Control view = TestAppBuilder.Services.GetRequiredService<DialogViewRegistry>().CreateView(viewModel);
        DialogWindow window = new DialogWindow(viewModel, view);

        window.Show();
        Dispatcher.UIThread.RunJobs();

        window.SystemDecorations.ShouldBe(SystemDecorations.None);
        window.Title.ShouldBe("Delete Instance?");
        TextBlock caption = window.GetVisualDescendants().OfType<TextBlock>().First(text => text.Name == DialogWindow.CaptionName);
        caption.Text.ShouldBe("Delete Instance?");
        caption.FontWeight.ShouldBe(FontWeight.Bold);
        Button[] buttons = window.GetVisualDescendants().OfType<Button>()
            .Where(button => button.Name is DialogWindow.AffirmativeButtonName or DialogWindow.NegativeButtonName)
            .ToArray();
        buttons.Length.ShouldBe(2);
        // The default button look, as the WPF dialog's buttons have.
        buttons.ShouldAllBe(button => button.FontWeight == FontWeight.Bold);
        buttons.ShouldAllBe(button => button.MinWidth == 64);
        window.Close();
    }
}
