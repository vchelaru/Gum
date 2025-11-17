using Gum.Converters;
using Gum.DataTypes.Variables;
using Gum.Wireframe;
using Gum.Forms.Controls;
using Microsoft.Xna.Framework;
using MonoGameGum.GueDeriving;
using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gum.Forms.DefaultVisuals.V3;

public class MenuItemVisual : InteractiveGue
{
    public NineSliceRuntime Background { get; private set; }
    public ContainerRuntime ContainerInstance { get; private set; }
    public TextRuntime TextInstance { get; private set; }

    public TextRuntime SubmenuIndicatorInstance { get; private set; }

    public class MenuItemCategoryStates
    {
        public StateSave Enabled { get; set; } = new StateSave() { Name = FrameworkElement.EnabledStateName };
        public StateSave Disabled { get; set; } = new StateSave() { Name = FrameworkElement.DisabledStateName };
        public StateSave Highlighted { get; set; } = new StateSave() { Name = FrameworkElement.HighlightedStateName };
        public StateSave Focused { get; set; } = new StateSave() { Name = FrameworkElement.FocusedStateName };
        public StateSave Selected { get; set; } = new StateSave() { Name = FrameworkElement.SelectedStateName };
    }

    public MenuItemCategoryStates States;

    public StateSaveCategory MenuItemCategory { get; private set; }

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
        Background.Color = Styling.ActiveStyle.Colors.DarkGray;
        Background.Texture = uiSpriteSheetTexture;
        Background.Visible = true;
        Background.ApplyState(Styling.ActiveStyle.NineSlice.Solid);
        this.AddChild(Background);

        ContainerInstance = new ContainerRuntime();
        ContainerInstance.Name = "ContainerInstance";
        ContainerInstance.ChildrenLayout = global::Gum.Managers.ChildrenLayout.LeftToRightStack;
        ContainerInstance.Anchor(Gum.Wireframe.Anchor.TopLeft);
        ContainerInstance.Height = 0f;
        ContainerInstance.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        ContainerInstance.Width = 0f;
        ContainerInstance.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        this.AddChild(ContainerInstance);

        TextInstance = new TextRuntime();
        TextInstance.Name = "TextInstance";
        TextInstance.Text = "Menu Item";
        TextInstance.Dock(Gum.Wireframe.Dock.Left);
        TextInstance.X = 4;
        TextInstance.Y = 0;
        TextInstance.Color = Styling.ActiveStyle.Colors.White;
        TextInstance.ApplyState(Styling.ActiveStyle.Text.Normal);
        TextInstance.Width = 2;
        TextInstance.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        TextInstance.Height = 2;
        TextInstance.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        ContainerInstance.AddChild(TextInstance);

        SubmenuIndicatorInstance = new TextRuntime();
        SubmenuIndicatorInstance.Name = "SubmenuIndicatorInstance";
        SubmenuIndicatorInstance.Dock(Gum.Wireframe.Dock.Left);
        SubmenuIndicatorInstance.X = 8;
        SubmenuIndicatorInstance.Height = 0f;
        SubmenuIndicatorInstance.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        SubmenuIndicatorInstance.HorizontalAlignment = global::RenderingLibrary.Graphics.HorizontalAlignment.Left;
        SubmenuIndicatorInstance.Text = @">";
        SubmenuIndicatorInstance.Width = 2f;
        SubmenuIndicatorInstance.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToChildren;

        ContainerInstance.AddChild(SubmenuIndicatorInstance);

        MenuItemCategory = new Gum.DataTypes.Variables.StateSaveCategory();
        MenuItemCategory.Name = "MenuItemCategory";
        this.AddCategory(MenuItemCategory);

        void AddVariable(StateSave state, string name, object value)
        {
            state.Variables.Add(new VariableSave
            {
                Name = name,
                Value = value
            });
        }

        void AddState(StateSave state, bool isBackgroundVisible, Color backgroundColor, Color textInstanceColor)
        {
            MenuItemCategory.States.Add(state);
            AddVariable(state, "Background.Visible", isBackgroundVisible);
            AddVariable(state, "Background.Color", backgroundColor);
            AddVariable(state, "TextInstance.Color", textInstanceColor);
        }

        AddState(States.Enabled, false, Styling.ActiveStyle.Colors.DarkGray, Styling.ActiveStyle.Colors.White);
        AddState(States.Disabled, false, Styling.ActiveStyle.Colors.DarkGray, Styling.ActiveStyle.Colors.Gray);
        AddState(States.Highlighted, true, Styling.ActiveStyle.Colors.LightGray, Styling.ActiveStyle.Colors.White);
        AddState(States.Selected, true, Styling.ActiveStyle.Colors.Primary, Styling.ActiveStyle.Colors.White);
        AddState(States.Focused, false, Styling.ActiveStyle.Colors.DarkGray, Styling.ActiveStyle.Colors.White);

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

    Gum.Forms.VisualTemplate DefaultScrollVisualTemplate => new (() =>
                new ScrollViewerVisualSizedToChildren(fullInstantiation: true, tryCreateFormsObject: false));

    public MenuItem FormsControl => FormsControlAsObject as MenuItem;
}
