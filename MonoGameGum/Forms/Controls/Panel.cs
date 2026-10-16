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
#endif

#if !FRB
namespace Gum.Forms.Controls;

#endif

/// <summary>
/// Base control which can contain multiple children. This is most commonly used
/// when instantiating a StackPanel.
/// </summary>
public class Panel :
#if FRB
    FrameworkElement
#else
    Gum.Forms.Controls.FrameworkElement
#endif
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

    /// <summary>
    /// Creates a new Panel using default visuals.
    /// </summary>
    public Panel() :
        base(new InteractiveGue(new InvisibleRenderable()))
    {
        
        IsVisible = true;

        this.Dock(global::Gum.Wireframe.Dock.SizeToChildren);

    }

#if !FRB
    /// <summary>
    /// Creates a new Panel. Its visual does not depend on a service, so this is the same as the
    /// parameterless constructor; it exists so every control can be constructed the same way.
    /// </summary>
    public Panel(global::RenderingLibrary.IGumService service) : this()
    {
    }
#endif

    /// <summary>
    /// Creates a new Panel using the specified visual.
    /// </summary>
    /// <param name="visual">The visual to use.</param>
    public Panel(InteractiveGue visual) : base(visual) { }


    protected override void ReactToVisualChanged()
    {
        
        if(Visual != null)
        {
            Visual.HasEvents = true;
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
