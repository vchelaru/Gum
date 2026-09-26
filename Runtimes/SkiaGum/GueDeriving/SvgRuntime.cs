using Gum.DataTypes;
using Gum.Wireframe;
using SkiaGum;
using SkiaSharp;
using Svg.Skia;

#if FRB
namespace SkiaGum.GueDeriving;
#else
namespace Gum.GueDeriving;
#endif

public class SvgRuntime : InteractiveGue
{
    VectorSprite? mContainedSprite;
    VectorSprite ContainedSprite
    {
        get
        {
            mContainedSprite ??= this.RenderableComponent as VectorSprite
                ?? throw new System.InvalidOperationException(
                    $"{nameof(SvgRuntime)} has no {nameof(VectorSprite)} renderable. Construct it with fullInstantiation: true or call SetContainedObject first.");
            return mContainedSprite;
        }
    }

    string? sourceFile;
    public string? SourceFile
    {
        // eventually we may want to store this off somehow
        get => sourceFile;
        set
        {
            if (sourceFile != value)
            {
                sourceFile = value;
                if (string.IsNullOrEmpty(value))
                {
                    Texture = null;
                }
                else
                {
                    var loaderManager = global::RenderingLibrary.Content.LoaderManager.Self;
                    SKSvg? skiaSvg = loaderManager.LoadContent<SKSvg>(value);
                    Texture = skiaSvg;
                }
            }
        }
    }

    public SKSvg? Texture
    {
        get => ContainedSprite.Texture;
        set => ContainedSprite.Texture = value;
    }

    public int Alpha
    {
        get => ContainedSprite.Alpha;
        set => ContainedSprite.Alpha = value;
    }

    public int Red
    {
        get => ContainedSprite.Red;
        set => ContainedSprite.Red = value;
    }

    public int Green
    {
        get => ContainedSprite.Green;
        set => ContainedSprite.Green = value;
    }

    public int Blue
    {
        get => ContainedSprite.Blue;
        set => ContainedSprite.Blue = value;
    }

    public SKColor Color
    {
        get => ContainedSprite.Color;
        set => ContainedSprite.Color = value;
    }

    public SvgRuntime(bool fullInstantiation = true)
    {
        if (fullInstantiation)
        {
            //SetGraphicalUiElement
            SetContainedObject(new VectorSprite());

            // Give it some good defaults:
            WidthUnits = DimensionUnitType.Absolute;
            HeightUnits = DimensionUnitType.MaintainFileAspectRatio;

            Width = 100;
            Height = 100;
        }
    }

    public override GraphicalUiElement Clone()
    {
        var toReturn = (SvgRuntime)base.Clone();

        toReturn.mContainedSprite = null;

        return toReturn;
    }
}
