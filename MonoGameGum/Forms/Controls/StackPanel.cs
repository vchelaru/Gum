using Gum.Wireframe;
using RenderingLibrary.Graphics;

#if FRB
using InteractiveGue = global::Gum.Wireframe.GraphicalUiElement;
namespace FlatRedBall.Forms.Controls;
#else
namespace MonoGameGum.Forms.Controls;

public enum Orientation
{
    Horizontal = 0,
    Vertical = 1
}

#endif

public class StackPanel : FrameworkElement
{
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

    protected override void ReactToVisualChanged()
    {
        base.ReactToVisualChanged();
        Visual.ExposeChildrenEvents = true;
        
        UpdateToOrientation();
    }

    public StackPanel() : 
        base(new InteractiveGue(new InvisibleRenderable()))
        //base(new global::Gum.Wireframe.GraphicalUiElement(new InvisibleRenderable(), null))
    {
        Width = 0;
        Visual.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
        Height = 10;
        Visual.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToChildren;

        IsVisible = true;

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
