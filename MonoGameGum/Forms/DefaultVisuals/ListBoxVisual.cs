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

namespace MonoGameGum.Forms.DefaultVisuals;

public class ListBoxVisual : InteractiveGue
{
    public NineSliceRuntime Background { get; private set; }
    public NineSliceRuntime FocusedIndicator { get; private set; }
    public ContainerRuntime ClipAndScrollContainer { get; private set; }
    public ScrollBarVisual VerticalScrollBarInstance { get; private set; }
    public ContainerRuntime ClipContainerParent { get; private set; }
    public ContainerRuntime ClipContainerInstance { get; private set; }
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

    public StateSaveCategory ListBoxCategory { get; private set; }

    public ListBoxVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true) : base(new InvisibleRenderable())
    {
        Width = 150;
        Height = 150;

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
        Background.Color = Styling.Colors.DarkGray;
        Background.Texture = uiSpriteSheetTexture;
        Background.ApplyState(Styling.NineSlice.Bordered);
        this.AddChild(Background);

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
        FocusedIndicator.Color = Styling.Colors.Warning;
        FocusedIndicator.Texture = uiSpriteSheetTexture;
        FocusedIndicator.ApplyState(Styling.NineSlice.Solid);
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
        this.AddChild(ClipAndScrollContainer);

        VerticalScrollBarInstance = new ScrollBarVisual();
        VerticalScrollBarInstance.Name = "VerticalScrollBarInstance";
        VerticalScrollBarInstance.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Right;
        VerticalScrollBarInstance.XUnits = GeneralUnitType.PixelsFromLarge;
        VerticalScrollBarInstance.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
        VerticalScrollBarInstance.YUnits = GeneralUnitType.PixelsFromMiddle;
        VerticalScrollBarInstance.Height = 0;
        VerticalScrollBarInstance.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
        ClipAndScrollContainer.Children.Add(VerticalScrollBarInstance);

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
        ClipAndScrollContainer.Children.Add(ClipContainerParent);

        ClipContainerInstance = new ContainerRuntime();
        ClipContainerInstance.Name = "ClipContainerInstance";
        ClipContainerInstance.ClipsChildren = true;
        ClipContainerInstance.Height = -4f;
        ClipContainerInstance.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        ClipContainerInstance.Width = -4f;
        ClipContainerInstance.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        ClipContainerInstance.X = 2f;
        ClipContainerInstance.Y = 2f;
        ClipContainerInstance.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Top;
        ClipContainerInstance.YUnits = GeneralUnitType.PixelsFromSmall;
        ClipContainerParent.Children.Add(ClipContainerInstance);

        InnerPanelInstance = new ContainerRuntime();
        InnerPanelInstance.Name = "InnerPanelInstance";
        InnerPanelInstance.Height = 0f;
        InnerPanelInstance.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        InnerPanelInstance.Width = 0f;
        InnerPanelInstance.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        InnerPanelInstance.ChildrenLayout = Gum.Managers.ChildrenLayout.TopToBottomStack;
        ClipContainerInstance.Children.Add(InnerPanelInstance);

        ListBoxCategory = new StateSaveCategory();
        ListBoxCategory.Name = "ListBoxCategory";
        this.AddCategory(ListBoxCategory);

        void AddVariable(StateSave state, string name, object value)
        {
            state.Variables.Add(new VariableSave
            {
                Name = name,
                Value = value
            });
        }

        // If the set of variables increases, create an AddState void method similar to tghe other V2s.

        ListBoxCategory.States.Add(States.Enabled);
        AddVariable(States.Enabled, "FocusedIndicator.Visible", false);

        ListBoxCategory.States.Add(States.Disabled);
        AddVariable(States.Disabled, "FocusedIndicator.Visible", false);

        ListBoxCategory.States.Add(States.DisabledFocused);
        AddVariable(States.DisabledFocused, "FocusedIndicator.Visible", true);

        ListBoxCategory.States.Add(States.Focused);
        AddVariable(States.Focused, "FocusedIndicator.Visible", true);

        ListBoxCategory.States.Add(States.Highlighted);
        AddVariable(States.Highlighted, "FocusedIndicator.Visible", false);

        ListBoxCategory.States.Add(States.HighlightedFocused);
        AddVariable(States.HighlightedFocused, "FocusedIndicator.Visible", true);

        ListBoxCategory.States.Add(States.Pushed);
        AddVariable(States.Pushed, "FocusedIndicator.Visible", false);

        if (tryCreateFormsObject)
        {
            FormsControlAsObject = new ListBox(this);
        }
    }

    public ListBox FormsControl => FormsControlAsObject as ListBox;
}
