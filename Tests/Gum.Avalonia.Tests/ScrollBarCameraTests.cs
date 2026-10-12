using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using Gum.Avalonia.Plugins.EditorTab;
using Gum.Controls;
using Gum.Plugins.InternalPlugins.EditorTab.Services;
using RenderingLibrary;
using Shouldly;

namespace Gum.Avalonia.Tests;

/// <summary>
/// The canvas scroll bars over the head's real <see cref="AvaloniaScrollBarAdapter"/>, which applies
/// each range property to the bar as it is set.
/// </summary>
public class ScrollBarCameraTests
{
    private sealed class FixedSurface : IScrollSurface
    {
        public int Width { get; init; }
        public int Height { get; init; }

        public event EventHandler? SizeChanged
        {
            add { }
            remove { }
        }
    }

    [AvaloniaFact]
    public void ZoomingIn_KeepsTheCameraWhereItWasPlaced()
    {
        // #5981: the focus hotkey jumps from 100% to a high zoom and places the camera in one move.
        AvaloniaScrollBarAdapter horizontal = new AvaloniaScrollBarAdapter(new ScrollBar { Orientation = global::Avalonia.Layout.Orientation.Horizontal, Width = 300 });
        AvaloniaScrollBarAdapter vertical = new AvaloniaScrollBarAdapter(new ScrollBar { Orientation = global::Avalonia.Layout.Orientation.Vertical, Height = 300 });
        global::Avalonia.Controls.Window window = new global::Avalonia.Controls.Window
        {
            Content = new global::Avalonia.Controls.StackPanel { Children = { horizontal.Element, vertical.Element } },
        };
        // A bar only clamps its value to the range once it is part of a shown window.
        window.Show();
        ScrollBarControlLogic logic = new ScrollBarControlLogic(horizontal, vertical, new FixedSurface { Width = 992, Height = 766 });
        Camera camera = new Camera { Zoom = 1, X = -30, Y = -30 };
        logic.Camera = camera;
        logic.SetDisplayedArea(800, 600);

        camera.Zoom = 16;
        camera.X = 289;
        camera.Y = 186;
        logic.UpdateScrollBars();
        logic.UpdateScrollBarsToCameraPosition();

        camera.X.ShouldBe(289);
        camera.Y.ShouldBe(186);
        window.Close();
    }
}
