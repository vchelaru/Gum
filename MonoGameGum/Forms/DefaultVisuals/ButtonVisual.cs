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
        public StateSave Enabled { get; set; }
        public StateSave Disabled { get; set; }
        public StateSave Highlighted { get; set; }
        public StateSave Pushed { get; set; }
        public StateSave HighlightedFocused { get; set; }
        public StateSave Focused { get; set; }
        public StateSave DisabledFocused { get; set; }
    }

    public ButtonCategoryStates States;

    public ButtonVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true) : base(new InvisibleRenderable())
    {
        if (fullInstantiation)
        {
            this.States = new ButtonCategoryStates();

            this.Width = 128;
            this.Height = 5;
            this.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;

            var uiSpriteSheetTexture = IconVisuals.ActiveVisual.SpriteSheet;

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
            Background.TextureAddress = Gum.Managers.TextureAddress.Custom;
            Background.Texture = uiSpriteSheetTexture;
            Background.ApplyState(Styling.NineSlice.Bordered); 
            this.Children.Add(Background);

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
            this.Children.Add(TextInstance);

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
            FocusedIndicator.TextureAddress = Gum.Managers.TextureAddress.Custom;
            FocusedIndicator.Texture = uiSpriteSheetTexture;
            FocusedIndicator.ApplyState(Styling.NineSlice.Solid);
            FocusedIndicator.Visible = false;
            FocusedIndicator.Color = Styling.Colors.Warning;
            FocusedIndicator.Name = "FocusedIndicator";
            this.Children.Add(FocusedIndicator);

            var buttonCategory = new Gum.DataTypes.Variables.StateSaveCategory();
            buttonCategory.Name = "ButtonCategory";
            this.AddCategory(buttonCategory);

            StateSave currentState;

            void AddState(string name)
            {
                var state = new StateSave();
                state.Name = name;
                buttonCategory.States.Add(state);
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

            // AddState is called first and the currentState is set
            // Then we add variables that belong to that state
            AddState(FrameworkElement.EnabledStateName);
            AddVariable("Background.Color", Styling.Colors.Primary);
            AddVariable("TextInstance.Color", Styling.Colors.White);
            AddVariable("FocusedIndicator.Visible", false);
            this.States.Enabled = currentState;

            AddState(FrameworkElement.DisabledStateName);
            AddVariable("Background.Color", Styling.Colors.DarkGray);
            AddVariable("TextInstance.Color", Styling.Colors.Gray);
            AddVariable("FocusedIndicator.Visible", false);
            this.States.Disabled = currentState;

            AddState(FrameworkElement.HighlightedStateName);
            AddVariable("Background.Color", Styling.Colors.PrimaryLight);
            AddVariable("TextInstance.Color", Styling.Colors.White);
            AddVariable("FocusedIndicator.Visible", false);
            this.States.Highlighted = currentState;

            AddState(FrameworkElement.PushedStateName);
            AddVariable("Background.Color", Styling.Colors.PrimaryDark);
            AddVariable("TextInstance.Color", Styling.Colors.White);
            AddVariable("FocusedIndicator.Visible", false);
            this.States.Pushed = currentState;

            AddState(FrameworkElement.HighlightedFocusedStateName);
            AddVariable("Background.Color", Styling.Colors.PrimaryLight);
            AddVariable("TextInstance.Color", Styling.Colors.White);
            AddVariable("FocusedIndicator.Visible", true);
            this.States.HighlightedFocused = currentState;

            AddState(FrameworkElement.FocusedStateName);
            AddVariable("Background.Color", Styling.Colors.Primary);
            AddVariable("TextInstance.Color", Styling.Colors.White);
            AddVariable("FocusedIndicator.Visible", true);
            this.States.Focused = currentState;

            AddState(FrameworkElement.DisabledFocusedStateName);
            AddVariable("Background.Color", Styling.Colors.DarkGray);
            AddVariable("TextInstance.Color", Styling.Colors.Gray);
            AddVariable("FocusedIndicator.Visible", true);
            this.States.DisabledFocused = currentState;

        }

        if (tryCreateFormsObject)
        {
            FormsControlAsObject = new Button(this);
        }

    }

    public Button FormsControl => FormsControlAsObject as Button;
}
