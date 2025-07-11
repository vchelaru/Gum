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
    public TextRuntime TextInstance { get; private set; }

    public class MenuItemCategoryStates
    {
        public StateSave Enabled { get; set; }
//        public StateSave Disabled { get; set; }
        public StateSave Highlighted { get; set; }
        public StateSave Focused { get; set; }
        public StateSave Selected { get; set; }
    }

    public MenuItemCategoryStates States;

    public MenuItemVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true) : base(new InvisibleRenderable())
    {
        if (fullInstantiation)
        {
            this.Width = 6;
            this.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
            this.Height = 3;
            this.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
            this.X = 0;
            this.Y = 0;

            States = new MenuItemCategoryStates();
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

            StateSave currentState;

            void AddState(string name)
            {
                var state = new StateSave();
                state.Name = name;
                menuItemCategory.States.Add(state);
                currentState = state;
            }

            void AddVariable(string name, object value)
            {
                currentState.Variables.Add(new VariableSave
                {
                    Name = name,
                    Value = value
                });
            }

            AddState(FrameworkElement.EnabledStateName);
            AddVariable("Background.Visible", true);
            AddVariable("Background.Color", Styling.Colors.DarkGray);
            States.Enabled = currentState;

            AddState(FrameworkElement.HighlightedStateName);
            AddVariable("Background.Visible", true);
            AddVariable("Background.Color", Styling.Colors.LightGray);
            States.Highlighted = currentState;

            AddState(FrameworkElement.SelectedStateName);
            AddVariable("Background.Visible", true);
            AddVariable("Background.Color", Styling.Colors.Primary);
            States.Selected = currentState;

            AddState(FrameworkElement.FocusedStateName);
            AddVariable("Background.Visible", true);
            AddVariable("Background.Color", Styling.Colors.DarkGray);
            States.Focused = currentState;


        }
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
