using Gum.Converters;
using Gum.DataTypes.Variables;
using Gum.Wireframe;
using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


#if XNALIKE
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameGum.GueDeriving;
#elif RAYLIB
using Raylib_cs;
using Gum.GueDeriving;
#endif
using Gum.Forms.Controls;
namespace Gum.Forms.DefaultVisuals.V3;

/// <summary>
/// Default V3 visual for a ListBoxItem control. Contains a bordered background, a text label,
/// and a focus indicator bar.
/// </summary>
public class ListBoxItemVisual : InteractiveGue
{
    /// <summary>
    /// The bordered background nine-slice. Visibility is toggled by states to indicate selection
    /// or highlighting.
    /// </summary>
    public NineSliceRuntime Background { get; private set; }

    /// <summary>
    /// The text label displaying the item content.
    /// </summary>
    public TextRuntime TextInstance { get; private set; }

    /// <summary>
    /// A thin bar displayed at the bottom of the item when focused.
    /// </summary>
    public NineSliceRuntime FocusedIndicator { get; private set; }

    public class ListBoxItemCategoryStates
    {
        public StateSave Enabled { get; set; } = new StateSave() { Name = FrameworkElement.EnabledStateName };
        public StateSave Highlighted { get; set; } = new StateSave() { Name = FrameworkElement.HighlightedStateName };
        public StateSave Selected { get; set; } = new StateSave() { Name = FrameworkElement.SelectedStateName };
        public StateSave Focused { get; set; } = new StateSave() { Name = FrameworkElement.FocusedStateName };
        public StateSave Disabled { get; set; } = new StateSave() { Name = FrameworkElement.DisabledStateName };
    }

    public ListBoxItemCategoryStates States;

    /// <summary>
    /// The state category used by the Forms control to apply visual states.
    /// </summary>
    public StateSaveCategory ListBoxItemCategory { get; private set; }

    Color _highlightedBackgroundColor;

    /// <summary>
    /// The background color used when the item is highlighted (hovered). Setting this value
    /// immediately updates the visual.
    /// </summary>
    public Color HighlightedBackgroundColor
    {
        get => _highlightedBackgroundColor;
        set
        {
            if (!value.Equals(_highlightedBackgroundColor))
            {
                _highlightedBackgroundColor = value;
                FormsControl?.UpdateState();
            }
        }
    }

    Color _selectedBackgroundColor;

    /// <summary>
    /// The background color used when the item is selected. Setting this value immediately
    /// updates the visual.
    /// </summary>
    public Color SelectedBackgroundColor
    {
        get => _selectedBackgroundColor;
        set
        {
            if (!value.Equals(_selectedBackgroundColor))
            {
                _selectedBackgroundColor = value;
                FormsControl?.UpdateState();
            }
        }
    }

    Color _foregroundColor;

    /// <summary>
    /// The base color applied to the text. Setting this value immediately updates the visual.
    /// States may tint this color (for example, disabled states convert to grayscale).
    /// </summary>
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

    Color _focusedIndicatorColor;

    /// <summary>
    /// The color of the focus indicator bar shown when the item has focus. Setting this value
    /// immediately updates the visual.
    /// </summary>
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

    public ListBoxItemVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true) : base(new InvisibleRenderable())
    {
        this.HasEvents = true;
        Height = 0f;
        HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        Width = 0f;
        WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;

        States = new ListBoxItemCategoryStates();
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
        TextInstance.Text = "ListBox Item";
        TextInstance.X = 0f;
        TextInstance.XUnits = GeneralUnitType.PixelsFromMiddle;
        TextInstance.Y = 0f;
        TextInstance.YUnits = GeneralUnitType.PixelsFromMiddle;
        TextInstance.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
        TextInstance.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
        TextInstance.Width = -8f;
        TextInstance.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        TextInstance.Height = 0f;
        TextInstance.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        TextInstance.HorizontalAlignment = global::RenderingLibrary.Graphics.HorizontalAlignment.Left;
        TextInstance.VerticalAlignment = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
        TextInstance.ApplyState(Styling.ActiveStyle.Text.Normal);
        this.AddChild(TextInstance);

        FocusedIndicator = new NineSliceRuntime();
        FocusedIndicator.Name = "FocusedIndicator";
        FocusedIndicator.X = 0f;
        FocusedIndicator.XUnits = GeneralUnitType.PixelsFromMiddle;
        FocusedIndicator.Y = -2f;
        FocusedIndicator.YUnits = GeneralUnitType.PixelsFromLarge;
        FocusedIndicator.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
        FocusedIndicator.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Top;
        FocusedIndicator.Width = 0f;
        FocusedIndicator.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        FocusedIndicator.Height = 2f;
        FocusedIndicator.HeightUnits = global::Gum.DataTypes.DimensionUnitType.Absolute;
        FocusedIndicator.Texture = uiSpriteSheetTexture;
        FocusedIndicator.ApplyState(Styling.ActiveStyle.NineSlice.Solid);
        this.AddChild(FocusedIndicator);

        ListBoxItemCategory = new Gum.DataTypes.Variables.StateSaveCategory();
        ListBoxItemCategory.Name = "ListBoxItemCategory";
        this.AddCategory(ListBoxItemCategory);

        HighlightedBackgroundColor = Styling.ActiveStyle.Colors.Accent;
        SelectedBackgroundColor = Styling.ActiveStyle.Colors.Primary;
        ForegroundColor = Styling.ActiveStyle.Colors.TextPrimary;
        FocusedIndicatorColor = Styling.ActiveStyle.Colors.Warning;

        DefineDynamicStyleChanges();

        if (tryCreateFormsObject)
        {
            FormsControlAsObject = new ListBoxItem(this);
        }
    }

    private void DefineDynamicStyleChanges()
    {
        ListBoxItemCategory.States.Add(States.Enabled);
        States.Enabled.Apply = () =>
        {
            SetValuesForState(false, false, ForegroundColor, HighlightedBackgroundColor);
        };

        ListBoxItemCategory.States.Add(States.Highlighted);
        States.Highlighted.Apply = () =>
        {
            SetValuesForState(true, false, ForegroundColor, HighlightedBackgroundColor);
        };

        ListBoxItemCategory.States.Add(States.Selected);
        States.Selected.Apply = () =>
        {
            SetValuesForState(true, false, ForegroundColor, SelectedBackgroundColor); // TODO: Discuss how to approach this.
        };

        ListBoxItemCategory.States.Add(States.Focused);
        States.Focused.Apply = () =>
        {
            SetValuesForState(false, true, ForegroundColor, SelectedBackgroundColor.ToGrayscale().Adjust(Styling.ActiveStyle.Colors.PercentGreyScaleDarken));
        };

        ListBoxItemCategory.States.Add(States.Disabled);
        States.Disabled.Apply = () =>
        {
            SetValuesForState(false, false, ForegroundColor.ToGrayscale().Adjust(Styling.ActiveStyle.Colors.PercentGreyScaleLighten), 
                HighlightedBackgroundColor);
        };
    }

    private void SetValuesForState(bool isBackgroundVisible, bool isFocusedVisible, Color foregroundColor, Color backgroundColor)
    {
        Background.Visible = isBackgroundVisible;
        FocusedIndicator.Visible = isFocusedVisible;
        TextInstance.Color = foregroundColor;
        Background.Color = backgroundColor;
        FocusedIndicator.Color = _focusedIndicatorColor;
    }

    /// <summary>
    /// Returns the strongly-typed ListBoxItem Forms control backing this visual.
    /// </summary>
    public ListBoxItem FormsControl => (ListBoxItem)FormsControlAsObject;
}
