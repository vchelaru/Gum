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

    /// <summary>Sets the canvas and re-lays out what hangs off the root, so percent-based and docked layouts follow.</summary>
    public static void ApplyToCanvas(GraphicalUiElement root, int width, int height)
    {
        GraphicalUiElement.CanvasWidth = width;
        GraphicalUiElement.CanvasHeight = height;
        root.UpdateLayout();
    }
}
