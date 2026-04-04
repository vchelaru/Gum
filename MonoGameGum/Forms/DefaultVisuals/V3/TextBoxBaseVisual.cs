using Gum.Converters;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Wireframe;
using Gum.Forms.Controls;
using RenderingLibrary.Graphics;
using System;


#if RAYLIB
using Gum.GueDeriving;
using Raylib_cs;

#else
using MonoGameGum.GueDeriving;
using Microsoft.Xna.Framework;
#endif


namespace Gum.Forms.DefaultVisuals.V3;

/// <summary>
/// Abstract base visual for text input controls (TextBox, PasswordBox). Contains a bordered
/// background, a clipped text area with selection highlighting, a text caret, placeholder text,
/// and a focus indicator bar.
/// </summary>
public abstract class TextBoxBaseVisual : InteractiveGue
{
    /// <summary>
    /// The bordered background nine-slice that fills the control.
    /// </summary>
    public NineSliceRuntime Background { get; private set; }

    /// <summary>
    /// The container that clips text content to the control bounds.
    /// </summary>
    public ContainerRuntime ClipContainer { get; private set; }

    /// <summary>
    /// The nine-slice used to highlight selected text.
    /// </summary>
    public NineSliceRuntime SelectionInstance { get; private set; }

    /// <summary>
    /// The text runtime displaying the entered text.
    /// </summary>
    public TextRuntime TextInstance { get; private set; }

    /// <summary>
    /// The text runtime displaying placeholder text when the input is empty.
    /// </summary>
    public TextRuntime PlaceholderTextInstance { get; private set; }

    /// <summary>
    /// The blinking text caret sprite indicating the cursor position.
    /// </summary>
    public SpriteRuntime CaretInstance { get; private set; }

    /// <summary>
    /// A thin bar displayed at the bottom of the control when focused.
    /// </summary>
    public NineSliceRuntime FocusedIndicator { get; private set; }

    Color _backgroundColor;
    /// <summary>
    /// The base color applied to the background. Setting this value immediately updates
    /// the visual. States may tint this color (for example, disabled states darken it).
    /// </summary>
    public Color BackgroundColor
    {
        get => _backgroundColor;
        set
        {
            if (!value.Equals(_backgroundColor))
            {
                // Just in case FormsControl hasn't been set yet, do ?. to check for null
                // UpdateState forcefully applies the current state, so it will work regardless of whether this is
                // Highlighted or Disabled etc
                _backgroundColor = value;
                (FormsControlAsObject as FrameworkElement)?.UpdateState();
            }
        }
    }
    Color _foregroundColor;
    /// <summary>
    /// The base color applied to the entered text. Setting this value immediately updates
    /// the visual. States may tint this color.
    /// </summary>
    public Color ForegroundColor
    {
        get => _foregroundColor;
        set
        {
            if (!value.Equals(_foregroundColor))
            {
                // Just in case FormsControl hasn't been set yet, do ?. to check for null
                // UpdateState forcefully applies the current state, so it will work regardless of whether this is
                // Highlighted or Disabled etc
                _foregroundColor = value;
                (FormsControlAsObject as FrameworkElement)?.UpdateState();
            }
        }
    }

    Color _selectionBackgroundColor;
    /// <summary>
    /// The color applied to the text selection highlight. Setting this value immediately
    /// updates the visual.
    /// </summary>
    public Color SelectionBackgroundColor
    {
        get => _selectionBackgroundColor;
        set
        {
            if (!value.Equals(_selectionBackgroundColor))
            {
                // Just in case FormsControl hasn't been set yet, do ?. to check for null
                // UpdateState forcefully applies the current state, so it will work regardless of whether this is
                // Highlighted or Disabled etc
                _selectionBackgroundColor = value;
                (FormsControlAsObject as FrameworkElement)?.UpdateState();
            }
        }
    }

    Color _placeholderColor;
    /// <summary>
    /// The color applied to the placeholder text. Setting this value immediately updates
    /// the visual.
    /// </summary>
    public Color PlaceholderColor
    {
        get => _placeholderColor;
        set
        {
            if (!value.Equals(_placeholderColor))
            {
                // Just in case FormsControl hasn't been set yet, do ?. to check for null
                // UpdateState forcefully applies the current state, so it will work regardless of whether this is
                // Highlighted or Disabled etc
                _placeholderColor = value;
                (FormsControlAsObject as TextBoxBase)?.UpdateState();
            }
        }
    }

    Color _caretColor;
    /// <summary>
    /// The color applied to the text caret. Setting this value immediately updates the visual.
    /// </summary>
    public Color CaretColor
    {
        get => _caretColor;
        set
        {
            if (!value.Equals(_caretColor))
            {
                _caretColor = value;
                (FormsControlAsObject as FrameworkElement)?.UpdateState();

            }
        }
    }

    Color _focusedIndicatorColor;
    /// <summary>
    /// The color of the focus indicator bar shown when the control has focus. Setting this
    /// value immediately updates the visual.
    /// </summary>
    public Color FocusedIndicatorColor
    {
        get => _focusedIndicatorColor;
        set
        {
            if (!value.Equals(_focusedIndicatorColor))
            {
                _focusedIndicatorColor = value;
                (FormsControlAsObject as FrameworkElement)?.UpdateState();
            }
        }
    }

    protected abstract string CategoryName { get; }

    public class TextBoxCategoryStates
    {
        public StateSave Enabled { get; set; } = new StateSave() { Name = FrameworkElement.EnabledStateName };
        public StateSave Disabled { get; set; } = new StateSave() { Name = FrameworkElement.DisabledStateName };
        public StateSave Highlighted { get; set; } = new StateSave() { Name = FrameworkElement.HighlightedStateName };
        public StateSave Focused { get; set; } = new StateSave() { Name = FrameworkElement.FocusedStateName };

        // These next were combined into the single "states" variable per discussion
        // But if we want we can always make this thing multi-layered with a "LineMode" sub-class.
        public StateSave SingleLineMode { get; set; } = new StateSave() { Name = "Single" }; 
        public StateSave MultiLineMode { get; set; } = new StateSave() { Name = "Multi" };
        public StateSave MultiLineModeNoWrap { get; set; } = new StateSave() { Name = "MultiNoWrap" };
    }

    public TextBoxCategoryStates States;
    /// <summary>
    /// The state category used by the Forms control to apply visual states.
    /// </summary>
    public StateSaveCategory TextboxCategory { get; private set; }

    /// <summary>
    /// The state category controlling single-line vs multi-line layout.
    /// </summary>
    public StateSaveCategory LineModeCategory { get; private set; }

    public TextBoxBaseVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true) : base(new InvisibleRenderable())
    {
        this.HasEvents = true;
        States = new TextBoxCategoryStates();
        Width = 256;
        Height = 24;

        var uiSpriteSheetTexture = Styling.ActiveStyle.SpriteSheet;

        Background = new NineSliceRuntime();
        Background.Name = "Background";
        Background.X = 0;
        Background.Y = 0;
        Background.XUnits = GeneralUnitType.PixelsFromMiddle;
        Background.YUnits = GeneralUnitType.PixelsFromMiddle;
        Background.XOrigin = HorizontalAlignment.Center;
        Background.YOrigin = VerticalAlignment.Center;
        Background.Width = 0;
        Background.Height = 0;
        Background.WidthUnits = DimensionUnitType.RelativeToParent;
        Background.HeightUnits = DimensionUnitType.RelativeToParent;
        Background.Texture = uiSpriteSheetTexture;
        Background.ApplyState(Styling.ActiveStyle.NineSlice.Bordered);
        this.AddChild(Background);

        ClipContainer = new ContainerRuntime();
        ClipContainer.Name = "ClipContiner";
        ClipContainer.Dock(Gum.Wireframe.Dock.Fill);
        ClipContainer.ClipsChildren = true;
        ClipContainer.HasEvents = false;
        this.AddChild(ClipContainer);

        SelectionInstance = new NineSliceRuntime();
        SelectionInstance.Name = "SelectionInstance";
        SelectionInstance.Height = -4f;
        SelectionInstance.HeightUnits = DimensionUnitType.RelativeToParent;
        SelectionInstance.Width = 7f;
        SelectionInstance.WidthUnits = DimensionUnitType.Absolute;
        SelectionInstance.X = 15f;
        SelectionInstance.XOrigin = HorizontalAlignment.Left;
        SelectionInstance.XUnits = GeneralUnitType.PixelsFromSmall;
        SelectionInstance.Y = 2f;
        SelectionInstance.Texture = uiSpriteSheetTexture;
        SelectionInstance.ApplyState(Styling.ActiveStyle.NineSlice.Solid);
        ClipContainer.AddChild(SelectionInstance);

        TextInstance = new TextRuntime();
        TextInstance.Name = "TextInstance";
        TextInstance.X = 4f;
        TextInstance.XUnits = GeneralUnitType.PixelsFromSmall;
        TextInstance.Y = 0f;
        TextInstance.YUnits = GeneralUnitType.PixelsFromMiddle;
        TextInstance.XOrigin = HorizontalAlignment.Left;
        TextInstance.YOrigin = VerticalAlignment.Center;
        TextInstance.Width = 0f;
        TextInstance.WidthUnits = DimensionUnitType.RelativeToChildren;
        TextInstance.Height = -4f;
        TextInstance.HeightUnits = DimensionUnitType.RelativeToParent;
        TextInstance.HorizontalAlignment = HorizontalAlignment.Left;
        TextInstance.VerticalAlignment = VerticalAlignment.Center;
        TextInstance.ApplyState(Styling.ActiveStyle.Text.Normal);
        TextInstance.SetTextNoTranslate("");

        ClipContainer.AddChild(TextInstance);

        PlaceholderTextInstance = new TextRuntime();
        PlaceholderTextInstance.Name = "PlaceholderTextInstance";
        PlaceholderTextInstance.X = 4f;
        PlaceholderTextInstance.XUnits = GeneralUnitType.PixelsFromSmall;
        PlaceholderTextInstance.Y = 0f;
        PlaceholderTextInstance.YUnits = GeneralUnitType.PixelsFromMiddle;
        PlaceholderTextInstance.XOrigin = HorizontalAlignment.Left;
        PlaceholderTextInstance.YOrigin = VerticalAlignment.Center;
        // Update January 6, 2026
        // By default placeholder text
        // should extend off the edge unless
        // in multi-line mode
        //PlaceholderTextInstance.Width = -8f;
        //PlaceholderTextInstance.WidthUnits = DimensionUnitType.RelativeToParent;
        PlaceholderTextInstance.WidthUnits = DimensionUnitType.RelativeToChildren;
        PlaceholderTextInstance.Height = -4f;
        PlaceholderTextInstance.HeightUnits = DimensionUnitType.RelativeToParent;
        PlaceholderTextInstance.Text = "Text Placeholder";
        PlaceholderTextInstance.VerticalAlignment = VerticalAlignment.Center;
        PlaceholderTextInstance.ApplyState(Styling.ActiveStyle.Text.Normal);
        ClipContainer.AddChild(PlaceholderTextInstance);

        CaretInstance = new SpriteRuntime();
        CaretInstance.Name = "CaretInstance";
        CaretInstance.Height = 18f;
        CaretInstance.HeightUnits = DimensionUnitType.Absolute;
        CaretInstance.Texture = uiSpriteSheetTexture;
        CaretInstance.TextureAddress = global::Gum.Managers.TextureAddress.Custom;
        CaretInstance.ApplyState(Styling.ActiveStyle.NineSlice.Solid);
        CaretInstance.Width = 1f;
        CaretInstance.WidthUnits = DimensionUnitType.Absolute;
        CaretInstance.X = 4f;
        CaretInstance.XOrigin = HorizontalAlignment.Left;
        CaretInstance.XUnits = GeneralUnitType.PixelsFromSmall;
        CaretInstance.Y = 0f;
        CaretInstance.YOrigin = VerticalAlignment.Center;
        CaretInstance.YUnits = GeneralUnitType.PixelsFromMiddle;
        ClipContainer.AddChild(CaretInstance);

        FocusedIndicator = new NineSliceRuntime();
        FocusedIndicator.Name = "FocusedIndicator";
        FocusedIndicator.X = 0;
        FocusedIndicator.Y = 2;
        FocusedIndicator.XUnits = GeneralUnitType.PixelsFromMiddle;
        FocusedIndicator.YUnits = GeneralUnitType.PixelsFromLarge;
        FocusedIndicator.XOrigin = HorizontalAlignment.Center;
        FocusedIndicator.YOrigin = VerticalAlignment.Top;
        FocusedIndicator.Width = 0;
        FocusedIndicator.Height = 2;
        FocusedIndicator.WidthUnits = DimensionUnitType.RelativeToParent;
        FocusedIndicator.HeightUnits = DimensionUnitType.Absolute;
        FocusedIndicator.Texture = uiSpriteSheetTexture;
        FocusedIndicator.ApplyState(Styling.ActiveStyle.NineSlice.Solid);
        FocusedIndicator.Visible = false;
        this.AddChild(FocusedIndicator);

        BackgroundColor = Styling.ActiveStyle.Colors.InputBackground;
        SelectionBackgroundColor = Styling.ActiveStyle.Colors.Primary;
        ForegroundColor = Styling.ActiveStyle.Colors.TextPrimary;
        PlaceholderColor = Styling.ActiveStyle.Colors.TextMuted;
        CaretColor = Styling.ActiveStyle.Colors.Primary;
        FocusedIndicatorColor = Styling.ActiveStyle.Colors.Warning;

        TextboxCategory = new StateSaveCategory();
        TextboxCategory.Name = CategoryName;
        this.AddCategory(TextboxCategory);

        DefineDynamicStyleChanges();
    }

    private void DefineDynamicStyleChanges()
    {
        TextboxCategory.States.Add(States.Enabled);
        States.Enabled.Apply = () =>
        {
            SetValuesForState(BackgroundColor, ForegroundColor, false, PlaceholderColor, SelectionBackgroundColor);
        };

        TextboxCategory.States.Add(States.Disabled);
        States.Disabled.Apply = () =>
        {
            SetValuesForState(BackgroundColor.Adjust(Styling.ActiveStyle.Colors.PercentGreyScaleDarken), 
                ForegroundColor.Adjust(Styling.ActiveStyle.Colors.PercentDarken), false, PlaceholderColor, SelectionBackgroundColor);
        };

        TextboxCategory.States.Add(States.Highlighted);
        States.Highlighted.Apply = () =>
        {
            SetValuesForState(BackgroundColor.Adjust(Styling.ActiveStyle.Colors.PercentGreyScaleLighten), 
                ForegroundColor, false, PlaceholderColor, SelectionBackgroundColor);
        };

        TextboxCategory.States.Add(States.Focused);
        States.Focused.Apply = () =>
        {
            SetValuesForState(BackgroundColor, ForegroundColor, true, PlaceholderColor, SelectionBackgroundColor);
        };



        LineModeCategory = new StateSaveCategory();
        LineModeCategory.Name = "LineModeCategory";
        this.AddCategory(LineModeCategory);

        void AddVariable(StateSave state, string name, object value)
        {
            state.Variables.Add(new VariableSave
            {
                Name = name,
                Value = value
            });
        }

        LineModeCategory.States.Add(States.SingleLineMode);
        AddVariable(States.SingleLineMode, "SelectionInstance.Height", -4f);
        AddVariable(States.SingleLineMode, "SelectionInstance.HeightUnits", DimensionUnitType.RelativeToParent);
        AddVariable(States.SingleLineMode, "TextInstance.Width", 0f);
        AddVariable(States.SingleLineMode, "TextInstance.WidthUnits", DimensionUnitType.RelativeToChildren);
        AddVariable(States.SingleLineMode, "PlaceholderTextInstance.VerticalAlignment", VerticalAlignment.Center);
        AddVariable(States.SingleLineMode, "PlaceholderTextInstance.WidthUnits", DimensionUnitType.RelativeToChildren);

        AddVariable(States.SingleLineMode, "TextInstance.VerticalAlignment", VerticalAlignment.Center);

        LineModeCategory.States.Add(States.MultiLineMode);
        AddVariable(States.MultiLineMode, "SelectionInstance.Height", 20f);
        AddVariable(States.MultiLineMode, "SelectionInstance.HeightUnits", DimensionUnitType.Absolute);
        AddVariable(States.MultiLineMode, "TextInstance.Width", -8f);
        AddVariable(States.MultiLineMode, "TextInstance.WidthUnits", DimensionUnitType.RelativeToParent);
        AddVariable(States.MultiLineMode, "PlaceholderTextInstance.VerticalAlignment", VerticalAlignment.Top);
        AddVariable(States.MultiLineMode, "PlaceholderTextInstance.WidthUnits", DimensionUnitType.RelativeToParent);
        AddVariable(States.MultiLineMode, "TextInstance.VerticalAlignment", VerticalAlignment.Top);

        LineModeCategory.States.Add(States.MultiLineModeNoWrap);
        AddVariable(States.MultiLineModeNoWrap, "SelectionInstance.Height", 20f);
        AddVariable(States.MultiLineModeNoWrap, "SelectionInstance.HeightUnits", DimensionUnitType.Absolute);
        AddVariable(States.MultiLineModeNoWrap, "TextInstance.Width", 0f);
        AddVariable(States.MultiLineModeNoWrap, "TextInstance.WidthUnits", DimensionUnitType.RelativeToChildren);
        AddVariable(States.MultiLineModeNoWrap, "PlaceholderTextInstance.VerticalAlignment", VerticalAlignment.Top);
        AddVariable(States.MultiLineModeNoWrap, "PlaceholderTextInstance.WidthUnits", DimensionUnitType.RelativeToChildren);
        AddVariable(States.MultiLineModeNoWrap, "TextInstance.VerticalAlignment", VerticalAlignment.Top);
    }

    private void SetValuesForState(Color backgroundColor, Color foregroundColor, bool isFocusIndicatorVisible, 
        Color placeholderColor, Color selectionColor)
    {
        Background.Color = backgroundColor;
        TextInstance.Color = foregroundColor;
        FocusedIndicator.Visible = isFocusIndicatorVisible;
        PlaceholderTextInstance.Color = placeholderColor;
        SelectionInstance.Color = selectionColor;
        FocusedIndicator.Color = _focusedIndicatorColor;

        CaretInstance.Color = _caretColor;

    }
}
