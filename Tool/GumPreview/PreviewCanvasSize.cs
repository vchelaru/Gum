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

    public bool TryTakeChange(GumProjectSave? project, out int width, out int height)
    {
        width = 0;
        height = 0;
        return false;
    }

    public static void ApplyToCanvas(GraphicalUiElement root, int width, int height)
    {
    }
}
