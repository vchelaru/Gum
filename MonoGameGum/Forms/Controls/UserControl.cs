using Gum.Wireframe;

#if FRB
using InteractiveGue = global::Gum.Wireframe.GraphicalUiElement;
namespace FlatRedBall.Forms.Controls;
#else
namespace MonoGameGum.Forms.Controls;
#endif

public class UserControl : FrameworkElement
{
    public UserControl() : base() { }

    public UserControl(InteractiveGue visual) : base(visual) { }
}
