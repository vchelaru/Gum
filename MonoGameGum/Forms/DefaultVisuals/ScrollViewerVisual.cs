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
#else
using Microsoft.Xna.Framework.Graphics;
using MonoGameGum.GueDeriving;
#endif
using Gum.Forms.Controls;
namespace Gum.Forms.DefaultVisuals;

public class ScrollViewerVisual : InteractiveGue
{
    public NineSliceRuntime Background { get; private set; }
    public ScrollBarVisual VerticalScrollBarInstance { get; private set; }
    public ScrollBarVisual HorizontalScrollBarInstance { get; private set; }
    public ContainerRuntime InnerPanelInstance { get; private set; }
    public ContainerRuntime ClipContainerInstance { get; private set; }
    public ContainerRuntime ScrollAndClipContainer { get; private set; }
    public ContainerRuntime ClipContainerContainer { get; private set; }
    public NineSliceRuntime FocusedIndicator { get; private set; }

    public class ScrollViewerCategoryStates
    {
        public StateSave Enabled { get; private set; } = new StateSave() { Name = FrameworkElement.EnabledStateName };
        public StateSave Focused { get; private set; } = new StateSave() { Name = FrameworkElement.FocusedStateName };
    }

    public ScrollViewerCategoryStates States;

    public StateSaveCategory ScrollViewerCategory { get; private set; }


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
        Width = 150;
        Height = 200;

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
            Background.Color = Styling.ActiveStyle.Colors.DarkGray;
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
            FocusedIndicator.Color = Styling.ActiveStyle.Colors.Warning;
            this.AddChild(FocusedIndicator);

            ScrollAndClipContainer = new ContainerRuntime();
            ScrollAndClipContainer.Name = "ScrollAndClipContainer";
            ScrollAndClipContainer.Width = 0;
            ScrollAndClipContainer.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
            ScrollAndClipContainer.Height = 0;
            ScrollAndClipContainer.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
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
                    ClipContainerContainer.AddChild(ClipContainerInstance);

                    {
                        InnerPanelInstance = new ContainerRuntime();
                        InnerPanelInstance.Name = "InnerPanelInstance";
                        InnerPanelInstance.Height = 0f;
                        InnerPanelInstance.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToChildren;
                        InnerPanelInstance.Width = 0f;
                        InnerPanelInstance.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
                        InnerPanelInstance.ChildrenLayout = Gum.Managers.ChildrenLayout.TopToBottomStack;
                        ClipContainerInstance.AddChild(InnerPanelInstance);
                    }
                }

            }

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

    public ScrollViewer? FormsControl => this.FormsControlAsObject as ScrollViewer;
}
