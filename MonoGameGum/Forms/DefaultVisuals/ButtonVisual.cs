using Gum.DataTypes.Variables;
using Gum.Wireframe;
using Microsoft.Xna.Framework.Graphics;
using MonoGameGum.Forms.Controls;
using MonoGameGum.GueDeriving;
using RenderingLibrary;
using RenderingLibrary.Graphics;

namespace MonoGameGum.Forms.DefaultVisuals;

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
        TextInstance.XOrigin = RenderingLibrary.Graphics.HorizontalAlignment.Center;
        TextInstance.YOrigin = RenderingLibrary.Graphics.VerticalAlignment.Center;
        TextInstance.XUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle;
        TextInstance.YUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle;
        TextInstance.HorizontalAlignment = RenderingLibrary.Graphics.HorizontalAlignment.Center;
        TextInstance.VerticalAlignment = RenderingLibrary.Graphics.VerticalAlignment.Center;
        TextInstance.ApplyState(Styling.Text.Normal);
        this.AddChild(TextInstance);

        FocusedIndicator = new NineSliceRuntime();
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
        FocusedIndicator.Name = "FocusedIndicator";
        this.AddChild(FocusedIndicator);

        var buttonCategory = new Gum.DataTypes.Variables.StateSaveCategory();
        buttonCategory.Name = "ButtonCategory";
        this.AddCategory(buttonCategory);

        void AddVariable(StateSave currentState, string name, object value)
        {
            currentState.Variables.Add(new VariableSave
            {
                Name = name,
                Value = value
            });
        }

        buttonCategory.States.Add(States.Enabled);
        AddVariable(States.Enabled, "Background.Color", Styling.Colors.Primary);
        AddVariable(States.Enabled, "TextInstance.Color", Styling.Colors.White);
        AddVariable(States.Enabled, "FocusedIndicator.Visible", false);

        buttonCategory.States.Add(States.Disabled);
        AddVariable(States.Disabled, "Background.Color", Styling.Colors.DarkGray);
        AddVariable(States.Disabled, "TextInstance.Color", Styling.Colors.Gray);
        AddVariable(States.Disabled, "FocusedIndicator.Visible", false);

        buttonCategory.States.Add(States.Highlighted);
        AddVariable(States.Highlighted, "Background.Color", Styling.Colors.PrimaryLight);
        AddVariable(States.Highlighted, "TextInstance.Color", Styling.Colors.White);
        AddVariable(States.Highlighted, "FocusedIndicator.Visible", false);

        buttonCategory.States.Add(States.Pushed);
        AddVariable(States.Pushed, "Background.Color", Styling.Colors.PrimaryDark);
        AddVariable(States.Pushed, "TextInstance.Color", Styling.Colors.White);
        AddVariable(States.Pushed, "FocusedIndicator.Visible", false);

        buttonCategory.States.Add(States.HighlightedFocused);
        AddVariable(States.HighlightedFocused, "Background.Color", Styling.Colors.PrimaryLight);
        AddVariable(States.HighlightedFocused, "TextInstance.Color", Styling.Colors.White);
        AddVariable(States.HighlightedFocused, "FocusedIndicator.Visible", true);

        buttonCategory.States.Add(States.Focused);
        AddVariable(States.Focused, "Background.Color", Styling.Colors.Primary);
        AddVariable(States.Focused, "TextInstance.Color", Styling.Colors.White);
        AddVariable(States.Focused, "FocusedIndicator.Visible", true);

        buttonCategory.States.Add(States.DisabledFocused);
        AddVariable(States.DisabledFocused, "Background.Color", Styling.Colors.DarkGray);
        AddVariable(States.DisabledFocused, "TextInstance.Color", Styling.Colors.Gray);
        AddVariable(States.DisabledFocused, "FocusedIndicator.Visible", true);

        if (tryCreateFormsObject)
        {
            FormsControlAsObject = new Button(this);
        }
    }

    public Button FormsControl => FormsControlAsObject as Button;
}
