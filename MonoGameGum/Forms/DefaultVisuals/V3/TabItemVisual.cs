#pragma warning disable CS0618, GUM001 // Default visuals intentionally use deprecated MonoGameGum.GueDeriving shim types for backward compatibility until V1/V2/V3 visuals are retired. See issue #2715.
using Gum.Converters;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Forms.Controls;
using Gum.Wireframe;
#if XNALIKE
using Microsoft.Xna.Framework;
using MonoGameGum.GueDeriving;
#else
using Gum.GueDeriving;
#endif
using RenderingLibrary.Graphics;
#if SKIA
using Color = SkiaSharp.SKColor;
#endif

namespace Gum.Forms.DefaultVisuals.V3;

/// <summary>
/// Default V3 visual for a TabItem: a background sized to its header text. The selected tab shows the
/// primary color.
/// </summary>
public class TabItemVisual : InteractiveGue
{
    /// <summary>The tab's background, colored by the Selected/Unselected states.</summary>
    public NineSliceRuntime Background { get; private set; }

    /// <summary>The header text.</summary>
    public TextRuntime TextInstance { get; private set; }

    public class TabItemCategoryStates
    {
        public StateSave Selected { get; set; } = new StateSave() { Name = FrameworkElement.SelectedStateName };
        public StateSave Unselected { get; set; } = new StateSave() { Name = "Unselected" };
    }

    /// <summary>This visual's category states, typed by name. Hides the untyped States dictionary on GraphicalUiElement.</summary>
    public new TabItemCategoryStates States;

    /// <summary>The state category the TabItem sets.</summary>
    public StateSaveCategory TabItemCategory { get; private set; }

    /// <summary>The background color of the selected tab.</summary>
    public Color SelectedBackgroundColor { get; set; }

    /// <summary>The background color of an unselected tab.</summary>
    public Color UnselectedBackgroundColor { get; set; }

    /// <summary>The header text color of the selected tab.</summary>
    public Color SelectedForegroundColor { get; set; } = new Color(16, 16, 16);

    /// <summary>The header text color of an unselected tab.</summary>
    public Color ForegroundColor { get; set; }

    public TabItemVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true) : base(new InvisibleRenderable())
    {
        HasEvents = true;
        Width = 16;
        WidthUnits = DimensionUnitType.RelativeToChildren;
        Height = 0;
        HeightUnits = DimensionUnitType.RelativeToParent;

        Background = new NineSliceRuntime();
        Background.Name = "Background";
        Background.Width = 0;
        Background.WidthUnits = DimensionUnitType.RelativeToParent;
        Background.Height = 0;
        Background.HeightUnits = DimensionUnitType.RelativeToParent;
        Background.Texture = Styling.ActiveStyle.SpriteSheet;
        Background.ApplyState(Styling.ActiveStyle.NineSlice.Solid);
        AddChild(Background);

        TextInstance = new TextRuntime();
        TextInstance.Name = "TextInstance";
        TextInstance.Text = "Tab";
        TextInstance.X = 0;
        TextInstance.XUnits = GeneralUnitType.PixelsFromMiddle;
        TextInstance.XOrigin = HorizontalAlignment.Center;
        TextInstance.Y = 0;
        TextInstance.YUnits = GeneralUnitType.PixelsFromMiddle;
        TextInstance.YOrigin = VerticalAlignment.Center;
        TextInstance.Width = 0;
        TextInstance.WidthUnits = DimensionUnitType.RelativeToChildren;
        TextInstance.Height = 0;
        TextInstance.HeightUnits = DimensionUnitType.RelativeToChildren;
        TextInstance.ApplyState(Styling.ActiveStyle.Text.Normal);
        AddChild(TextInstance);

        SelectedBackgroundColor = Styling.ActiveStyle.Colors.Primary;
        UnselectedBackgroundColor = Styling.ActiveStyle.Colors.InputBackground;
        ForegroundColor = Styling.ActiveStyle.Colors.TextPrimary;

        TabItemCategory = new StateSaveCategory();
        TabItemCategory.Name = "TabItemCategory";
        AddCategory(TabItemCategory);

        States = new TabItemCategoryStates();
        TabItemCategory.States.Add(States.Selected);
        States.Selected.Apply = () =>
        {
            Background.Color = SelectedBackgroundColor;
            TextInstance.Color = SelectedForegroundColor;
        };
        TabItemCategory.States.Add(States.Unselected);
        States.Unselected.Apply = () =>
        {
            Background.Color = UnselectedBackgroundColor;
            TextInstance.Color = ForegroundColor;
        };

        if (tryCreateFormsObject)
        {
            FormsControlAsObject = new TabItem(this);
        }
    }

    /// <summary>Returns the strongly-typed TabItem Forms control backing this visual.</summary>
    public TabItem FormsControl => (TabItem)FormsControlAsObject!;
}
