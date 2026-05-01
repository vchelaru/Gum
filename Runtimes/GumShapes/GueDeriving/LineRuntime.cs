using MonoGameAndGum.Renderables;

namespace MonoGameGum.GueDeriving;

/// <summary>
/// Runtime for a line shape that draws from the top-left to the bottom-right of its bounding rectangle.
/// Uses Apos.Shapes for rendering.
/// </summary>
public class LineRuntime : AposShapeRuntime
{
    protected override RenderableShapeBase ContainedRenderable => ContainedLine;

    Line _containedLine = default!;

    Line ContainedLine
    {
        get
        {
            if (_containedLine == null)
            {
                _containedLine = (Line)this.RenderableComponent;
            }
            return _containedLine;
        }
    }

    /// <summary>
    /// Whether the line endpoints are rounded. If false, endpoints are flat.
    /// </summary>
    public bool IsRounded
    {
        get => ContainedLine.IsRounded;
        set => ContainedLine.IsRounded = value;
    }

    public LineRuntime(bool fullInstantiation = true)
    {
        if (fullInstantiation)
        {
            SetContainedShape(new Line());
            Color = Microsoft.Xna.Framework.Color.White;
            Width = 100;
            Height = 0;
            StrokeWidth = 2;
            StrokeWidthUnits = Gum.DataTypes.DimensionUnitType.ScreenPixel;

            DropshadowAlpha = 255;
            DropshadowRed = 0;
            DropshadowGreen = 0;
            DropshadowBlue = 0;

            DropshadowOffsetX = 0;
            DropshadowOffsetY = 3;
            DropshadowBlurX = 0;
            DropshadowBlurY = 3;
        }
    }
}
