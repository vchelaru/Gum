#pragma warning disable CS0618, GUM001 // Default visuals intentionally use deprecated MonoGameGum.GueDeriving shim types for backward compatibility until V1/V2/V3 visuals are retired. See issue #2715.
using Gum.Converters;
using Gum.DataTypes.Variables;
using Gum.Wireframe;
using Gum.Forms.Controls;
#if XNALIKE
using MonoGameGum.GueDeriving;
#else
using Gum.GueDeriving;
#endif
using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#if XNALIKE
using Microsoft.Xna.Framework;
#endif

#if SKIA
using Color = SkiaSharp.SKColor;
#endif

namespace Gum.Forms.DefaultVisuals.V3;

/// <summary>
/// Default V3 visual for a Menu control. Contains a solid background and a wrapping
/// left-to-right stacked inner panel for menu items.
/// </summary>
public class MenuVisual : InteractiveGue
{
    /// <summary>
    /// The solid background nine-slice that fills the control.
    /// </summary>
    public NineSliceRuntime Background {  get; private set; }
    /// <summary>
    /// The wrapping left-to-right stacking container that holds the menu items.
    /// </summary>
    public ContainerRuntime InnerPanelInstance { get; private set; }

    /// <summary>
    /// A thin bar displayed at the bottom of the control when focused.
    /// </summary>
    public NineSliceRuntime FocusedIndicator { get; private set; }

    public class MenuCategoryStates
    {
        public StateSave Enabled { get; set; } = new StateSave() { Name = FrameworkElement.EnabledStateName };
        public StateSave Disabled { get; set; } = new StateSave() { Name = FrameworkElement.DisabledStateName };
        public StateSave DisabledFocused { get; set; } = new StateSave() { Name = FrameworkElement.DisabledFocusedStateName };
        public StateSave Focused { get; set; } = new StateSave() { Name = FrameworkElement.FocusedStateName };
    }

    /// <summary>This visual's category states, typed by name. Hides the untyped States dictionary on GraphicalUiElement.</summary>
    public new MenuCategoryStates States;

    Color _focusedIndicatorColor;

    /// <summary>
    /// The color of the focus indicator bar shown when the control has focus. Setting this value
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
                FocusedIndicator.Color = value;
            }
        }
    }

    /// <summary>
    /// The state category used by the Forms control to apply visual states.
    /// </summary>
    public StateSaveCategory MenuCategory { get; private set; }

    Color _backgroundColor;
    /// <summary>
    /// The color applied to the background. Setting this value immediately updates the visual.
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

                // Currently there are no states
                //FormsControl?.UpdateState();
                // For now, since there is no state, we need to set this hard coded
                Background.Color = BackgroundColor;
            }
        }
    }

    public MenuVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true) : base(new InvisibleRenderable())
    {
        this.HasEvents = true;
        X = 0;
        XUnits = GeneralUnitType.PixelsFromMiddle;
        Y = 0;
        XOrigin = HorizontalAlignment.Center;
        Width = 0;
        WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
        Height = 0;
        HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        // a small value that prevents it from being invisible due to 0 height with no children
        MinHeight = 5;

        var uiSpriteSheetTexture = Styling.ActiveStyle.SpriteSheet;


        Background = new NineSliceRuntime();
        Background.Name = "Background";
        Background.X = 0;
        Background.XUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle;
        Background.Y = 0;
        Background.YUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle;
        Background.XOrigin = HorizontalAlignment.Center;
        Background.YOrigin = VerticalAlignment.Center;
        Background.Width = 0;
        Background.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
        Background.Height = 0;
        Background.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
        Background.Texture = uiSpriteSheetTexture;
        Background.Visible = true;
        Background.ApplyState(Styling.ActiveStyle.NineSlice.Solid);
        this.AddChild(Background);

        InnerPanelInstance = new ContainerRuntime();
        InnerPanelInstance.Name = "InnerPanelInstance";
        InnerPanelInstance.XUnits = GeneralUnitType.PixelsFromMiddle;
        InnerPanelInstance.YUnits = GeneralUnitType.PixelsFromMiddle;
        InnerPanelInstance.XOrigin  = HorizontalAlignment.Center;
        InnerPanelInstance.YOrigin = VerticalAlignment.Center;
        InnerPanelInstance.Height = 0f;
        InnerPanelInstance.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        InnerPanelInstance.Width = 0f;
        InnerPanelInstance.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        InnerPanelInstance.WrapsChildren = true;
        InnerPanelInstance.ChildrenLayout = Gum.Managers.ChildrenLayout.LeftToRightStack;
        InnerPanelInstance.StackSpacing = 2;
        this.AddChild(InnerPanelInstance);

        FocusedIndicator = new NineSliceRuntime();
        FocusedIndicator.Name = "FocusedIndicator";
        FocusedIndicator.X = 0f;
        FocusedIndicator.XUnits = GeneralUnitType.PixelsFromMiddle;
        FocusedIndicator.Y = 2f;
        FocusedIndicator.YUnits = GeneralUnitType.PixelsFromLarge;
        FocusedIndicator.XOrigin = HorizontalAlignment.Center;
        FocusedIndicator.YOrigin = VerticalAlignment.Top;
        FocusedIndicator.Width = 0f;
        FocusedIndicator.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
        FocusedIndicator.Height = 2f;
        FocusedIndicator.HeightUnits = Gum.DataTypes.DimensionUnitType.Absolute;
        FocusedIndicator.Texture = uiSpriteSheetTexture;
        FocusedIndicator.ApplyState(Styling.ActiveStyle.NineSlice.Solid);
        FocusedIndicator.Visible = false;
        this.AddChild(FocusedIndicator);

        MenuCategory = new Gum.DataTypes.Variables.StateSaveCategory();
        // SetProperty("MenuCategoryState", ...) applies a state from the category named "MenuCategory".
        MenuCategory.Name = "MenuCategory";
        this.AddCategory(MenuCategory);

        States = new MenuCategoryStates();
        MenuCategory.States.Add(States.Enabled);
        States.Enabled.Apply = () => FocusedIndicator.Visible = false;
        MenuCategory.States.Add(States.Disabled);
        States.Disabled.Apply = () => FocusedIndicator.Visible = false;
        MenuCategory.States.Add(States.DisabledFocused);
        States.DisabledFocused.Apply = () => FocusedIndicator.Visible = true;
        MenuCategory.States.Add(States.Focused);
        States.Focused.Apply = () => FocusedIndicator.Visible = true;

        BackgroundColor = Styling.ActiveStyle.Colors.InputBackground;
        FocusedIndicatorColor = Styling.ActiveStyle.Colors.Warning;

        if (tryCreateFormsObject)
        {
            FormsControlAsObject = new Menu(this);
        }
    }

    /// <summary>
    /// Returns the strongly-typed Menu Forms control backing this visual.
    /// </summary>
    public Menu FormsControl => (Menu)FormsControlAsObject!;
}
