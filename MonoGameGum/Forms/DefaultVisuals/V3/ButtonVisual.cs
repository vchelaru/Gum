using Gum.DataTypes.Variables;
using Gum.Wireframe;
using RenderingLibrary;
using RenderingLibrary.Graphics;



#if RAYLIB
using Raylib_cs;
using Gum.GueDeriving;

#else
using Microsoft.Xna.Framework;
using MonoGameGum.GueDeriving;
using Microsoft.Xna.Framework.Graphics;
#endif
using Gum.Forms.Controls;
namespace Gum.Forms.DefaultVisuals.V3;

public class ButtonVisual : InteractiveGue
{
    public NineSliceRuntime Background { get; private set; }

    public TextRuntime TextInstance { get; private set; }

    public NineSliceRuntime FocusedIndicator { get; private set; }

    public class ButtonCategoryStates
    {
        public StateSave Enabled { get; set; } = new StateSave() { Name = FrameworkElement.EnabledStateName };
        public StateSave Disabled { get; set; } = new StateSave() { Name = FrameworkElement.DisabledStateName };
        public StateSave Highlighted { get; set; } = new StateSave() { Name = FrameworkElement.HighlightedStateName };
        public StateSave Pushed { get; set; } = new StateSave() { Name = FrameworkElement.PushedStateName };
        public StateSave HighlightedFocused { get; set; } = new StateSave() { Name = FrameworkElement.HighlightedFocusedStateName };
        public StateSave Focused { get; set; } = new StateSave() { Name = FrameworkElement.FocusedStateName };
        public StateSave DisabledFocused { get; set; } = new StateSave() { Name = FrameworkElement.DisabledFocusedStateName };
    }

    public ButtonCategoryStates States;

    public StateSaveCategory ButtonCategory { get; private set; }

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
            if (!value.Equals(_foregroundColor))
            {
                // Just in case FormsControl hasn't been set yet, do ?. to check for null
                // UpdateState forcefully applies the current state, so it will work regardless of whether this is
                // Highlighted or Disabled etc
                _foregroundColor = value;
                FormsControl?.UpdateState();
            }
        }
    }

    Color _focusedIndicatorColor;
    public Color FocusedIndicatorColor
    {
        get => _focusedIndicatorColor;
        set
        {
            if(!value.Equals(_focusedIndicatorColor))
            {
                _focusedIndicatorColor = value;
                FormsControl?.UpdateState();
            }
        }
    }

    public ButtonVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true) : base(new InvisibleRenderable())
    {
        Width = 128;
        Height = 5;
        HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;

        States = new ButtonCategoryStates();
        var uiSpriteSheetTexture = Styling.ActiveStyle.SpriteSheet;

        Background = new NineSliceRuntime();
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
        Background.Name = "Background";
        Background.Texture = uiSpriteSheetTexture;
        Background.ApplyState(Styling.ActiveStyle.NineSlice.Bordered); 
        this.AddChild(Background);

        TextInstance = new TextRuntime();
        TextInstance.X = 0;
        TextInstance.Y = 0;
        TextInstance.Width = 0;
        TextInstance.Height = 5;
        TextInstance.Name = "TextInstance";
        TextInstance.Text = "Click Me";
        TextInstance.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
        TextInstance.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        TextInstance.XOrigin = HorizontalAlignment.Center;
        TextInstance.YOrigin = VerticalAlignment.Center;
        TextInstance.XUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle;
        TextInstance.YUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle;
        TextInstance.HorizontalAlignment = HorizontalAlignment.Center;
        TextInstance.VerticalAlignment = VerticalAlignment.Center;
        TextInstance.ApplyState(Styling.ActiveStyle.Text.Normal);
        this.AddChild(TextInstance);

        FocusedIndicator = new NineSliceRuntime();
        FocusedIndicator.Name = "FocusedIndicator";
        FocusedIndicator.X = 0;
        FocusedIndicator.Y = 2;
        FocusedIndicator.XUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle;
        FocusedIndicator.YUnits = Gum.Converters.GeneralUnitType.PixelsFromLarge;
        FocusedIndicator.XOrigin = HorizontalAlignment.Center;
        FocusedIndicator.YOrigin = VerticalAlignment.Top;
        FocusedIndicator.Width = 0;
        FocusedIndicator.Height = 2;
        FocusedIndicator.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
        FocusedIndicator.HeightUnits = Gum.DataTypes.DimensionUnitType.Absolute;
        FocusedIndicator.Texture = uiSpriteSheetTexture;
        FocusedIndicator.ApplyState(Styling.ActiveStyle.NineSlice.Solid);
        FocusedIndicator.Visible = false;
        this.AddChild(FocusedIndicator);

        ButtonCategory = new Gum.DataTypes.Variables.StateSaveCategory();
        ButtonCategory.Name = "ButtonCategory";
        this.AddCategory(ButtonCategory);

        FocusedIndicatorColor = Styling.ActiveStyle.Colors.Warning;
        BackgroundColor = Styling.ActiveStyle.Colors.Primary;
        ForegroundColor = Styling.ActiveStyle.Colors.TextPrimary;

        DefineDynamicStyleChanges();

        if (tryCreateFormsObject)
        {
            FormsControlAsObject = new Button(this);
        }
    }

    // These are a replacement for setting the style variables to explicit colors
    // Instead the entire styling for the button is based off 2 colors
    // the BackgroundColor and the ForegroundColor
    private void DefineDynamicStyleChanges()
    {
        ButtonCategory.States.Add(States.Enabled);
        States.Enabled.Apply = () =>
        {
            SetValuesForState(
                BackgroundColor, 
                ForegroundColor, 
                false);
        };

        ButtonCategory.States.Add(States.Disabled);
        States.Disabled.Apply = () =>
        {
            SetValuesForState(
                BackgroundColor.ToGrayscale().Adjust(Styling.ActiveStyle.Colors.PercentGreyScaleDarken),
                ForegroundColor.ToGrayscale().Adjust(Styling.ActiveStyle.Colors.PercentGreyScaleDarken), 
                false);
        };

        ButtonCategory.States.Add(States.Highlighted);
        States.Highlighted.Apply = () =>
        {
            SetValuesForState(
                BackgroundColor.Adjust(Styling.ActiveStyle.Colors.PercentLighten),
                ForegroundColor, 
                false);
        };

        ButtonCategory.States.Add(States.Pushed);
        States.Pushed.Apply = () =>
        {
            SetValuesForState(
                BackgroundColor.Adjust(Styling.ActiveStyle.Colors.PercentDarken), 
                ForegroundColor, 
                false);
        };

        ButtonCategory.States.Add(States.HighlightedFocused);
        States.HighlightedFocused.Apply = () =>
        {
            SetValuesForState(
                BackgroundColor.Adjust(Styling.ActiveStyle.Colors.PercentLighten), 
                ForegroundColor, 
                true);
        };

        ButtonCategory.States.Add(States.Focused);
        States.Focused.Apply = () =>
        {
            SetValuesForState(
                BackgroundColor, 
                ForegroundColor, 
                true);
        };

        ButtonCategory.States.Add(States.DisabledFocused);
        States.DisabledFocused.Apply = () =>
        {
            SetValuesForState(
                BackgroundColor.ToGrayscale().Adjust(Styling.ActiveStyle.Colors.PercentGreyScaleDarken), 
                ForegroundColor.ToGrayscale().Adjust(Styling.ActiveStyle.Colors.PercentGreyScaleDarken), 
                true);
        };
    }

    private void SetValuesForState(Color backgroundColor, Color foregroundColor, bool isFocused)
    {
        Background.Color = backgroundColor;
        TextInstance.Color = foregroundColor;
        FocusedIndicator.Visible = isFocused;
        FocusedIndicator.Color = FocusedIndicatorColor;
    }

    public Button FormsControl => (Button)FormsControlAsObject;
}
