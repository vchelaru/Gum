#if FRB
namespace FlatRedBall.Forms;
#else
namespace Gum.Forms;
#endif

/// <summary>
/// Values for an element's resize behavior.
/// </summary>
public enum ResizeMode
{
    /// <summary>
    /// Resizing using the cursor is not enabled
    /// </summary>
    NoResize,
    /// <summary>
    /// Resizing is enabled according to the enabled border instances.
    /// </summary>
    CanResize
}
