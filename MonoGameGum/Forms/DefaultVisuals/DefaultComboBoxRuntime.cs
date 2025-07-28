using Gum.Converters;
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

namespace MonoGameGum.Forms.DefaultVisuals
{
    public class DefaultComboBoxRuntime : InteractiveGue
    {
        public DefaultListBoxRuntime ListBoxInstance;
        public RectangleRuntime FocusedIndicator { get; private set; }

        public DefaultComboBoxRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true) : base(new InvisibleRenderable())
        {
            if (fullInstantiation)
            {
                var background = new ColoredRectangleRuntime();
                background.Name = "Background";

                var TextInstance = new TextRuntime();
                TextInstance.Name = "TextInstance";

                ListBoxInstance = new DefaultListBoxRuntime(tryCreateFormsObject:false);
                ListBoxInstance.Name = "ListBoxInstance";


                // I dont' think we need an icon or focus indicator for the basic implementation.

                this.Height = 24f;
                this.Width = 256f;

                background.Color = Styling.ActiveStyle.Colors.DarkGray;
                background.Height = 0f;
                background.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
                background.Width = 0f;
                background.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
                background.X = 0f;
                background.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
                background.XUnits = GeneralUnitType.PixelsFromMiddle;
                background.Y = 0f;
                background.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
                background.YUnits = GeneralUnitType.PixelsFromMiddle;
                background.Name = "Background";
                this.Children.Add(background);

                TextInstance.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
                TextInstance.Text = "Selected Item";
                TextInstance.VerticalAlignment = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
                TextInstance.Width = -8f;
                TextInstance.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
                TextInstance.Height = 0;
                TextInstance.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
                TextInstance.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
                TextInstance.XUnits = GeneralUnitType.PixelsFromMiddle;
                TextInstance.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
                TextInstance.YUnits = GeneralUnitType.PixelsFromMiddle;
                this.Children.Add(TextInstance);

                FocusedIndicator = new RectangleRuntime();
                FocusedIndicator.X = 0;
                FocusedIndicator.Y = 0;
                FocusedIndicator.XUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle;
                FocusedIndicator.YUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle;
                FocusedIndicator.XOrigin = HorizontalAlignment.Center;
                FocusedIndicator.YOrigin = VerticalAlignment.Center;
                FocusedIndicator.Width = -4;
                FocusedIndicator.Height = -4;
                FocusedIndicator.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
                FocusedIndicator.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
                FocusedIndicator.Color = Color.White;
                FocusedIndicator.Visible = false;
                FocusedIndicator.Name = "FocusedIndicator";
                this.Children.Add(FocusedIndicator);

                var rightSideText = new TextRuntime();
                rightSideText.Text = "v";
                rightSideText.XOrigin = HorizontalAlignment.Right;
                rightSideText.XUnits = GeneralUnitType.PixelsFromLarge;
                rightSideText.X = -10;
                rightSideText.HorizontalAlignment = HorizontalAlignment.Right;
                rightSideText.Name = "DropdownIndicator";

                this.Children.Add(rightSideText);

                ListBoxInstance.Height = 128f;
                ListBoxInstance.Width = 0f;
                ListBoxInstance.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
                ListBoxInstance.Y = 28f;
                this.Children.Add(ListBoxInstance);
                ListBoxInstance.Visible = false;

                var comboBoxCategory = new StateSaveCategory();
                comboBoxCategory.Name = "ComboBoxCategory";
                this.AddCategory(comboBoxCategory);

                StateSave currentState;

                void AddState(string name)
                {
                    var state = new StateSave();
                    state.Name = name;
                    comboBoxCategory.States.Add(state);
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

                AddState(FrameworkElement.DisabledStateName);
                AddVariable("TextInstance.Color", Styling.ActiveStyle.Colors.Gray);
                AddVariable("DropdownIndicator.Color", Styling.ActiveStyle.Colors.Gray);
                AddVariable("FocusedIndicator.Visible", false);

                AddState(FrameworkElement.DisabledFocusedStateName);
                AddVariable("TextInstance.Color", Styling.ActiveStyle.Colors.Gray);
                AddVariable("DropdownIndicator.Color", Styling.ActiveStyle.Colors.Gray);
                AddVariable("FocusedIndicator.Visible", true);

                AddState(FrameworkElement.EnabledStateName);
                AddVariable("TextInstance.Color", Styling.ActiveStyle.Colors.White);
                AddVariable("DropdownIndicator.Color", Styling.ActiveStyle.Colors.White);
                AddVariable("FocusedIndicator.Visible", false);

                AddState(FrameworkElement.FocusedStateName);
                AddVariable("TextInstance.Color", Styling.ActiveStyle.Colors.White);
                AddVariable("DropdownIndicator.Color", Styling.ActiveStyle.Colors.White);
                AddVariable("FocusedIndicator.Visible", true);

                AddState(FrameworkElement.HighlightedStateName);
                AddVariable("TextInstance.Color", Styling.ActiveStyle.Colors.PrimaryLight);
                AddVariable("DropdownIndicator.Color", Styling.ActiveStyle.Colors.PrimaryLight);
                AddVariable("FocusedIndicator.Visible", false);

                AddState(FrameworkElement.HighlightedFocusedStateName);
                AddVariable("TextInstance.Color", Styling.ActiveStyle.Colors.PrimaryLight);
                AddVariable("DropdownIndicator.Color", Styling.ActiveStyle.Colors.PrimaryLight);
                AddVariable("FocusedIndicator.Visible", true);

                AddState(FrameworkElement.PushedStateName);
                AddVariable("TextInstance.Color", Styling.ActiveStyle.Colors.PrimaryDark);
                AddVariable("DropdownIndicator.Color", Styling.ActiveStyle.Colors.PrimaryDark);
                AddVariable("FocusedIndicator.Visible", false);

            }
            if (tryCreateFormsObject)
            {
                FormsControlAsObject = new ComboBox(this);
            }
        }

        public ComboBox FormsControl => FormsControlAsObject as ComboBox;

    }
}
