using Avalonia;
using Avalonia.Headless;
using Avalonia.Input;
using Gum.Avalonia.Canvas;
using Gum.Avalonia.Tests.Harness;
using Gum.Avalonia.Tests.TextureCoordinates;
using Gum.DataTypes;
using Gum.Wireframe;
using Shouldly;

namespace Gum.Avalonia.Tests.EndToEnd;

/// <summary>
/// Each canvas's cursor takes that canvas's display scale (#5554): at 200% the double-click
/// distance is 8 physical pixels, so a second click 8 pixels from the first is a double click.
/// The canvas host's <see cref="AvaloniaGraphicsDeviceControl.DisplayScaleOverride"/> stands in for
/// a 200% monitor.
/// </summary>
[Trait("Category", "EndToEnd")]
public class DisplayScaleCanvasScenarioTests
{
    [SkippableFact]
    public void EditorCanvas_At200Percent_ASecondClick8PixelsAway_IsADoubleClick()
    {
        Skip.IfNot(CanvasHarness.CanRun, CanvasHarness.SkipReason);
        CanvasHarness.OnUiThread(() =>
        {
            using CanvasHarness canvas = new CanvasHarness();
            ComponentSave button = canvas.Project.AddComponent("Button");
            canvas.Tree.Click(canvas.Tree.NodeFor(button));
            canvas.Canvas.DisplayScaleOverride = 2;
            ManualTimeProvider time = new ManualTimeProvider();
            TimeManager.Self.SetTimeProvider(time);
            try
            {
                Point first = canvas.WindowPointOf(300, 300);

                List<string> trace = new List<string>();
                bool isDoubleClick = ClickTwice(canvas.Input.Window, first, new Point(first.X + 8, first.Y), canvas.Frame,
                    time, TimeSpan.FromMilliseconds(100), () => InputLibrary.Cursor.Self.PrimaryDoubleClick,
                    () =>
                    {
                        InputLibrary.Cursor cursor = InputLibrary.Cursor.Self;
                        trace.Add($"({cursor.X}, {cursor.Y}) click {cursor.PrimaryClick} double {cursor.PrimaryDoubleClick}");
                    });

                isDoubleClick.ShouldBeTrue(canvas.Describe() + "; per frame: " + string.Join(" | ", trace));
            }
            finally
            {
                TimeManager.Self.SetTimeProvider(TimeProvider.System);
                canvas.Canvas.DisplayScaleOverride = null;
                canvas.Frame();
            }
        });
    }

    [SkippableFact]
    public void TextureCoordinatesCanvas_At200Percent_ASecondClick8PixelsAway_IsADoubleClick()
    {
        Skip.IfNot(TextureCoordinateTabHarness.CanRun, TextureCoordinateTabHarness.SkipReason);
        TextureCoordinateTabHarness.OnUiThread(() =>
        {
            using TextureCoordinateTabHarness tab = new TextureCoordinateTabHarness();
            ComponentSave button = tab.Project.AddComponent("Button");
            string atlas = tab.AddTextureFile("Atlas.png");
            InstanceSave icon = tab.AddSprite(button, "Icon", atlas, left: 32, top: 32, width: 64, height: 64);
            tab.Select(icon);
            tab.CanvasControl.DisplayScaleOverride = 2;
            ManualTimeProvider time = new ManualTimeProvider();
            tab.Canvas.TimeManager.SetTimeProvider(time);
            try
            {
                Point first = tab.WindowPointOf(200, 200);

                bool isDoubleClick = ClickTwice(tab.Input.Window, first, new Point(first.X + 8, first.Y), tab.Frame,
                    time, TimeSpan.FromMilliseconds(100), () => tab.Canvas.XnaCursor.PrimaryDoubleClick);

                isDoubleClick.ShouldBeTrue(tab.Describe());
            }
            finally
            {
                tab.CanvasControl.DisplayScaleOverride = null;
                tab.Frame();
            }
        });
    }

    [SkippableFact]
    public void TextureCoordinatesCanvas_ASecondClickAfterTheDoubleClickWindow_IsNotADoubleClick()
    {
        Skip.IfNot(TextureCoordinateTabHarness.CanRun, TextureCoordinateTabHarness.SkipReason);
        TextureCoordinateTabHarness.OnUiThread(() =>
        {
            using TextureCoordinateTabHarness tab = new TextureCoordinateTabHarness();
            ComponentSave button = tab.Project.AddComponent("Button");
            string atlas = tab.AddTextureFile("Atlas.png");
            InstanceSave icon = tab.AddSprite(button, "Icon", atlas, left: 32, top: 32, width: 64, height: 64);
            tab.Select(icon);
            tab.CanvasControl.DisplayScaleOverride = 2;
            ManualTimeProvider time = new ManualTimeProvider();
            tab.Canvas.TimeManager.SetTimeProvider(time);
            try
            {
                Point first = tab.WindowPointOf(200, 200);

                bool isDoubleClick = ClickTwice(tab.Input.Window, first, first, tab.Frame,
                    time, TimeSpan.FromMilliseconds(500), () => tab.Canvas.XnaCursor.PrimaryDoubleClick);

                isDoubleClick.ShouldBeFalse(tab.Describe());
            }
            finally
            {
                tab.CanvasControl.DisplayScaleOverride = null;
                tab.Frame();
            }
        });
    }

    // Two left clicks a fixed, known time apart on a manual clock, so a slow runner cannot push
    // the second click outside the double-click window. Returns whether the cursor read a double
    // click on the second.
    private static bool ClickTwice(global::Avalonia.Controls.Window window, Point first, Point second, Action frame,
        ManualTimeProvider time, TimeSpan between, Func<bool> isDoubleClick, Action? afterFrame = null)
    {
        void Step()
        {
            frame();
            afterFrame?.Invoke();
        }

        window.MouseMove(first, RawInputModifiers.None);
        Step();
        window.MouseDown(first, MouseButton.Left, RawInputModifiers.None);
        Step();
        window.MouseUp(first, MouseButton.Left, RawInputModifiers.None);
        Step();
        time.Advance(between);
        window.MouseDown(second, MouseButton.Left, RawInputModifiers.None);
        Step();
        window.MouseUp(second, MouseButton.Left, RawInputModifiers.None);
        Step();
        return isDoubleClick();
    }
}
