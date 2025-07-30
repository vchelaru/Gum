using Gum.Wireframe;

#if FRB
using InteractiveGue = global::Gum.Wireframe.GraphicalUiElement;
namespace FlatRedBall.Forms.Controls;
#endif

#if !FRB
namespace Gum.Forms.Controls;

#endif

public class UserControl : MonoGameGum.Forms.Controls.FrameworkElement
{
    public UserControl() : base() { }

    public UserControl(InteractiveGue visual) : base(visual) { }
}
