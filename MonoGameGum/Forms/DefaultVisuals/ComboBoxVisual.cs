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
            public StateSave Enabled { get; set; } = new StateSave() { Name = FrameworkElement.EnabledStateName };
            public StateSave Disabled { get; set; } = new StateSave() { Name = FrameworkElement.DisabledStateName };
            public StateSave DisabledFocused { get; set; } = new StateSave() { Name = FrameworkElement.DisabledFocusedStateName };
            public StateSave Focused { get; set; } = new StateSave() { Name = FrameworkElement.FocusedStateName };
            public StateSave Highlighted { get; set; } = new StateSave() { Name = FrameworkElement.HighlightedStateName };
            public StateSave HighlightedFocused { get; set; } = new StateSave() { Name = FrameworkElement.HighlightedFocusedStateName };
            public StateSave Pushed { get; set; } = new StateSave() { Name = FrameworkElement.PushedStateName };
        }

        public ComboBoxCategoryStates States;

        public StateSaveCategory ComboBoxCategory { get; private set; }

        public ComboBoxVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true) : base(new InvisibleRenderable())
        {
            Height = 24f;
            Width = 256f;

            States = new ComboBoxCategoryStates();
            var uiSpriteSheetTexture = Styling.ActiveStyle.SpriteSheet;

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
            Background.Texture = uiSpriteSheetTexture;
            Background.ApplyState(Styling.NineSlice.Bordered);
            this.AddChild(Background);

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
            TextInstance.ApplyState(Styling.Text.Strong);
            this.AddChild(TextInstance);

            ListBoxInstance = new ListBoxVisual(tryCreateFormsObject: false);
            ListBoxInstance.Name = "ListBoxInstance";
            ListBoxInstance.Y = 28f;
            ListBoxInstance.Width = 0f;
            ListBoxInstance.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
            ListBoxInstance.Height = 128f;
            ListBoxInstance.Visible = false;
            this.AddChild(ListBoxInstance);

            DropdownIndicator = new SpriteRuntime();
            DropdownIndicator.Name = "DropdownIndicator";
            DropdownIndicator.X = -12f;
            DropdownIndicator.XUnits = GeneralUnitType.PixelsFromLarge;
            DropdownIndicator.Y = 12f;
            DropdownIndicator.YUnits = GeneralUnitType.PixelsFromSmall;
            DropdownIndicator.XOrigin = HorizontalAlignment.Center;
            DropdownIndicator.YOrigin = VerticalAlignment.Center;
            DropdownIndicator.Width = 100f;
            DropdownIndicator.WidthUnits = Gum.DataTypes.DimensionUnitType.PercentageOfSourceFile;
            DropdownIndicator.Height = 100f;
            DropdownIndicator.HeightUnits = Gum.DataTypes.DimensionUnitType.PercentageOfSourceFile;
            DropdownIndicator.Rotation = -90;
            DropdownIndicator.Texture = uiSpriteSheetTexture;
            DropdownIndicator.Color = Styling.Colors.Primary;
            DropdownIndicator.ApplyState(Styling.Icons.Arrow2);
            this.AddChild(DropdownIndicator);

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

            ComboBoxCategory = new StateSaveCategory();
            ComboBoxCategory.Name = "ComboBoxCategory";
            this.AddCategory(ComboBoxCategory);

            void AddVariable(StateSave state, string name, object value)
            {
                state.Variables.Add(new VariableSave
                {
                    Name = name,
                    Value = value
                });
            }

            void AddState(StateSave state, Color dropdownIndicatorColor, Color textInstanceColor, bool isFocusedVisible)
            {
                ComboBoxCategory.States.Add(state);
                AddVariable(state, "DropdownIndicator.Color", dropdownIndicatorColor);
                AddVariable(state, "TextInstance.Color", textInstanceColor);
                AddVariable(state, "FocusedIndicator.Visible", isFocusedVisible);
            }

            AddState(States.Enabled, Styling.Colors.Primary, Styling.Colors.White, false);
            AddState(States.Disabled, Styling.Colors.Gray, Styling.Colors.Gray, false);
            AddState(States.DisabledFocused, Styling.Colors.Gray, Styling.Colors.Gray, true);
            AddState(States.Focused, Styling.Colors.White, Styling.Colors.White, true);
            AddState(States.Highlighted, Styling.Colors.PrimaryLight, Styling.Colors.PrimaryLight, false);
            AddState(States.HighlightedFocused, Styling.Colors.PrimaryLight, Styling.Colors.PrimaryLight, true);
            AddState(States.Pushed, Styling.Colors.PrimaryDark, Styling.Colors.PrimaryDark, false);

            if (tryCreateFormsObject)
            {
                FormsControlAsObject = new ComboBox(this);
            }
        }

        public ComboBox FormsControl => FormsControlAsObject as ComboBox;

    }
}
