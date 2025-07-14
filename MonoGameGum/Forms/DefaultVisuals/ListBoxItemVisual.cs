using Gum.Converters;
using Gum.DataTypes.Variables;
using Gum.Wireframe;
using Microsoft.Xna.Framework.Graphics;
using MonoGameGum.Forms.Controls;
using MonoGameGum.GueDeriving;
using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonoGameGum.Forms.DefaultVisuals;

public class ListBoxItemVisual : InteractiveGue
{
    public NineSliceRuntime Background { get; private set; }
    public TextRuntime TextInstance { get; private set; }
    public NineSliceRuntime FocusedIndicator { get; private set; }

    public class ListBoxItemCategoryStates
    {
        public StateSave Enabled { get; set; }
        public StateSave Highlighted { get; set; }
        public StateSave Selected { get; set; }
        public StateSave Focused { get; set; }
    }

    public ListBoxItemCategoryStates States;

    public ListBoxItemVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true) : base(new InvisibleRenderable())
    {
        if (fullInstantiation)
        {
            this.States= new ListBoxItemCategoryStates();
            var uiSpriteSheetTexture = Styling.ActiveStyle.SpriteSheet;

            this.Height = 6f;
            this.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToChildren;
            this.Width = 0f;
            this.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;

            Background = new NineSliceRuntime();
            Background.Name = "Background";
            Background.X = 0f;
            Background.XUnits = GeneralUnitType.PixelsFromMiddle;
            Background.Y = 0f;
            Background.YUnits = GeneralUnitType.PixelsFromMiddle;
            Background.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
            Background.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
            Background.Width = 0f;
            Background.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
            Background.Height = 0f;
            Background.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
            Background.Color = Styling.Colors.DarkGray;
            Background.Texture = uiSpriteSheetTexture;
            Background.ApplyState(Styling.NineSlice.Bordered);
            this.Children.Add(Background);

            TextInstance = new TextRuntime();
            TextInstance.Name = "TextInstance";
            TextInstance.Text = "ListBox Item";
            TextInstance.X = 0f;
            TextInstance.XUnits = GeneralUnitType.PixelsFromMiddle;
            TextInstance.Y = 0f;
            TextInstance.YUnits = GeneralUnitType.PixelsFromMiddle;
            TextInstance.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
            TextInstance.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
            TextInstance.Width = -8f;
            TextInstance.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
            TextInstance.Height = 0f;
            TextInstance.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToChildren;
            TextInstance.HorizontalAlignment = global::RenderingLibrary.Graphics.HorizontalAlignment.Left;
            TextInstance.VerticalAlignment = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
            TextInstance.ApplyState(Styling.Text.Normal);
            TextInstance.Color = Styling.Colors.White;
            this.Children.Add(TextInstance);

            FocusedIndicator = new NineSliceRuntime();
            FocusedIndicator.Name = "FocusedIndicator";
            FocusedIndicator.X = 0f;
            FocusedIndicator.XUnits = GeneralUnitType.PixelsFromMiddle;
            FocusedIndicator.Y = -2f;
            FocusedIndicator.YUnits = GeneralUnitType.PixelsFromLarge;
            FocusedIndicator.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
            FocusedIndicator.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Top;
            FocusedIndicator.Width = 0f;
            FocusedIndicator.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
            FocusedIndicator.Height = 2f;
            FocusedIndicator.HeightUnits = global::Gum.DataTypes.DimensionUnitType.Absolute;
            FocusedIndicator.Color = Styling.Colors.Warning;
            FocusedIndicator.Texture = uiSpriteSheetTexture;
            FocusedIndicator.ApplyState(Styling.NineSlice.Solid);
            this.Children.Add(FocusedIndicator);

            var listBoxItemCategory = new Gum.DataTypes.Variables.StateSaveCategory();
            listBoxItemCategory.Name = "ListBoxItemCategory";
            this.AddCategory(listBoxItemCategory);


            StateSave currentState;

            void AddState(string name)
            {
                var state = new StateSave();
                state.Name = name;
                listBoxItemCategory.States.Add(state);
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
            AddVariable("Background.Visible", false);
            AddVariable("FocusedIndicator.Visible", false);
            States.Enabled = currentState;

            AddState(FrameworkElement.HighlightedStateName);
            AddVariable("Background.Visible", true);
            AddVariable("Background.Color", Styling.Colors.Primary);
            AddVariable("FocusedIndicator.Visible", false);
            States.Highlighted = currentState;

            AddState(FrameworkElement.SelectedStateName);
            AddVariable("Background.Visible", true);
            AddVariable("Background.Color", Styling.Colors.Accent);
            AddVariable("FocusedIndicator.Visible", false);
            States.Selected = currentState;

            AddState(FrameworkElement.FocusedStateName);
            AddVariable("Background.Visible", false);
            AddVariable("FocusedIndicator.Visible", true);
            States.Focused = currentState;
        }

        if (tryCreateFormsObject)
        {
            FormsControlAsObject = new ListBoxItem(this);
        }
    }

    public ListBoxItem FormsControl => FormsControlAsObject as ListBoxItem;
}
