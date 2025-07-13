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

        ContainerInstance = new ContainerRuntime();
        ContainerInstance.Name = "ContainerInstance";
        ContainerInstance.ChildrenLayout = global::Gum.Managers.ChildrenLayout.LeftToRightStack;
        ContainerInstance.Anchor(Gum.Wireframe.Anchor.TopLeft);
        ContainerInstance.Height = 0f;
        ContainerInstance.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        ContainerInstance.Width = 0f;
        ContainerInstance.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        this.Children.Add(ContainerInstance);

        TextInstance = new TextRuntime();
        TextInstance.Name = "TextInstance";
        TextInstance.Text = "Menu Item";
        TextInstance.Dock(Gum.Wireframe.Dock.Left);
        TextInstance.X = 4;
        TextInstance.Y = 0;
        TextInstance.Color = Styling.Colors.White;
        TextInstance.ApplyState(Styling.Text.Normal);
        TextInstance.Width = 2;
        TextInstance.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        TextInstance.Height = 2;
        TextInstance.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        ContainerInstance.Children.Add(TextInstance);

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

        ContainerInstance.Children.Add(SubmenuIndicatorInstance);

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

        menuItemCategory.States.Add(States.Enabled);
        AddVariable(States.Enabled, "Background.Visible", true);
        AddVariable(States.Enabled, "Background.Color", Styling.Colors.DarkGray);
        AddVariable(States.Enabled, "TextInstance.Color", Styling.Colors.White);

        menuItemCategory.States.Add(States.Disabled);
        AddVariable(States.Disabled, "Background.Visible", true);
        AddVariable(States.Disabled, "Background.Color", Styling.Colors.DarkGray);
        AddVariable(States.Disabled, "TextInstance.Color", Styling.Colors.Gray);

        menuItemCategory.States.Add(States.Highlighted);
        AddVariable(States.Highlighted, "Background.Visible", true);
        AddVariable(States.Highlighted, "Background.Color", Styling.Colors.LightGray);
        AddVariable(States.Highlighted, "TextInstance.Color", Styling.Colors.White);

        menuItemCategory.States.Add(States.Selected);
        AddVariable(States.Selected, "Background.Visible", true);
        AddVariable(States.Selected, "Background.Color", Styling.Colors.Primary);
        AddVariable(States.Selected, "TextInstance.Color", Styling.Colors.White);

        menuItemCategory.States.Add(States.Focused);
        AddVariable(States.Focused, "Background.Visible", true);
        AddVariable(States.Focused, "Background.Color", Styling.Colors.DarkGray);
        AddVariable(States.Focused, "TextInstance.Color", Styling.Colors.White);

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
