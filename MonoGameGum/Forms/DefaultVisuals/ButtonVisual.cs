using Gum.DataTypes.Variables;
using Gum.Wireframe;
using RenderingLibrary;
using RenderingLibrary.Graphics;



#if RAYLIB
using Raylib_cs;
using Gum.GueDeriving;
using Gum.Forms.Controls;
namespace Gum.Forms.DefaultVisuals;

#else
using Microsoft.Xna.Framework;
using MonoGameGum.GueDeriving;
using Microsoft.Xna.Framework.Graphics;
using MonoGameGum.Forms.Controls;
namespace MonoGameGum.Forms.DefaultVisuals;
#endif

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
        Background.ApplyState(Styling.NineSlice.Bordered); 
        this.AddChild(Background);

        TextInstance = new TextRuntime();
        TextInstance.X = 0;
        TextInstance.Y = 0;
        TextInstance.Width = 0;
        TextInstance.Height = 5;
        TextInstance.Name = "TextInstance";
        TextInstance.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
        TextInstance.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        TextInstance.XOrigin = HorizontalAlignment.Center;
        TextInstance.YOrigin = VerticalAlignment.Center;
        TextInstance.XUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle;
        TextInstance.YUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle;
        TextInstance.HorizontalAlignment = HorizontalAlignment.Center;
        TextInstance.VerticalAlignment = VerticalAlignment.Center;
        TextInstance.ApplyState(Styling.Text.Normal);
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
        FocusedIndicator.ApplyState(Styling.NineSlice.Solid);
        FocusedIndicator.Visible = false;
        FocusedIndicator.Color = Styling.Colors.Warning;
        this.AddChild(FocusedIndicator);

        ButtonCategory = new Gum.DataTypes.Variables.StateSaveCategory();
        ButtonCategory.Name = "ButtonCategory";
        this.AddCategory(ButtonCategory);

        void AddVariable(StateSave state, string name, object value)
        {
            state.Variables.Add(new VariableSave
            {
                Name = name,
                Value = value
            });
        }

        void AddState(StateSave state, Color backgroundColor, Color textInstanceColor, bool isFocusedVisible)
        {
            ButtonCategory.States.Add(state);
            AddVariable(state, "Background.Color", backgroundColor);
            AddVariable(state, "TextInstance.Color", textInstanceColor);
            AddVariable(state, "FocusedIndicator.Visible", isFocusedVisible);
        }

        AddState(States.Enabled, Styling.Colors.Primary, Styling.Colors.White, false);
        AddState(States.Disabled, Styling.Colors.DarkGray, Styling.Colors.Gray, false);
        AddState(States.Highlighted, Styling.Colors.PrimaryLight, Styling.Colors.White, false);
        AddState(States.Pushed, Styling.Colors.PrimaryDark, Styling.Colors.White, false);
        AddState(States.HighlightedFocused, Styling.Colors.PrimaryLight, Styling.Colors.White, true);
        AddState(States.Focused, Styling.Colors.Primary, Styling.Colors.White, true);
        AddState(States.DisabledFocused, Styling.Colors.DarkGray, Styling.Colors.Gray, true);

        if (tryCreateFormsObject)
        {
            FormsControlAsObject = new Button(this);
        }
    }

    public Button FormsControl => FormsControlAsObject as Button;
}
