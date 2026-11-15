using Gum.Forms;
using Gum.Forms.Controls;
using Gum.Forms.DefaultVisuals.V3;
using Gum.Forms.Input;
using Gum.Wireframe;
using Shouldly;
using System.Linq;
using Xunit;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// A TextBox in a StackPanel that is switched to Wrap live, then typed into and Shift+Up'd, draws the
/// selection the way a TextBox created with Wrap does (the user saw line 2's highlight missing until the
/// window lost focus).
/// </summary>
public class TextBoxLiveWrapShiftUpInStackTests : BaseTestClass
{
    private const string Typed = "asfsaf sd as dfas das dfads dsa das as dd sa";

    private static TextBox Build(bool toggleLive, out StackPanel stack)
    {
        stack = new StackPanel();
        stack.Visual.X = 20;
        stack.Visual.Y = 20;
        stack.AddChild(new Button());
        TextBox box = new();
        box.Height = 200;
        box.Width = 120;
        if (!toggleLive)
        {
            box.TextWrapping = TextWrapping.Wrap;
        }
        stack.AddChild(box);
        stack.AddChild(new Button());
        stack.Visual.UpdateLayout();
        if (toggleLive)
        {
            box.TextWrapping = TextWrapping.Wrap;
            // The next frame of the running app.
            stack.Visual.UpdateLayout();
        }
        box.IsFocused = true;
        foreach (char c in Typed)
        {
            box.HandleCharEntered(c);
        }
        return box;
    }

    private static string Probe(TextBox box)
    {
        GraphicalUiElement text = box.Visual.GetGraphicalUiElementByName("TextInstance")!;
        return $"widthUnits={text.WidthUnits} width={text.Width} abs={text.GetAbsoluteWidth():0.##} lines={((RenderingLibrary.Graphics.IFormsText)text.RenderableComponent!).WrappedText.Count} caret={box.CaretIndex} wrapping={box.TextWrapping} lines=[{string.Join("|", ((RenderingLibrary.Graphics.IFormsText)text.RenderableComponent!).WrappedText)}] top={text.AbsoluteTop:0.##} h={text.GetAbsoluteHeight():0.##}";
    }

    private static string Describe(TextBox box) =>
        string.Join(",", ((TextBoxBaseVisual)box.Visual).ClipContainer.Children.OfType<GraphicalUiElement>()
            .Where(c => c.Name == "SelectionInstance" && c.Visible)
            .Select(r => $"{r.AbsoluteTop:0.##}:{r.AbsoluteLeft:0.##}+{r.GetAbsoluteWidth():0.##}x{r.GetAbsoluteHeight():0.##}"));

    // Only one TextBox holds focus at a time, so each is built and used before the other exists.
    private static (string probe, int caret, int start, int length, string selection) Use(bool toggleLive)
    {
        TextBox box = Build(toggleLive, out _);
        string probe = Probe(box);
        box.HandleKeyDown(Keys.Up, true, false, false);
        return (probe, box.CaretIndex, box.SelectionStart, box.SelectionLength, Describe(box));
    }

    [Fact]
    public void ShiftUp_AfterLiveSwitchToWrapInAStack_DrawsTheSameSelectionAsAFreshWrapTextBox()
    {
        var live = Use(toggleLive: true);
        var fresh = Use(toggleLive: false);

        live.probe.ShouldBe(fresh.probe);
        (live.caret, live.start, live.length).ShouldBe((fresh.caret, fresh.start, fresh.length));
        live.selection.ShouldBe(fresh.selection);
    }

    [Fact]
    public void ShiftUp_AfterLiveSwitchToWrapInAStack_FocusLossAndRegainChangesNothing()
    {
        TextBox live = Build(toggleLive: true, out _);
        live.HandleKeyDown(Keys.Up, true, false, false);
        string before = Describe(live);

        live.IsFocused = false;
        live.IsFocused = true;

        Describe(live).ShouldBe(before);
    }
}
