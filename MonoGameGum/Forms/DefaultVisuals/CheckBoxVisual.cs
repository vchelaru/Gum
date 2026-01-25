using Gum.DataTypes.Variables;
using Gum.Wireframe;
using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;



#if RAYLIB
using Gum.GueDeriving;
using Raylib_cs;
#else
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameGum.GueDeriving;
using RenderingLibrary.Math.Geometry;
#endif
using Gum.Forms.Controls;
namespace Gum.Forms.DefaultVisuals;

public class CheckBoxVisual : InteractiveGue
{
    public NineSliceRuntime CheckBoxBackground { get; private set; }
    public SpriteRuntime InnerCheck { get; private set; }
    public TextRuntime TextInstance { get; private set; }
    public NineSliceRuntime FocusedIndicator { get; private set; }

    public class CheckBoxCategoryStates
    {
        public StateSave EnabledOn { get; set; } = new StateSave { Name = nameof(EnabledOn) };
        public StateSave EnabledOff { get; set; } = new StateSave { Name = nameof(EnabledOff) };
        public StateSave EnabledIndeterminate { get; set; } = new StateSave { Name = nameof(EnabledIndeterminate) };
        public StateSave DisabledOn { get; set; } = new StateSave { Name = nameof(DisabledOn) };
        public StateSave DisabledOff { get; set; } = new StateSave { Name = nameof(DisabledOff) };
        public StateSave DisabledIndeterminate { get; set; } = new StateSave { Name = nameof(DisabledIndeterminate) };
        public StateSave HighlightedOn { get; set; } = new StateSave { Name = nameof(HighlightedOn) };
        public StateSave HighlightedOff { get; set; } = new StateSave { Name = nameof(HighlightedOff) };
        public StateSave HighlightedIndeterminate { get; set; } = new StateSave { Name = nameof(HighlightedIndeterminate) };
        public StateSave PushedOn { get; set; } = new StateSave { Name = nameof(PushedOn) };
        public StateSave PushedOff { get; set; } = new StateSave { Name = nameof(PushedOff) };
        public StateSave PushedIndeterminate { get; set; } = new StateSave { Name = nameof(PushedIndeterminate) };
        public StateSave FocusedOn { get; set; } = new StateSave { Name = nameof(FocusedOn) };
        public StateSave FocusedOff { get; set; } = new StateSave { Name = nameof(FocusedOff) };
        public StateSave FocusedIndeterminate { get; set; } = new StateSave { Name = nameof(FocusedIndeterminate) };
        public StateSave HighlightedFocusedOn { get; set; } = new StateSave { Name = nameof(HighlightedFocusedOn) };
        public StateSave HighlightedFocusedOff { get; set; } = new StateSave { Name = nameof(HighlightedFocusedOff) };
        public StateSave HighlightedFocusedIndeterminate { get; set; } = new StateSave { Name = nameof(HighlightedFocusedIndeterminate) };
        public StateSave DisabledFocusedOn { get; set; } = new StateSave { Name = nameof(DisabledFocusedOn) };
        public StateSave DisabledFocusedOff { get; set; } = new StateSave { Name = nameof(DisabledFocusedOff) };
        public StateSave DisabledFocusedIndeterminate { get; set; } = new StateSave { Name = nameof(DisabledFocusedIndeterminate) };
    }


    public CheckBoxCategoryStates States;

    public StateSaveCategory CheckboxCategory { get; private set; }

    public CheckBoxVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true) : base(new InvisibleRenderable())
    {
        this.HasEvents = true;
        Height = 32;
        Width = 128;

        States = new CheckBoxCategoryStates();
        var uiSpriteSheetTexture = Styling.ActiveStyle.SpriteSheet;

        CheckBoxBackground = new NineSliceRuntime();
        CheckBoxBackground.Width = 24;
        CheckBoxBackground.Height = 24;
        CheckBoxBackground.Color = Styling.ActiveStyle.Colors.Warning;
        CheckBoxBackground.YUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle;
        CheckBoxBackground.YOrigin = VerticalAlignment.Center;
        CheckBoxBackground.Name = "CheckBoxBackground";
        CheckBoxBackground.Texture = uiSpriteSheetTexture;
        CheckBoxBackground.ApplyState(Styling.ActiveStyle.NineSlice.Bordered);
        this.AddChild(CheckBoxBackground);

        InnerCheck = new SpriteRuntime();
        InnerCheck.Width = 100f;
        InnerCheck.Height = 100f;
        InnerCheck.WidthUnits = Gum.DataTypes.DimensionUnitType.PercentageOfSourceFile;
        InnerCheck.HeightUnits = Gum.DataTypes.DimensionUnitType.PercentageOfSourceFile;
        InnerCheck.XUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle;
        InnerCheck.YUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle;
        InnerCheck.XOrigin = HorizontalAlignment.Center;
        InnerCheck.YOrigin = VerticalAlignment.Center;
        InnerCheck.Name = "InnerCheck";
        InnerCheck.Color = Styling.ActiveStyle.Colors.White;
        InnerCheck.Texture = uiSpriteSheetTexture;
        InnerCheck.ApplyState(Styling.ActiveStyle.Icons.Check);
        CheckBoxBackground.Children.Add(InnerCheck);

        TextInstance = new TextRuntime();
        TextInstance.X = 0;
        TextInstance.Y = 0;
        TextInstance.XUnits = Gum.Converters.GeneralUnitType.PixelsFromLarge;
        TextInstance.YUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle;
        TextInstance.XOrigin = HorizontalAlignment.Right;
        TextInstance.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
        TextInstance.Width = -28;
        TextInstance.Height = 0;
        TextInstance.Name = "TextInstance";
        TextInstance.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
        TextInstance.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        TextInstance.ApplyState(Styling.ActiveStyle.Text.Normal);
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
        FocusedIndicator.ApplyState(Styling.ActiveStyle.NineSlice.Solid);
        FocusedIndicator.Visible = false;
        FocusedIndicator.Color = Styling.ActiveStyle.Colors.Warning;
        this.AddChild(FocusedIndicator);

        CheckboxCategory = new Gum.DataTypes.Variables.StateSaveCategory();
        CheckboxCategory.Name = "CheckBoxCategory";
        this.AddCategory(CheckboxCategory);

        void AddVariable(StateSave state, string name, object value)
        {
            state.Variables.Add(new VariableSave
            {
                Name = name,
                Value = value
            });
        }

        void AddVariablesFromIconVisual(StateSave state, string parentObject, StateSave iconSaveState )
        {
            foreach( var variable in iconSaveState.Variables )
            {
                AddVariable(state, $"{parentObject}.{variable.Name}", variable.Value);
            }
        }

        void AddState(StateSave state, Color checkboxBackgroundColor,
            Color textColor, Color checkColor, bool isFocused, bool checkVisible, StateSave iconSaveState)
        {
            CheckboxCategory.States.Add(state);
            AddVariable(state, "InnerCheck.Visible", checkVisible);
            AddVariable(state, "InnerCheck.Color", checkColor);
            AddVariable(state, "CheckBoxBackground.Color", checkboxBackgroundColor);
            AddVariable(state, "FocusedIndicator.Visible", isFocused);
            AddVariable(state, "TextInstance.Color", textColor);
            AddVariablesFromIconVisual(state, "InnerCheck", iconSaveState);
        }

        AddState(States.EnabledOn, Styling.ActiveStyle.Colors.Primary, Styling.ActiveStyle.Colors.White, Styling.ActiveStyle.Colors.White, false, true, Styling.ActiveStyle.Icons.Check);
        AddState(States.EnabledOff, Styling.ActiveStyle.Colors.Primary, Styling.ActiveStyle.Colors.White, Styling.ActiveStyle.Colors.White, false, false, Styling.ActiveStyle.Icons.Check);
        AddState(States.EnabledIndeterminate, Styling.ActiveStyle.Colors.Primary, Styling.ActiveStyle.Colors.White, Styling.ActiveStyle.Colors.White, false, true, Styling.ActiveStyle.Icons.Dash);

        AddState(States.DisabledOn, Styling.ActiveStyle.Colors.DarkGray, Styling.ActiveStyle.Colors.Gray, Styling.ActiveStyle.Colors.Gray, false, true, Styling.ActiveStyle.Icons.Check);
        AddState(States.DisabledOff, Styling.ActiveStyle.Colors.DarkGray, Styling.ActiveStyle.Colors.Gray, Styling.ActiveStyle.Colors.Gray, false, false, Styling.ActiveStyle.Icons.Check);
        AddState(States.DisabledIndeterminate, Styling.ActiveStyle.Colors.DarkGray, Styling.ActiveStyle.Colors.Gray, Styling.ActiveStyle.Colors.Gray, false, true, Styling.ActiveStyle.Icons.Dash);

        AddState(States.DisabledFocusedOn, Styling.ActiveStyle.Colors.DarkGray, Styling.ActiveStyle.Colors.Gray, Styling.ActiveStyle.Colors.Gray, true, true, Styling.ActiveStyle.Icons.Check);
        AddState(States.DisabledFocusedOff, Styling.ActiveStyle.Colors.DarkGray, Styling.ActiveStyle.Colors.Gray, Styling.ActiveStyle.Colors.Gray, true, false, Styling.ActiveStyle.Icons.Check);
        AddState(States.DisabledFocusedIndeterminate, Styling.ActiveStyle.Colors.DarkGray, Styling.ActiveStyle.Colors.Gray, Styling.ActiveStyle.Colors.Gray, true, true, Styling.ActiveStyle.Icons.Dash);

        AddState(States.FocusedOn, Styling.ActiveStyle.Colors.Primary, Styling.ActiveStyle.Colors.White, Styling.ActiveStyle.Colors.White, true, true, Styling.ActiveStyle.Icons.Check);
        AddState(States.FocusedOff, Styling.ActiveStyle.Colors.Primary, Styling.ActiveStyle.Colors.White, Styling.ActiveStyle.Colors.White, true, false, Styling.ActiveStyle.Icons.Check);
        AddState(States.FocusedIndeterminate, Styling.ActiveStyle.Colors.Primary, Styling.ActiveStyle.Colors.White, Styling.ActiveStyle.Colors.White, true, true, Styling.ActiveStyle.Icons.Dash);

        AddState(States.HighlightedOn, Styling.ActiveStyle.Colors.PrimaryLight, Styling.ActiveStyle.Colors.White, Styling.ActiveStyle.Colors.White, false, true, Styling.ActiveStyle.Icons.Check);
        AddState(States.HighlightedOff, Styling.ActiveStyle.Colors.PrimaryLight, Styling.ActiveStyle.Colors.White, Styling.ActiveStyle.Colors.White, false, false, Styling.ActiveStyle.Icons.Check);
        AddState(States.HighlightedIndeterminate, Styling.ActiveStyle.Colors.PrimaryLight, Styling.ActiveStyle.Colors.White, Styling.ActiveStyle.Colors.White, false, true, Styling.ActiveStyle.Icons.Dash);

        AddState(States.HighlightedFocusedOn, Styling.ActiveStyle.Colors.PrimaryLight, Styling.ActiveStyle.Colors.White, Styling.ActiveStyle.Colors.White, true, true, Styling.ActiveStyle.Icons.Check);
        AddState(States.HighlightedFocusedOff, Styling.ActiveStyle.Colors.PrimaryLight, Styling.ActiveStyle.Colors.White, Styling.ActiveStyle.Colors.White, true, false, Styling.ActiveStyle.Icons.Check);
        AddState(States.HighlightedFocusedIndeterminate, Styling.ActiveStyle.Colors.PrimaryLight, Styling.ActiveStyle.Colors.White, Styling.ActiveStyle.Colors.White, true, true, Styling.ActiveStyle.Icons.Dash);

        // PER V1 comment: // dark looks weird so staying with normal primary. This matches the default template
        AddState(States.PushedOn, Styling.ActiveStyle.Colors.Primary, Styling.ActiveStyle.Colors.White, Styling.ActiveStyle.Colors.White, false, true, Styling.ActiveStyle.Icons.Check);
        AddState(States.PushedOff, Styling.ActiveStyle.Colors.Primary, Styling.ActiveStyle.Colors.White, Styling.ActiveStyle.Colors.White, false, false, Styling.ActiveStyle.Icons.Check);
        AddState(States.PushedIndeterminate, Styling.ActiveStyle.Colors.Primary, Styling.ActiveStyle.Colors.White, Styling.ActiveStyle.Colors.White, false, true, Styling.ActiveStyle.Icons.Dash);

        if (tryCreateFormsObject)
        {
            FormsControlAsObject = new CheckBox(this);
        }
    }

    public CheckBox FormsControl => FormsControlAsObject as CheckBox;
}
