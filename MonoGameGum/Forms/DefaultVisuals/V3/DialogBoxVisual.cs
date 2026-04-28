using Gum.DataTypes.Variables;
using Gum.Forms.Controls.Games;
using Gum.Wireframe;
using RenderingLibrary;
using RenderingLibrary.Graphics;

#if XNALIKE
using Microsoft.Xna.Framework;
using MonoGameGum.GueDeriving;
#else
using Gum.GueDeriving;
#endif

namespace Gum.Forms.DefaultVisuals.V3;

/// <summary>
/// Default V3 visual for a DialogBox control. Contains a bordered background,
/// padded text area for typewriter dialog text, and a continue indicator anchored
/// bottom-right that the DialogBox shows once the current page has finished typing.
/// </summary>
public class DialogBoxVisual : InteractiveGue
{
    public NineSliceRuntime Background { get; private set; }
    public TextRuntime TextInstance { get; private set; }
    public NineSliceRuntime ContinueIndicatorInstance { get; private set; }

    Color _backgroundColor;
    public Color BackgroundColor
    {
        get => _backgroundColor;
        set
        {
            if (!value.Equals(_backgroundColor))
            {
                _backgroundColor = value;
                Background.Color = _backgroundColor;
            }
        }
    }

    Color _foregroundColor;
    public Color ForegroundColor
    {
        get => _foregroundColor;
        set
        {
            if (!value.Equals(_foregroundColor))
            {
                _foregroundColor = value;
                TextInstance.Color = _foregroundColor;
                ContinueIndicatorInstance.Color = _foregroundColor;
            }
        }
    }

    public DialogBoxVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true)
        : base(new InvisibleRenderable())
    {
        this.HasEvents = true;

        Width = 600;
        Height = 140;

        var spriteSheet = Styling.ActiveStyle.SpriteSheet;

        Background = new NineSliceRuntime();
        Background.Name = "Background";
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
        Background.Texture = spriteSheet;
        Background.ApplyState(Styling.ActiveStyle.NineSlice.Bordered);
        this.AddChild(Background);

        // Padded text area. Fixed-relative size so MaxLettersToShow truncation
        // and DialogBox pagination both behave predictably (DialogBox.ConvertToPages
        // skips pagination when the text component is RelativeToChildren on height).
        TextInstance = new TextRuntime();
        TextInstance.Name = "TextInstance";
        TextInstance.X = 12;
        TextInstance.Y = 12;
        TextInstance.XUnits = Gum.Converters.GeneralUnitType.PixelsFromSmall;
        TextInstance.YUnits = Gum.Converters.GeneralUnitType.PixelsFromSmall;
        TextInstance.XOrigin = HorizontalAlignment.Left;
        TextInstance.YOrigin = VerticalAlignment.Top;
        TextInstance.Width = -24;
        TextInstance.Height = -32;
        TextInstance.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
        TextInstance.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
        TextInstance.HorizontalAlignment = HorizontalAlignment.Left;
        TextInstance.VerticalAlignment = VerticalAlignment.Top;
        // TruncateLine (rather than the default SpillOver) is required for
        // DialogBox.ConvertToPages to detect that this control limits its
        // visible line count and split long text into multiple pages.
        TextInstance.TextOverflowVerticalMode = TextOverflowVerticalMode.TruncateLine;
        TextInstance.Text = string.Empty;
        TextInstance.ApplyState(Styling.ActiveStyle.Text.Normal);
        this.AddChild(TextInstance);

        // Solid square placeholder. Game projects typically replace this with
        // an animated arrow sprite, but a small solid block renders reliably
        // without depending on glyph or sprite-sheet content.
        ContinueIndicatorInstance = new NineSliceRuntime();
        ContinueIndicatorInstance.Name = "ContinueIndicatorInstance";
        ContinueIndicatorInstance.X = -10;
        ContinueIndicatorInstance.Y = -10;
        ContinueIndicatorInstance.XUnits = Gum.Converters.GeneralUnitType.PixelsFromLarge;
        ContinueIndicatorInstance.YUnits = Gum.Converters.GeneralUnitType.PixelsFromLarge;
        ContinueIndicatorInstance.XOrigin = HorizontalAlignment.Right;
        ContinueIndicatorInstance.YOrigin = VerticalAlignment.Bottom;
        ContinueIndicatorInstance.Width = 10;
        ContinueIndicatorInstance.Height = 10;
        ContinueIndicatorInstance.WidthUnits = Gum.DataTypes.DimensionUnitType.Absolute;
        ContinueIndicatorInstance.HeightUnits = Gum.DataTypes.DimensionUnitType.Absolute;
        ContinueIndicatorInstance.Texture = spriteSheet;
        ContinueIndicatorInstance.ApplyState(Styling.ActiveStyle.NineSlice.Solid);
        ContinueIndicatorInstance.Visible = false;
        this.AddChild(ContinueIndicatorInstance);

        BackgroundColor = Styling.ActiveStyle.Colors.Primary;
        ForegroundColor = Styling.ActiveStyle.Colors.TextPrimary;

        if (tryCreateFormsObject)
        {
            FormsControlAsObject = new DialogBox(this);
        }
    }

    public DialogBox FormsControl => (DialogBox)FormsControlAsObject;
}
