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
/// Per-frame managed allocation of Forms input on the Skia runtime (issue #1934): the input pump, Update
/// and Draw while the cursor or keyboard acts on a control every frame. Each case repeats a two-frame
/// cycle so the measured frames are steady state. Zero is the target; a nonzero limit is a ratchet.
/// </summary>
public class FormsInputAllocationTests : BaseTestClass
{
    private readonly ITestOutputHelper _output;
    private readonly SkiaSceneHost _host = new SkiaSceneHost();
    private readonly FrameCursor _cursor = new FrameCursor();
    private readonly FrameKeyboard _keyboard = new FrameKeyboard();
    private readonly ICursor _previousCursor = FrameworkElement.MainCursor;
    private readonly IInputReceiverKeyboard _previousKeyboard = FrameworkElement.MainKeyboard;
    private double _inputSeconds;

    public FormsInputAllocationTests(ITestOutputHelper output)
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

    // One frame as an interactive Skia host runs it: the Forms input pump, then Update and Draw.
    private void RunFrame()
    {
        _inputSeconds += 1.0 / 60.0;
        FormsUtilities.Update(_inputSeconds, GumService.Default.Root);
        _host.Update();
        _host.Draw();
    }

    // Builds the scene for a case and returns what it does to the input on a given frame.
    private Action<int> CreateCase(string kind)
    {
        StackPanel panel = new StackPanel { X = 10, Y = 10, Spacing = 4 };
        panel.AddToRoot();

        switch (kind)
        {
            case "HoverButtonStill":
            {
                Button button = new Button { Text = "Play" };
                panel.AddChild(button);
                return _ => _cursor.MoveToCenterOf(button.Visual);
            }
            case "HoverButtonEnterExit":
            {
                Button button = new Button { Text = "Play" };
                panel.AddChild(button);
                return frame =>
                {
                    if (frame % 2 == 0)
                    {
                        _cursor.MoveToCenterOf(button.Visual);
                    }
                    else
                    {
                        _cursor.X = 600;
                        _cursor.Y = 460;
                    }
                };
            }
            case "PressReleaseButton":
            {
                Button button = new Button { Text = "Play" };
                panel.AddChild(button);
                return frame =>
                {
                    _cursor.MoveToCenterOf(button.Visual);
                    _cursor.PrimaryHeld = frame % 2 == 0;
                };
            }
            case "ScrollListBox":
            {
                ListBox listBox = new ListBox { Height = 200 };
                for (int i = 0; i < 30; i++)
                {
                    listBox.Items!.Add("Save Slot " + i);
                }
                panel.AddChild(listBox);
                return frame =>
                {
                    _cursor.MoveToCenterOf(listBox.Visual);
                    _cursor.Wheel += frame % 2 == 0 ? -120 : 120;
                };
            }
            case "TypeAndDeleteInTextBox":
            {
                TextBox textBox = new TextBox { Text = "Player One" };
                panel.AddChild(textBox);
                textBox.IsFocused = true;
                return frame =>
                {
                    if (frame % 2 == 0)
                    {
                        _keyboard.Type("a");
                        _keyboard.SetKey(GumKeys.Back, held: false);
                    }
                    else
                    {
                        _keyboard.SetKey(GumKeys.Back, held: true);
                    }
                };
            }
            case "OpenCloseComboBox":
            {
                ComboBox comboBox = new ComboBox();
                foreach (string item in new[] { "Easy", "Normal", "Hard", "Insane" })
                {
                    comboBox.Items!.Add(item);
                }
                panel.AddChild(comboBox);
                // A click is a push frame then a release frame, so the box toggles every second frame.
                return frame =>
                {
                    _cursor.MoveToCenterOf(comboBox.Visual);
                    _cursor.PrimaryHeld = frame % 2 == 0;
                };
            }
            default:
                throw new ArgumentException("Unknown kind " + kind);
        }
    }

    [Theory]
    [InlineData("HoverButtonStill", 100)]
    [InlineData("HoverButtonEnterExit", 450)]
    [InlineData("PressReleaseButton", 700)]
    [InlineData("ScrollListBox", 400)]
    [InlineData("TypeAndDeleteInTextBox", 28000)]
    [InlineData("OpenCloseComboBox", 38000)]
    public void UpdateAndDraw_FormsInput_AllocationBaseline(string kind, int limitBytesPerFrame)
    {
        Action<int> act = CreateCase(kind);
        int frame = 0;

        AllocationResult result = AllocationMeasurer.MeasureMinimum(
            () =>
            {
                frame++;
                act(frame);
                RunFrame();
            },
            warmupIterations: 50,
            measuredIterations: 300);

        _output.WriteLine($"{kind}: {result.BytesPerIteration:N0} bytes/frame");

        _host.CountPaintedSamples().ShouldBeGreaterThan(0);
        result.BytesPerIteration.ShouldBeLessThanOrEqualTo(limitBytesPerFrame);
    }
}
