using Gum.Wireframe;
using SkiaGum.Renderables;
using SkiaSharp;
using System;

#if FRB
namespace SkiaGum.GueDeriving;
#else
namespace Gum.GueDeriving;
#endif

[Obsolete("Use RectangleRuntime with FillColor instead. SolidRectangleRuntime will be removed in a future release. See docs/gum-tool/upgrading/migrating-to-2026-may.md for the full migration guide.")]
public class SolidRectangleRuntime : InteractiveGue
{
    SolidRectangle? mContainedRectangle;
    SolidRectangle ContainedRectangle
    {
        get
        {
            mContainedRectangle ??= this.RenderableComponent as SolidRectangle
                ?? throw new System.InvalidOperationException(
                    $"{nameof(SolidRectangleRuntime)} has no SolidRectangle renderable. Construct it with fullInstantiation: true or call SetContainedObject first.");
            return mContainedRectangle;
        }
    }

    public SKColor Color
    {
        get => ContainedRectangle.Color;
        set => ContainedRectangle.Color = value;
    }

    public SolidRectangleRuntime(bool fullInstantiation = true)
    {
        if(fullInstantiation)
        {
            SetContainedObject(new SolidRectangle());
        }
    }

    public override GraphicalUiElement Clone()
    {
        var toReturn = (SolidRectangleRuntime)base.Clone();

        toReturn.mContainedRectangle = null;

        return toReturn;
    }
}
