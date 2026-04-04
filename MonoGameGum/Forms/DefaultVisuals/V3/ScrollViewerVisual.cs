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

/// <summary>
/// Default V3 visual for a ScrollViewer control. Contains a bordered background, vertical and
/// horizontal scroll bars, a clipped content area with an inner panel, and a focus indicator bar.
/// </summary>
public class ScrollViewerVisual : InteractiveGue
{
    /// <summary>
    /// The bordered background nine-slice that fills the control.
    /// </summary>
    public NineSliceRuntime Background { get; private set; }

    /// <summary>
    /// The vertical scroll bar on the right side of the content area.
    /// </summary>
    public ScrollBarVisual VerticalScrollBarInstance { get; private set; }

    /// <summary>
    /// The horizontal scroll bar at the bottom of the content area.
    /// </summary>
    public ScrollBarVisual HorizontalScrollBarInstance { get; private set; }

    /// <summary>
    /// The stacked container that holds the scrollable content. Uses TopToBottomStack children layout.
    /// </summary>
    public ContainerRuntime InnerPanelInstance { get; private set; }

    /// <summary>
    /// The container that clips its children to provide scrollable content bounds.
    /// </summary>
    public ContainerRuntime ClipContainerInstance { get; private set; }

    /// <summary>
    /// The container that holds the scroll bars and the clipped content area.
    /// </summary>
    public ContainerRuntime ScrollAndClipContainer { get; private set; }

    /// <summary>
    /// The intermediate container between ScrollAndClipContainer and ClipContainerInstance,
    /// sized to account for scroll bar width/height.
    /// </summary>
    public ContainerRuntime ClipContainerContainer { get; private set; }

    /// <summary>
    /// A thin bar displayed at the bottom of the control when focused.
    /// </summary>
    public NineSliceRuntime FocusedIndicator { get; private set; }

    public class ScrollViewerCategoryStates
    {
        public StateSave Enabled { get; private set; } = new StateSave() { Name = FrameworkElement.EnabledStateName };
        public StateSave Focused { get; private set; } = new StateSave() { Name = FrameworkElement.FocusedStateName };
    }

    public ScrollViewerCategoryStates States;

    /// <summary>
    /// The state category used by the Forms control to apply visual states.
    /// </summary>
    public StateSaveCategory ScrollViewerCategory { get; private set; }

    Color _backgroundColor;
    /// <summary>
    /// The color applied to the background. Setting this value immediately updates the visual.
    /// </summary>
    public Color BackgroundColor
    {
        get => _backgroundColor;
        set
        {
            if(!value.Equals(_backgroundColor))
            {
                _backgroundColor = value;
                Background.Color = _backgroundColor;
            }
        }
    }

    Color _focusedIndicatorColor;
    /// <summary>
    /// The color of the focus indicator bar shown when the control has focus.
    /// Setting this value immediately updates the visual.
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
    /// Configures the scroll viewer and its containers to size to their children content
    /// rather than using fixed dimensions.
    /// </summary>
    public void MakeSizedToChildren()
    {
        Height = 0;
        HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        ScrollAndClipContainer.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        ClipContainerContainer.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        ClipContainerContainer.Height = 4;
        ClipContainerInstance.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        ClipContainerInstance.Height = 0;
        InnerPanelInstance.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;

        Width = 0;
        WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        ScrollAndClipContainer.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        ClipContainerContainer.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        ClipContainerContainer.Width = 4;
        ClipContainerInstance.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        ClipContainerInstance.Width = 0;
        InnerPanelInstance.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
    }

    public ScrollViewerVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true) : base(new InvisibleRenderable())
    {
        this.HasEvents = true;
        Width = 150;
        Height = 150;

        var uiSpriteSheetTexture = Styling.ActiveStyle.SpriteSheet;

        {
            Background = new NineSliceRuntime();
            Background.Name = "Background";
            Background.Height = 0f;
            Background.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
            Background.Width = 0f;
            Background.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
            Background.X = 0f;
            Background.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
            Background.XUnits = GeneralUnitType.PixelsFromMiddle;
            Background.Y = 0f;
            Background.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
            Background.YUnits = GeneralUnitType.PixelsFromMiddle;
            Background.Texture = uiSpriteSheetTexture;
            Background.ApplyState(Styling.ActiveStyle.NineSlice.Bordered);
            this.AddChild(Background);

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

            ScrollAndClipContainer = new ContainerRuntime();
            ScrollAndClipContainer.Name = "ScrollAndClipContainer";
            ScrollAndClipContainer.Width = 0;
            ScrollAndClipContainer.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
            ScrollAndClipContainer.Height = 0;
            ScrollAndClipContainer.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
            ScrollAndClipContainer.HasEvents = false;
            this.AddChild(ScrollAndClipContainer);

            {
                VerticalScrollBarInstance = new ScrollBarVisual();
                VerticalScrollBarInstance.Name = "VerticalScrollBarInstance";
                VerticalScrollBarInstance.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Right;
                VerticalScrollBarInstance.XUnits = GeneralUnitType.PixelsFromLarge;
                VerticalScrollBarInstance.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Top;
                VerticalScrollBarInstance.YUnits = GeneralUnitType.PixelsFromSmall;
                VerticalScrollBarInstance.Height = -24;
                VerticalScrollBarInstance.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
                ScrollAndClipContainer.AddChild(VerticalScrollBarInstance);

                HorizontalScrollBarInstance = new ScrollBarVisual();
                HorizontalScrollBarInstance.Name = "HorizontalScrollBarInstance";
                HorizontalScrollBarInstance.FormsControl.Orientation = Orientation.Horizontal;
                HorizontalScrollBarInstance.XOrigin = HorizontalAlignment.Left;
                HorizontalScrollBarInstance.XUnits = GeneralUnitType.PixelsFromSmall;
                HorizontalScrollBarInstance.YOrigin = VerticalAlignment.Bottom;
                HorizontalScrollBarInstance.YUnits = GeneralUnitType.PixelsFromLarge;
                HorizontalScrollBarInstance.Width = -24;
                HorizontalScrollBarInstance.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
                ScrollAndClipContainer.AddChild(HorizontalScrollBarInstance);


                // ClipContainerContainer uses a ratio to fill available space,
                // and the clip container is inside of that and adds its own margins
                ClipContainerContainer = new ContainerRuntime();
                ClipContainerContainer.Name = "ClipContainerContainer";
                ClipContainerContainer.Height = -24f;
                ClipContainerContainer.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
                ClipContainerContainer.Width = -24;
                ClipContainerContainer.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
                ClipContainerContainer.HasEvents = false;
                ScrollAndClipContainer.AddChild(ClipContainerContainer);

                {
                    ClipContainerInstance = new ContainerRuntime();
                    ClipContainerInstance.Name = "ClipContainerInstance";
                    ClipContainerInstance.ClipsChildren = true;
                    ClipContainerInstance.Height = -4f;
                    ClipContainerInstance.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
                    ClipContainerInstance.Width = -4;
                    ClipContainerInstance.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
                    ClipContainerInstance.X = 2f;
                    ClipContainerInstance.Y = 2f;
                    ClipContainerInstance.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Top;
                    ClipContainerInstance.YUnits = GeneralUnitType.PixelsFromSmall;
                    ClipContainerInstance.HasEvents = false;
                    ClipContainerContainer.AddChild(ClipContainerInstance);

                    {
                        InnerPanelInstance = new ContainerRuntime();
                        InnerPanelInstance.Name = "InnerPanelInstance";
                        InnerPanelInstance.Height = 0f;
                        InnerPanelInstance.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToChildren;
                        InnerPanelInstance.Width = 0f;
                        InnerPanelInstance.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
                        InnerPanelInstance.ChildrenLayout = Gum.Managers.ChildrenLayout.TopToBottomStack;
                        InnerPanelInstance.HasEvents = false;
                        ClipContainerInstance.AddChild(InnerPanelInstance);
                    }
                }

            }

            BackgroundColor = Styling.ActiveStyle.Colors.InputBackground;
            FocusedIndicatorColor = Styling.ActiveStyle.Colors.Warning;
        }

        CreateStates();

        if (tryCreateFormsObject)
        {
            this.FormsControlAsObject = new ScrollViewer(this);
            RefreshMarginsFromScrollBarVisibility();
        }
    }

    private void CreateStates()
    {
        CreateScrollViewerCategory();

        CreateScrollBarVisibilityCategory();
    }

    private void CreateScrollViewerCategory()
    {
        ScrollViewerCategory = new StateSaveCategory();
        ScrollViewerCategory.Name = ScrollViewer.ScrollViewerCategoryName;
        this.AddCategory(ScrollViewerCategory);

        void AddVariable(StateSave state, string name, object value)
        {
            state.Variables.Add(new VariableSave
            {
                Name = name,
                Value = value
            });
        }

        void AddState(StateSave state, bool isFocusedVisible)
        {
            ScrollViewerCategory.States.Add(state);
            AddVariable(state, "FocusedIndicator.Visible", isFocusedVisible);
        }

        States = new ScrollViewerCategoryStates();
        AddState(States.Enabled, false);
        AddState(States.Focused, true);
    }

    private void CreateScrollBarVisibilityCategory()
    {
        var category = new StateSaveCategory();
        category.Name = ScrollViewer.ScrollBarVisibilityCategoryName;

        StateSave state;

        state = new StateSave
        {
            Name = "VerticalScrollVisible"
        };
        state.Apply = RefreshMarginsFromScrollBarVisibility;
        category.States.Add(state);


        state = new StateSave
        {
            Name = "HorizontalScrollVisible"
        };
        state.Apply = RefreshMarginsFromScrollBarVisibility;
        category.States.Add(state);

        state = new StateSave
        {
            Name = "BothScrollVisible"
        };
        state.Apply = RefreshMarginsFromScrollBarVisibility;
        category.States.Add(state);

        state = new StateSave
        {
            Name = "NoScrollBar"
        };
        state.Apply = RefreshMarginsFromScrollBarVisibility;
        category.States.Add(state);


        this.AddCategory(category);

    }

    private void RefreshMarginsFromScrollBarVisibility()
    {
        if (VerticalScrollBarInstance.Parent == ScrollAndClipContainer)
        {
            float margin = 0;
            // Check the parent to verify that the user hasn't removed the ScrollBar
            if (VerticalScrollBarInstance.Visible)
            {
                margin = VerticalScrollBarInstance.GetAbsoluteWidth();
            }
            ClipContainerContainer.Width = -margin;
            if(HorizontalScrollBarInstance != null)
            {
                HorizontalScrollBarInstance.Width = -margin;
            }
        }

        if (HorizontalScrollBarInstance.Parent == ScrollAndClipContainer)
        {
            float margin = 0;
            if (HorizontalScrollBarInstance.Visible)
            {
                margin = HorizontalScrollBarInstance.GetAbsoluteHeight();
            }
            ClipContainerContainer.Height = -margin;
            if(VerticalScrollBarInstance != null)
            {
                VerticalScrollBarInstance.Height = -margin;
            }
        }
    }

    /// <summary>
    /// Returns the strongly-typed ScrollViewer Forms control backing this visual.
    /// </summary>
    public ScrollViewer FormsControl => (ScrollViewer)this.FormsControlAsObject;
}
