using Gum.Converters;
using Gum.DataTypes.Variables;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using MonoGameGum.Forms.Controls;
using MonoGameGum.GueDeriving;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonoGameGum.Forms.DefaultVisuals;
public class RadioButtonVisual : InteractiveGue
{
    public NineSliceRuntime Background { get; private set; }
    public SpriteRuntime InnerCheckbox { get; private set; }
    public TextRuntime TextInstance { get; private set; }
    public NineSliceRuntime FocusedIndicator { get; private set; }

    public class RadioButtonCategoryStates
    {
        public StateSave EnabledOn { get; set; }
        public StateSave EnabledOff { get; set; }
        public StateSave DisabledOn { get; set; }
        public StateSave DisabledOff { get; set; }
        public StateSave HighlightedOn { get; set; }
        public StateSave HighlightedOff { get; set; }
        public StateSave PushedOn { get; set; }
        public StateSave PushedOff { get; set; }
        public StateSave FocusedOn { get; set; }
        public StateSave FocusedOff { get; set; }
        public StateSave HighlightedFocusedOn { get; set; }
        public StateSave HighlightedFocusedOff { get; set; }
        public StateSave DisabledFocusedOn { get; set; }
        public StateSave DisabledFocusedOff { get; set; }

    }

    public RadioButtonCategoryStates States;

    public RadioButtonVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true) : base(new InvisibleRenderable())
    {
        if(fullInstantiation)
        {
            this.Height = 32;
            this.Width = 128;

            this.States = new RadioButtonCategoryStates();
            var uiSpriteSheetTexture = Styling.ActiveStyle.SpriteSheet;

            Background = new NineSliceRuntime();
            Background.Name = "Background";
            Background.X = 0;
            Background.Y = 0;
            Background.XUnits = Gum.Converters.GeneralUnitType.PixelsFromSmall;
            Background.YUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle;
            Background.XOrigin = HorizontalAlignment.Left;
            Background.YOrigin = VerticalAlignment.Center;
            Background.Width = 24;
            Background.Height = 24;
            Background.WidthUnits = Gum.DataTypes.DimensionUnitType.Absolute;
            Background.HeightUnits = Gum.DataTypes.DimensionUnitType.Absolute;
            Background.Color = Styling.Colors.Primary;
            Background.TextureAddress = Gum.Managers.TextureAddress.Custom;
            Background.Texture = uiSpriteSheetTexture;
            Background.ApplyState(Styling.NineSlice.CircleBordered);
            this.Children.Add(Background);

            InnerCheckbox = new SpriteRuntime();
            InnerCheckbox.Width = 100;
            InnerCheckbox.Height = 100;
            InnerCheckbox.WidthUnits = Gum.DataTypes.DimensionUnitType.PercentageOfSourceFile;
            InnerCheckbox.HeightUnits = Gum.DataTypes.DimensionUnitType.PercentageOfSourceFile;
            InnerCheckbox.XUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle;
            InnerCheckbox.YUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle;
            InnerCheckbox.XOrigin = HorizontalAlignment.Center;
            InnerCheckbox.YOrigin = VerticalAlignment.Center;
            InnerCheckbox.Name = "InnerCheck";
            InnerCheckbox.Color = Styling.Colors.White;
            InnerCheckbox.Texture = uiSpriteSheetTexture;
            InnerCheckbox.ApplyState(Styling.Icons.Circle2);
            Background.Children.Add(InnerCheckbox);

            TextInstance = new TextRuntime();
            TextInstance.X = 0;
            TextInstance.Y = 0;
            TextInstance.XUnits = Gum.Converters.GeneralUnitType.PixelsFromLarge;
            TextInstance.YUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle;
            TextInstance.XOrigin = HorizontalAlignment.Right;
            TextInstance.YOrigin = RenderingLibrary.Graphics.VerticalAlignment.Center;
            TextInstance.Width = -28;
            TextInstance.Height = 0;
            TextInstance.Name = "TextInstance";
            TextInstance.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
            TextInstance.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
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

            var radioButtonCategory = new Gum.DataTypes.Variables.StateSaveCategory();
            radioButtonCategory.Name = "RadioButtonCategory";
            this.AddCategory(radioButtonCategory);

            StateSave currentState;

            void AddState(string name)
            {
                var state = new StateSave();
                state.Name = name;
                radioButtonCategory.States.Add(state);
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

            void AddOnOffState(string state, Color backgroundColor, 
                Color textColor, Color checkColor, bool isFocused)
            {
                AddState(state + "On");
                AddVariable("InnerCheck.Visible", true);
                AddVariable("InnerCheck.Color", checkColor);
                AddVariable("Background.Color", backgroundColor);
                AddVariable("FocusedIndicator.Visible", isFocused);
                AddVariable("TextInstance.Color", textColor);


                AddState(state + "Off");
                AddVariable("InnerCheck.Visible", false);
                AddVariable("InnerCheck.Color", checkColor);
                AddVariable("Background.Color", backgroundColor);
                AddVariable("FocusedIndicator.Visible", isFocused);
                AddVariable("TextInstance.Color", textColor);
            }


            AddOnOffState(FrameworkElement.DisabledStateName, Styling.Colors.DarkGray,
                Styling.Colors.Gray, Styling.Colors.Gray, false);
            AddOnOffState(FrameworkElement.DisabledFocusedStateName, Styling.Colors.DarkGray,
                Styling.Colors.Gray, Styling.Colors.Gray, true);
            AddOnOffState(FrameworkElement.EnabledStateName, Styling.Colors.Primary,
                Styling.Colors.White, Styling.Colors.White, false);
            AddOnOffState(FrameworkElement.FocusedStateName, Styling.Colors.Primary,
                Styling.Colors.White, Styling.Colors.White, true);
            AddOnOffState(FrameworkElement.HighlightedStateName, Styling.Colors.PrimaryLight,
                Styling.Colors.White, Styling.Colors.White, false);
            AddOnOffState(FrameworkElement.HighlightedFocusedStateName, Styling.Colors.PrimaryLight,
                Styling.Colors.White, Styling.Colors.White, true);

            // dark looks weird so staying with normal primary. This matches the default template
            //AddOnOffState(FrameworkElement.PushedState, Styling.Colors.PrimaryDark,
            AddOnOffState(FrameworkElement.PushedStateName, Styling.Colors.Primary,
                Styling.Colors.White, Styling.Colors.White, false);

            // Attach the built up States to the exposed flatened States
            this.States.EnabledOn = radioButtonCategory.States.Find(x => x.Name == nameof(this.States.EnabledOn));
            this.States.EnabledOff = radioButtonCategory.States.Find(x => x.Name == nameof(this.States.EnabledOff));

            this.States.DisabledOn = radioButtonCategory.States.Find(x => x.Name == nameof(this.States.DisabledOn));
            this.States.DisabledOff = radioButtonCategory.States.Find(x => x.Name == nameof(this.States.DisabledOff));

            this.States.HighlightedOn = radioButtonCategory.States.Find(x => x.Name == nameof(this.States.HighlightedOn));
            this.States.HighlightedOff = radioButtonCategory.States.Find(x => x.Name == nameof(this.States.HighlightedOff));

            this.States.PushedOn = radioButtonCategory.States.Find(x => x.Name == nameof(this.States.PushedOn));
            this.States.PushedOff = radioButtonCategory.States.Find(x => x.Name == nameof(this.States.PushedOff));

            this.States.FocusedOn = radioButtonCategory.States.Find(x => x.Name == nameof(this.States.FocusedOn));
            this.States.FocusedOff = radioButtonCategory.States.Find(x => x.Name == nameof(this.States.FocusedOff));

            this.States.HighlightedFocusedOn = radioButtonCategory.States.Find(x => x.Name == nameof(this.States.HighlightedFocusedOn));
            this.States.HighlightedFocusedOff = radioButtonCategory.States.Find(x => x.Name == nameof(this.States.HighlightedFocusedOff));

            this.States.DisabledFocusedOn = radioButtonCategory.States.Find(x => x.Name == nameof(this.States.DisabledFocusedOn));
            this.States.DisabledFocusedOff = radioButtonCategory.States.Find(x => x.Name == nameof(this.States.DisabledFocusedOff));
        }

        if (tryCreateFormsObject)
        {
            FormsControlAsObject = new RadioButton(this);
        }
    }
    public RadioButton FormsControl => FormsControlAsObject as RadioButton;
}
