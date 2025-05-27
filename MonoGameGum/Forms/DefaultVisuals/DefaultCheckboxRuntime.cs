using Gum.DataTypes.Variables;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using MonoGameGum.Forms.Controls;
using MonoGameGum.GueDeriving;
using RenderingLibrary.Graphics;
using RenderingLibrary.Math.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonoGameGum.Forms.DefaultVisuals
{
    public class DefaultCheckboxRuntime : InteractiveGue
    {
        public RectangleRuntime FocusedIndicator { get; private set; }


        public DefaultCheckboxRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true) : base(new InvisibleRenderable())
        {
            if (fullInstantiation)
            {
                this.Height = 32;
                this.Width = 128;

                var checkboxBackground = new ColoredRectangleRuntime();
                checkboxBackground.Width = 24;
                checkboxBackground.Height = 24;
                checkboxBackground.Color = new Color(41, 55, 52);
                checkboxBackground.YUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle;
                checkboxBackground.YOrigin = VerticalAlignment.Center;
                checkboxBackground.Name = "CheckBoxBackground";
                this.Children.Add(checkboxBackground);

                var innerCheck = new ColoredRectangleRuntime();
                innerCheck.Width = 12;
                innerCheck.Height = 12;
                innerCheck.XUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle;
                innerCheck.YUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle;
                innerCheck.XOrigin = HorizontalAlignment.Center;
                innerCheck.YOrigin = VerticalAlignment.Center;
                innerCheck.Name = "InnerCheck";
                innerCheck.Color = Styling.Colors.White;
                checkboxBackground.Children.Add(innerCheck);

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

                //DisabledState = "Disabled";
                //DisabledFocusedState = "DisabledFocused";
                //EnabledState = "Enabled";
                //FocusedState = "Focused";
                //HighlightedState = "Highlighted";
                //HighlightedFocusedState = "HighlightedFocused";
                //PushedState = "Pushed";

                var checkboxCategory = new Gum.DataTypes.Variables.StateSaveCategory();
                checkboxCategory.Name = "CheckBoxCategory";
                StateSave currentState;

                void AddState(string name)
                {
                    var state = new StateSave();
                    state.Name = name;
                    checkboxCategory.States.Add(state);
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

                void AddOnOffState(string state, Color checkboxBackgroundColor, 
                    Color textColor, Color checkColor, bool isFocused)
                {
                    AddState(state + "On");
                    AddVariable("InnerCheck.Visible", true);
                    AddVariable("InnerCheck.Color", checkColor);
                    AddVariable("InnerCheck.Height", 12);
                    AddVariable("CheckBoxBackground.Color", checkboxBackgroundColor);
                    AddVariable("FocusedIndicator.Visible", isFocused);
                    AddVariable("TextInstance.Color", textColor);


                    AddState(state + "Off");
                    AddVariable("InnerCheck.Visible", false);
                    AddVariable("InnerCheck.Color", checkColor);
                    AddVariable("InnerCheck.Height", 12);
                    AddVariable("CheckBoxBackground.Color", checkboxBackgroundColor);
                    AddVariable("FocusedIndicator.Visible", isFocused);
                    AddVariable("TextInstance.Color", textColor);

                    AddState(state + "Indeterminate");
                    AddVariable("InnerCheck.Visible", true);
                    AddVariable("InnerCheck.Color", checkColor);
                    AddVariable("InnerCheck.Height", 6);
                    AddVariable("CheckBoxBackground.Color", checkboxBackgroundColor);
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


                this.AddCategory(checkboxCategory);
            }

            if (tryCreateFormsObject)
            {
                FormsControlAsObject = new CheckBox(this);
            }
        }

        public CheckBox FormsControl => FormsControlAsObject as CheckBox;
    }
}
