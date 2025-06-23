﻿using Gum.DataTypes.Variables;
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

internal class Styling
{
    internal class Colors
    {
        public static Color Primary { get; private set; } = new Color(6, 159, 177);
        public static Color PrimaryLight { get; private set; } = new Color(74, 180, 193);
        public static Color PrimaryDark { get; private set; } = new Color(4, 120, 137);

        public static Color DarkGray { get; private set; } = new Color(70, 70, 80);
        public static Color Gray { get; private set; } = new Color(130, 130, 130);

        public static Color White { get; private set; } = new Color(255, 255, 255);

        public static Color Accent { get; private set; } = new Color(140, 48, 138);
    }
}

public class DefaultButtonRuntime : InteractiveGue
{
    public TextRuntime TextInstance { get; private set; }

    public RectangleRuntime FocusedIndicator { get; private set; }



    public DefaultButtonRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true) : base(new InvisibleRenderable())
    {
        if (fullInstantiation)
        {
            this.Width = 128;
            this.Height = 5;
            this.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;

            var background = new ColoredRectangleRuntime();
            background.Width = 0;
            background.Height = 0;
            background.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
            background.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
            background.Name = "ButtonBackground";
            this.Children.Add(background);

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

            AddState(FrameworkElement.EnabledStateName);
            AddVariable("ButtonBackground.Color", Styling.Colors.Primary);
            AddVariable("TextInstance.Color", Styling.Colors.White);
            AddVariable("FocusedIndicator.Visible", false);

            AddState(FrameworkElement.FocusedStateName);
            AddVariable("ButtonBackground.Color", Styling.Colors.Primary);
            AddVariable("TextInstance.Color", Styling.Colors.White);
            AddVariable("FocusedIndicator.Visible", true);

            AddState(FrameworkElement.HighlightedStateName);
            AddVariable("ButtonBackground.Color", Styling.Colors.PrimaryLight);
            AddVariable("TextInstance.Color", Styling.Colors.White);
            AddVariable("FocusedIndicator.Visible", false);

            AddState(FrameworkElement.HighlightedFocusedStateName);
            AddVariable("ButtonBackground.Color", Styling.Colors.PrimaryLight);
            AddVariable("TextInstance.Color", Styling.Colors.White);
            AddVariable("FocusedIndicator.Visible", true);

            AddState(FrameworkElement.PushedStateName);
            AddVariable("ButtonBackground.Color", Styling.Colors.PrimaryDark);
            AddVariable("TextInstance.Color", Styling.Colors.White);
            AddVariable("FocusedIndicator.Visible", false);

            AddState(FrameworkElement.DisabledStateName);
            AddVariable("ButtonBackground.Color", Styling.Colors.DarkGray);
            AddVariable("TextInstance.Color", Styling.Colors.Gray);
            AddVariable("FocusedIndicator.Visible", false);

            AddState(FrameworkElement.DisabledFocusedStateName);
            AddVariable("ButtonBackground.Color", Styling.Colors.DarkGray);
            AddVariable("TextInstance.Color", Styling.Colors.Gray);
            AddVariable("FocusedIndicator.Visible", true);

        }

        if (tryCreateFormsObject)
        {
            FormsControlAsObject = new Button(this);
        }

    }

    public Button FormsControl => FormsControlAsObject as Button;
}
