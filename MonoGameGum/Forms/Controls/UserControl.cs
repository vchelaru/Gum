using Gum.Wireframe;

#if FRB
using InteractiveGue = global::Gum.Wireframe.GraphicalUiElement;
namespace FlatRedBall.Forms.Controls;
#endif

#if !FRB
namespace Gum.Forms.Controls;

#endif

public class UserControl :
#if FRB
    FrameworkElement
#else
    Gum.Forms.Controls.FrameworkElement
#endif

{
    public UserControl() : base() { }

#if !FRB
    /// <summary>
    /// Creates a new UserControl using the default visual registered for <paramref name="service"/>, without
    /// having to make that service current first. The service that was current is restored afterward.
    /// </summary>
    public UserControl(global::RenderingLibrary.IGumService service) : base(service) { }
#endif

    public UserControl(InteractiveGue visual) : base(visual) { }
}
