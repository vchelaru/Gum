using Gum.Converters;
using Gum.DataTypes.Variables;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
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
    public class ComboBoxVisual : InteractiveGue
    {
        public NineSliceRuntime Background {  get; private set; }
        public TextRuntime TextInstance { get; private set; }
        public ListBoxVisual ListBoxInstance { get; private set; }
        public SpriteRuntime DropdownIndicator { get; private set; }
        public NineSliceRuntime FocusedIndicator { get; private set; }

        public class ComboBoxCategoryStates
        {
            public StateSave Enabled { get; set; }
            public StateSave Disabled { get; set; }
            public StateSave DisabledFocused { get; set; }
            public StateSave Focused { get; set; }
            public StateSave Highlighted { get; set; }
            public StateSave HighlightedFocused { get; set; }
            public StateSave Pushed { get; set; }
        }

        public ComboBoxCategoryStates States;

        public ComboBoxVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true) : base(new InvisibleRenderable())
        {
            if (fullInstantiation)
            {
                this.States = new ComboBoxCategoryStates();
                var uiSpriteSheetTexture = (Texture2D)RenderingLibrary.Content.LoaderManager.Self.GetDisposable($"EmbeddedResource.{RenderingLibrary.SystemManagers.AssemblyPrefix}.UISpriteSheet.png");

                this.Height = 24f;
                this.Width = 256f;

                Background = new NineSliceRuntime();
                Background.Name = "Background";
                Background.X = 0f;
                Background.XUnits = GeneralUnitType.PixelsFromMiddle;
                Background.Y = 0f;
                Background.YUnits = GeneralUnitType.PixelsFromMiddle;
                Background.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
                Background.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
                Background.Width = 0f;
                Background.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
                Background.Height = 0f;
                Background.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
                Background.Color = Styling.Colors.DarkGray;
                Background.TextureAddress = Gum.Managers.TextureAddress.Custom;
                Background.Texture = uiSpriteSheetTexture;
                Background.ApplyState(NineSliceStyles.Bordered);
                this.Children.Add(Background);

                TextInstance = new TextRuntime();
                TextInstance.Name = "TextInstance";
                TextInstance.Text = "Selected Item";
                TextInstance.X = 0f;
                TextInstance.XUnits = GeneralUnitType.PixelsFromMiddle;
                TextInstance.Y = 0f;
                TextInstance.YUnits = GeneralUnitType.PixelsFromMiddle;
                TextInstance.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
                TextInstance.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
                TextInstance.Width = -8f;
                TextInstance.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
                TextInstance.Height = 0;
                TextInstance.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
                TextInstance.HorizontalAlignment = global::RenderingLibrary.Graphics.HorizontalAlignment.Left;
                TextInstance.VerticalAlignment = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
                TextInstance.Color = Styling.Colors.White;
                TextInstance.ApplyState(TextStyles.Strong);
                this.Children.Add(TextInstance);

                ListBoxInstance = new ListBoxVisual(tryCreateFormsObject: false);
                ListBoxInstance.Name = "ListBoxInstance";
                ListBoxInstance.Y = 28f;
                ListBoxInstance.Width = 0f;
                ListBoxInstance.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
                ListBoxInstance.Height = 128f;
                ListBoxInstance.Visible = false;
                this.Children.Add(ListBoxInstance);

                DropdownIndicator = new SpriteRuntime();
                DropdownIndicator.Name = "DropdownIndicator";
                DropdownIndicator.X = -12f;
                DropdownIndicator.XUnits = GeneralUnitType.PixelsFromLarge;
                DropdownIndicator.Y = 12f;
                DropdownIndicator.YUnits = GeneralUnitType.PixelsFromSmall;
                DropdownIndicator.XOrigin = HorizontalAlignment.Center;
                DropdownIndicator.YOrigin = VerticalAlignment.Center;
                DropdownIndicator.Width = 24f;
                DropdownIndicator.WidthUnits = Gum.DataTypes.DimensionUnitType.Absolute;
                DropdownIndicator.Height = 24f;
                DropdownIndicator.HeightUnits = Gum.DataTypes.DimensionUnitType.Absolute;
                DropdownIndicator.Rotation = -90;
                DropdownIndicator.Texture = uiSpriteSheetTexture;
                DropdownIndicator.Color = Styling.Colors.Primary;
                DropdownIndicator.ApplyState(IconVisuals.Arrow2);
                this.Children.Add(DropdownIndicator);

                FocusedIndicator = new NineSliceRuntime();
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
                FocusedIndicator.Color = Styling.Colors.White;
                FocusedIndicator.Visible = false;
                FocusedIndicator.Name = "FocusedIndicator";
                FocusedIndicator.TextureAddress = Gum.Managers.TextureAddress.Custom;
                FocusedIndicator.Texture = uiSpriteSheetTexture;
                FocusedIndicator.ApplyState(NineSliceStyles.Bordered);
                this.Children.Add(FocusedIndicator);

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


                AddState(FrameworkElement.EnabledStateName);
                AddVariable("TextInstance.Color", Styling.Colors.White);
                AddVariable("DropdownIndicator.Color", Styling.Colors.Primary);
                AddVariable("FocusedIndicator.Visible", false);
                States.Enabled = currentState;

                AddState(FrameworkElement.DisabledStateName);
                AddVariable("TextInstance.Color", Styling.Colors.Gray);
                AddVariable("DropdownIndicator.Color", Styling.Colors.Gray);
                AddVariable("FocusedIndicator.Visible", false);
                States.Disabled = currentState;

                AddState(FrameworkElement.DisabledFocusedStateName);
                AddVariable("TextInstance.Color", Styling.Colors.Gray);
                AddVariable("DropdownIndicator.Color", Styling.Colors.Gray);
                AddVariable("FocusedIndicator.Visible", true);
                States.DisabledFocused = currentState;

                AddState(FrameworkElement.FocusedStateName);
                AddVariable("TextInstance.Color", Styling.Colors.White);
                AddVariable("DropdownIndicator.Color", Styling.Colors.White);
                AddVariable("FocusedIndicator.Visible", true);
                States.Focused = currentState;

                AddState(FrameworkElement.HighlightedStateName);
                AddVariable("TextInstance.Color", Styling.Colors.PrimaryLight);
                AddVariable("DropdownIndicator.Color", Styling.Colors.PrimaryLight);
                AddVariable("FocusedIndicator.Visible", false);
                States.Highlighted = currentState;

                AddState(FrameworkElement.HighlightedFocusedStateName);
                AddVariable("TextInstance.Color", Styling.Colors.PrimaryLight);
                AddVariable("DropdownIndicator.Color", Styling.Colors.PrimaryLight);
                AddVariable("FocusedIndicator.Visible", true);
                States.HighlightedFocused = currentState;

                AddState(FrameworkElement.PushedStateName);
                AddVariable("TextInstance.Color", Styling.Colors.PrimaryDark);
                AddVariable("DropdownIndicator.Color", Styling.Colors.PrimaryDark);
                AddVariable("FocusedIndicator.Visible", false);
                States.Pushed = currentState;

            }
            if (tryCreateFormsObject)
            {
                FormsControlAsObject = new ComboBox(this);
            }
        }

        public ComboBox FormsControl => FormsControlAsObject as ComboBox;

    }
}
