using Gum.Forms;
using Gum.Input;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using Moq;
using System;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// Shared arrangement for the ToolTipService test classes: a mock cursor that reports whichever
/// visual the test sets as <c>VisualOver</c> / <c>WindowPushed</c>, and a clock that ticks the
/// real <c>GumService.Update</c> (which calls <c>ToolTipService.Update</c>).
/// </summary>
internal static class ToolTipTestHarness
{
    /// <summary>The cursor position the mock reports, in gum units.</summary>
    public const float CursorX = 100;
    public const float CursorY = 100;

    public static Mock<ICursor> SetupCursor()
    {
        Mock<ICursor> cursor = new();
        cursor.Setup(x => x.LastInputDevice).Returns(InputDevice.Mouse);
        cursor.SetupProperty(x => x.VisualOver);
        cursor.SetupProperty(x => x.WindowPushed);
        cursor.Setup(x => x.X).Returns((int)CursorX);
        cursor.Setup(x => x.Y).Returns((int)CursorY);
        cursor.Setup(x => x.XRespectingGumZoomAndBounds()).Returns(CursorX);
        cursor.Setup(x => x.YRespectingGumZoomAndBounds()).Returns(CursorY);
        FormsUtilities.SetCursor(cursor.Object);
        return cursor;
    }

    public static void TickTo(double totalSeconds)
    {
        GameTime gameTime = new GameTime(
            totalGameTime: TimeSpan.FromSeconds(totalSeconds),
            elapsedGameTime: TimeSpan.FromMilliseconds(16));
        Gum.GumService.Default.Update(gameTime);
    }

    /// <summary>
    /// Reports <paramref name="visual"/> as the visual under the cursor. A fixed return value
    /// (not a settable property) so the per-frame cursor activity cannot write its own hit test over it.
    /// </summary>
    public static void Over(Mock<ICursor> cursor, InteractiveGue? visual)
    {
        cursor.Setup(x => x.VisualOver).Returns(visual);
    }

    public static void Pushed(Mock<ICursor> cursor, InteractiveGue? visual)
    {
        cursor.Setup(x => x.WindowPushed).Returns(visual);
    }

    /// <summary>Hovers <paramref name="visual"/>, then ticks past the initial delay so its tooltip opens.</summary>
    public static void HoverUntilOpen(Mock<ICursor> cursor, InteractiveGue visual, double startSeconds = 0.0)
    {
        ToolTipTestHarness.Over(cursor, visual);
        TickTo(startSeconds);
        TickTo(startSeconds + 0.6);
    }
}
