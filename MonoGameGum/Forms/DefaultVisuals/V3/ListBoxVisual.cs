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
#else
using Gum.GueDeriving;
#endif
using Gum.Forms.Controls;
namespace Gum.Forms.DefaultVisuals.V3;

/// <summary>
/// Default V3 visual for a ListBox control. Contains a bordered background, a focus indicator bar,
/// a vertical scroll bar, and a clipped scrollable inner panel for list items.
/// </summary>
public class ListBoxVisual : InteractiveGue
{
    /// <summary>
    /// The bordered background nine-slice that fills the control.
    /// </summary>
    public NineSliceRuntime Background { get; private set; }

    /// <summary>
    /// A thin bar displayed at the bottom of the control when focused.
    /// </summary>
    public NineSliceRuntime FocusedIndicator { get; private set; }

    /// <summary>
    /// The container that holds both the scroll bar and the clipped content area.
    /// </summary>
    public ContainerRuntime ClipAndScrollContainer { get; private set; }

    /// <summary>
    /// The vertical scroll bar for scrolling the list content. May be null if the default
    /// ScrollBar does not use a visual of type ScrollBarVisual.
    /// </summary>
    public ScrollBarVisual? VerticalScrollBarInstance { get; private set; }

    /// <summary>
    /// The parent container that uses ratio sizing to fill space beside the scroll bar.
    /// </summary>
    public ContainerRuntime ClipContainerParent { get; private set; }

    /// <summary>
    /// The container that clips its children to provide scrollable content bounds.
    /// </summary>
    public ContainerRuntime ClipContainerInstance { get; private set; }

    /// <summary>
    /// The stacked container that holds the list items. Uses TopToBottomStack children layout.
    /// </summary>
    public ContainerRuntime InnerPanelInstance { get; private set; }

    public class ListBoxCategoryStates
    {
        public StateSave Enabled { get; set; } = new StateSave() { Name = FrameworkElement.EnabledStateName };
        public StateSave Disabled { get; set; } = new StateSave() { Name = FrameworkElement.DisabledStateName };
        public StateSave DisabledFocused { get; set; } = new StateSave() { Name = FrameworkElement.DisabledFocusedStateName };
        public StateSave Focused { get; set; } = new StateSave() { Name = FrameworkElement.FocusedStateName };
        public StateSave Highlighted { get; set; } = new StateSave() { Name = FrameworkElement.HighlightedStateName };
        public StateSave HighlightedFocused { get; set; } = new StateSave() { Name = FrameworkElement.HighlightedFocusedStateName };
        public StateSave Pushed { get; set; } = new StateSave() { Name = FrameworkElement.PushedStateName };
    }

    public ListBoxCategoryStates States;

    /// <summary>
    /// The state category used by the Forms control to apply visual states.
    /// </summary>
    public StateSaveCategory ListBoxCategory { get; private set; }

    Color _backgroundColor;

    /// <summary>
    /// The base color applied to the background. Setting this value immediately updates the
    /// visual. States may tint this color.
    /// </summary>
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
                FormsControl?.UpdateState();
            }
        }
    }

    public ListBoxVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true) : base(new InvisibleRenderable())
    {
        this.HasEvents = true;
        Width = 256;
        Height = 256;

        States = new ListBoxCategoryStates();
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

        FocusedIndicator = new NineSliceRuntime();
        FocusedIndicator.Name = "FocusedIndicator";
        FocusedIndicator.X = 0f;
        FocusedIndicator.XUnits = GeneralUnitType.PixelsFromMiddle;
        FocusedIndicator.Y = 2f;
        FocusedIndicator.YUnits = GeneralUnitType.PixelsFromLarge;
        FocusedIndicator.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
        FocusedIndicator.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Top;
        FocusedIndicator.Width = 0f;
        FocusedIndicator.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        FocusedIndicator.Height = 2f;
        FocusedIndicator.HeightUnits = global::Gum.DataTypes.DimensionUnitType.Absolute;
        FocusedIndicator.Texture = uiSpriteSheetTexture;
        FocusedIndicator.ApplyState(Styling.ActiveStyle.NineSlice.Solid);
        // NOTE: Focus for a ListBox must come from code 
        // or from a user enabling keyboard navigation with FrameworkElement.KeyboardsForUiControl.Add(GumUI.Keyboard);
        FocusedIndicator.Visible = false; 
        this.AddChild(FocusedIndicator);

        ClipAndScrollContainer = new ContainerRuntime();
        ClipAndScrollContainer.Name = "ClipAndScrollContainer";
        ClipAndScrollContainer.X = 0f;
        ClipAndScrollContainer.XUnits = GeneralUnitType.PixelsFromMiddle;
        ClipAndScrollContainer.Y = 0f;
        ClipAndScrollContainer.YUnits = GeneralUnitType.PixelsFromMiddle;
        ClipAndScrollContainer.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
        ClipAndScrollContainer.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
        ClipAndScrollContainer.Width = 0f;
        ClipAndScrollContainer.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        ClipAndScrollContainer.Height = 0f;
        ClipAndScrollContainer.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        ClipAndScrollContainer.HasEvents = false;
        this.AddChild(ClipAndScrollContainer);

        var scrollBar = new ScrollBar();

        var scrollBarVisual = scrollBar.Visual;

        scrollBarVisual.Name = "VerticalScrollBarInstance";
        scrollBarVisual.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Right;
        scrollBarVisual.XUnits = GeneralUnitType.PixelsFromLarge;
        scrollBarVisual.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
        scrollBarVisual.YUnits = GeneralUnitType.PixelsFromMiddle;
        scrollBarVisual.Height = 0;
        scrollBarVisual.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
        ClipAndScrollContainer.Children.Add(scrollBarVisual);
        VerticalScrollBarInstance = scrollBarVisual as ScrollBarVisual;

        ClipContainerParent = new ContainerRuntime();
        ClipContainerParent.Name = "ClipContainerParent";
        ClipContainerParent.X = 0f;
        ClipContainerParent.XUnits = GeneralUnitType.PixelsFromSmall;
        ClipContainerParent.Y = 0f;
        ClipContainerParent.YUnits = GeneralUnitType.PixelsFromMiddle;
        ClipContainerParent.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Left;
        ClipContainerParent.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
        ClipContainerParent.Width = 1f;
        ClipContainerParent.WidthUnits = global::Gum.DataTypes.DimensionUnitType.Ratio;
        ClipContainerParent.Height = 0f;
        ClipContainerParent.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        ClipContainerParent.HasEvents = false;
        ClipAndScrollContainer.Children.Add(ClipContainerParent);

        ClipContainerInstance = new ContainerRuntime();
        ClipContainerInstance.Name = "ClipContainerInstance";
        ClipContainerInstance.X = 2f;
        ClipContainerInstance.Y = 2f;
        ClipContainerInstance.ClipsChildren = true;
        ClipContainerInstance.Height = -4f;
        ClipContainerInstance.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        ClipContainerInstance.Width = -4f;
        ClipContainerInstance.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        ClipContainerInstance.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Top;
        ClipContainerInstance.YUnits = GeneralUnitType.PixelsFromSmall;
        ClipContainerInstance.HasEvents = false;
        ClipContainerParent.Children.Add(ClipContainerInstance);

        InnerPanelInstance = new ContainerRuntime();
        InnerPanelInstance.Name = "InnerPanelInstance";
        InnerPanelInstance.Width = 0f;
        InnerPanelInstance.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        InnerPanelInstance.Height = 0f;
        InnerPanelInstance.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        InnerPanelInstance.ChildrenLayout = Gum.Managers.ChildrenLayout.TopToBottomStack;
        InnerPanelInstance.HasEvents = false;
        ClipContainerInstance.Children.Add(InnerPanelInstance);

        ListBoxCategory = new StateSaveCategory();
        ListBoxCategory.Name = "ListBoxCategory";
        this.AddCategory(ListBoxCategory);

        BackgroundColor = Styling.ActiveStyle.Colors.InputBackground;
        FocusedIndicatorColor = Styling.ActiveStyle.Colors.Warning;

        DefineDynamicStyleChanges();

        if (tryCreateFormsObject)
        {
            FormsControlAsObject = new ListBox(this);
        }
    }


    private void DefineDynamicStyleChanges()
    {
        ListBoxCategory.States.Add(States.Enabled);
        States.Enabled.Apply = () =>
        {
            SetValuesForState(false, BackgroundColor);
        };

        ListBoxCategory.States.Add(States.Disabled);
        States.Disabled.Apply = () =>
        {
            SetValuesForState(false, BackgroundColor.Adjust(Styling.ActiveStyle.Colors.PercentGreyScaleDarken));
        };

        ListBoxCategory.States.Add(States.DisabledFocused);
        States.DisabledFocused.Apply = () =>
        {
            SetValuesForState(true, BackgroundColor.Adjust(Styling.ActiveStyle.Colors.PercentGreyScaleDarken));
        };

        ListBoxCategory.States.Add(States.Focused);
        States.Focused.Apply = () =>
        {
            SetValuesForState(true, BackgroundColor);
        };

        ListBoxCategory.States.Add(States.Highlighted);
        States.Highlighted.Apply = () =>
        {
            SetValuesForState(false, BackgroundColor.Adjust(Styling.ActiveStyle.Colors.PercentGreyScaleLighten));
        };

        ListBoxCategory.States.Add(States.HighlightedFocused);
        States.HighlightedFocused.Apply = () =>
        {
            SetValuesForState(true, BackgroundColor.Adjust(Styling.ActiveStyle.Colors.PercentGreyScaleLighten));
        };

        ListBoxCategory.States.Add(States.Pushed);
        States.Pushed.Apply = () =>
        {
            SetValuesForState(false, BackgroundColor.Adjust(Styling.ActiveStyle.Colors.PercentGreyScaleDarken));
        };

    }

    private void SetValuesForState(bool isFocusedVisible, Color backgroundColor)
    {
        FocusedIndicator.Visible = isFocusedVisible;
        Background.Color = backgroundColor;
        FocusedIndicator.Color = _focusedIndicatorColor;

    }

    /// <summary>
    /// Returns the strongly-typed ListBox Forms control backing this visual.
    /// </summary>
    /// <summary>
    /// Configures the list box and its containers to size their height to children content
    /// rather than using a fixed height.
    /// </summary>
    public void MakeHeightSizedToChildren()
    {
        Height = 0;
        HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        ClipAndScrollContainer.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        ClipContainerParent.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        ClipContainerParent.Height = 4;
        ClipContainerInstance.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        ClipContainerInstance.Height = 0;
        InnerPanelInstance.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToChildren;
    }

    /// <summary>
    /// Reverts the list box height to its default fixed size (256px), undoing
    /// <see cref="MakeHeightSizedToChildren"/>.
    /// </summary>
    public void MakeHeightFixedSize()
    {
        Height = 256;
        HeightUnits = global::Gum.DataTypes.DimensionUnitType.Absolute;
        ClipAndScrollContainer.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        ClipAndScrollContainer.Height = 0;
        ClipContainerParent.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        ClipContainerParent.Height = 0;
        ClipContainerInstance.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        ClipContainerInstance.Height = -4;
        InnerPanelInstance.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToChildren;
    }

    /// <summary>
    /// Returns the strongly-typed ListBox Forms control backing this visual.
    /// </summary>
    public ListBox FormsControl => (ListBox)FormsControlAsObject;
}
