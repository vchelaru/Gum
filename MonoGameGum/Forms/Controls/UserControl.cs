using Gum.Wireframe;

#if FRB
using InteractiveGue = global::Gum.Wireframe.GraphicalUiElement;
namespace FlatRedBall.Forms.Controls;
#endif

#if !FRB
namespace Gum.Forms.Controls;

#endif

public class UserControl :
#if RAYLIB || FRB
    FrameworkElement
#else
    Gum.Forms.Controls.FrameworkElement
#endif

{
    public UserControl() : base() { }

    public UserControl(InteractiveGue visual) : base(visual) { }
}
