using Gum.Input;
using Shouldly;
using System.Windows.Controls;

namespace GumToolUnitTests.Input;

/// <summary>
/// Pins the opt-out that keeps MainWindow's app-wide Ctrl+=/Ctrl+- font zoom from claiming those
/// keys while a render canvas that zooms its own camera has focus.
/// </summary>
public class CameraZoomScopeTests
{
    [StaFact]
    public void IsEntireAppZoomEnabledFor_ShouldBeFalse_ForAnElementMarkedAsOwningCameraZoom()
    {
        Grid canvas = new Grid();
        CameraZoomScope.SetOwnsCameraZoom(canvas, true);

        CameraZoomScope.IsEntireAppZoomEnabledFor(canvas).ShouldBeFalse();
    }

    [StaFact]
    public void IsEntireAppZoomEnabledFor_ShouldBeFalse_ForAChildOfAnElementMarkedAsOwningCameraZoom()
    {
        Grid canvas = new Grid();
        CameraZoomScope.SetOwnsCameraZoom(canvas, true);
        Button child = new Button();
        canvas.Children.Add(child);

        CameraZoomScope.IsEntireAppZoomEnabledFor(child).ShouldBeFalse();
    }

    [StaFact]
    public void IsEntireAppZoomEnabledFor_ShouldBeTrue_ForAnUnmarkedElement()
    {
        CameraZoomScope.IsEntireAppZoomEnabledFor(new Grid()).ShouldBeTrue();
    }

    [Fact]
    public void IsEntireAppZoomEnabledFor_ShouldBeTrue_ForANonDependencyObjectSource()
    {
        CameraZoomScope.IsEntireAppZoomEnabledFor(null).ShouldBeTrue();
    }
}
