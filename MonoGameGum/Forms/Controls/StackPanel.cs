using Gum.Wireframe;
using RenderingLibrary.Graphics;

#if FRB
using InteractiveGue = global::Gum.Wireframe.GraphicalUiElement;
namespace FlatRedBall.Forms.Controls;
#endif

#if !FRB
namespace Gum.Forms.Controls;
#endif



public class StackPanel : Panel
{
    #region Fields/Properties

    Orientation orientation = Orientation.Vertical;
    public Orientation Orientation
    {
        get { return orientation; }
        set
        {
            if(value != orientation)
            {
                orientation = value;
                UpdateToOrientation();
            }
        }
    }

    public float Spacing
    {
        get => Visual.StackSpacing;
        set => Visual.StackSpacing = value;
    }

    #endregion

    protected override void ReactToVisualChanged()
    {
        base.ReactToVisualChanged();
        
        UpdateToOrientation();
    }

    public StackPanel() :  base()
        //base(new global::Gum.Wireframe.GraphicalUiElement(new InvisibleRenderable(), null))
    {
    }

#if !FRB
    /// <summary>
    /// Creates a new StackPanel using the default visual registered for <paramref name="service"/>, without
    /// having to make that service current first. The service that was current is restored afterward.
    /// </summary>
    public StackPanel(global::RenderingLibrary.IGumService service) : base(service)
    {
    }
#endif

    public StackPanel(InteractiveGue visual) : base(visual) 
    {
    }


    private void UpdateToOrientation()
    {
        if(Visual != null)
        {
            if(Orientation == Orientation.Horizontal)
            {
                Visual.ChildrenLayout = 
                    global::Gum.Managers.ChildrenLayout.LeftToRightStack;
            }
            else
            {
                Visual.ChildrenLayout =
                    global::Gum.Managers.ChildrenLayout.TopToBottomStack;
            }
        }
    }
}
