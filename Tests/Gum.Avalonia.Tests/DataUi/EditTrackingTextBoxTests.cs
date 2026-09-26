using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using AvaloniaDataUi.Controls;
using Shouldly;

namespace Gum.Avalonia.Tests.DataUi;

/// <summary>
/// <see cref="EditTrackingTextBox"/> asks for a commit on focus loss only for text the user put in
/// the field, whether typed, deleted or pasted; text the tool sets is never a pending edit.
/// </summary>
public class EditTrackingTextBoxTests
{
    [AvaloniaFact]
    public void Leaving_AfterTyping_RequestsACommit()
    {
        (Window window, EditTrackingTextBox field, TextBox elsewhere) = Show("1");
        int requests = 0;
        field.EditCommitRequested += (_, _) => requests++;
        field.Focus();
        window.KeyTextInput("5");

        elsewhere.Focus();

        requests.ShouldBe(1);
        window.Close();
    }

    [AvaloniaFact]
    public void Leaving_AfterDeleting_RequestsACommit()
    {
        (Window window, EditTrackingTextBox field, TextBox elsewhere) = Show("12");
        int requests = 0;
        field.EditCommitRequested += (_, _) => requests++;
        field.Focus();
        field.CaretIndex = 2;
        window.KeyPress(Key.Back, RawInputModifiers.None, PhysicalKey.Backspace, null);

        elsewhere.Focus();

        field.Text.ShouldBe("1");
        requests.ShouldBe(1);
        window.Close();
    }

    [AvaloniaFact]
    public void Leaving_AfterAPasteThatFinishesLater_RequestsACommit()
    {
        (Window window, EditTrackingTextBox field, TextBox elsewhere) = Show("1");
        int requests = 0;
        field.EditCommitRequested += (_, _) => requests++;
        field.Focus();

        // A desktop clipboard read completes after the key handling returns: the paste announces
        // itself, then its text lands on its own.
        field.RaiseEvent(new RoutedEventArgs(TextBox.PastingFromClipboardEvent));
        field.Text = "77";
        elsewhere.Focus();

        requests.ShouldBe(1);
        window.Close();
    }

    [AvaloniaFact]
    public void Leaving_AfterTheToolSetsText_OrAfterEnter_RequestsNothing()
    {
        (Window window, EditTrackingTextBox field, TextBox elsewhere) = Show("1");
        int requests = 0;
        field.EditCommitRequested += (_, _) => requests++;
        field.Focus();
        window.KeyTextInput("5");
        window.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null);
        field.Text = "9";

        elsewhere.Focus();

        requests.ShouldBe(0);
        window.Close();
    }

    private static (Window Window, EditTrackingTextBox Field, TextBox Elsewhere) Show(string text)
    {
        EditTrackingTextBox field = new EditTrackingTextBox { Text = text };
        TextBox elsewhere = new TextBox();
        Window window = new Window { Content = new StackPanel { Children = { field, elsewhere } }, Width = 300, Height = 200 };
        window.Show();
        window.UpdateLayout();
        return (window, field, elsewhere);
    }
}
