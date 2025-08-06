using Gum.Converters;
using Gum.DataTypes.Variables;
using Gum.Wireframe;
using Gum.Forms.Controls;
using MonoGameGum.GueDeriving;
using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gum.Forms.DefaultVisuals;

public class MenuVisual : InteractiveGue
{
    public NineSliceRuntime Background {  get; private set; }
    public ContainerRuntime InnerPanelInstance { get; private set; }

    public StateSaveCategory MenuCategory { get; private set; }

    public MenuVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true) : base(new InvisibleRenderable())
    {
        X = 0;
        Y = 0;
        // a small value that prevents it from being invisible due to 0 height
        MinHeight = 5;
        Width = 0;
        WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
        Height = 0;
        HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;

        var uiSpriteSheetTexture = Styling.ActiveStyle.SpriteSheet;

        Background = new NineSliceRuntime();
        Background.Name = "Background";
        Background.X = 0;
        Background.Y = 0;
        Background.XUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle;
        Background.YUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle;
        Background.XOrigin = HorizontalAlignment.Center;
        Background.YOrigin = VerticalAlignment.Center;
        Background.Width = 0;
        Background.Height = 0;
        Background.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
        Background.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
        Background.Color = Styling.ActiveStyle.Colors.DarkGray;
        Background.Texture = uiSpriteSheetTexture;
        Background.Visible = true;
        Background.ApplyState(Styling.ActiveStyle.NineSlice.Solid);
        this.AddChild(Background);

        InnerPanelInstance = new ContainerRuntime();
        InnerPanelInstance.Name = "InnerPanelInstance";
        InnerPanelInstance.Height = 0f;
        InnerPanelInstance.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        InnerPanelInstance.Width = 0f;
        InnerPanelInstance.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        InnerPanelInstance.WrapsChildren = true;
        InnerPanelInstance.ChildrenLayout = Gum.Managers.ChildrenLayout.LeftToRightStack;
        InnerPanelInstance.StackSpacing = 2;
        this.AddChild(InnerPanelInstance);

        MenuCategory = new Gum.DataTypes.Variables.StateSaveCategory();
        MenuCategory.Name = Menu.MenuCategoryStateName;
        this.AddCategory(MenuCategory);

        if (tryCreateFormsObject)
        {
            FormsControlAsObject = new Menu(this);
        }
    }

    public Menu FormsControl => FormsControlAsObject as Menu;
}
