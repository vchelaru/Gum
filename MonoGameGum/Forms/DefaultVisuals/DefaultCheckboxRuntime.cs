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
                innerCheck.Color = new Color(128, 255, 0);
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

                void AddOnOffState(string state, Color checkboxColor, bool isFocused)
                {
                    AddState(state + "On");
                    AddVariable("InnerCheck.Visible", true);
                    AddVariable("CheckBoxBackground.Color", checkboxColor);
                    AddVariable("FocusedIndicator.Visible", isFocused);


                    AddState(state + "Off");
                    AddVariable("InnerCheck.Visible", false);
                    AddVariable("CheckBoxBackground.Color", checkboxColor);
                    AddVariable("FocusedIndicator.Visible", isFocused);
                }

                AddOnOffState(FrameworkElement.DisabledState, DefaultButtonRuntime.DisabledButtonColor, false);
                AddOnOffState(FrameworkElement.DisabledFocusedState, DefaultButtonRuntime.DisabledButtonColor, true);
                AddOnOffState(FrameworkElement.EnabledState, DefaultButtonRuntime.EnabledbuttonColor, false);
                AddOnOffState(FrameworkElement.FocusedState, DefaultButtonRuntime.EnabledbuttonColor, true);
                AddOnOffState(FrameworkElement.HighlightedState, DefaultButtonRuntime.HighlightedButtonColor, false);
                AddOnOffState(FrameworkElement.HighlightedFocusedState, DefaultButtonRuntime.HighlightedButtonColor, true);
                AddOnOffState(FrameworkElement.PushedState, DefaultButtonRuntime.HighlightedButtonColor, false);


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
