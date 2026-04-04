using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using MonoGameGum.GueDeriving;
using RenderingLibrary.Graphics;

namespace Gum.Themes.Editor;

/// <summary>
/// Editor-themed visual for the Expander control.
/// Contains a clickable header row with an arrow indicator and text,
/// and a content area that shows/hides when toggled.
/// </summary>
public class ExpanderVisual : InteractiveGue
{
    public ExpanderVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true)
        : base(new InvisibleRenderable())
    {
        this.HasEvents = true;
        this.Width = 0;
        this.WidthUnits = DimensionUnitType.RelativeToParent;
        this.Height = 0;
        this.HeightUnits = DimensionUnitType.RelativeToChildren;
        this.ChildrenLayout = Gum.Managers.ChildrenLayout.TopToBottomStack;

        // --- Header row ---
        var headerContainer = new ContainerRuntime();
        headerContainer.Name = "HeaderContainer";
        headerContainer.Width = 0;
        headerContainer.WidthUnits = DimensionUnitType.RelativeToParent;
        headerContainer.Height = 28;
        headerContainer.HeightUnits = DimensionUnitType.Absolute;
        headerContainer.ChildrenLayout = Gum.Managers.ChildrenLayout.LeftToRightStack;
        this.Children.Add(headerContainer);

        // Arrow indicator
        var arrow = new TextRuntime();
        arrow.Name = "ArrowIndicator";
        arrow.Text = "►";
        arrow.Width = 20;
        arrow.WidthUnits = DimensionUnitType.Absolute;
        arrow.Height = 0;
        arrow.HeightUnits = DimensionUnitType.RelativeToParent;
        arrow.YOrigin = VerticalAlignment.Center;
        arrow.Y = 0;
        arrow.YUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle;
        arrow.HorizontalAlignment = HorizontalAlignment.Center;
        arrow.Color = new Color(180, 180, 180);
        headerContainer.Children.Add(arrow);

        // Header text
        var textInstance = new Label();
        textInstance.Name = "TextInstance";
        textInstance.Text = "Expander";
        textInstance.Width = -20;
        textInstance.WidthUnits = DimensionUnitType.RelativeToParent;
        textInstance.Height = 0;
        textInstance.HeightUnits = DimensionUnitType.RelativeToParent;
        textInstance.YOrigin = VerticalAlignment.Center;
        textInstance.Y = 0;
        textInstance.YUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle;
        headerContainer.Children.Add(textInstance.Visual);

        // --- Content area ---
        var contentContainer = new ContainerRuntime();
        contentContainer.Name = "ContentContainer";
        contentContainer.Width = 0;
        contentContainer.WidthUnits = DimensionUnitType.RelativeToParent;
        contentContainer.Height = 0;
        contentContainer.HeightUnits = DimensionUnitType.RelativeToChildren;
        contentContainer.ChildrenLayout = Gum.Managers.ChildrenLayout.TopToBottomStack;
        contentContainer.Visible = false;
        this.Children.Add(contentContainer);

        // --- State category ---
        var expanderCategory = new Gum.DataTypes.Variables.StateSaveCategory();
        expanderCategory.Name = "ExpanderCategory";
        this.AddCategory(expanderCategory);

        var collapsedState = new Gum.DataTypes.Variables.StateSave { Name = "Collapsed" };
        collapsedState.Apply = () =>
        {
            arrow.Text = "►";
            contentContainer.Visible = false;
        };
        expanderCategory.States.Add(collapsedState);

        var expandedState = new Gum.DataTypes.Variables.StateSave { Name = "Expanded" };
        expandedState.Apply = () =>
        {
            arrow.Text = "▼";
            contentContainer.Visible = true;
        };
        expanderCategory.States.Add(expandedState);

        if (tryCreateFormsObject)
        {
            FormsControlAsObject = new Expander(this);
        }
    }
}
