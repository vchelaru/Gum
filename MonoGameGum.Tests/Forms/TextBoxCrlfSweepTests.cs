using Gum.Forms.Controls;
using Gum.Forms.DefaultVisuals.V3;
using Gum.Forms.Input;
using Gum.Wireframe;
using Shouldly;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// Every caret position and select-then-delete key combination leaves a CRLF box in the same state
/// as the same content with LF (#6996): text, selection, and how many highlight rectangles show.
/// </summary>
public class TextBoxCrlfSweepTests : BaseTestClass
{
    const string Lf = "a\nb\nc\nd";
    const string Crlf = "a\r\nb\r\nc\r\nd";

    [Fact]
    public void ShiftSelectThenDelete_EveryCaretAndKey_ShouldLeaveSameStateAsLf()
    {
        List<string> mismatches = new();
        foreach (Keys selectKey in new[] { Keys.End, Keys.Home, Keys.Right, Keys.Left, Keys.Down, Keys.Up })
        {
            foreach (Keys deleteKey in new[] { Keys.Delete, Keys.Back })
            {
                for (int crlfCaret = 0; crlfCaret <= Crlf.Length; crlfCaret++)
                {
                    // A caret inside a CRLF acts as the one before the break.
                    int lfCaret = crlfCaret - Crlf.Take(crlfCaret).Count(c => c == '\r');

                    // Only one TextBox holds focus at a time, so finish with the LF box first.
                    TextBox lf = CreateTextBox(Lf);
                    lf.CaretIndex = lfCaret;
                    lf.HandleKeyDown(selectKey, true, false, false);
                    int lfSelectedRects = VisibleSelectionCount(lf);
                    lf.HandleKeyDown(deleteKey, false, false, false);
                    string expected = $"{lf.Text!.Replace("\n", "\r\n")}|{lf.SelectionLength}|{VisibleSelectionCount(lf)}|{lfSelectedRects}";

                    TextBox crlf = CreateTextBox(Crlf);
                    crlf.CaretIndex = crlfCaret;
                    crlf.HandleKeyDown(selectKey, true, false, false);
                    int crlfSelectedRects = VisibleSelectionCount(crlf);
                    crlf.HandleKeyDown(deleteKey, false, false, false);
                    string actual = $"{crlf.Text}|{crlf.SelectionLength}|{VisibleSelectionCount(crlf)}|{crlfSelectedRects}";

                    if (expected != actual)
                    {
                        mismatches.Add($"{selectKey}+{deleteKey} from {crlfCaret}: expected {expected} but was {actual}");
                    }
                }
            }
        }

        mismatches.ShouldBeEmpty(string.Join(" ; ", mismatches).Replace("\r\n", "<CRLF>").Replace("\n", "<LF>"));
    }

    static TextBox CreateTextBox(string text)
    {
        TextBox textBox = new();
        textBox.TextWrapping = Gum.Forms.TextWrapping.Wrap;
        textBox.AcceptsReturn = true;
        textBox.Height = 200;
        textBox.Width = 300;
        textBox.Text = text;
        textBox.IsFocused = true;
        return textBox;
    }

    static int VisibleSelectionCount(TextBox textBox) =>
        ((TextBoxBaseVisual)textBox.Visual).ClipContainer.Children
            .OfType<GraphicalUiElement>()
            .Count(child => child.Name == "SelectionInstance" && child.Visible);
}
