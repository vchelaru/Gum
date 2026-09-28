using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Gum.Avalonia.Dialogs;
using Gum.Services.Dialogs;
using Microsoft.Extensions.DependencyInjection;

namespace Gum.Avalonia.Tests.Harness;

/// <summary>
/// A dialog shown in the head's own <see cref="DialogWindow"/> around the view the head registers
/// for its view model, as a headless window, and driven with real input: clicks, typing, keys and
/// the copy gesture. <see cref="ScriptedDialogService.AnswerNextInWindow{T}"/> opens one for the
/// next dialog the code under test shows.
/// </summary>
internal sealed class DialogWindowDriver : IDisposable
{
    public DialogWindowDriver(DialogViewModel viewModel)
    {
        Control view = TestAppBuilder.Services.GetRequiredService<DialogViewRegistry>().CreateView(viewModel);
        Window = new DialogWindow(viewModel, view);
        Window.Show();
        // As HeadlessWindowDriver: two render ticks so hit testing sees this window's first frame.
        Dispatcher.UIThread.RunJobs();
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        Dispatcher.UIThread.RunJobs();
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        Layout();
    }

    public DialogWindow Window { get; }

    /// <summary>What the window closed with: true for OK, false for Cancel, null while it is open.</summary>
    public bool? Result => Window.Result;

    public bool IsOpen => Window.IsVisible;

    /// <summary>The window's title bar text.</summary>
    public string? Title => Window.Title;

    /// <summary>The OK (affirmative) footer button.</summary>
    public Button AffirmativeButton => Find<Button>(button => button.Name == DialogWindow.AffirmativeButtonName);

    /// <summary>The first control of type <typeparamref name="T"/> in the window matching <paramref name="predicate"/>.</summary>
    public T Find<T>(Func<T, bool>? predicate = null) where T : Control
    {
        Layout();
        return Window.GetVisualDescendants().OfType<T>().FirstOrDefault(control => predicate?.Invoke(control) ?? true)
            ?? throw new InvalidOperationException($"The {Window.DataContext?.GetType().Name} window shows no matching {typeof(T).Name}.");
    }

    /// <summary>Every control of type <typeparamref name="T"/> in the window.</summary>
    public List<T> FindAll<T>() where T : Control
    {
        Layout();
        return Window.GetVisualDescendants().OfType<T>().ToList();
    }

    /// <summary>Every text a TextBlock in the window shows, joined by new lines.</summary>
    public string Text()
    {
        Layout();
        return string.Join("\n", Window.GetVisualDescendants().OfType<TextBlock>().Select(block => block.Text).Where(text => !string.IsNullOrEmpty(text)));
    }

    public void Click(Control control)
    {
        control.BringIntoView();
        Layout();
        Point point = control.TranslatePoint(new Point(control.Bounds.Width / 2, control.Bounds.Height / 2), Window)
            ?? throw new InvalidOperationException($"{control.GetType().Name} is not in the window.");
        Window.MouseMove(point);
        Window.MouseDown(point, MouseButton.Left);
        Window.MouseUp(point, MouseButton.Left);
        Layout();
    }

    /// <summary>Clicks the button whose text is <paramref name="text"/>, access-key underscores ignored.</summary>
    public void ClickButton(string text) =>
        Click(Find<Button>(button => button.IsEffectivelyVisible && (button.Content as string)?.Replace("_", "") == text));

    /// <summary>Clicks into <paramref name="box"/>, selects its text and types <paramref name="text"/> over it.</summary>
    public void TypeInto(TextBox box, string text)
    {
        Click(box);
        box.Focus();
        box.SelectAll();
        Window.KeyTextInput(text);
        Layout();
    }

    /// <summary>Presses and releases a key; a key that answers the dialog closes it on the press.</summary>
    public void Press(Key key, PhysicalKey physicalKey, RawInputModifiers modifiers = RawInputModifiers.None, string? symbol = null)
    {
        Window.KeyPress(key, modifiers, physicalKey, symbol);
        if (Window.IsVisible)
        {
            Window.KeyRelease(key, modifiers, physicalKey, symbol);
        }
        Layout();
    }

    /// <summary>The platform's copy gesture (Ctrl+C, or Cmd+C on macOS).</summary>
    public void PressCopy()
    {
        KeyGesture copy = Window.PlatformSettings?.HotkeyConfiguration.Copy.FirstOrDefault() ?? new KeyGesture(Key.C, KeyModifiers.Control);
        Press(copy.Key, PhysicalKey.C, (RawInputModifiers)copy.KeyModifiers, "c");
    }

    /// <summary>The text on the window's clipboard.</summary>
    public string? ClipboardText()
    {
        Task<string?> read = Window.Clipboard!.TryGetTextAsync();
        Dispatcher.UIThread.RunJobs();
        return read.Result;
    }

    public void Layout()
    {
        Dispatcher.UIThread.RunJobs();
        if (Window.IsVisible)
        {
            Window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();
        }
    }

    public void Dispose()
    {
        if (Window.IsVisible)
        {
            Window.Close();
            Dispatcher.UIThread.RunJobs();
        }
    }
}
