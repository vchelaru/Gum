using Gum.Converters;
using Gum.DataTypes.Variables;
using Gum.Wireframe;
using Gum.Forms.Controls;
using MonoGameGum.GueDeriving;
using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#if RAYLIB
using Raylib_cs;

#else
using Microsoft.Xna.Framework;
#endif

namespace Gum.Forms.DefaultVisuals.V3;

/// <summary>
/// Default V3 visual for a MenuItem control. Contains a solid background, a left-to-right
/// stacked container with text label and submenu indicator.
/// </summary>
public class MenuItemVisual : InteractiveGue
{
    /// <summary>
    /// The solid background nine-slice. Visibility is toggled by states to indicate highlighting or selection.
    /// </summary>
    public NineSliceRuntime Background { get; private set; }
    /// <summary>
    /// The left-to-right stacking container that holds the text and submenu indicator.
    /// </summary>
    public ContainerRuntime ContainerInstance { get; private set; }
    /// <summary>
    /// The text label displaying the menu item text.
    /// </summary>
    public TextRuntime TextInstance { get; private set; }

    /// <summary>
    /// The text runtime displaying a '>' character to indicate the item has a submenu.
    /// </summary>
    public TextRuntime SubmenuIndicatorInstance { get; private set; }

    public class MenuItemCategoryStates
    {
        public StateSave Enabled { get; set; } = new StateSave() { Name = FrameworkElement.EnabledStateName };
        public StateSave Disabled { get; set; } = new StateSave() { Name = FrameworkElement.DisabledStateName };
        public StateSave Highlighted { get; set; } = new StateSave() { Name = FrameworkElement.HighlightedStateName };
        public StateSave Focused { get; set; } = new StateSave() { Name = FrameworkElement.FocusedStateName };
        public StateSave Selected { get; set; } = new StateSave() { Name = FrameworkElement.SelectedStateName };
    }

    public MenuItemCategoryStates States;

    /// <summary>
    /// The state category used by the Forms control to apply visual states.
    /// </summary>
    public StateSaveCategory MenuItemCategory { get; private set; }


    Color _highlightedBackgroundColor;
    /// <summary>
    /// The background color used when the item is highlighted (hovered). Setting this value immediately updates the visual.
    /// </summary>
    public Color HighlightedBackgroundColor
    {
        get => _highlightedBackgroundColor;
        set
        {
            if (!value.Equals(_highlightedBackgroundColor))
            {
                // Just in case FormsControl hasn't been set yet, do ?. to check for null
                // UpdateState forcefully applies the current state, so it will work regardless of whether this is
                // Highlighted or Disabled etc
                _highlightedBackgroundColor = value;
                FormsControl?.UpdateState();
            }
        }
    }

    Color _selectedBackgroundColor;
    /// <summary>
    /// The background color used when the item is selected. Setting this value immediately updates the visual.
    /// </summary>
    public Color SelectedBackgroundColor
    {
        get => _selectedBackgroundColor;
        set
        {
            if(!value.Equals(_selectedBackgroundColor))
            {
                _selectedBackgroundColor = value;
                FormsControl?.UpdateState();
            }
        }
    }

    Color _foregroundColor;
    /// <summary>
    /// The base color applied to the text label. Setting this value immediately updates the visual.
    /// States may tint this color (for example, disabled states convert to grayscale and darken).
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
                FormsControl?.UpdateState();
            }
        }
    }

    Color _submenuIndicatorColor;
    /// <summary>
    /// The base color applied to the submenu indicator. Setting this value immediately updates the visual.
    /// States may tint this color (for example, disabled states convert to grayscale and darken).
    /// </summary>
    public Color SubmenuIndicatorColor
    {
        get => _submenuIndicatorColor;
        set
        {
            if (!value.Equals(_submenuIndicatorColor))
            {
                // Just in case FormsControl hasn't been set yet, do ?. to check for null
                // UpdateState forcefully applies the current state, so it will work regardless of whether this is
                // Highlighted or Disabled etc
                _submenuIndicatorColor = value;
                FormsControl?.UpdateState();
            }
        }
    }

    public MenuItemVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true) : base(new InvisibleRenderable())
    {
        this.HasEvents = true;
        X = 0;
        Y = 0;
        Width = 0;
        WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        Height = 0;
        HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;

        States = new MenuItemCategoryStates();
        var uiSpriteSheetTexture = Styling.ActiveStyle.SpriteSheet;

        Background = new NineSliceRuntime();
        Background.Name = "Background";
        Background.X = 0;
        Background.Y = 0;
        Background.XUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle;
        Background.YUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle;
        Background.XOrigin = HorizontalAlignment.Center;
        Background.YOrigin = VerticalAlignment.Center;
        Background.Width = 0;
        Background.Height = 0;
        Background.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
        Background.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
        Background.Texture = uiSpriteSheetTexture;
        Background.Visible = true;
        Background.ApplyState(Styling.ActiveStyle.NineSlice.Solid);
        this.AddChild(Background);

        ContainerInstance = new ContainerRuntime();
        ContainerInstance.Name = "ContainerInstance";
        ContainerInstance.ChildrenLayout = global::Gum.Managers.ChildrenLayout.LeftToRightStack;
        ContainerInstance.Anchor(Gum.Wireframe.Anchor.TopLeft);
        ContainerInstance.Height = 0f;
        ContainerInstance.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        ContainerInstance.Width = 0f;
        ContainerInstance.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        ContainerInstance.HasEvents = false;
        this.AddChild(ContainerInstance);

        TextInstance = new TextRuntime();
        TextInstance.Name = "TextInstance";
        TextInstance.Text = "Menu Item";
        TextInstance.Dock(Gum.Wireframe.Dock.Left);
        TextInstance.X = 0;
        TextInstance.Y = 0;
        TextInstance.ApplyState(Styling.ActiveStyle.Text.Normal);
        TextInstance.Width = 2;
        TextInstance.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        TextInstance.Height = 0;
        TextInstance.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        ContainerInstance.AddChild(TextInstance);

        SubmenuIndicatorInstance = new TextRuntime();
        SubmenuIndicatorInstance.Name = "SubmenuIndicatorInstance";
        SubmenuIndicatorInstance.Dock(Gum.Wireframe.Dock.Left);
        SubmenuIndicatorInstance.X = 8;
        SubmenuIndicatorInstance.Y = 0;
        SubmenuIndicatorInstance.YUnits = GeneralUnitType.PixelsFromMiddle;
        SubmenuIndicatorInstance.YOrigin = VerticalAlignment.Center;
        SubmenuIndicatorInstance.Width = 2f;
        SubmenuIndicatorInstance.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        SubmenuIndicatorInstance.Height = 0f;
        SubmenuIndicatorInstance.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        SubmenuIndicatorInstance.Text = @">";
        SubmenuIndicatorInstance.HorizontalAlignment = global::RenderingLibrary.Graphics.HorizontalAlignment.Left;
        ContainerInstance.AddChild(SubmenuIndicatorInstance);

        MenuItemCategory = new Gum.DataTypes.Variables.StateSaveCategory();
        MenuItemCategory.Name = "MenuItemCategory";
        this.AddCategory(MenuItemCategory);

        HighlightedBackgroundColor = Styling.ActiveStyle.Colors.Accent;
        SelectedBackgroundColor = Styling.ActiveStyle.Colors.Primary;

        ForegroundColor = Styling.ActiveStyle.Colors.TextPrimary;
        SubmenuIndicatorColor = Styling.ActiveStyle.Colors.TextPrimary;

        DefineDynamicStyleChanges();

        if (tryCreateFormsObject)
        {
            FormsControlAsObject = new MenuItem(this);
        }
    }


    private void DefineDynamicStyleChanges()
    {
        MenuItemCategory.States.Add(States.Enabled);
        States.Enabled.Apply = () =>
        {
            SetValuesForState(false, HighlightedBackgroundColor // background is hidden, set to any value
                , ForegroundColor
                , SubmenuIndicatorColor);
        };

        MenuItemCategory.States.Add(States.Disabled);
        States.Disabled.Apply = () =>
        {
            SetValuesForState(false, HighlightedBackgroundColor // background is hidden, set to any value
                , ForegroundColor.ToGrayscale().Adjust(Styling.ActiveStyle.Colors.PercentGreyScaleDarken)
                , SubmenuIndicatorColor.ToGrayscale().Adjust(Styling.ActiveStyle.Colors.PercentGreyScaleDarken));
        };

        MenuItemCategory.States.Add(States.Highlighted);
        States.Highlighted.Apply = () =>
        {
            SetValuesForState(true, HighlightedBackgroundColor
                , ForegroundColor
                , SubmenuIndicatorColor);
        };

        MenuItemCategory.States.Add(States.Selected);
        States.Selected.Apply = () =>
        {
            SetValuesForState(true, SelectedBackgroundColor
                , ForegroundColor
                , SubmenuIndicatorColor);
        };

        MenuItemCategory.States.Add(States.Focused);
        States.Focused.Apply = () =>
        {
            SetValuesForState(false, HighlightedBackgroundColor // background is hidden, set to any value
                , ForegroundColor
                , SubmenuIndicatorColor);
        };
    }

    private void SetValuesForState(bool isBackgroundVisible, Color backgroundColor, Color foregroundColor, Color submenuIndicatorColor)
    {
        Background.Visible = isBackgroundVisible;
        Background.Color = backgroundColor;
        TextInstance.Color = foregroundColor;
        SubmenuIndicatorInstance.Color = submenuIndicatorColor;
    }

    /// <summary>
    /// Sets the FormsControlAsObject and configures the MenuItem's scroll viewer visual template.
    /// </summary>
    public override object FormsControlAsObject
    {
        get => base.FormsControlAsObject;
        set
        {
            base.FormsControlAsObject = value;
            if(value is MenuItem menuItem)
            {
                menuItem.ScrollViewerVisualTemplate = DefaultScrollVisualTemplate;
            }
        }
    }

    Gum.Forms.VisualTemplate DefaultScrollVisualTemplate => new (() =>
                new ScrollViewerVisualSizedToChildren(fullInstantiation: true, tryCreateFormsObject: false));

    /// <summary>
    /// Returns the strongly-typed MenuItem Forms control backing this visual.
    /// </summary>
    public MenuItem FormsControl => (MenuItem)FormsControlAsObject;
}
