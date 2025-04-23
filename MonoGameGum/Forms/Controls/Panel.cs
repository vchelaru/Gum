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
    List<FrameworkElement> _children = new List<FrameworkElement>();

    /// <summary>
    /// Returns a read-only list of the children FrameworkElements of this panel. 
    /// </summary>
    /// <remarks>
    /// This list is updated whenever the underlying Visual's Children change.
    /// This only contains FrameworkElements, so if a non-FrameworkElement Visual
    /// is added to this panel, it will not appear in this list.
    /// </remarks>
    public IReadOnlyList<FrameworkElement> Children
    {
        get => _children;
    }

    public Panel() :
        base(new InteractiveGue(new InvisibleRenderable()))
    {
        
        IsVisible = true;

        this.Dock(Gum.Wireframe.Dock.SizeToChildren);

    }

    public Panel(InteractiveGue visual) : base(visual) { }


    protected override void ReactToVisualChanged()
    {
        
        if(Visual != null)
        {
            Visual.ExposeChildrenEvents = true;

            // Note - if Visual is changed multiple times, this causes a slight
            // memory leak. However there is no way around this unless we have an
            // event for when the visual is removed.
            Visual.Children.CollectionChanged += (s, e) =>
            {
                // When the children change, we need to update our internal list
                // to match the visual's children
                _children.Clear();
                foreach (var child in Visual.Children)
                {
                    if (child is InteractiveGue gue && gue.FormsControlAsObject is FrameworkElement frameworkElement)
                    {
                        _children.Add(frameworkElement);
                    }
                }
            };
        }

        base.ReactToVisualChanged();
    }
}
