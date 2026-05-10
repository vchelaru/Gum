#if MONOGAME || FNA || KNI
#define XNALIKE
#endif

using Gum.Wireframe;
using RenderingLibrary;
using System;

#if RAYLIB
using Gum.Renderables;
using Color = Raylib_cs.Color;
using Texture2D = Raylib_cs.Texture2D;
using ContainedNineSliceType = Gum.Renderables.NineSlice;
#elif SOKOL
using Gum.Graphics.Animation;
using Gum.Renderables;
using Color = SokolGum.Color;
using Texture2D = SokolGum.Texture2D;
using ContainedNineSliceType = Gum.Renderables.NineSlice;
#else
using Gum.Graphics.Animation;
using Gum.Managers;
using Gum.RenderingLibrary;
using RenderingLibrary.Graphics;
using Color = Microsoft.Xna.Framework.Color;
using Texture2D = Microsoft.Xna.Framework.Graphics.Texture2D;
using ContainedNineSliceType = global::RenderingLibrary.Graphics.NineSlice;
#endif

#if FRB
namespace MonoGameGum.GueDeriving;
#else
namespace Gum.GueDeriving;
#endif

// NineSliceRuntime intentionally inherits InteractiveGue (not GraphicalUiElement),
// even though a nine-slice is conceptually decorative. In practice users commonly
// build forms by stacking controls inside a NineSliceRuntime that acts as the
// container/window chrome. We slightly discourage that pattern, but it is common
// enough that demoting the base class would break existing projects — so we will
// not demote it. Click-through is not broken in practice because HasEvents
// defaults to false; events are only absorbed when the user opts in.
public class NineSliceRuntime : InteractiveGue
{
#if XNALIKE
    #region Static Defaults

    [Obsolete("This is not currently functional")]
    public static string DefaultSourceFile { get; set; }
    public static int DefaultTextureLeft;
    public static int DefaultTextureTop;
    public static int DefaultTextureWidth;
    public static int DefaultTextureHeight;
    public static TextureAddress DefaultTextureAddress;

    #endregion
#endif

    #region Contained Nineslice

    ContainedNineSliceType mContainedNineSlice;

    ContainedNineSliceType ContainedNineSlice
    {
        get
        {
            if (mContainedNineSlice == null)
            {
                mContainedNineSlice = (ContainedNineSliceType)this.RenderableComponent;
            }
            return mContainedNineSlice;
        }
    }

    #endregion

    #region Color/Blend

    public int Alpha
    {
        get => ContainedNineSlice.Alpha;
        set
        {
            ContainedNineSlice.Alpha = value;
            NotifyPropertyChanged();
        }
    }

#if XNALIKE
    public Microsoft.Xna.Framework.Graphics.BlendState BlendState
    {
        get => ContainedNineSlice.BlendState.ToXNA();
        set
        {
            ContainedNineSlice.BlendState = value.ToGum();
            NotifyPropertyChanged();
            NotifyPropertyChanged(nameof(Blend));
        }
    }

    public Gum.RenderingLibrary.Blend? Blend
    {
        get
        {
            return Gum.RenderingLibrary.BlendExtensions.ToBlend(ContainedNineSlice.BlendState);
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

    public int Blue
    {
        get => ContainedNineSlice.Blue;
        set
        {
            ContainedNineSlice.Blue = value;
            NotifyPropertyChanged();
        }
    }

    public int Green
    {
        get => ContainedNineSlice.Green;
        set
        {
            ContainedNineSlice.Green = value;
            NotifyPropertyChanged();
        }
    }

    public int Red
    {
        get => ContainedNineSlice.Red;
        set
        {
            ContainedNineSlice.Red = value;
            NotifyPropertyChanged();
        }
    }

    public Color Color
    {
#if XNALIKE
        get => global::RenderingLibrary.Graphics.XNAExtensions.ToXNA(ContainedNineSlice.Color);
        set
        {
            ContainedNineSlice.Color = global::RenderingLibrary.Graphics.XNAExtensions.ToSystemDrawing(value);
            NotifyPropertyChanged();
        }
#else
        get => ContainedNineSlice.Color;
        set
        {
            ContainedNineSlice.Color = value;
            NotifyPropertyChanged();
        }
#endif
    }

    #endregion

#if XNALIKE || SOKOL
    #region Animation

    public bool Animate
    {
        get => ContainedNineSlice.Animate;
        set
        {
            ContainedNineSlice.Animate = value;
#if SOKOL
            NotifyPropertyChanged();
#endif
        }
    }

    public string CurrentChainName
    {
        get => ContainedNineSlice.CurrentChainName;
        set
        {
            ContainedNineSlice.CurrentChainName = value;
#if SOKOL
            NotifyPropertyChanged();
#endif
        }
    }

    public AnimationChainList AnimationChains
    {
        get => ContainedNineSlice.AnimationChains;
        set
        {
            ContainedNineSlice.AnimationChains = value;
#if XNALIKE
            if (ContainedNineSlice.UpdateToCurrentAnimationFrame())
            {
                UpdateTextureValuesFrom(ContainedNineSlice);
            }
#else
            NotifyPropertyChanged();
#endif
        }
    }

#if SOKOL
    public float AnimationSpeed
    {
        get => ContainedNineSlice.AnimationSpeed;
        set
        {
            ContainedNineSlice.AnimationSpeed = value;
            NotifyPropertyChanged();
        }
    }
#endif

    #endregion
#endif

    #region Source File / Texture

#if XNALIKE
    [Obsolete("Use Texture to set Texture2D, or use SourceFileName to set the file")]
    public Texture2D? SourceFile
    {
        get => Texture;
        set => Texture = value;
    }
#endif

    public Texture2D? Texture
    {
        get
        {
#if XNALIKE
            return ContainedNineSlice.TopLeftTexture;
#else
            return ContainedNineSlice.Texture;
#endif
        }
        set
        {
#if XNALIKE
            ContainedNineSlice.SetSingleTexture(value);
#else
            ContainedNineSlice.Texture = value;
#endif
        }
    }

    public string SourceFileName
    {
        set
        {
            base.SetProperty("SourceFile", value);
#if XNALIKE
            if (ContainedNineSlice.UpdateToCurrentAnimationFrame())
            {
                UpdateTextureValuesFrom(ContainedNineSlice);
            }
            // todo - need to support .achx in raylib NineSlices
#endif
        }
    }

    #endregion

#if XNALIKE || SOKOL
    /// <summary>
    /// Sets the width or height of the nine slice edges in pixels. If null,
    /// the NineSlice uses 1/3 of the texture size. If set, this overrides the
    /// 1/3 width and uses the specified value.
    /// </summary>
    public float? CustomFrameTextureCoordinateWidth
    {
        get => ContainedNineSlice.CustomFrameTextureCoordinateWidth;
        set
        {
            ContainedNineSlice.CustomFrameTextureCoordinateWidth = value;
#if SOKOL
            NotifyPropertyChanged();
#endif
        }
    }
#endif

#if XNALIKE
    /// <summary>
    /// Whether to tile (repeat) the middle sections instead of stretching them.
    /// When true, the Top, Bottom, Left, Right, and Center sections are rendered
    /// as repeating tiles at their natural texture size scaled by BorderScale.
    /// </summary>
    public bool IsTilingMiddleSections
    {
        get => ContainedNineSlice.IsTilingMiddleSections;
        set => ContainedNineSlice.IsTilingMiddleSections = value;
    }

    public float BorderScale
    {
        get => ContainedNineSlice.BorderScale;
        set => ContainedNineSlice.BorderScale = value;
    }
#endif

    /// <inheritdoc cref="GraphicalUiElement.AddToManagers()"/>
    [Obsolete("Use the AddToRoot extension method instead (e.g. myNineSlice.AddToRoot()).")]
    public void AddToManagers() => base.AddToManagers(SystemManagers.Default, layer: null);


    public NineSliceRuntime(bool fullInstantiation = true)
    {
        if (fullInstantiation)
        {
            var contained = new ContainedNineSliceType();
            SetContainedObject(contained);

#if XNALIKE
            // todo - need to make this work with different relative directories...
            //this.SourceFileName = DefaultSourceFile;
            this.TextureLeft = DefaultTextureLeft;
            this.TextureTop = DefaultTextureTop;
            this.TextureWidth = DefaultTextureWidth;
            this.TextureHeight = DefaultTextureHeight;

            this.TextureAddress = DefaultTextureAddress;
#else
            Width = 100;
            Height = 100;
#endif
        }
    }
}
