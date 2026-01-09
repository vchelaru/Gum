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
            if (!value.Equals(_backgroundColor))
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
            if (!value.Equals(_foregroundColor))
            {
                _foregroundColor = value;
                FormsControl?.UpdateState();
            }
        }
    }

    Color _dropdownIndicatorColor;
    public Color DropdownIndicatorColor
    {
        get => _dropdownIndicatorColor;
        set
        {
            if (!value.Equals(_dropdownIndicatorColor))
            {
                _dropdownIndicatorColor = value;
                FormsControl?.UpdateState();
            }
        }
    }

    Color _focusedIndicatorColor;
    public Color FocusedIndicatorColor
    {
        get => _focusedIndicatorColor;
        set
        {
            if (!value.Equals(_focusedIndicatorColor))
            {
                _focusedIndicatorColor = value;
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
        this.AddChild(FocusedIndicator);

        ComboBoxCategory = new StateSaveCategory();
        ComboBoxCategory.Name = "ComboBoxCategory";
        this.AddCategory(ComboBoxCategory);

        BackgroundColor = Styling.ActiveStyle.Colors.InputBackground;
        ForegroundColor = Styling.ActiveStyle.Colors.TextPrimary;
        DropdownIndicatorColor = Styling.ActiveStyle.Colors.Primary;
        FocusedIndicatorColor = Styling.ActiveStyle.Colors.Warning;

        DefineDynamicStyleChanges();

        if (tryCreateFormsObject)
        {
            FormsControlAsObject = new ComboBox(this);
        }
    }

    private void DefineDynamicStyleChanges()
    {
        ComboBoxCategory.States.Add(States.Enabled);
        States.Enabled.Apply = () =>
        {
            SetValuesForState(BackgroundColor, DropdownIndicatorColor, ForegroundColor, false);
        };

        ComboBoxCategory.States.Add(States.Disabled);
        States.Disabled.Apply = () =>
        {
            SetValuesForState(BackgroundColor, DropdownIndicatorColor.ToGrayscale().Adjust(Styling.ActiveStyle.Colors.PercentGreyScaleDarken), ForegroundColor.ToGrayscale().Adjust(Styling.ActiveStyle.Colors.PercentGreyScaleDarken), false);
        };

        ComboBoxCategory.States.Add(States.DisabledFocused);
        States.DisabledFocused.Apply = () =>
        {
            SetValuesForState(BackgroundColor, DropdownIndicatorColor.ToGrayscale().Adjust(Styling.ActiveStyle.Colors.PercentGreyScaleDarken), ForegroundColor.ToGrayscale().Adjust(Styling.ActiveStyle.Colors.PercentGreyScaleDarken), true);
        };

        ComboBoxCategory.States.Add(States.Focused);
        States.Focused.Apply = () =>
        {
            SetValuesForState(BackgroundColor, ForegroundColor, ForegroundColor, true);
        };

        ComboBoxCategory.States.Add(States.Highlighted);
        States.Highlighted.Apply = () =>
        {
            SetValuesForState(BackgroundColor, ForegroundColor, ForegroundColor, true);
        };

        ComboBoxCategory.States.Add(States.Highlighted);
        States.Highlighted.Apply = () =>
        {
            SetValuesForState(BackgroundColor, DropdownIndicatorColor.Adjust(Styling.ActiveStyle.Colors.PercentLighten), ForegroundColor.Adjust(Styling.ActiveStyle.Colors.PercentLighten), false);
        };

        ComboBoxCategory.States.Add(States.HighlightedFocused);
        States.HighlightedFocused.Apply = () =>
        {
            SetValuesForState(BackgroundColor, DropdownIndicatorColor.Adjust(Styling.ActiveStyle.Colors.PercentLighten), ForegroundColor.Adjust(Styling.ActiveStyle.Colors.PercentLighten), true);
        };

        ComboBoxCategory.States.Add(States.Pushed);
        States.Pushed.Apply = () =>
        {
            SetValuesForState(BackgroundColor, DropdownIndicatorColor.Adjust(Styling.ActiveStyle.Colors.PercentDarken), ForegroundColor.Adjust(Styling.ActiveStyle.Colors.PercentDarken), true);
        };

    }

    private void SetValuesForState(Color backgroundColor, Color ddIndicatorColor, Color foregroundColor, bool isFocused)
    {
        Background.Color = backgroundColor;
        DropdownIndicator.Color = ddIndicatorColor;
        TextInstance.Color = foregroundColor;
        FocusedIndicator.Visible = isFocused;
        FocusedIndicator.Color = _focusedIndicatorColor;
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

    public ComboBox FormsControl => (ComboBox)FormsControlAsObject;

}
