#if MONOGAME || FNA || KNI
#define XNALIKE
#endif

using Gum.Wireframe;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using System;

#if RAYLIB
using Gum.Renderables;
using Color = Raylib_cs.Color;
using ContainedRectangleType = Gum.Renderables.SolidRectangle;
namespace Gum.GueDeriving;
#elif SOKOL
using Gum.Renderables;
using Color = SokolGum.Color;
using ContainedRectangleType = Gum.Renderables.SolidRectangle;
namespace Gum.GueDeriving;
#elif SKIA
using SkiaGum.Renderables;
using Color = SkiaSharp.SKColor;
using ContainedRectangleType = SkiaGum.Renderables.RoundedRectangle;
namespace SkiaGum.GueDeriving;
#else
using Gum.RenderingLibrary;
using Color = Microsoft.Xna.Framework.Color;
using ContainedRectangleType = RenderingLibrary.Graphics.SolidRectangle;
namespace MonoGameGum.GueDeriving;
#endif


public class ColoredRectangleRuntime : GraphicalUiElement
{
    public static float DefaultWidth = 50;
    public static float DefaultHeight = 50;

    ContainedRectangleType mContainedColoredRectangle;
    ContainedRectangleType ContainedColoredRectangle
    {
        get
        {
            if (mContainedColoredRectangle == null)
            {
                mContainedColoredRectangle = this.RenderableComponent as ContainedRectangleType;
            }
            return mContainedColoredRectangle;
        }
    }

    public int Alpha
    {
#if RAYLIB
        get => ContainedColoredRectangle.Color.A;
        set
        {
            var color = ContainedColoredRectangle.Color;
            color.A = (byte)value;
            ContainedColoredRectangle.Color = color;
            NotifyPropertyChanged();
        }
#else
        get => ContainedColoredRectangle.Alpha;
        set
        {
            ContainedColoredRectangle.Alpha = value;
            NotifyPropertyChanged();
        }
#endif
    }

#if XNALIKE
    public Microsoft.Xna.Framework.Graphics.BlendState BlendState
    {
        get => ContainedColoredRectangle.BlendState.ToXNA();
        set
        {
            ContainedColoredRectangle.BlendState = value.ToGum();
            NotifyPropertyChanged();
            NotifyPropertyChanged(nameof(Blend));
        }
    }

    public Gum.RenderingLibrary.Blend? Blend
    {
        get
        {
            return Gum.RenderingLibrary.BlendExtensions.ToBlend(ContainedColoredRectangle.BlendState);
        }
        set
        {
            if (value.HasValue)
            {
                BlendState = value.Value.ToBlendState().ToXNA();
            }
            // NotifyPropertyChanged handled by BlendState:
        }
    }
#endif

    public int Red
    {
        get => ContainedColoredRectangle.Red;
        set
        {
            ContainedColoredRectangle.Red = value;
            NotifyPropertyChanged();
        }
    }

    public int Green
    {
        get => ContainedColoredRectangle.Green;
        set
        {
            ContainedColoredRectangle.Green = value;
            NotifyPropertyChanged();
        }
    }

    public int Blue
    {
        get => ContainedColoredRectangle.Blue;
        set
        {
            ContainedColoredRectangle.Blue = value;
            NotifyPropertyChanged();
        }
    }

    public Color Color
    {
#if XNALIKE
        get => RenderingLibrary.Graphics.XNAExtensions.ToXNA(ContainedColoredRectangle.Color);
        set
        {
            ContainedColoredRectangle.Color = RenderingLibrary.Graphics.XNAExtensions.ToSystemDrawing(value);
            NotifyPropertyChanged();
        }
#else
        get => ContainedColoredRectangle.Color;
        set
        {
            ContainedColoredRectangle.Color = value;
            NotifyPropertyChanged();
        }
#endif
    }

#if !SOKOL
    /// <inheritdoc cref="GraphicalUiElement.AddToManagers()"/>
    [Obsolete("Use the AddToRoot extension method instead (e.g. myColoredRectangle.AddToRoot()).")]
    public void AddToManagers() => base.AddToManagers(SystemManagers.Default, layer: null);
#endif

    public ColoredRectangleRuntime(bool fullInstantiation = true)
    {
        if (fullInstantiation)
        {
            var rectangle = new ContainedRectangleType();
            SetContainedObject(rectangle);
            mContainedColoredRectangle = rectangle;

#if SKIA
            rectangle.CornerRadius = 0;
            rectangle.Color = SkiaSharp.SKColors.White;
#elif RAYLIB
            rectangle.Color = Raylib_cs.Color.White;
#elif SOKOL
            rectangle.Color = SokolGum.Color.White;
#else
            rectangle.Color = System.Drawing.Color.White;
#endif
            Width = DefaultWidth;
            Height = DefaultHeight;
        }
    }

    public override GraphicalUiElement Clone()
    {
        var toReturn = (ColoredRectangleRuntime)base.Clone();

        toReturn.mContainedColoredRectangle = null;

        return toReturn;
    }
}
