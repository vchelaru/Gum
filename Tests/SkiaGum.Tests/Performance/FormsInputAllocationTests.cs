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
/// Each case also checks that its input reached the control, so a case cannot pass by doing nothing.
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

    // What a case does to the input on a given frame, and the check, run after measuring, that the input
    // reached the control.
    private sealed record InputCase(Action<int> Act, Action AssertInputLanded);

    private InputCase CreateCase(string kind)
    {
        StackPanel panel = new StackPanel { X = 10, Y = 10, Spacing = 4 };
        panel.AddToRoot();

        switch (kind)
        {
            case "HoverButtonStill":
            {
                Button button = new Button { Text = "Play" };
                panel.AddChild(button);
                int rollOn = 0;
                button.Visual.RollOn += (_, _) => rollOn++;
                return new InputCase(
                    _ => _cursor.MoveToCenterOf(button.Visual),
                    () => rollOn.ShouldBeGreaterThan(0));
            }
            case "CursorStillOffControls":
            {
                Button button = new Button { Text = "Play" };
                panel.AddChild(button);
                int rollOn = 0;
                button.Visual.RollOn += (_, _) => rollOn++;
                // The cursor sits away from the button, so it must never roll on.
                return new InputCase(
                    _ =>
                    {
                        _cursor.X = 600;
                        _cursor.Y = 460;
                    },
                    () => rollOn.ShouldBe(0));
            }
            case "HoverButtonEnterExit":
            {
                Button button = new Button { Text = "Play" };
                panel.AddChild(button);
                int rollOn = 0;
                int rollOff = 0;
                button.Visual.RollOn += (_, _) => rollOn++;
                button.Visual.RollOff += (_, _) => rollOff++;
                return new InputCase(
                    frame =>
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
                    },
                    () =>
                    {
                        rollOn.ShouldBeGreaterThan(0);
                        rollOff.ShouldBeGreaterThan(0);
                    });
            }
            case "PressReleaseButton":
            {
                Button button = new Button { Text = "Play" };
                panel.AddChild(button);
                int push = 0;
                int click = 0;
                button.Push += (_, _) => push++;
                button.Click += (_, _) => click++;
                return new InputCase(
                    frame =>
                    {
                        _cursor.MoveToCenterOf(button.Visual);
                        _cursor.PrimaryHeld = frame % 2 == 0;
                    },
                    () =>
                    {
                        push.ShouldBeGreaterThan(0);
                        click.ShouldBeGreaterThan(0);
                    });
            }
            case "ScrollListBox":
            {
                ListBox listBox = new ListBox { Height = 200 };
                for (int i = 0; i < 30; i++)
                {
                    listBox.Items!.Add("Save Slot " + i);
                }
                panel.AddChild(listBox);
                double maxScroll = 0;
                return new InputCase(
                    frame =>
                    {
                        maxScroll = Math.Max(maxScroll, listBox.VerticalScrollBarValue);
                        _cursor.MoveToCenterOf(listBox.Visual);
                        _cursor.Wheel += frame % 2 == 0 ? -120 : 120;
                    },
                    () => maxScroll.ShouldBeGreaterThan(0));
            }
            case "TypeAndDeleteInTextBox":
            {
                TextBox textBox = new TextBox { Text = "Player One" };
                panel.AddChild(textBox);
                textBox.IsFocused = true;
                int previousLength = textBox.Text!.Length;
                int grew = 0;
                int shrank = 0;
                return new InputCase(
                    frame =>
                    {
                        int length = textBox.Text!.Length;
                        if (length > previousLength)
                        {
                            grew++;
                        }
                        else if (length < previousLength)
                        {
                            shrank++;
                        }
                        previousLength = length;

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
                    () =>
                    {
                        grew.ShouldBeGreaterThan(0);
                        shrank.ShouldBeGreaterThan(0);
                    });
            }
            case "OpenCloseComboBox":
            {
                ComboBox comboBox = new ComboBox();
                foreach (string item in new[] { "Easy", "Normal", "Hard", "Insane" })
                {
                    comboBox.Items!.Add(item);
                }
                panel.AddChild(comboBox);
                bool everOpen = false;
                // A click is a push frame then a release frame, so the box toggles every second frame.
                return new InputCase(
                    frame =>
                    {
                        everOpen |= comboBox.IsDropDownOpen;
                        _cursor.MoveToCenterOf(comboBox.Visual);
                        _cursor.PrimaryHeld = frame % 2 == 0;
                    },
                    () => everOpen.ShouldBeTrue());
            }
            default:
                throw new ArgumentException("Unknown kind " + kind);
        }
    }

    // An idle Forms frame (cursor still, no keys) must allocate nothing: every Forms game pays it.
    [Theory]
    [InlineData("HoverButtonStill")]
    [InlineData("CursorStillOffControls")]
    public void UpdateAndDraw_IdleFormsFrame_ShouldNotAllocate(string kind)
    {
        InputCase inputCase = CreateCase(kind);

        AllocationResult result = AllocationMeasurer.MeasureMinimum(
            () =>
            {
                inputCase.Act(0);
                RunFrame();
            },
            warmupIterations: 50,
            measuredIterations: 300);

        _output.WriteLine($"{kind}: {result.BytesPerIteration:N0} bytes/frame");

        _host.CountPaintedSamples().ShouldBeGreaterThan(0);
        inputCase.AssertInputLanded();
        result.BytesPerIteration.ShouldBe(0d);
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
        InputCase inputCase = CreateCase(kind);
        int frame = 0;

        AllocationResult result = AllocationMeasurer.MeasureMinimum(
            () =>
            {
                frame++;
                inputCase.Act(frame);
                RunFrame();
            },
            warmupIterations: 50,
            measuredIterations: 300);

        _output.WriteLine($"{kind}: {result.BytesPerIteration:N0} bytes/frame");

        _host.CountPaintedSamples().ShouldBeGreaterThan(0);
        inputCase.AssertInputLanded();
        result.BytesPerIteration.ShouldBeLessThanOrEqualTo(limitBytesPerFrame);
    }
}
