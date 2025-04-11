using Gum.DataTypes.Variables;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using MonoGameGum.Forms.Controls;
using MonoGameGum.GueDeriving;
using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonoGameGum.Forms.DefaultVisuals;
public class DefaultRadioButtonRuntime : InteractiveGue
{
    public TextRuntime TextInstance { get; private set; }
    public RectangleRuntime FocusedIndicator { get; private set; }

    public DefaultRadioButtonRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true) : base(new InvisibleRenderable())
    {
        if(fullInstantiation)
        {
            this.Height = 32;
            this.Width = 128;

            var radioButtonBackground = new CircleRuntime();

            //radioButtonBackground.Width = 24;
            //radioButtonBackground.Height = 24;
            radioButtonBackground.Radius = 12;
            radioButtonBackground.YUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle;
            radioButtonBackground.YOrigin = VerticalAlignment.Center;
            radioButtonBackground.Color = Color.White;
            radioButtonBackground.Name = "RadioButtonBackground";
            this.Children.Add(radioButtonBackground);

            var innerCheck = new ColoredRectangleRuntime();
            innerCheck.Width = 12;
            innerCheck.Height = 12;
            innerCheck.XUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle;
            innerCheck.YUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle;
            innerCheck.XOrigin = HorizontalAlignment.Center;
            innerCheck.YOrigin = VerticalAlignment.Center;
            innerCheck.Name = "InnerCheck";
            innerCheck.Color = new Color(128, 255, 0);
            radioButtonBackground.Children.Add(innerCheck);

            var text = new TextRuntime();
            text.X = 28;
            text.Y = 0;
            text.YOrigin = RenderingLibrary.Graphics.VerticalAlignment.Center;
            text.YUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle;
            text.Width = -36;
            text.Height = 0;
            text.Name = "TextInstance";
            text.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
            this.Children.Add(text);

            FocusedIndicator = new RectangleRuntime();
            FocusedIndicator.X = 0;
            FocusedIndicator.Y = 0;
            FocusedIndicator.XUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle;
            FocusedIndicator.YUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle;
            FocusedIndicator.XOrigin = HorizontalAlignment.Center;
            FocusedIndicator.YOrigin = VerticalAlignment.Center;
            FocusedIndicator.Width = 0;
            FocusedIndicator.Height = 0;
            FocusedIndicator.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
            FocusedIndicator.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
            FocusedIndicator.Color = Color.White;
            FocusedIndicator.Visible = false;
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
                AddVariable("RadioButtonBackground.Color", backgroundColor);
                AddVariable("FocusedIndicator.Visible", isFocused);
                AddVariable("TextInstance.Color", textColor);


                AddState(state + "Off");
                AddVariable("InnerCheck.Visible", false);
                AddVariable("InnerCheck.Color", checkColor);
                AddVariable("RadioButtonBackground.Color", backgroundColor);
                AddVariable("FocusedIndicator.Visible", isFocused);
                AddVariable("TextInstance.Color", textColor);
            }


            AddOnOffState(FrameworkElement.DisabledState, Styling.Colors.DarkGray,
                Styling.Colors.Gray, Styling.Colors.Gray, false);
            AddOnOffState(FrameworkElement.DisabledFocusedState, Styling.Colors.DarkGray,
                Styling.Colors.Gray, Styling.Colors.Gray, true);
            AddOnOffState(FrameworkElement.EnabledState, Styling.Colors.Primary,
                Styling.Colors.White, Styling.Colors.White, false);
            AddOnOffState(FrameworkElement.FocusedState, Styling.Colors.Primary,
                Styling.Colors.White, Styling.Colors.White, true);
            AddOnOffState(FrameworkElement.HighlightedState, Styling.Colors.PrimaryLight,
                Styling.Colors.White, Styling.Colors.White, false);
            AddOnOffState(FrameworkElement.HighlightedFocusedState, Styling.Colors.PrimaryLight,
                Styling.Colors.White, Styling.Colors.White, true);

            // dark looks weird so staying with normal primary. This matches the default template
            //AddOnOffState(FrameworkElement.PushedState, Styling.Colors.PrimaryDark,
            AddOnOffState(FrameworkElement.PushedState, Styling.Colors.Primary,
                Styling.Colors.White, Styling.Colors.White, false);


        }
    }

}
