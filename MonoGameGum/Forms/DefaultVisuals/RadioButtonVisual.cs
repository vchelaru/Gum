﻿using Gum.Converters;
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
    public SpriteRuntime InnerCheck { get; private set; }
    public TextRuntime TextInstance { get; private set; }
    public NineSliceRuntime FocusedIndicator { get; private set; }

    public class RadioButtonCategoryStates
    {
        public StateSave EnabledOn { get; set; } = new StateSave() { Name = nameof(EnabledOn) };
        public StateSave EnabledOff { get; set; } = new StateSave() { Name = nameof(EnabledOff) };
        public StateSave DisabledOn { get; set; } = new StateSave() { Name = nameof(DisabledOn) };
        public StateSave DisabledOff { get; set; } = new StateSave() { Name = nameof(DisabledOff) };
        public StateSave HighlightedOn { get; set; } = new StateSave() { Name = nameof(HighlightedOn) };
        public StateSave HighlightedOff { get; set; } = new StateSave() { Name = nameof(HighlightedOff) };
        public StateSave PushedOn { get; set; } = new StateSave() { Name = nameof(PushedOn) };
        public StateSave PushedOff { get; set; } = new StateSave() { Name = nameof(PushedOff) };
        public StateSave FocusedOn { get; set; } = new StateSave() { Name = nameof(FocusedOn) };
        public StateSave FocusedOff { get; set; } = new StateSave() { Name = nameof(FocusedOff) };
        public StateSave HighlightedFocusedOn { get; set; } = new StateSave() { Name = nameof(HighlightedFocusedOn) };
        public StateSave HighlightedFocusedOff { get; set; } = new StateSave() { Name = nameof(HighlightedFocusedOff) };
        public StateSave DisabledFocusedOn { get; set; } = new StateSave() { Name = nameof(DisabledFocusedOn) };
        public StateSave DisabledFocusedOff { get; set; } = new StateSave() { Name = nameof(DisabledFocusedOff) };

    }

    public RadioButtonCategoryStates States;

    public StateSaveCategory RadioButtonCategory { get; private set; }

    public RadioButtonVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true) : base(new InvisibleRenderable())
    {
        Height = 32;
        Width = 128;

        States = new RadioButtonCategoryStates();
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
        Background.Texture = uiSpriteSheetTexture;
        Background.ApplyState(Styling.NineSlice.CircleBordered);
        this.AddChild(Background);

        InnerCheck = new SpriteRuntime();
        InnerCheck.Name = "InnerCheck";
        InnerCheck.Width = 100;
        InnerCheck.Height = 100;
        InnerCheck.WidthUnits = Gum.DataTypes.DimensionUnitType.PercentageOfSourceFile;
        InnerCheck.HeightUnits = Gum.DataTypes.DimensionUnitType.PercentageOfSourceFile;
        InnerCheck.XUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle;
        InnerCheck.YUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle;
        InnerCheck.XOrigin = HorizontalAlignment.Center;
        InnerCheck.YOrigin = VerticalAlignment.Center;
        InnerCheck.Color = Styling.Colors.White;
        InnerCheck.Texture = uiSpriteSheetTexture;
        InnerCheck.ApplyState(Styling.Icons.Circle2);
        Background.Children.Add(InnerCheck);

        TextInstance = new TextRuntime();
        TextInstance.Name = "TextInstance";
        TextInstance.X = 0;
        TextInstance.Y = 0;
        TextInstance.XUnits = Gum.Converters.GeneralUnitType.PixelsFromLarge;
        TextInstance.YUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle;
        TextInstance.XOrigin = HorizontalAlignment.Right;
        TextInstance.YOrigin = RenderingLibrary.Graphics.VerticalAlignment.Center;
        TextInstance.Width = -28;
        TextInstance.Height = 0;
        TextInstance.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
        TextInstance.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
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

        RadioButtonCategory = new Gum.DataTypes.Variables.StateSaveCategory();
        RadioButtonCategory.Name = "RadioButtonCategory";
        this.AddCategory(RadioButtonCategory);

        void AddVariable(StateSave state, string name, object value)
        {
            state.Variables.Add(new VariableSave
            {
                Name = name,
                Value = value
            });
        }

        void AddState(StateSave state, Color backgroundColor,
            Color textColor, Color checkColor, bool isFocused, bool checkVisible)
        {
            RadioButtonCategory.States.Add(state);
            AddVariable(state, "InnerCheck.Visible", checkVisible);
            AddVariable(state, "InnerCheck.Color", checkColor);
            AddVariable(state, "Background.Color", backgroundColor);
            AddVariable(state, "FocusedIndicator.Visible", isFocused);
            AddVariable(state, "TextInstance.Color", textColor);
        }

        AddState(States.EnabledOn, Styling.Colors.Primary, Styling.Colors.White, Styling.Colors.White, false, true);
        AddState(States.EnabledOff, Styling.Colors.Primary, Styling.Colors.White, Styling.Colors.White, false, false);

        AddState(States.DisabledOn, Styling.Colors.DarkGray, Styling.Colors.Gray, Styling.Colors.Gray, false, true);
        AddState(States.DisabledOff, Styling.Colors.DarkGray, Styling.Colors.Gray, Styling.Colors.Gray, false, false);

        AddState(States.DisabledFocusedOn, Styling.Colors.DarkGray, Styling.Colors.Gray, Styling.Colors.Gray, true, true);
        AddState(States.DisabledFocusedOff, Styling.Colors.DarkGray, Styling.Colors.Gray, Styling.Colors.Gray, true, false);

        AddState(States.FocusedOn, Styling.Colors.Primary, Styling.Colors.White, Styling.Colors.White, true, true);
        AddState(States.FocusedOff, Styling.Colors.Primary, Styling.Colors.White, Styling.Colors.White, true, false);

        AddState(States.HighlightedOn, Styling.Colors.PrimaryLight, Styling.Colors.White, Styling.Colors.White, false, true);
        AddState(States.HighlightedOff, Styling.Colors.PrimaryLight, Styling.Colors.White, Styling.Colors.White, false, false);

        AddState(States.HighlightedFocusedOn, Styling.Colors.PrimaryLight, Styling.Colors.White, Styling.Colors.White, true, true);
        AddState(States.HighlightedFocusedOff, Styling.Colors.PrimaryLight, Styling.Colors.White, Styling.Colors.White, true, false);

        // PER V1 comment: // dark looks weird so staying with normal primary. This matches the default template
        AddState(States.PushedOn, Styling.Colors.Primary, Styling.Colors.White, Styling.Colors.White, false, true);
        AddState(States.PushedOff, Styling.Colors.Primary, Styling.Colors.White, Styling.Colors.White, false, false);

        if (tryCreateFormsObject)
        {
            FormsControlAsObject = new RadioButton(this);
        }
    }
    public RadioButton FormsControl => FormsControlAsObject as RadioButton;
}
