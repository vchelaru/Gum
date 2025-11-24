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
namespace Gum.Forms.DefaultVisuals.V3;

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

    Color _backgroundColor;
    public Color BackgroundColor
    {
        get => _backgroundColor;
        set
        {
            if (value != _backgroundColor)
            {
                _backgroundColor = value;
                FormsControl?.UpdateState();
            }
        }
    }
    Color _foregroundColor;
    public Color ForegroundColor
    {
        get => _foregroundColor;
        set
        {
            if (value != _foregroundColor)
            {
                _foregroundColor = value;
                FormsControl?.UpdateState();
            }
        }
    }


    public ListBoxItemVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true) : base(new InvisibleRenderable())
    {
        Height = 6f;
        HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        Width = 0f;
        WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;

        States = new ListBoxItemCategoryStates();
        var uiSpriteSheetTexture = Styling.ActiveStyle.SpriteSheet;

        BackgroundColor = Styling.ActiveStyle.Colors.Primary;
        ForegroundColor = Styling.ActiveStyle.Colors.White;

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

        DefineDynamicStyleChanges();

        if (tryCreateFormsObject)
        {
            FormsControlAsObject = new ListBoxItem(this);
        }
    }

    private void DefineDynamicStyleChanges()
    {
        ListBoxItemCategory.States.Add(States.Enabled);
        States.Enabled.Apply = () =>
        {
            SetValuesForState(false, false, ForegroundColor, BackgroundColor.ToGreyscale().Adjust(Styling.ActiveStyle.Colors.PercentGreyScaleDarken));
        };

        ListBoxItemCategory.States.Add(States.Highlighted);
        States.Highlighted.Apply = () =>
        {
            SetValuesForState(true, false, ForegroundColor, BackgroundColor);
        };

        ListBoxItemCategory.States.Add(States.Selected);
        States.Selected.Apply = () =>
        {
            SetValuesForState(true, false, ForegroundColor, Styling.ActiveStyle.Colors.Accent); // TODO: Discuss how to approach this.
        };

        ListBoxItemCategory.States.Add(States.Focused);
        States.Focused.Apply = () =>
        {
            SetValuesForState(false, true, ForegroundColor, BackgroundColor.ToGreyscale().Adjust(Styling.ActiveStyle.Colors.PercentGreyScaleDarken));
        };

        ListBoxItemCategory.States.Add(States.Disabled);
        States.Disabled.Apply = () =>
        {
            SetValuesForState(false, false, ForegroundColor.ToGreyscale().Adjust(Styling.ActiveStyle.Colors.PercentGreyScaleLighten), BackgroundColor.ToGreyscale().Adjust(Styling.ActiveStyle.Colors.PercentGreyScaleDarken));
        };
    }

    private void SetValuesForState(bool isBackgroundVisible, bool isFocusedVisible, Color foregroundColor, Color backgroundColor)
    {
        Background.Visible = isBackgroundVisible;
        FocusedIndicator.Visible = isFocusedVisible;
        TextInstance.Color = foregroundColor;
        Background.Color = backgroundColor;
    }

    public ListBoxItem FormsControl => FormsControlAsObject as ListBoxItem;
}
