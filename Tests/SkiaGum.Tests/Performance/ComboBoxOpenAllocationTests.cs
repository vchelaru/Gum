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
/// Managed allocation of opening and closing a <see cref="ComboBox"/> drop-down on the Skia runtime (#7870):
/// open it and run one frame, close it and run one frame, over and over once the popup exists. Opening used
/// to take the list box out of the tree, lay it out attached to the ComboBox, detach it, change its size
/// and units one setter at a time and lay out its rows after each (about 25 KB per open); the rows are
/// the same strings each time, so what is left is the part that cannot be avoided.
/// </summary>
public class ComboBoxOpenAllocationTests : BaseTestClass
{
    // Open: the setter plus the frame after it. Close: the same.
    private const int OpenLimitBytes = 3500;
    private const int CloseLimitBytes = 1000;

    private readonly ITestOutputHelper _output;
    private readonly SkiaSceneHost _host = new SkiaSceneHost();
    private readonly FrameCursor _cursor = new FrameCursor();
    private readonly FrameKeyboard _keyboard = new FrameKeyboard();
    private readonly ICursor _previousCursor = FrameworkElement.MainCursor;
    private readonly IInputReceiverKeyboard _previousKeyboard = FrameworkElement.MainKeyboard;
    private double _seconds;

    public ComboBoxOpenAllocationTests(ITestOutputHelper output)
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

    private void RunFrame()
    {
        _seconds += 1.0 / 60.0;
        FormsUtilities.Update(_seconds, GumService.Default.Root);
        _host.Update();
        _host.Draw();
    }

    private static ComboBox CreateComboBox(int itemCount, StackPanel panel)
    {
        ComboBox comboBox = new ComboBox();
        for (int i = 0; i < itemCount; i++)
        {
            comboBox.Items!.Add(new[] { "Easy", "Normal", "Hard", "Insane", "Nightmare", "Custom" }[i]);
        }
        panel.AddChild(comboBox);
        return comboBox;
    }

    private (long OpenBytes, long CloseBytes, int Opened) MeasureCycles(ComboBox comboBox)
    {
        const int warmup = 50;
        const int measured = 300;
        long openBest = long.MaxValue;
        long closeBest = long.MaxValue;
        long open = 0;
        long close = 0;
        int opened = 0;
        comboBox.DropDownOpened += (_, _) => opened++;

        // Even frames open and run a frame, odd frames close and run a frame; each phase is read on its own.
        SkiaAllocationMeasurer.MeasureMinimum(
            frame =>
            {
                if (frame == warmup)
                {
                    open = 0;
                    close = 0;
                }

                long before = GC.GetAllocatedBytesForCurrentThread();
                comboBox.IsDropDownOpen = frame % 2 == 0;
                RunFrame();
                long used = GC.GetAllocatedBytesForCurrentThread() - before;

                if (frame >= warmup)
                {
                    if (frame % 2 == 0)
                    {
                        open += used;
                    }
                    else
                    {
                        close += used;
                    }
                }

                if (frame == warmup + measured - 1)
                {
                    openBest = Math.Min(openBest, open);
                    closeBest = Math.Min(closeBest, close);
                }
            },
            warmupIterations: warmup,
            measuredIterations: measured);

        return (openBest / (measured / 2), closeBest / (measured / 2), opened);
    }

    [Theory]
    [InlineData(4)]
    [InlineData(6)]
    public void OpenAndClose_ReopenedDropDown_AllocatesLittle(int itemCount)
    {
        StackPanel panel = new StackPanel { X = 10, Y = 10, Spacing = 4 };
        panel.AddToRoot();
        ComboBox comboBox = CreateComboBox(itemCount, panel);
        RunFrame();

        (long openBytes, long closeBytes, int opened) = MeasureCycles(comboBox);

        _output.WriteLine($"{itemCount} items: open+frame {openBytes:N0} bytes, close+frame {closeBytes:N0} bytes");

        // The drop-down really opened every time, and drew.
        opened.ShouldBeGreaterThan(100);
        comboBox.IsDropDownOpen.ShouldBeFalse();
        comboBox.IsDropDownOpen = true;
        RunFrame();
        _host.CountPaintedSamples().ShouldBeGreaterThan(0);
        comboBox.ListBox.Visual.AbsoluteHeight.ShouldBeGreaterThan(0);

        openBytes.ShouldBeLessThanOrEqualTo(OpenLimitBytes);
        closeBytes.ShouldBeLessThanOrEqualTo(CloseLimitBytes);
    }
}
