#if FRB
namespace FlatRedBall.Forms.Controls;
#endif

#if !FRB
namespace Gum.Forms.Controls;
#endif

public enum ScrollBarVisibility
{
    /// <summary>
    /// The ScrollBar displays only if needed based on the size of the inner panel
    /// </summary>
    Auto = 1,
    /// <summary>
    /// The ScrollBar remains invisible even if the contents of the inner panel exceed the size of its container
    /// </summary>
    Hidden = 2,
    /// <summary>
    /// The ScrollBar always displays
    /// </summary>
    Visible = 3
}
