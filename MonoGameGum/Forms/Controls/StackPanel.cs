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

    #endregion

    protected override void ReactToVisualChanged()
    {
        base.ReactToVisualChanged();
        
        UpdateToOrientation();
    }

    public StackPanel() :  base()
        //base(new global::Gum.Wireframe.GraphicalUiElement(new InvisibleRenderable(), null))
    {
        Width = 0;
        Visual.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        Height = 10;
        Visual.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToChildren;
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
