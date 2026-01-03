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

#if RAYLIB
using Raylib_cs;

#else
using Microsoft.Xna.Framework;
#endif

namespace Gum.Forms.DefaultVisuals.V3;

public class MenuVisual : InteractiveGue
{
    public NineSliceRuntime Background {  get; private set; }
    public ContainerRuntime InnerPanelInstance { get; private set; }

    public StateSaveCategory MenuCategory { get; private set; }

    Color _backgroundColor;
    public Color BackgroundColor
    {
        get => _backgroundColor;
        set
        {
            if (!value.Equals(_backgroundColor))
            {
                // Just in case FormsControl hasn't been set yet, do ?. to check for null
                // UpdateState forcefully applies the current state, so it will work regardless of whether this is
                // Highlighted or Disabled etc
                _backgroundColor = value;

                // Currently there are no states
                //FormsControl?.UpdateState();
                // For now, since there is no state, we need to set this hard coded
                Background.Color = BackgroundColor;
            }
        }
    }

    public MenuVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true) : base(new InvisibleRenderable())
    {
        X = 0;
        XUnits = GeneralUnitType.PixelsFromMiddle;
        Y = 0;
        XOrigin = HorizontalAlignment.Center;
        Width = 0;
        WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
        Height = 0;
        HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        // a small value that prevents it from being invisible due to 0 height with no children
        MinHeight = 5;

        var uiSpriteSheetTexture = Styling.ActiveStyle.SpriteSheet;


        Background = new NineSliceRuntime();
        Background.Name = "Background";
        Background.X = 0;
        Background.XUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle;
        Background.Y = 0;
        Background.YUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle;
        Background.XOrigin = HorizontalAlignment.Center;
        Background.YOrigin = VerticalAlignment.Center;
        Background.Width = 0;
        Background.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
        Background.Height = 0;
        Background.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
        Background.Texture = uiSpriteSheetTexture;
        Background.Visible = true;
        Background.ApplyState(Styling.ActiveStyle.NineSlice.Solid);
        this.AddChild(Background);

        InnerPanelInstance = new ContainerRuntime();
        InnerPanelInstance.Name = "InnerPanelInstance";
        InnerPanelInstance.XUnits = GeneralUnitType.PixelsFromMiddle;
        InnerPanelInstance.YUnits = GeneralUnitType.PixelsFromMiddle;
        InnerPanelInstance.XOrigin  = HorizontalAlignment.Center;
        InnerPanelInstance.YOrigin = VerticalAlignment.Center;
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

        BackgroundColor = Styling.ActiveStyle.Colors.InputBackground;

        if (tryCreateFormsObject)
        {
            FormsControlAsObject = new Menu(this);
        }
    }

    public Menu FormsControl => FormsControlAsObject as Menu;
}
