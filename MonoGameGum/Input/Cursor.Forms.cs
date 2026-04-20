#if MONOGAME || KNI || XNA4 || FNA
#define XNALIKE
#endif

using Gum.Forms.Controls;
using Gum.Wireframe;

#if XNALIKE
namespace MonoGameGum.Input;
#else
namespace Gum.Input;
#endif

public partial class Cursor
{
    /// <summary>
    /// Gets the control that was under the cursor when the cursor (left button) was pushed.
    /// </summary>
    public FrameworkElement? FrameworkElementPushed => VisualPushed?.FormsControlAsObject as FrameworkElement;

    /// <summary>
    /// Gets the control that was under the cursor when the cursor right button was pushed.
    /// </summary>
    public FrameworkElement? FrameworkElementRightPushed => VisualRightPushed?.FormsControlAsObject as FrameworkElement;

    /// <summary>
    /// Gets the control that is currently under the cursor.
    /// </summary>
    public FrameworkElement? FrameworkElementOver => VisualOver?.FormsControlAsObject as FrameworkElement;
}
