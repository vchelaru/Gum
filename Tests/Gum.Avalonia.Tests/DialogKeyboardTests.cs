using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Gum.Avalonia.Dialogs;
using Gum.Diagnostics;
using Gum.Services.Dialogs;
using ImportFromGumxPlugin.ViewModels;
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

    [AvaloniaFact]
    public void FreezeDiagnosticsPrompt_CopyGesture_CopiesTheMessageWithoutControlLabels()
    {
        FreezeDiagnosticsPromptViewModel viewModel = new FreezeDiagnosticsPromptViewModel
        {
            Message = "Gum froze.\nFiles are in:\nC:\\Gum\\Reported",
        };
        DialogWindow window = Open(viewModel);

        PressCopy(window);

        ClipboardText(window).ShouldBe("Gum froze.\nFiles are in:\nC:\\Gum\\Reported");
        window.Close();
    }

    [AvaloniaFact]
    public void DeleteDialog_CopyGesture_CopiesItsText()
    {
        DeleteOptionsDialogViewModel viewModel = new DeleteOptionsDialogViewModel { Title = "Delete?", Message = "Delete Button?" };
        DialogWindow window = Open(viewModel);

        PressCopy(window);

        ClipboardText(window).ShouldNotBeNull().ShouldContain("Delete Button?");
        window.Close();
    }

    [AvaloniaFact]
    public void PluginsDialog_Enter_DoesNotCloseTheWindow()
    {
        Moq.Mock<Gum.Plugins.IPluginManager> pluginManager = new Moq.Mock<Gum.Plugins.IPluginManager>();
        pluginManager.Setup(manager => manager.GetAllPluginSummaries()).Returns(new List<Gum.Plugins.PluginSummary>
        {
            new Gum.Plugins.PluginSummary("A", "A", IsEnabled: true, HasFailureDetails: false, PluginHandle: new object()),
        });
        pluginManager.Setup(manager => manager.GetPluginScanReport())
            .Returns(new Gum.Plugins.PluginScanReport("/plugins", FolderExists: true, Files: Array.Empty<Gum.Plugins.PluginFileScan>()));
        PluginsDialogViewModel viewModel = new PluginsDialogViewModel(
            Moq.Mock.Of<IDialogService>(), pluginManager.Object, Moq.Mock.Of<Gum.Services.IClipboardService>());

        AssertEnterOnAFocusedControlKeepsOpen(viewModel);
    }

    [AvaloniaFact]
    public void StandardDiffDetailsDialog_CloseButton_IsNotTheDefaultButton()
    {
        StandardDiffDetailsViewModel viewModel = new StandardDiffDetailsViewModel(
            "Sprite", new[] { new StandardDiffRowViewModel("Variable", "Width differs") });
        DialogWindow window = Open(viewModel);

        AffirmativeButton(window).IsDefault.ShouldBeFalse();
        window.Close();
    }

    [AvaloniaFact]
    public void AffirmativeIsDefault_ChangedWhileOpen_UpdatesTheButtonsDefaultState()
    {
        ToggleableDefaultViewModel viewModel = new ToggleableDefaultViewModel();
        DialogWindow window = Open(viewModel);
        AffirmativeButton(window).IsDefault.ShouldBeTrue();

        viewModel.TurnOffDefault();
        Dispatcher.UIThread.RunJobs();

        AffirmativeButton(window).IsDefault.ShouldBeFalse();
        window.Close();
    }

    private static Button AffirmativeButton(DialogWindow window) =>
        window.GetVisualDescendants().OfType<Button>().Single(button => button.Name == DialogWindow.AffirmativeButtonName);

    private sealed class ToggleableDefaultViewModel : DialogViewModel
    {
        public void TurnOffDefault() => AffirmativeIsDefault = false;
    }

    private static void AssertEnterOnAFocusedControlKeepsOpen(DialogViewModel viewModel)
    {
        DialogWindow window = Open(viewModel);
        Control? target = window.GetVisualDescendants().OfType<Control>()
            .FirstOrDefault(control => control.Focusable && control.IsEffectivelyVisible && control is not Button);
        (target ?? window).Focus();

        window.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, "\r");
        Dispatcher.UIThread.RunJobs();

        window.IsVisible.ShouldBeTrue("Enter must not press a Close button");
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
        Task<string?> read = window.Clipboard!.TryGetTextAsync();
        Dispatcher.UIThread.RunJobs();
        return read.Result;
    }
}
