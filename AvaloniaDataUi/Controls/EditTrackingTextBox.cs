using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;

namespace AvaloniaDataUi.Controls;

/// <summary>
/// A text field that knows whether the user edited it since the tool last put text in it. Leaving
/// the field raises <see cref="EditCommitRequested"/> only then, so text the tool showed (a refresh,
/// an undo, a scrub, a stale value under the caret) is never written back when focus moves on. Every
/// editor field that commits on focus loss commits on that event, not on <see cref="InputElement.LostFocus"/>.
/// </summary>
public class EditTrackingTextBox : TextBox
{
    private int _userInputDepth;
    private string _shownText;

    /// <summary>Builds the field.</summary>
    public EditTrackingTextBox()
    {
        _shownText = string.Empty;
        AddHandler(PastingFromClipboardEvent, HandlePastingFromClipboard, RoutingStrategies.Bubble);
        AddHandler(CuttingToClipboardEvent, HandleCuttingToClipboard, RoutingStrategies.Bubble);
        // Runs after the editors' own Enter handlers (tunnel), which have committed by then.
        AddHandler(KeyDownEvent, HandleKeyDownAfterEditors, RoutingStrategies.Bubble, handledEventsToo: true);
    }

    /// <inheritdoc/>
    protected override Type StyleKeyOverride => typeof(TextBox);

    /// <summary>Raised when the field loses focus holding text the user typed since it last showed or committed a value.</summary>
    public event EventHandler? EditCommitRequested;

    /// <summary>Whether the text differs from what the tool last showed or the user last committed.</summary>
    public bool HasPendingEdit => (Text ?? string.Empty) != _shownText;

    /// <summary>Treats the current text as committed, so leaving the field does not commit it again.</summary>
    public void AcceptText()
    {
        _shownText = Text ?? string.Empty;
    }

    /// <inheritdoc/>
    protected override void OnKeyDown(KeyEventArgs e)
    {
        _userInputDepth++;
        try
        {
            base.OnKeyDown(e);
        }
        finally
        {
            _userInputDepth--;
        }
    }

    /// <inheritdoc/>
    protected override void OnTextInput(TextInputEventArgs e)
    {
        _userInputDepth++;
        try
        {
            base.OnTextInput(e);
        }
        finally
        {
            _userInputDepth--;
        }
    }

    /// <inheritdoc/>
    protected override void OnLostFocus(RoutedEventArgs e)
    {
        base.OnLostFocus(e);

        if (HasPendingEdit)
        {
            AcceptText();
            EditCommitRequested?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <inheritdoc/>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        // Watched here rather than through TextChanged, which is raised later, outside the input
        // handling. The text box's own key and text handling is the user; any other change is the
        // tool showing a value.
        if (change.Property == TextProperty && _userInputDepth == 0)
        {
            AcceptText();
        }
    }

    // The text box's own paste and cut change the text after an awaited clipboard call, outside the
    // key handling, so they are done here instead, with the text change inside the user's input.
    private async void HandlePastingFromClipboard(object? sender, RoutedEventArgs e)
    {
        e.Handled = true;
        IClipboard? clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
        if (clipboard == null)
        {
            return;
        }

        string? text;
        try
        {
            text = await clipboard.GetTextAsync();
        }
        catch
        {
            // An unreadable clipboard pastes nothing, as the text box's own paste does.
            return;
        }

        if (!string.IsNullOrEmpty(text))
        {
            RaiseEvent(new TextInputEventArgs { RoutedEvent = TextInputEvent, Text = text });
        }
    }

    private async void HandleCuttingToClipboard(object? sender, RoutedEventArgs e)
    {
        e.Handled = true;
        string text = SelectedText;
        _userInputDepth++;
        try
        {
            SelectedText = string.Empty;
        }
        finally
        {
            _userInputDepth--;
        }

        IClipboard? clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
        if (clipboard == null)
        {
            return;
        }

        try
        {
            await clipboard.SetTextAsync(text);
        }
        catch
        {
            // The text is cut either way, as it is when the text box's own cut cannot reach the clipboard.
        }
    }

    private void HandleKeyDownAfterEditors(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && !AcceptsReturn)
        {
            AcceptText();
        }
    }
}
