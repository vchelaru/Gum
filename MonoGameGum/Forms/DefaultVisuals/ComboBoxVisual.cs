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

            var comboBoxCategory = new StateSaveCategory();
            comboBoxCategory.Name = "ComboBoxCategory";
            this.AddCategory(comboBoxCategory);

            void AddVariable(StateSave state, string name, object value)
            {
                state.Variables.Add(new VariableSave
                {
                    Name = name,
                    Value = value
                });
            }

            comboBoxCategory.States.Add(States.Enabled);
            AddVariable(States.Enabled, "TextInstance.Color", Styling.Colors.White);
            AddVariable(States.Enabled, "DropdownIndicator.Color", Styling.Colors.Primary);
            AddVariable(States.Enabled, "FocusedIndicator.Visible", false);

            comboBoxCategory.States.Add(States.Disabled);
            AddVariable(States.Disabled, "TextInstance.Color", Styling.Colors.Gray);
            AddVariable(States.Disabled, "DropdownIndicator.Color", Styling.Colors.Gray);
            AddVariable(States.Disabled, "FocusedIndicator.Visible", false);

            comboBoxCategory.States.Add(States.DisabledFocused);
            AddVariable(States.DisabledFocused, "TextInstance.Color", Styling.Colors.Gray);
            AddVariable(States.DisabledFocused, "DropdownIndicator.Color", Styling.Colors.Gray);
            AddVariable(States.DisabledFocused, "FocusedIndicator.Visible", true);

            comboBoxCategory.States.Add(States.Focused);
            AddVariable(States.Focused, "TextInstance.Color", Styling.Colors.White);
            AddVariable(States.Focused, "DropdownIndicator.Color", Styling.Colors.White);
            AddVariable(States.Focused, "FocusedIndicator.Visible", true);

            comboBoxCategory.States.Add(States.Highlighted);
            AddVariable(States.Highlighted, "TextInstance.Color", Styling.Colors.PrimaryLight);
            AddVariable(States.Highlighted, "DropdownIndicator.Color", Styling.Colors.PrimaryLight);
            AddVariable(States.Highlighted, "FocusedIndicator.Visible", false);

            comboBoxCategory.States.Add(States.HighlightedFocused);
            AddVariable(States.HighlightedFocused, "TextInstance.Color", Styling.Colors.PrimaryLight);
            AddVariable(States.HighlightedFocused, "DropdownIndicator.Color", Styling.Colors.PrimaryLight);
            AddVariable(States.HighlightedFocused, "FocusedIndicator.Visible", true);

            comboBoxCategory.States.Add(States.Pushed);
            AddVariable(States.Pushed, "TextInstance.Color", Styling.Colors.PrimaryDark);
            AddVariable(States.Pushed, "DropdownIndicator.Color", Styling.Colors.PrimaryDark);
            AddVariable(States.Pushed, "FocusedIndicator.Visible", false);

            if (tryCreateFormsObject)
            {
                FormsControlAsObject = new ComboBox(this);
            }
        }

        public ComboBox FormsControl => FormsControlAsObject as ComboBox;

    }
}
