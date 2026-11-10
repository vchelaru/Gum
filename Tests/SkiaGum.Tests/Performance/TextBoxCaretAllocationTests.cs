using System.Text;
using Gum;
using Gum.Forms;
using Gum.Forms.Controls;
using Gum.Input;
using Gum.Wireframe;
using MonoGameGum.TestsCommon;
using Shouldly;
using Xunit.Abstractions;
using GumKeys = Gum.Forms.Input.Keys;

namespace SkiaGum.Tests.Performance;

/// <summary>
/// What a Skia <c>TextBox</c> allocates per caret move, click and keystroke (#7863): the caret and selection code
/// reads positions from the text's cached layout and its reused measuring block instead of building a throwaway
/// <c>TextBlock</c> per measurement, and an idle focused frame reads the keyboard without allocating. The typing
/// case still pays RichTextKit's own reshape of the whole text, which upstream pull requests shrink separately, so
/// its limit only guards Gum's share. Each case also checks its input reached the box, so none passes by doing
/// nothing.
/// </summary>
public class TextBoxCaretAllocationTests : BaseTestClass
{
    private readonly ITestOutputHelper _output;
    private readonly SkiaSceneHost _host = new SkiaSceneHost();
    private readonly FrameCursor _cursor = new FrameCursor();
    private readonly FrameKeyboard _keyboard = new FrameKeyboard();
    private readonly ICursor _previousCursor = FrameworkElement.MainCursor;
    private readonly IInputReceiverKeyboard _previousKeyboard = FrameworkElement.MainKeyboard;
    private double _inputSeconds;

    public TextBoxCaretAllocationTests(ITestOutputHelper output)
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

    private TextBox CreateBox(int paragraphs = 10)
    {
        TextBox textBox = new TextBox
        {
            X = 10,
            Y = 10,
            Width = 300,
            Height = 150,
            TextWrapping = TextWrapping.Wrap,
            AcceptsReturn = true,
            Text = BuildParagraphs(paragraphs),
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

    private double Measure(string label, Action<int> act, out int frames)
    {
        int count = 0;
        AllocationResult result = SkiaAllocationMeasurer.MeasureMinimum(
            frame =>
            {
                count++;
                act(frame);
                RunFrame();
            },
            warmupIterations: 60,
            measuredIterations: 300);
        frames = count;
        _output.WriteLine($"{label}: {result.BytesPerIteration:N0} bytes/frame");
        _host.CountPaintedSamples().ShouldBeGreaterThan(0);
        return result.BytesPerIteration;
    }

    [Fact]
    public void Click_PlacingCaretInWrappedBox_AllocationBaseline()
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
            },
            out _);

        carets.Count.ShouldBeGreaterThan(10);
        bytes.ShouldBeLessThanOrEqualTo(Limits.Click);
    }

    [Fact]
    public void CaretLeftRight_InWrappedBox_AllocationBaseline()
    {
        TextBox textBox = CreateBox();
        HashSet<int> carets = new HashSet<int>();
        double bytes = Measure("caret Left/Right",
            frame =>
            {
                carets.Add(textBox.CaretIndex);
                // Pressed on even frames, released on odd ones; ten moves one way, then ten back.
                bool pressed = frame % 2 == 0;
                GumKeys key = (frame / 20) % 2 == 0 ? GumKeys.Right : GumKeys.Left;
                _keyboard.SetKey(GumKeys.Left, pressed && key == GumKeys.Left);
                _keyboard.SetKey(GumKeys.Right, pressed && key == GumKeys.Right);
            },
            out _);

        carets.Count.ShouldBeGreaterThan(8);
        bytes.ShouldBeLessThanOrEqualTo(Limits.CaretMove);
    }

    [Fact]
    public void ShiftArrow_SelectingInWrappedBox_AllocationBaseline()
    {
        TextBox textBox = CreateBox();
        HashSet<int> lengths = new HashSet<int>();
        double bytes = Measure("shift+arrow",
            frame =>
            {
                lengths.Add(textBox.SelectionLength);
                bool pressed = frame % 2 == 0;
                GumKeys key = (frame / 20) % 2 == 0 ? GumKeys.Right : GumKeys.Left;
                _keyboard.SetKey(GumKeys.LeftShift, true);
                _keyboard.SetKey(GumKeys.Left, pressed && key == GumKeys.Left);
                _keyboard.SetKey(GumKeys.Right, pressed && key == GumKeys.Right);
            },
            out _);

        lengths.Count.ShouldBeGreaterThan(5);
        bytes.ShouldBeLessThanOrEqualTo(Limits.ShiftArrow);
    }

    [Fact]
    public void TypeOneCharacter_InWrappedBox_AllocationBaseline()
    {
        TextBox textBox = CreateBox();
        int grew = 0;
        int shrank = 0;
        int previous = textBox.Text!.Length;
        double bytes = Measure("type one char / backspace",
            frame =>
            {
                int length = textBox.Text!.Length;
                grew += length > previous ? 1 : 0;
                shrank += length < previous ? 1 : 0;
                previous = length;
                if (frame % 2 == 0)
                {
                    _keyboard.Type("a");
                    _keyboard.SetKey(GumKeys.Back, held: false);
                }
                else
                {
                    _keyboard.SetKey(GumKeys.Back, held: true);
                }
            },
            out _);

        grew.ShouldBeGreaterThan(0);
        shrank.ShouldBeGreaterThan(0);
        bytes.ShouldBeLessThanOrEqualTo(Limits.Type);
    }

    [Fact]
    public void FocusedIdleFrame_InWrappedBox_AllocatesOnlyTheFocusPreviewArguments()
    {
        TextBox textBox = CreateBox();
        double bytes = Measure("focused idle", _ => { }, out _);

        textBox.IsFocused.ShouldBeTrue();
        // The one RoutedEventArgs (24 bytes) the focus preview raises per receiver each frame, in InteractiveGue;
        // the keyboard read itself allocates nothing (it was 248 bytes with the delegate and iterator).
        bytes.ShouldBeLessThanOrEqualTo(48d);
    }

    // Bytes per frame; set from the measurements in the pull request, with headroom.
    private static class Limits
    {
        public const int Click = 24000;
        public const int CaretMove = 2000;
        public const int ShiftArrow = 4000;
        public const int Type = 55000;
    }
}
