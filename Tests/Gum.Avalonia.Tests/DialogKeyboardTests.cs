using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Gum.Avalonia.Dialogs;
using Gum.Services.Dialogs;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Gum.Avalonia.Tests;

/// <summary>Keyboard answers and copying in dialogs, matching the WPF head.</summary>
public class DialogKeyboardTests
{
    [AvaloniaTheory]
    [InlineData(Key.Y, PhysicalKey.Y, "y", RawInputModifiers.None, true)]
    [InlineData(Key.N, PhysicalKey.N, "n", RawInputModifiers.None, false)]
    [InlineData(Key.Y, PhysicalKey.Y, "y", RawInputModifiers.Alt, true)]
    [InlineData(Key.N, PhysicalKey.N, "n", RawInputModifiers.Alt, false)]
    public void DeleteDialog_YOrN_WithOrWithoutAlt_AnswersIt(Key key, PhysicalKey physicalKey, string symbol, RawInputModifiers modifiers, bool expected)
    {
        DeleteOptionsDialogViewModel viewModel = new DeleteOptionsDialogViewModel { Title = "Delete?", Message = "Delete Button?" };
        DialogWindow window = Open(viewModel);

        // No key release: answering closes the window, and a closed headless window takes no input.
        window.KeyPress(key, modifiers, physicalKey, symbol);
        Dispatcher.UIThread.RunJobs();

        window.Result.ShouldBe(expected);
        window.IsVisible.ShouldBeFalse();
    }

    [AvaloniaFact]
    public void MessageDialog_CopyGesture_CopiesTheWholeMessage()
    {
        MessageDialogViewModel viewModel = new MessageDialogViewModel { Title = "Error", Message = "Could not load Button.gucx" };
        DialogWindow window = Open(viewModel);

        PressCopy(window);

        ClipboardText(window).ShouldBe("Could not load Button.gucx");
        window.Close();
    }

    [AvaloniaFact]
    public void MessageDialog_TextIsSelectable_AndCopyGestureCopiesOnlyTheSelection()
    {
        MessageDialogViewModel viewModel = new MessageDialogViewModel { Title = "Error", Message = "Could not load Button.gucx" };
        DialogWindow window = Open(viewModel);
        SelectableTextBlock message = window.GetVisualDescendants().OfType<SelectableTextBlock>().Single();
        message.Focus();
        message.SelectionStart = 15;
        message.SelectionEnd = 21;

        PressCopy(window);

        ClipboardText(window).ShouldBe("Button");
        window.Close();
    }

    private static DialogWindow Open(DialogViewModel viewModel)
    {
        Control view = TestAppBuilder.Services.GetRequiredService<DialogViewRegistry>().CreateView(viewModel);
        DialogWindow window = new DialogWindow(viewModel, view);
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return window;
    }

    // The platform's own copy gesture: Ctrl+C, or Cmd+C on macOS.
    private static void PressCopy(DialogWindow window)
    {
        KeyGesture copy = window.PlatformSettings?.HotkeyConfiguration.Copy.FirstOrDefault() ?? new KeyGesture(Key.C, KeyModifiers.Control);
        RawInputModifiers modifiers = (RawInputModifiers)copy.KeyModifiers;
        window.KeyPress(copy.Key, modifiers, PhysicalKey.C, "c");
        window.KeyRelease(copy.Key, modifiers, PhysicalKey.C, "c");
        Dispatcher.UIThread.RunJobs();
    }

    private static string? ClipboardText(DialogWindow window)
    {
        Task<string?> read = window.Clipboard!.GetTextAsync();
        Dispatcher.UIThread.RunJobs();
        return read.Result;
    }
}
