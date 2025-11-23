using Gum.Converters;
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
#endif
using Gum.Forms.Controls;
namespace Gum.Forms.DefaultVisuals.V3;

public class ComboBoxVisual : InteractiveGue
{
    public NineSliceRuntime Background {  get; private set; }
    public TextRuntime TextInstance { get; private set; }

    ListBoxVisual listBoxInstance;
    public ListBoxVisual ListBoxInstance 
    { 
        get => listBoxInstance;
        set
        {
#if FULL_DIAGNOSTICS
            if (value == null)
            {
                throw new NullReferenceException("ListBoxInstance cannot be set to a null ListBox");
            }
            if(value.Name != "ListBoxInstance")
            {
                throw new InvalidOperationException("The assigned ListBox must be named ListBoxInstance");
            }
#endif
            listBoxInstance = value;
            this.FormsControl.ListBox = listBoxInstance.FormsControl as ListBox;
            PositionAndAttachListBox(listBoxInstance);
        }
    }
    public SpriteRuntime DropdownIndicator { get; private set; }
    public NineSliceRuntime FocusedIndicator { get; private set; }

    Color _backgroundColor;
    public Color BackgroundColor
    {
        get => _backgroundColor;
        set
        {
            if (value != _backgroundColor)
            {
                _backgroundColor = value;
                FormsControl?.UpdateState();
            }
        }
    }
    Color _foregroundColor;
    public Color ForegroundColor
    {
        get => _foregroundColor;
        set
        {
            if (value != _foregroundColor)
            {
                _foregroundColor = value;
                FormsControl?.UpdateState();
            }
        }
    }

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

        BackgroundColor = Styling.ActiveStyle.Colors.Primary;
        ForegroundColor = Styling.ActiveStyle.Colors.White;

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
        Background.Color = Styling.ActiveStyle.Colors.DarkGray;
        Background.Texture = uiSpriteSheetTexture;
        Background.ApplyState(Styling.ActiveStyle.NineSlice.Bordered);
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
        TextInstance.Color = Styling.ActiveStyle.Colors.White;
        TextInstance.ApplyState(Styling.ActiveStyle.Text.Strong);
        this.AddChild(TextInstance);

        listBoxInstance = new ListBoxVisual(tryCreateFormsObject: false);
        PositionAndAttachListBox(listBoxInstance);

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
        DropdownIndicator.Color = Styling.ActiveStyle.Colors.Primary;
        DropdownIndicator.ApplyState(Styling.ActiveStyle.Icons.Arrow2);
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
        FocusedIndicator.ApplyState(Styling.ActiveStyle.NineSlice.Solid);
        FocusedIndicator.Visible = false;
        FocusedIndicator.Color = Styling.ActiveStyle.Colors.Warning;
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

        //AddState(States.Enabled, Styling.ActiveStyle.Colors.Primary, Styling.ActiveStyle.Colors.White, false);
        //AddState(States.Disabled, Styling.ActiveStyle.Colors.Gray, Styling.ActiveStyle.Colors.Gray, false);
        //AddState(States.DisabledFocused, Styling.ActiveStyle.Colors.Gray, Styling.ActiveStyle.Colors.Gray, true);
        //AddState(States.Focused, Styling.ActiveStyle.Colors.White, Styling.ActiveStyle.Colors.White, true);
        AddState(States.Highlighted, Styling.ActiveStyle.Colors.PrimaryLight, Styling.ActiveStyle.Colors.PrimaryLight, false);
        AddState(States.HighlightedFocused, Styling.ActiveStyle.Colors.PrimaryLight, Styling.ActiveStyle.Colors.PrimaryLight, true);
        AddState(States.Pushed, Styling.ActiveStyle.Colors.PrimaryDark, Styling.ActiveStyle.Colors.PrimaryDark, false);

        DefineDynamicStyleChanges();

        if (tryCreateFormsObject)
        {
            FormsControlAsObject = new ComboBox(this);
        }
    }

    private void DefineDynamicStyleChanges()
    {
        // Some named constants vs magic values
        const float darker = -0.25f;
        const float lighter = 0.25f;
        const float greyScaleDarker = -0.30f;
        const float greyScaleLighter = 0.30f;
        const float greyScaleSuperDark = -0.50f;

        Color _backgroundColor = BackgroundColor.ToGreyscale().Adjust(greyScaleSuperDark);

        ComboBoxCategory.States.Add(States.Enabled);
        States.Enabled.Apply = () =>
        {
            SetValuesForState(_backgroundColor, BackgroundColor, ForegroundColor, false);
        };

        ComboBoxCategory.States.Add(States.Disabled);
        States.Disabled.Apply = () =>
        {
            SetValuesForState(_backgroundColor, BackgroundColor.ToGreyscale().Adjust(greyScaleDarker), ForegroundColor.ToGreyscale().Adjust(greyScaleDarker), false);
        };

        ComboBoxCategory.States.Add(States.DisabledFocused);
        States.DisabledFocused.Apply = () =>
        {
            SetValuesForState(_backgroundColor, BackgroundColor.ToGreyscale().Adjust(greyScaleDarker), ForegroundColor.ToGreyscale().Adjust(greyScaleDarker), true);
        };

        ComboBoxCategory.States.Add(States.Focused);
        States.Focused.Apply = () =>
        {
            SetValuesForState(_backgroundColor, ForegroundColor, ForegroundColor, true);
        };

        ComboBoxCategory.States.Add(States.Focused);
        States.Focused.Apply = () =>
        {
            SetValuesForState(_backgroundColor, ForegroundColor, ForegroundColor, true);
        };

        //AddState(States.Focused, Styling.ActiveStyle.Colors.White, Styling.ActiveStyle.Colors.White, true);
        //AddState(States.Highlighted, Styling.ActiveStyle.Colors.PrimaryLight, Styling.ActiveStyle.Colors.PrimaryLight, false);
        //AddState(States.HighlightedFocused, Styling.ActiveStyle.Colors.PrimaryLight, Styling.ActiveStyle.Colors.PrimaryLight, true);
        //AddState(States.Pushed, Styling.ActiveStyle.Colors.PrimaryDark, Styling.ActiveStyle.Colors.PrimaryDark, false);

    }

    private void SetValuesForState(Color backgroundColor, Color ddIndicatorColor, Color foregroundColor, bool isFocused)
    {
        Background.Color = backgroundColor;
        DropdownIndicator.Color = ddIndicatorColor;
        TextInstance.Color = foregroundColor;
        FocusedIndicator.Visible = isFocused;
    }

private void PositionAndAttachListBox(ListBoxVisual listBoxVisual)
    {
        listBoxVisual.Name = "ListBoxInstance";
        listBoxVisual.Y = 28f;
        listBoxVisual.Width = 0f;
        listBoxVisual.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        listBoxVisual.Height = 128f;
        listBoxVisual.Visible = false;
        this.AddChild(listBoxInstance);
    }

    public ComboBox FormsControl => FormsControlAsObject as ComboBox;

}
