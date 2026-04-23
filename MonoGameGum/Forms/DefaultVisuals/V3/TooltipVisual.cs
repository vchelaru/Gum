using Gum.DataTypes.Variables;
using Gum.Wireframe;
using RenderingLibrary;
using RenderingLibrary.Graphics;


#if XNALIKE
using Microsoft.Xna.Framework;
using MonoGameGum.GueDeriving;
using Microsoft.Xna.Framework.Graphics;
#else
using Gum.GueDeriving;
#endif
using Gum.Forms.Controls;
namespace Gum.Forms.DefaultVisuals.V3;

/// <summary>
/// Default V3 visual for a <see cref="Tooltip"/> control. Contains a bordered background
/// and a centered text label. No hover/pressed states — tooltips are passive overlays.
/// </summary>
public class TooltipVisual : InteractiveGue
{
    /// <summary>
    /// The bordered background nine-slice that fills the control.
    /// </summary>
    public NineSliceRuntime Background { get; private set; }

    /// <summary>
    /// The text label displayed inside the tooltip.
    /// </summary>
    public TextRuntime TextInstance { get; private set; }

    /// <summary>
    /// Placeholder category for future per-state styling. No states are registered in v1;
    /// the category exists so downstream styling can grow into it without a breaking change.
    /// </summary>
    public StateSaveCategory TooltipCategory { get; private set; }

    private Color _backgroundColor;
    /// <summary>
    /// The base color applied to the tooltip background. Setting this value immediately updates the visual.
    /// </summary>
    public Color BackgroundColor
    {
        get => _backgroundColor;
        set
        {
            if (!value.Equals(_backgroundColor))
            {
                _backgroundColor = value;
                if (Background != null)
                {
                    Background.Color = value;
                }
            }
        }
    }

    private Color _foregroundColor;
    /// <summary>
    /// The base color applied to the tooltip text. Setting this value immediately updates the visual.
    /// </summary>
    public Color ForegroundColor
    {
        get => _foregroundColor;
        set
        {
            if (!value.Equals(_foregroundColor))
            {
                _foregroundColor = value;
                if (TextInstance != null)
                {
                    TextInstance.Color = value;
                }
            }
        }
    }

    /// <summary>
    /// Creates a new <see cref="TooltipVisual"/>.
    /// </summary>
    /// <param name="fullInstantiation">Whether to fully build out children and default sizing. Pass false when cloning or restoring from data.</param>
    /// <param name="tryCreateFormsObject">Whether to create the backing <see cref="Tooltip"/> Forms control.</param>
    public TooltipVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true) : base(new InvisibleRenderable())
    {
        this.HasEvents = false;
        Width = 4;
        Height = 4;
        WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;

        var uiSpriteSheetTexture = Styling.ActiveStyle?.SpriteSheet;

        Background = new NineSliceRuntime();
        Background.X = 0;
        Background.Y = 0;
        Background.XUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle;
        Background.YUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle;
        Background.XOrigin = HorizontalAlignment.Center;
        Background.YOrigin = VerticalAlignment.Center;
        Background.Width = 0;
        Background.Height = 0;
        Background.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
        Background.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
        Background.Name = "Background";
        Background.Texture = uiSpriteSheetTexture;
        if (Styling.ActiveStyle != null)
        {
            Background.ApplyState(Styling.ActiveStyle.NineSlice.Bordered);
        }
        this.AddChild(Background);

        TextInstance = new TextRuntime();
        TextInstance.X = 4;
        TextInstance.Y = 2;
        TextInstance.Width = 0;
        TextInstance.Height = 0;
        TextInstance.Name = "TextInstance";
        TextInstance.Text = "";
        TextInstance.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        TextInstance.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        TextInstance.XOrigin = HorizontalAlignment.Left;
        TextInstance.YOrigin = VerticalAlignment.Top;
        TextInstance.XUnits = Gum.Converters.GeneralUnitType.PixelsFromSmall;
        TextInstance.YUnits = Gum.Converters.GeneralUnitType.PixelsFromSmall;
        if (Styling.ActiveStyle != null)
        {
            TextInstance.ApplyState(Styling.ActiveStyle.Text.Normal);
        }
        this.AddChild(TextInstance);

        TooltipCategory = new StateSaveCategory();
        TooltipCategory.Name = "TooltipCategory";
        this.AddCategory(TooltipCategory);

        if (Styling.ActiveStyle != null)
        {
            BackgroundColor = Styling.ActiveStyle.Colors.Primary;
            ForegroundColor = Styling.ActiveStyle.Colors.TextPrimary;
        }

        if (tryCreateFormsObject)
        {
            FormsControlAsObject = new Tooltip(this);
        }
    }

    /// <summary>
    /// Returns the strongly-typed <see cref="Tooltip"/> Forms control backing this visual.
    /// </summary>
    public Tooltip FormsControl => (Tooltip)FormsControlAsObject;
}
