using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Gum.Avalonia.Dialogs;
using Gum.Dialogs;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Gum.Avalonia.Tests;

/// <summary>Where keyboard focus lands when a dialog opens.</summary>
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
}
