using Gum.Forms.Controls;
using Gum.Forms.Input;
using Shouldly;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// Shift+Up repeated in a wrapped TextBox that was never toggled: each press moves the caret to the same
/// column one visual line up (clamped to that line's length) and the selection spans anchor to caret.
/// </summary>
public class TextBoxShiftUpWrappedSelectionTests : BaseTestClass
{
    private static TextBox Create(string text)
    {
        TextBox box = new();
        box.TextWrapping = Gum.Forms.TextWrapping.Wrap;
        box.AcceptsReturn = true;
        box.Height = 200;
        box.Width = 120;
        box.Visual.X = 100;
        box.Visual.Y = 80;
        box.Text = text;
        box.IsFocused = true;
        return box;
    }

    private static List<(int start, int length)> VisualLines(TextBox box)
    {
        string text = box.Text!;
        List<string> wrapped = box.Visual.GetGraphicalUiElementByName("TextInstance") is Gum.Wireframe.GraphicalUiElement gue
            && gue.RenderableComponent is RenderingLibrary.Graphics.IFormsText itext
            ? itext.WrappedText.ToList()
            : new List<string>();
        List<(int, int)> lines = new();
        int index = 0;
        foreach (string line in wrapped)
        {
            lines.Add((index, line.Length));
            index += line.Length;
            while (index < text.Length && (text[index] == ' ' || text[index] == '\n' || text[index] == '\r') && line.Length == 0)
            {
                index++;
            }
        }
        return lines;
    }

    [Fact]
    public void ShiftUp_RepeatedFromTheEndOfWrappedText_KeepsColumnAndStopsAtTheFirstLine()
    {
        string text = string.Concat(Enumerable.Repeat("abcdefghijklmnopqrstuvwxyz", 6));
        TextBox box = Create(text);
        box.CaretIndex = text.Length;
        List<(int start, int length)> lines = VisualLines(box);
        lines.Count.ShouldBeGreaterThan(3, "the text must wrap for this test to mean anything");

        int anchor = text.Length;
        int line = lines.Count - 1;
        int column = anchor - lines[line].start;
        for (int press = 1; press <= lines.Count + 2; press++)
        {
            box.HandleKeyDown(Keys.Up, true, false, false);

            if (line > 0)
            {
                line--;
            }
            int expectedCaret = line == 0 && press >= lines.Count
                ? 0
                : lines[line].start + System.Math.Min(column, lines[line].length);
            int expectedStart = System.Math.Min(anchor, expectedCaret);
            int expectedLength = System.Math.Abs(anchor - expectedCaret);
            (box.SelectionStart, box.SelectionLength).ShouldBe((expectedStart, expectedLength), "after press " + press);
        }
    }
}
