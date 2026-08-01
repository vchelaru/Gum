using System;
using Gum.Controls;
using Gum.Plugins.InternalPlugins.EditorTab.Services;
using RenderingLibrary;
using Shouldly;

namespace Gum.Presentation.Tests;

/// <summary>
/// Pins <see cref="ScrollBarControlLogic"/>'s range/camera math after its relocation to
/// Gum.Presentation (ADR-0005, part of #3833) — it previously drove WinForms
/// <c>ThemedScrollBar</c>s docked into a WinForms <c>Panel</c> and sized itself from a WinForms
/// <c>Control</c>, which is what blocked both the move and a WPF-native scroll bar.
/// </summary>
public class ScrollBarControlLogicTests
{
    private sealed class FakeScrollBar : IScrollBar
    {
        private int _value;

        public int Minimum { get; set; }
        public int Maximum { get; set; }
        public int LargeChange { get; set; }

        public int Value
        {
            get => _value;
            set
            {
                if (_value == value)
                {
                    return;
                }
                _value = value;
                ValueChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public event EventHandler? ValueChanged;
    }

    private sealed class FakeScrollSurface : IScrollSurface
    {
        public int Width { get; set; }
        public int Height { get; set; }

        public event EventHandler? SizeChanged;

        public void RaiseSizeChanged() => SizeChanged?.Invoke(this, EventArgs.Empty);
    }

    private readonly FakeScrollBar _horizontal = new();
    private readonly FakeScrollBar _vertical = new();
    private readonly FakeScrollSurface _surface = new() { Width = 800, Height = 600 };
    private readonly ScrollBarControlLogic _logic;

    public ScrollBarControlLogicTests()
    {
        _logic = new ScrollBarControlLogic(_horizontal, _vertical, _surface);
    }

    [Fact]
    public void SetDisplayedArea_SizesBothBarsFromTheAreaAndTheSurface()
    {
        Camera camera = new Camera { Zoom = 1 };
        _logic.Camera = camera;

        _logic.SetDisplayedArea(800, 600);

        // The area is centered on the origin, so it starts half its size to the left/above, and the
        // bar can scroll a further screenful past its far edge.
        _horizontal.Minimum.ShouldBe(-400);
        _horizontal.Maximum.ShouldBe(1600);
        _horizontal.LargeChange.ShouldBe(800);

        _vertical.Minimum.ShouldBe(-300);
        _vertical.Maximum.ShouldBe(1200);
        _vertical.LargeChange.ShouldBe(600);
    }

    [Fact]
    public void UpdateScrollBars_ClampsBarValueToThePreviousRangeButLeavesTheCameraAlone()
    {
        Camera camera = new Camera { Zoom = 1, X = 5000, Y = 5000 };
        _logic.Camera = camera;
        _horizontal.Minimum = 0;
        _horizontal.Maximum = 100;
        _vertical.Minimum = 0;
        _vertical.Maximum = 100;

        _logic.UpdateScrollBars();

        _horizontal.Value.ShouldBe(100);
        _vertical.Value.ShouldBe(100);
        // The user is allowed to pan past the scrollable area, so the camera keeps its position even
        // though the bars clamped.
        camera.X.ShouldBe(5000);
        camera.Y.ShouldBe(5000);
    }

    [Fact]
    public void UpdateScrollBars_DoesNothingWhileTheCameraIsUnassigned()
    {
        _logic.UpdateScrollBars();
        // A bar can also be moved before the rendering surface has handed over its camera.
        _horizontal.Value = 42;
        _vertical.Value = 24;

        _horizontal.Maximum.ShouldBe(0);
        _vertical.Maximum.ShouldBe(0);
    }

    [Fact]
    public void UpdateScrollBars_ScalesTheVisibleAreaByCameraZoom()
    {
        Camera camera = new Camera { Zoom = 2 };
        _logic.Camera = camera;

        _logic.SetDisplayedArea(800, 600);

        // 800 / 600 surface pixels cover half as much world space at 2x zoom.
        _horizontal.LargeChange.ShouldBe(400);
        _horizontal.Maximum.ShouldBe(1200);
        _vertical.LargeChange.ShouldBe(300);
        _vertical.Maximum.ShouldBe(900);
    }

    [Fact]
    public void SurfaceSizeChanged_ResizesTheBars()
    {
        Camera camera = new Camera { Zoom = 1 };
        _logic.Camera = camera;
        _logic.SetDisplayedArea(800, 600);

        _surface.Width = 1000;
        _surface.Height = 900;
        _surface.RaiseSizeChanged();

        _horizontal.LargeChange.ShouldBe(1000);
        _vertical.LargeChange.ShouldBe(900);
    }

    [Fact]
    public void ScrollBarValueChanged_MovesTheCamera()
    {
        Camera camera = new Camera { Zoom = 1 };
        _logic.Camera = camera;

        _horizontal.Value = 42;
        _vertical.Value = 24;

        camera.X.ShouldBe(42);
        camera.Y.ShouldBe(24);
    }

    [Fact]
    public void UpdateScrollBarsToCameraPosition_ClampsTheBarsToTheirOwnRange()
    {
        Camera camera = new Camera { Zoom = 1 };
        _logic.Camera = camera;
        _logic.SetDisplayedArea(800, 600);
        camera.X = 99999;
        camera.Y = -99999;

        _logic.UpdateScrollBarsToCameraPosition();

        _horizontal.Value.ShouldBe(_horizontal.Maximum);
        _vertical.Value.ShouldBe(_vertical.Minimum);
    }

    [Fact]
    public void UpdateScrollBarsToCameraPosition_PreservesTheCamerasFractionalPositionWithinRange()
    {
        Camera camera = new Camera { Zoom = 2, X = 100.75f, Y = 50.25f };
        _logic.Camera = camera;
        _logic.SetDisplayedArea(800, 600);

        _logic.UpdateScrollBarsToCameraPosition();

        // Setting the (int) bar value round-trips through ValueChanged back into the camera - the
        // same feedback UpdateScrollBars() guards against by restoring X/Y afterward. This method
        // must guard it the same way, or every mouse-pan tick truncates sub-pixel camera position.
        camera.X.ShouldBe(100.75f);
        camera.Y.ShouldBe(50.25f);
    }
}
