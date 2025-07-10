using Gum.Converters;
using Gum.Wireframe;
using MonoGameGum.Forms.Controls;
using MonoGameGum.GueDeriving;
using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonoGameGum.Forms.DefaultVisuals;

internal class MenuVisual : InteractiveGue
{
    public NineSliceRuntime Background {  get; private set; }
    public ContainerRuntime InnerPanelInstance { get; private set; }

    public MenuVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true) : base(new InvisibleRenderable())
    {
        if(fullInstantiation)
        {
            this.Width = 0;
            this.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
            this.Height = 0;
            this.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
            this.X = 0;
            this.Y = 0;

            var uiSpriteSheetTexture = IconVisuals.ActiveVisual.SpriteSheet;

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
            Background.Color = Styling.Colors.DarkGray;
            Background.TextureAddress = Gum.Managers.TextureAddress.Custom;
            Background.Texture = uiSpriteSheetTexture;
            Background.Visible = true;
            Background.ApplyState(NineSliceStyles.Solid);
            this.Children.Add(Background);

            InnerPanelInstance = new ContainerRuntime();
            InnerPanelInstance.Name = "InnerPanelInstance";
            InnerPanelInstance.Height = 0f;
            InnerPanelInstance.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToChildren;
            InnerPanelInstance.Width = 0f;
            InnerPanelInstance.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
            InnerPanelInstance.WrapsChildren = true;
            InnerPanelInstance.ChildrenLayout = Gum.Managers.ChildrenLayout.LeftToRightStack;
            InnerPanelInstance.StackSpacing = 2;
            this.Children.Add(InnerPanelInstance);

            var menuCategory = new Gum.DataTypes.Variables.StateSaveCategory();
            menuCategory.Name = Menu.MenuCategoryState;
        }

        if (tryCreateFormsObject)
        {
            FormsControlAsObject = new Menu(this);
        }
    }

    public Menu FormsControl => FormsControlAsObject as Menu;
}
