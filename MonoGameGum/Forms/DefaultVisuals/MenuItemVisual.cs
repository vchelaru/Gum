using Gum.Converters;
using Gum.DataTypes.Variables;
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

public class MenuItemVisual : InteractiveGue
{
    public NineSliceRuntime Background { get; private set; }
    //public ContainerRuntime ContainerInstance { get; private set; }
    public TextRuntime TextInstance { get; private set; }

    public class MenuItemCategoryStates
    {
        public StateSave Enabled { get; set; } = new StateSave() { Name = FrameworkElement.EnabledStateName };
        //        public StateSave Disabled { get; set; }
        public StateSave Highlighted { get; set; } = new StateSave() { Name = FrameworkElement.HighlightedStateName };
        public StateSave Focused { get; set; } = new StateSave() { Name = FrameworkElement.FocusedStateName };
        public StateSave Selected { get; set; } = new StateSave() { Name = FrameworkElement.SelectedStateName };
    }

    public MenuItemCategoryStates States;

    public MenuItemVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true) : base(new InvisibleRenderable())
    {
        X = 0;
        Y = 0;
        Width = 6;
        WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        Height = 3;
        HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;

        States = new MenuItemCategoryStates();
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
        Background.Color = Styling.Colors.DarkGray;
        Background.TextureAddress = Gum.Managers.TextureAddress.Custom;
        Background.Texture = uiSpriteSheetTexture;
        Background.Visible = true;
        Background.ApplyState(Styling.NineSlice.Solid);
        this.Children.Add(Background);

        TextInstance = new TextRuntime();
        TextInstance.Name = "TextInstance";
        TextInstance.Text = "Menu Item";
        TextInstance.X = 2;
        TextInstance.Y = 0;
        TextInstance.XUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle;
        TextInstance.YUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle;
        TextInstance.XOrigin = HorizontalAlignment.Center;
        TextInstance.YOrigin = VerticalAlignment.Center;
        TextInstance.Width = 2;
        TextInstance.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        TextInstance.Height = 0;
        TextInstance.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        TextInstance.HorizontalAlignment = global::RenderingLibrary.Graphics.HorizontalAlignment.Left;
        TextInstance.VerticalAlignment = VerticalAlignment.Center;
        TextInstance.Color = Styling.Colors.White;
        TextInstance.ApplyState(Styling.Text.Normal);
        this.Children.Add(TextInstance);

        var menuItemCategory = new Gum.DataTypes.Variables.StateSaveCategory();
        menuItemCategory.Name = "MenuItemCategory";
        this.AddCategory(menuItemCategory);

        void AddVariable(StateSave currentState, string name, object value)
        {
            currentState.Variables.Add(new VariableSave
            {
                Name = name,
                Value = value
            });
        }

        AddVariable(States.Enabled, "Background.Visible", true);
        AddVariable(States.Enabled, "Background.Color", Styling.Colors.DarkGray);

        AddVariable(States.Highlighted, "Background.Visible", true);
        AddVariable(States.Highlighted, "Background.Color", Styling.Colors.LightGray);
        
        AddVariable(States.Selected, "Background.Visible", true);
        AddVariable(States.Selected, "Background.Color", Styling.Colors.Primary);

        AddVariable(States.Focused, "Background.Visible", true);
        AddVariable(States.Focused, "Background.Color", Styling.Colors.DarkGray);

        if (tryCreateFormsObject)
        {
            FormsControlAsObject = new MenuItem(this);
        }
    }

    public override object FormsControlAsObject 
    { 
        get => base.FormsControlAsObject;
        set
        {
            base.FormsControlAsObject = value;
            if(value is MenuItem menuItem)
            {
                menuItem.ScrollViewerVisualTemplate = DefaultScrollVisualTemplate;
            }
        }
    }

    VisualTemplate DefaultScrollVisualTemplate => new VisualTemplate(() =>
                new ScrollViewerVisualSizedToChildren(fullInstantiation: true, tryCreateFormsObject: false));

    public MenuItem FormsControl => FormsControlAsObject as MenuItem;
}
