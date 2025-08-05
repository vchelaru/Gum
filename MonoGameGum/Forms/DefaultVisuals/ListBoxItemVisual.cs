using Gum.Converters;
using Gum.DataTypes.Variables;
using Gum.Wireframe;
using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


#if RAYLIB
using Raylib_cs;
using Gum.GueDeriving;

#else
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameGum.GueDeriving;
#endif
using Gum.Forms.Controls;
namespace Gum.Forms.DefaultVisuals;

public class ListBoxItemVisual : InteractiveGue
{
    public NineSliceRuntime Background { get; private set; }
    public TextRuntime TextInstance { get; private set; }
    public NineSliceRuntime FocusedIndicator { get; private set; }

    public class ListBoxItemCategoryStates
    {
        public StateSave Enabled { get; set; } = new StateSave() { Name = FrameworkElement.EnabledStateName };
        public StateSave Highlighted { get; set; } = new StateSave() { Name = FrameworkElement.HighlightedStateName };
        public StateSave Selected { get; set; } = new StateSave() { Name = FrameworkElement.SelectedStateName };
        public StateSave Focused { get; set; } = new StateSave() { Name = FrameworkElement.FocusedStateName };
        public StateSave Disabled { get; set; } = new StateSave() { Name = FrameworkElement.DisabledStateName };
    }

    public ListBoxItemCategoryStates States;

    public StateSaveCategory ListBoxItemCategory { get; private set; }

    public ListBoxItemVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true) : base(new InvisibleRenderable())
    {
        Height = 6f;
        HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        Width = 0f;
        WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;

        States = new ListBoxItemCategoryStates();
        var uiSpriteSheetTexture = Styling.ActiveStyle.SpriteSheet;

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
        Background.Color = Styling.ActiveStyle.Colors.DarkGray;
        Background.Texture = uiSpriteSheetTexture;
        Background.ApplyState(Styling.ActiveStyle.NineSlice.Bordered);
        this.AddChild(Background);

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
        TextInstance.ApplyState(Styling.ActiveStyle.Text.Normal);
        TextInstance.Color = Styling.ActiveStyle.Colors.White;
        this.AddChild(TextInstance);

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
        FocusedIndicator.Color = Styling.ActiveStyle.Colors.Warning;
        FocusedIndicator.Texture = uiSpriteSheetTexture;
        FocusedIndicator.ApplyState(Styling.ActiveStyle.NineSlice.Solid);
        this.AddChild(FocusedIndicator);

        ListBoxItemCategory = new Gum.DataTypes.Variables.StateSaveCategory();
        ListBoxItemCategory.Name = "ListBoxItemCategory";
        this.AddCategory(ListBoxItemCategory);

        void AddVariable(StateSave state, string name, object value)
        {
            state.Variables.Add(new VariableSave
            {
                Name = name,
                Value = value
            });
        }

        void AddState(StateSave state, bool isBackgroundVisible, bool isFocusedVisible, 
            Color textColor, Color? backgroundColor = null)
        {
            ListBoxItemCategory.States.Add(state);
            AddVariable(state, "Background.Visible", isBackgroundVisible);
            AddVariable(state, "FocusedIndicator.Visible", isFocusedVisible);
            AddVariable(state, "TextInstance.Color", textColor);
            if (backgroundColor != null)
            {
                AddVariable(state, "Background.Color", backgroundColor);
            }
        }

        AddState(States.Enabled, false, false, Styling.ActiveStyle.Colors.White);
        AddState(States.Highlighted, true, false, Styling.ActiveStyle.Colors.White, Styling.ActiveStyle.Colors.Primary);
        AddState(States.Selected, true, false, Styling.ActiveStyle.Colors.White, Styling.ActiveStyle.Colors.Accent);
        AddState(States.Focused, false, true, Styling.ActiveStyle.Colors.White);
        AddState(States.Disabled, false, false, Styling.ActiveStyle.Colors.Gray);

        if (tryCreateFormsObject)
        {
            FormsControlAsObject = new ListBoxItem(this);
        }
    }

    public ListBoxItem FormsControl => FormsControlAsObject as ListBoxItem;
}
