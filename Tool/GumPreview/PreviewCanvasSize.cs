using System;
using Gum.DataTypes;
using Gum.Wireframe;

namespace GumPreview;

/// <summary>
/// Decides when Preview applies the project's canvas size. A fresh launch sizes the window and the
/// canvas to the saved size; afterwards it is applied again only when a reload finds a different saved
/// size, never when it is unchanged, so a window the user resized is kept until the saved size changes.
/// </summary>
internal sealed class PreviewCanvasSize
{
    public const int FallbackWidth = 800;
    public const int FallbackHeight = 600;

    private (int Width, int Height)? _lastSaved;

    /// <summary>
    /// True when <paramref name="project"/>'s saved size should be applied: the first time it is
    /// looked at, or when it differs from the saved size last looked at. The window's own size is never
    /// compared, so a window the user resized is kept until the saved size changes.
    /// </summary>
    public bool TryTakeChange(GumProjectSave? project, out int width, out int height)
    {
        width = project != null && project.DefaultCanvasWidth > 0 ? project.DefaultCanvasWidth : FallbackWidth;
        height = project != null && project.DefaultCanvasHeight > 0 ? project.DefaultCanvasHeight : FallbackHeight;

        if (_lastSaved == (width, height))
        {
            return false;
        }
        _lastSaved = (width, height);
        return true;
    }

    /// <summary>
    /// The canvas size, in pixels, whose window fits a display that has room for
    /// <paramref name="maxPointWidth"/> by <paramref name="maxPointHeight"/> window points (one point
    /// covers <paramref name="pointScale"/> pixels). A fresh launch gets this from the OS on both axes;
    /// a window that is already showing is not clamped the same way, so the reload path asks for it.
    /// A non-positive maximum means the room is unknown and leaves that axis alone.
    /// </summary>
    public static (int Width, int Height) FitToDisplay(int width, int height, double pointScale, int maxPointWidth, int maxPointHeight)
    {
        return (FitAxis(width, pointScale, maxPointWidth), FitAxis(height, pointScale, maxPointHeight));
    }

    private static int FitAxis(int pixels, double pointScale, int maxPoints)
    {
        if (maxPoints <= 0 || Math.Ceiling(pixels / pointScale) <= maxPoints)
        {
            return pixels;
        }
        return (int)Math.Round(maxPoints * pointScale);
    }

    /// <summary>Sets the canvas and re-lays out what hangs off the root, so percent-based and docked layouts follow.</summary>
    public static void ApplyToCanvas(GraphicalUiElement root, int width, int height)
    {
        GraphicalUiElement.CanvasWidth = width;
        GraphicalUiElement.CanvasHeight = height;
        root.UpdateLayout();
    }
}
