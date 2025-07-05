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
        public NineSliceRuntime CheckBoxBackground {  get; private set; }
        public RectangleRuntime FocusedIndicator { get; private set; }
        public TextRuntime TextInstance { get; private set; }

        public class CheckBoxCategoryStates
        {
            public StateSave EnabledOn { get; set; }
            public StateSave EnabledOff { get; set; }
            public StateSave EnabledIndeterminate { get; set; }
            public StateSave DisabledOn { get; set; }
            public StateSave DisabledOff { get; set; }
            public StateSave DisabledIndeterminate { get; set; }
            public StateSave HighlightedOn { get; set; }
            public StateSave HighlightedOff { get; set; }
            public StateSave HighlightedIndeterminate { get; set; }
            public StateSave PushedOn { get; set; }
            public StateSave PushedOff { get; set; }
            public StateSave PushedIndeterminate { get; set; }
            public StateSave FocusedOn { get; set; }
            public StateSave FocusedOff { get; set; }
            public StateSave FocusedIndeterminate { get; set; }
            public StateSave HighlightedFocusedOn { get; set; }
            public StateSave HighlightedFocusedOff { get; set; }
            public StateSave HighlightedFocusedIndeterminate { get; set; }
            public StateSave DisabledFocusedOn { get; set; }
            public StateSave DisabledFocusedOff { get; set; }
            public StateSave DisabledFocusedIndeterminate { get; set; }

        }

        public CheckBoxCategoryStates States;


        public CheckBoxVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true) : base(new InvisibleRenderable())
        {
            if (fullInstantiation)
            {
                this.Height = 32;
                this.Width = 128;

                this.States = new CheckBoxCategoryStates();

                var uiSpriteSheetTexture = (Texture2D)RenderingLibrary.Content.LoaderManager.Self.GetDisposable($"EmbeddedResource.{RenderingLibrary.SystemManagers.AssemblyPrefix}.UISpriteSheet.png");

                CheckBoxBackground = new NineSliceRuntime();
                CheckBoxBackground.Width = 24;
                CheckBoxBackground.Height = 24;
                CheckBoxBackground.Color = Styling.Colors.Warning;
                CheckBoxBackground.YUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle;
                CheckBoxBackground.YOrigin = VerticalAlignment.Center;
                CheckBoxBackground.Name = "CheckBoxBackground";
                CheckBoxBackground.Texture = uiSpriteSheetTexture;
                CheckBoxBackground.ApplyState(NineSliceStyles.Bordered);
                this.Children.Add(CheckBoxBackground);

                var innerCheck = new SpriteRuntime();
                innerCheck.Width = 0;
                innerCheck.Height = 0;
                innerCheck.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
                innerCheck.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
                innerCheck.XUnits = Gum.Converters.GeneralUnitType.PixelsFromSmall;
                innerCheck.YUnits = Gum.Converters.GeneralUnitType.PixelsFromSmall;
                innerCheck.XOrigin = HorizontalAlignment.Left;
                innerCheck.YOrigin = VerticalAlignment.Top;
                innerCheck.Name = "InnerCheck";
                innerCheck.Color = Styling.Colors.White;
                innerCheck.Texture = uiSpriteSheetTexture;
                innerCheck.ApplyState(IconVisuals.Check);
                CheckBoxBackground.Children.Add(innerCheck);

                TextInstance = new TextRuntime();
                TextInstance.X = 28;
                TextInstance.Y = 0;
                TextInstance.YOrigin = RenderingLibrary.Graphics.VerticalAlignment.Center;
                TextInstance.YUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle;
                TextInstance.Width = -36;
                TextInstance.Height = 0;
                TextInstance.Name = "TextInstance";
                TextInstance.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
                TextInstance.ApplyState(TextStyles.Normal);
                this.Children.Add(TextInstance);

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
                    AddVariablesFromIconVIsual("InnerCheck", IconVisuals.Check);


                    AddState(state + "Off");
                    AddVariable("InnerCheck.Visible", false);
                    AddVariable("InnerCheck.Color", checkColor);
                    AddVariable("CheckBoxBackground.Color", checkboxBackgroundColor);
                    AddVariable("FocusedIndicator.Visible", isFocused);
                    AddVariable("TextInstance.Color", textColor);
                    AddVariablesFromIconVIsual("InnerCheck", IconVisuals.Check);

                    AddState(state + "Indeterminate");
                    AddVariable("InnerCheck.Visible", true);
                    AddVariable("InnerCheck.Color", checkColor);
                    AddVariable("CheckBoxBackground.Color", checkboxBackgroundColor);
                    AddVariable("FocusedIndicator.Visible", isFocused);
                    AddVariable("TextInstance.Color", textColor);
                    AddVariablesFromIconVIsual("InnerCheck", IconVisuals.Dash);

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
