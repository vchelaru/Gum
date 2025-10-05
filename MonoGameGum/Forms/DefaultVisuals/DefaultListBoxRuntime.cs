using Gum.Converters;
using Gum.DataTypes.Variables;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using Gum.Forms.Controls;
using MonoGameGum.GueDeriving;
using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Styling = Gum.Forms.DefaultVisuals.Styling;


namespace MonoGameGum.Forms.DefaultVisuals;

public class DefaultListBoxRuntime : InteractiveGue
{
    public RectangleRuntime FocusedIndicator { get; private set; }

    public DefaultListBoxRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true) : base(new InvisibleRenderable())
    {
        if (fullInstantiation)
        {
            Width = 150;
            Height = 150;

            var background = new ColoredRectangleRuntime();
            background.Name = "Background";

            var InnerPanel = new ContainerRuntime();
            InnerPanel.Name = "InnerPanelInstance";
            var ClipContainer = new ContainerRuntime();
            ClipContainer.Name = "ClipContainerInstance";
            var VerticalScrollBarInstance = new DefaultScrollBarRuntime();
            VerticalScrollBarInstance.Name = "VerticalScrollBarInstance";
            //var HorizontalScrollBarInstance = new DefaultScrollBarRuntime();
            //HorizontalScrollBarInstance.Name = "HorizontalScrollBarInstance";

            background.Height = 0f;
            background.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
            background.Width = 0f;
            background.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
            background.X = 0f;
            background.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
            background.XUnits = GeneralUnitType.PixelsFromMiddle;
            background.Y = 0f;
            background.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
            background.YUnits = GeneralUnitType.PixelsFromMiddle;
            background.Color = Styling.ActiveStyle.Colors.DarkGray;
            this.Children.Add(background);

            VerticalScrollBarInstance.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Right;
            VerticalScrollBarInstance.XUnits = GeneralUnitType.PixelsFromLarge;
            VerticalScrollBarInstance.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
            VerticalScrollBarInstance.YUnits = GeneralUnitType.PixelsFromMiddle;
            //VerticalScrollBarInstance.Width = 24;
            VerticalScrollBarInstance.Height = 0;
            VerticalScrollBarInstance.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
            this.Children.Add(VerticalScrollBarInstance);


            ClipContainer.ClipsChildren = true;
            ClipContainer.Height = -4f;
            ClipContainer.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
            ClipContainer.Width = -27f;
            ClipContainer.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
            ClipContainer.X = 2f;
            ClipContainer.Y = 2f;
            ClipContainer.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Top;
            ClipContainer.YUnits = GeneralUnitType.PixelsFromSmall;
            this.Children.Add(ClipContainer);


            InnerPanel.Height = 0f;
            InnerPanel.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToChildren;
            InnerPanel.Width = 0f;
            InnerPanel.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
            InnerPanel.ChildrenLayout = Gum.Managers.ChildrenLayout.TopToBottomStack;
            ClipContainer.Children.Add(InnerPanel);


            FocusedIndicator = new RectangleRuntime();
            FocusedIndicator.X = 0;
            FocusedIndicator.Y = 0;
            FocusedIndicator.XUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle;
            FocusedIndicator.YUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle;
            FocusedIndicator.XOrigin = HorizontalAlignment.Center;
            FocusedIndicator.YOrigin = VerticalAlignment.Center;
            FocusedIndicator.Width = 0;
            FocusedIndicator.Height = 0;
            FocusedIndicator.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
            FocusedIndicator.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
            FocusedIndicator.Color = Color.White;
            FocusedIndicator.Visible = false;
            FocusedIndicator.Name = "FocusedIndicator";
            this.Children.Add(FocusedIndicator);


            var listBoxCategory = new StateSaveCategory();
            listBoxCategory.Name = "ListBoxCategory";
            this.AddCategory(listBoxCategory);

            StateSave currentState;

            void AddState(string name)
            {
                var state = new StateSave();
                state.Name = name;
                listBoxCategory.States.Add(state);
                currentState = state;
            }

            void AddVariable(string name, object value)
            {
                currentState.Variables.Add(new VariableSave
                {
                    Name = name,
                    Value = value
                });
            }

            // For now let's just have the focus indicator show/hide.

            AddState(FrameworkElement.DisabledStateName);
            AddVariable("FocusedIndicator.Visible", false);

            AddState(FrameworkElement.DisabledFocusedStateName);
            AddVariable("FocusedIndicator.Visible", true);

            AddState(FrameworkElement.EnabledStateName);
            AddVariable("FocusedIndicator.Visible", false);

            AddState(FrameworkElement.FocusedStateName);
            AddVariable("FocusedIndicator.Visible", true);

            AddState(FrameworkElement.HighlightedStateName);
            AddVariable("FocusedIndicator.Visible", false);

            AddState(FrameworkElement.HighlightedFocusedStateName);
            AddVariable("FocusedIndicator.Visible", true);

            AddState(FrameworkElement.PushedStateName);
            AddVariable("FocusedIndicator.Visible", false);




        }

        if (tryCreateFormsObject)
        {
            FormsControlAsObject = new ListBox(this);
        }
    }

    public ListBox FormsControl => FormsControlAsObject as ListBox;
}
