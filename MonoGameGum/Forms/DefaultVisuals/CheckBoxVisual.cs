using Gum.DataTypes.Variables;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
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
    public class CheckBoxVisual : InteractiveGue
    {
        public NineSliceRuntime CheckBoxBackground { get; private set; }
        public SpriteRuntime InnerCheckbox { get; private set; }
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


        public CheckBoxVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true) : base(new InvisibleRenderable())
        {
            if (fullInstantiation)
            {
                Height = 32;
                Width = 128;

                States = new CheckBoxCategoryStates();
                var uiSpriteSheetTexture = Styling.ActiveStyle.SpriteSheet;

                CheckBoxBackground = new NineSliceRuntime();
                CheckBoxBackground.Width = 24;
                CheckBoxBackground.Height = 24;
                CheckBoxBackground.Color = Styling.Colors.Warning;
                CheckBoxBackground.YUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle;
                CheckBoxBackground.YOrigin = VerticalAlignment.Center;
                CheckBoxBackground.Name = "CheckBoxBackground";
                CheckBoxBackground.Texture = uiSpriteSheetTexture;
                CheckBoxBackground.ApplyState(Styling.NineSlice.Bordered);
                this.AddChild(CheckBoxBackground);

                InnerCheckbox = new SpriteRuntime();
                InnerCheckbox.Width = 100f;
                InnerCheckbox.Height = 100f;
                InnerCheckbox.WidthUnits = Gum.DataTypes.DimensionUnitType.PercentageOfSourceFile;
                InnerCheckbox.HeightUnits = Gum.DataTypes.DimensionUnitType.PercentageOfSourceFile;
                InnerCheckbox.XUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle;
                InnerCheckbox.YUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle;
                InnerCheckbox.XOrigin = HorizontalAlignment.Center;
                InnerCheckbox.YOrigin = VerticalAlignment.Center;
                InnerCheckbox.Name = "InnerCheck";
                InnerCheckbox.Color = Styling.Colors.White;
                InnerCheckbox.Texture = uiSpriteSheetTexture;
                InnerCheckbox.ApplyState(Styling.Icons.Check);
                CheckBoxBackground.Children.Add(InnerCheckbox);

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
                FocusedIndicator.TextureAddress = Gum.Managers.TextureAddress.Custom;
                FocusedIndicator.Texture = uiSpriteSheetTexture;
                FocusedIndicator.ApplyState(Styling.NineSlice.Solid);
                FocusedIndicator.Visible = false;
                FocusedIndicator.Color = Styling.Colors.Warning;
                FocusedIndicator.Name = "FocusedIndicator";
                this.AddChild(FocusedIndicator);

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

                void AddVariablesFromIconVIsual(string parentObject, StateSave iconSaveState )
                {
                    foreach( var variable in iconSaveState.Variables )
                    {
                        AddVariable($"{parentObject}.{variable.Name}", variable.Value);
                    }
                }

                void AddOnOffState(string state, Color checkboxBackgroundColor, 
                    Color textColor, Color checkColor, bool isFocused)
                {
                    AddState(state + "On");
                    AddVariable("InnerCheck.Visible", true);
                    AddVariable("InnerCheck.Color", checkColor);
                    AddVariable("CheckBoxBackground.Color", checkboxBackgroundColor);
                    AddVariable("FocusedIndicator.Visible", isFocused);
                    AddVariable("TextInstance.Color", textColor);
                    AddVariablesFromIconVIsual("InnerCheck", Styling.Icons.Check);

                    AddState(state + "Off");
                    AddVariable("InnerCheck.Visible", false);
                    AddVariable("InnerCheck.Color", checkColor);
                    AddVariable("CheckBoxBackground.Color", checkboxBackgroundColor);
                    AddVariable("FocusedIndicator.Visible", isFocused);
                    AddVariable("TextInstance.Color", textColor);
                    AddVariablesFromIconVIsual("InnerCheck", Styling.Icons.Check);

                    AddState(state + "Indeterminate");
                    AddVariable("InnerCheck.Visible", true);
                    AddVariable("InnerCheck.Color", checkColor);
                    AddVariable("CheckBoxBackground.Color", checkboxBackgroundColor);
                    AddVariable("FocusedIndicator.Visible", isFocused);
                    AddVariable("TextInstance.Color", textColor);
                    AddVariablesFromIconVIsual("InnerCheck", Styling.Icons.Dash);

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
                this.States.EnabledOn = checkboxCategory.States.Find(x => x.Name == nameof(this.States.EnabledOn));
                this.States.EnabledOff = checkboxCategory.States.Find(x => x.Name == nameof(this.States.EnabledOff));
                this.States.EnabledIndeterminate = checkboxCategory.States.Find(x => x.Name == nameof(this.States.EnabledIndeterminate));

                this.States.DisabledOn = checkboxCategory.States.Find(x => x.Name == nameof(this.States.DisabledOn));
                this.States.DisabledOff = checkboxCategory.States.Find(x => x.Name == nameof(this.States.DisabledOff));
                this.States.DisabledIndeterminate = checkboxCategory.States.Find(x => x.Name == nameof(this.States.DisabledIndeterminate));

                this.States.HighlightedOn = checkboxCategory.States.Find(x => x.Name == nameof(this.States.HighlightedOn));
                this.States.HighlightedOff = checkboxCategory.States.Find(x => x.Name == nameof(this.States.HighlightedOff));
                this.States.HighlightedIndeterminate = checkboxCategory.States.Find(x => x.Name == nameof(this.States.HighlightedIndeterminate));

                this.States.PushedOn = checkboxCategory.States.Find(x => x.Name == nameof(this.States.PushedOn));
                this.States.PushedOff = checkboxCategory.States.Find(x => x.Name == nameof(this.States.PushedOff));
                this.States.PushedIndeterminate = checkboxCategory.States.Find(x => x.Name == nameof(this.States.PushedIndeterminate));

                this.States.FocusedOn = checkboxCategory.States.Find(x => x.Name == nameof(this.States.FocusedOn));
                this.States.FocusedOff = checkboxCategory.States.Find(x => x.Name == nameof(this.States.FocusedOff));
                this.States.FocusedIndeterminate = checkboxCategory.States.Find(x => x.Name == nameof(this.States.FocusedIndeterminate));

                this.States.HighlightedFocusedOn = checkboxCategory.States.Find(x => x.Name == nameof(this.States.HighlightedFocusedOn));
                this.States.HighlightedFocusedOff = checkboxCategory.States.Find(x => x.Name == nameof(this.States.HighlightedFocusedOff));
                this.States.HighlightedFocusedIndeterminate = checkboxCategory.States.Find(x => x.Name == nameof(this.States.HighlightedFocusedIndeterminate));

                this.States.DisabledFocusedOn = checkboxCategory.States.Find(x => x.Name == nameof(this.States.DisabledFocusedOn));
                this.States.DisabledFocusedOff = checkboxCategory.States.Find(x => x.Name == nameof(this.States.DisabledFocusedOff));
                this.States.DisabledFocusedIndeterminate = checkboxCategory.States.Find(x => x.Name == nameof(this.States.DisabledFocusedIndeterminate));


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
