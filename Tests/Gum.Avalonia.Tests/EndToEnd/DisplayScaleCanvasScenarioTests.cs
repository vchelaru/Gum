using Avalonia;
using Avalonia.Headless;
using Avalonia.Input;
using Gum.Avalonia.Canvas;
using Gum.Avalonia.Tests.TextureCoordinates;
using Gum.DataTypes;
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
            try
            {
                Point first = canvas.WindowPointOf(300, 300);

                bool isDoubleClick = ClickTwice(canvas.Input.Window, first, new Point(first.X + 8, first.Y), canvas.Frame,
                    () => InputLibrary.Cursor.Self.PrimaryDoubleClick);

                isDoubleClick.ShouldBeTrue(canvas.Describe());
            }
            finally
            {
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
            try
            {
                Point first = tab.WindowPointOf(200, 200);

                bool isDoubleClick = ClickTwice(tab.Input.Window, first, new Point(first.X + 8, first.Y), tab.Frame,
                    () => tab.Canvas.XnaCursor.PrimaryDoubleClick);

                isDoubleClick.ShouldBeTrue(tab.Describe());
            }
            finally
            {
                tab.CanvasControl.DisplayScaleOverride = null;
                tab.Frame();
            }
        });
    }

    // Two left clicks with as few frames as possible between them, since the double-click time
    // is measured on the real clock. Returns whether the cursor read a double click on the second.
    private static bool ClickTwice(global::Avalonia.Controls.Window window, Point first, Point second, Action frame, Func<bool> isDoubleClick)
    {
        window.MouseMove(first, RawInputModifiers.None);
        frame();
        window.MouseDown(first, MouseButton.Left, RawInputModifiers.None);
        frame();
        window.MouseUp(first, MouseButton.Left, RawInputModifiers.None);
        frame();
        window.MouseDown(second, MouseButton.Left, RawInputModifiers.None);
        frame();
        window.MouseUp(second, MouseButton.Left, RawInputModifiers.None);
        frame();
        return isDoubleClick();
    }
}
