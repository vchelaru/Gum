#pragma warning disable CS0618, GUM001 // Default visuals intentionally use deprecated MonoGameGum.GueDeriving shim types for backward compatibility until V1/V2/V3 visuals are retired. See issue #2715.
using Gum.Converters;
using Gum.DataTypes;
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
/// Default V3 visual for a TabControl: a strip of tab headers (InnerPanelInstance) above a bordered
/// content panel (ContentPanelInstance) that holds the pages.
/// </summary>
public class TabControlVisual : InteractiveGue
{
    /// <summary>The height of the tab strip.</summary>
    public const float StripHeight = 32;

    /// <summary>The strip the tab headers are stacked in, left to right.</summary>
    public ContainerRuntime InnerPanelInstance { get; private set; }

    /// <summary>The bordered background behind the pages.</summary>
    public NineSliceRuntime ContentBackground { get; private set; }

    /// <summary>The container the pages are placed in. Each page fills it.</summary>
    public ContainerRuntime ContentPanelInstance { get; private set; }

    public TabControlVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true) : base(new InvisibleRenderable())
    {
        HasEvents = true;
        Width = 400;
        Height = 300;

        InnerPanelInstance = new ContainerRuntime();
        InnerPanelInstance.Name = "InnerPanelInstance";
        InnerPanelInstance.Width = 0;
        InnerPanelInstance.WidthUnits = DimensionUnitType.RelativeToParent;
        InnerPanelInstance.Height = StripHeight;
        InnerPanelInstance.HeightUnits = DimensionUnitType.Absolute;
        InnerPanelInstance.ChildrenLayout = Gum.Managers.ChildrenLayout.LeftToRightStack;
        InnerPanelInstance.StackSpacing = 2;
        AddChild(InnerPanelInstance);

        ContentBackground = new NineSliceRuntime();
        ContentBackground.Name = "ContentBackground";
        ContentBackground.Y = StripHeight;
        ContentBackground.Width = 0;
        ContentBackground.WidthUnits = DimensionUnitType.RelativeToParent;
        ContentBackground.Height = -StripHeight;
        ContentBackground.HeightUnits = DimensionUnitType.RelativeToParent;
        ContentBackground.Texture = Styling.ActiveStyle.SpriteSheet;
        ContentBackground.ApplyState(Styling.ActiveStyle.NineSlice.Bordered);
        ContentBackground.Color = Styling.ActiveStyle.Colors.InputBackground;
        AddChild(ContentBackground);

        ContentPanelInstance = new ContainerRuntime();
        ContentPanelInstance.Name = TabControl.ContentPanelInstanceName;
        ContentPanelInstance.Y = StripHeight;
        ContentPanelInstance.Width = 0;
        ContentPanelInstance.WidthUnits = DimensionUnitType.RelativeToParent;
        ContentPanelInstance.Height = -StripHeight;
        ContentPanelInstance.HeightUnits = DimensionUnitType.RelativeToParent;
        AddChild(ContentPanelInstance);

        if (tryCreateFormsObject)
        {
            FormsControlAsObject = new TabControl(this);
        }
    }

    /// <summary>Returns the strongly-typed TabControl Forms control backing this visual.</summary>
    public TabControl FormsControl => (TabControl)FormsControlAsObject!;
}
