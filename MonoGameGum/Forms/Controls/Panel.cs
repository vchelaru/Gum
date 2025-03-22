using Gum.Wireframe;
using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#if FRB
using InteractiveGue = global::Gum.Wireframe.GraphicalUiElement;
using FlatRedBall.Forms.Controls;
namespace FlatRedBall.Forms.Controls;
#else
namespace MonoGameGum.Forms.Controls;
#endif

public class Panel : FrameworkElement
{
    public Panel() :
        base(new InteractiveGue(new InvisibleRenderable()))
    {
        
        IsVisible = true;

    }

    protected override void ReactToVisualChanged()
    {
        Visual.ExposeChildrenEvents = true;

        base.ReactToVisualChanged();
    }
}
