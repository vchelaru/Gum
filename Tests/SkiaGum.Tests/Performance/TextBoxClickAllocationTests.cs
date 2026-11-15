using System.Text;
using Gum;
using Gum.Forms;
using Gum.Forms.Controls;
using Gum.Input;
using Gum.Wireframe;
using MonoGameGum.TestsCommon;
using Shouldly;
using Xunit.Abstractions;

namespace SkiaGum.Tests.Performance;

/// <summary>
/// What a Skia <c>TextBox</c> allocates when a mouse click places the caret and while a held drag extends the
/// selection (#7872). Turning a point into a caret index walks the line one character at a time, and on Skia each
/// character's advance used to be a RichTextKit layout of a one-character string. Each case also checks its input
/// moved the caret or selection, so none passes by doing nothing.
/// </summary>
public class TextBoxClickAllocationTests : BaseTestClass
{
    private readonly ITestOutputHelper _output;
    private readonly SkiaSceneHost _host = new SkiaSceneHost();
    private readonly FrameCursor _cursor = new FrameCursor();
    private readonly FrameKeyboard _keyboard = new FrameKeyboard();
    private readonly ICursor _previousCursor = FrameworkElement.MainCursor;
    private readonly IInputReceiverKeyboard _previousKeyboard = FrameworkElement.MainKeyboard;
    private double _inputSeconds;

    public TextBoxClickAllocationTests(ITestOutputHelper output)
    {
        _output = output;
        FrameworkElement.MainCursor = _cursor;
        FrameworkElement.MainKeyboard = _keyboard;
        FrameworkElement.KeyboardsForUiControl.Add(_keyboard);
    }

    public override void Dispose()
    {
        InteractiveGue.CurrentInputReceiver = null;
        FrameworkElement.KeyboardsForUiControl.Remove(_keyboard);
        FrameworkElement.MainCursor = _previousCursor;
        FrameworkElement.MainKeyboard = _previousKeyboard;
        GumService.Default.PopupRoot.Children!.Clear();
        _keyboard.Dispose();
        _host.Dispose();
        base.Dispose();
    }

    // Ten paragraphs of about 45 characters; at 300 wide each wraps, so the box is a wrapped multi-line one.
    private static string BuildParagraphs(int count)
    {
        StringBuilder builder = new StringBuilder();
        for (int i = 0; i < count; i++)
        {
            if (i > 0)
            {
                builder.Append('\n');
            }
            builder.Append($"Line {i} of the quick brown fox jumps over it");
        }
        return builder.ToString();
    }

    private TextBox CreateBox()
    {
        TextBox textBox = new TextBox
        {
            X = 10,
            Y = 10,
            Width = 300,
            Height = 150,
            TextWrapping = TextWrapping.Wrap,
            AcceptsReturn = true,
            Text = BuildParagraphs(10),
        };
        textBox.AddToRoot();
        textBox.IsFocused = true;
        textBox.CaretIndex = 100;
        for (int i = 0; i < 3; i++)
        {
            RunFrame();
        }
        return textBox;
    }

    private void RunFrame()
    {
        _inputSeconds += 1.0 / 60.0;
        FormsUtilities.Update(_inputSeconds, GumService.Default.Root);
        _host.Update();
        _host.Draw();
    }

    private double Measure(string label, Action<int> act)
    {
        AllocationResult result = SkiaAllocationMeasurer.MeasureMinimum(
            frame =>
            {
                act(frame);
                RunFrame();
            },
            warmupIterations: 60,
            measuredIterations: 300);
        _output.WriteLine($"{label}: {result.BytesPerIteration:N0} bytes/frame");
        _host.CountPaintedSamples().ShouldBeGreaterThan(0);
        return result.BytesPerIteration;
    }

    [Fact]
    public void Click_PlacingCaretInWrappedBox_AllocatesLittle()
    {
        TextBox textBox = CreateBox();
        HashSet<int> carets = new HashSet<int>();
        double bytes = Measure("click places caret",
            frame =>
            {
                carets.Add(textBox.CaretIndex);
                // A click is a push frame then a release frame, each on a new spot so the caret moves.
                int spot = frame / 2;
                _cursor.X = (int)textBox.Visual.AbsoluteLeft + 20 + (spot * 37) % 250;
                _cursor.Y = (int)textBox.Visual.AbsoluteTop + 8 + (spot * 23) % 130;
                _cursor.PrimaryHeld = frame % 2 == 0;
            });

        carets.Count.ShouldBeGreaterThan(10);
        bytes.ShouldBeLessThanOrEqualTo(Limits.Click);
    }

    [Fact]
    public void DragSelect_AcrossWrappedBox_AllocatesLittle()
    {
        TextBox textBox = CreateBox();
        HashSet<int> lengths = new HashSet<int>();
        _cursor.X = (int)textBox.Visual.AbsoluteLeft + 20;
        _cursor.Y = (int)textBox.Visual.AbsoluteTop + 8;
        _cursor.PrimaryHeld = true;
        double bytes = Measure("drag select",
            frame =>
            {
                lengths.Add(textBox.SelectionLength);
                // The button stays down; the pointer zig-zags over the lines so the selection changes every frame.
                _cursor.X = (int)textBox.Visual.AbsoluteLeft + 20 + (frame * 37) % 250;
                _cursor.Y = (int)textBox.Visual.AbsoluteTop + 8 + (frame * 23) % 130;
            });

        lengths.Count.ShouldBeGreaterThan(10);
        bytes.ShouldBeLessThanOrEqualTo(Limits.DragFrame);
    }

    // Bytes per frame; set from the measurements in the pull request, with headroom.
    private static class Limits
    {
        public const int Click = 2000;
        public const int DragFrame = 2000;
    }
}
