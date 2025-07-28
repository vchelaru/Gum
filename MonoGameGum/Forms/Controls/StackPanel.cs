using Gum.Wireframe;
using RenderingLibrary.Graphics;

#if FRB
using InteractiveGue = global::Gum.Wireframe.GraphicalUiElement;
namespace FlatRedBall.Forms.Controls;
#endif

#if !FRB

namespace Gum.Forms.Controls;
public enum Orientation
{
    Horizontal = 0,
    Vertical = 1
}

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
