#pragma warning disable CS0618, GUM001 // Default visuals intentionally use deprecated MonoGameGum.GueDeriving shim types for backward compatibility until V1/V2/V3 visuals are retired. See issue #2715.
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Forms.Controls;
using Gum.Wireframe;
#if XNALIKE
using MonoGameGum.GueDeriving;
#else
using Gum.GueDeriving;
#endif
using RenderingLibrary.Graphics;

namespace Gum.Forms.DefaultVisuals.V3;

/// <summary>
/// Default V3 visual for a TabPage: fills the TabControl's content panel, with content placed in a
/// padded, top-to-bottom InnerPanelInstance. The Unselected state hides it.
/// </summary>
public class TabPageVisual : InteractiveGue
{
    /// <summary>The padding between the page's edge and its content.</summary>
    public const float Padding = 8;

    /// <summary>The container that holds the page's content.</summary>
    public ContainerRuntime InnerPanelInstance { get; private set; }

    public class TabPageCategoryStates
    {
        public StateSave Selected { get; set; } = new StateSave() { Name = FrameworkElement.SelectedStateName };
        public StateSave Unselected { get; set; } = new StateSave() { Name = TabItem.UnselectedStateName };
    }

    /// <summary>This visual's category states, typed by name. Hides the untyped States dictionary on GraphicalUiElement.</summary>
    public new TabPageCategoryStates States;

    /// <summary>The state category the TabPage sets.</summary>
    public StateSaveCategory TabPageCategory { get; private set; }

    public TabPageVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true) : base(new InvisibleRenderable())
    {
        Width = 0;
        WidthUnits = DimensionUnitType.RelativeToParent;
        Height = 0;
        HeightUnits = DimensionUnitType.RelativeToParent;

        InnerPanelInstance = new ContainerRuntime();
        InnerPanelInstance.Name = "InnerPanelInstance";
        InnerPanelInstance.X = Padding;
        InnerPanelInstance.Y = Padding;
        InnerPanelInstance.Width = -Padding * 2;
        InnerPanelInstance.WidthUnits = DimensionUnitType.RelativeToParent;
        InnerPanelInstance.Height = -Padding * 2;
        InnerPanelInstance.HeightUnits = DimensionUnitType.RelativeToParent;
        InnerPanelInstance.ChildrenLayout = Gum.Managers.ChildrenLayout.TopToBottomStack;
        InnerPanelInstance.StackSpacing = 4;
        AddChild(InnerPanelInstance);

        TabPageCategory = new StateSaveCategory();
        TabPageCategory.Name = TabPage.TabPageCategoryName;
        AddCategory(TabPageCategory);

        States = new TabPageCategoryStates();
        TabPageCategory.States.Add(States.Selected);
        States.Selected.Apply = () => Visible = true;
        TabPageCategory.States.Add(States.Unselected);
        States.Unselected.Apply = () => Visible = false;

        if (tryCreateFormsObject)
        {
            FormsControlAsObject = new TabPage(this);
        }
    }

    /// <summary>Returns the strongly-typed TabPage Forms control backing this visual.</summary>
    public TabPage FormsControl => (TabPage)FormsControlAsObject!;
}
