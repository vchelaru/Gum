using Gum.Wireframe;
using RenderingLibrary;
using System;

#if RAYLIB
using Gum.Graphics.Animation;
using Gum.Renderables;
using RaylibGum.Helpers;
using Color = Raylib_cs.Color;
using Texture2D = Raylib_cs.Texture2D;
using ContainedNineSliceType = Gum.Renderables.NineSlice;
#elif SOKOL
using Gum.Graphics.Animation;
using Gum.Renderables;
using SokolGum.Helpers;
using Color = SokolGum.Color;
using Texture2D = SokolGum.Texture2D;
using ContainedNineSliceType = Gum.Renderables.NineSlice;
#elif SKIA
using Gum.Graphics.Animation;
using SkiaGum.Renderables;
using SkiaGum.Helpers;
using Color = SkiaSharp.SKColor;
using Texture2D = SkiaSharp.SKBitmap;
using ContainedNineSliceType = SkiaGum.Renderables.NineSlice;
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
    // Bucket 1 (#2908) decision: these per-instance defaults are an XNA-only legacy
    // surface with no backend-neutral equivalent (texture-coordinate model). They stay
    // a permanent #if XNALIKE gate rather than being unified or dropped — dropping the
    // public statics would be an API break for existing MonoGame consumers.
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

    ContainedNineSliceType _containedNineSlice;

    ContainedNineSliceType ContainedNineSlice
    {
        get
        {
            if (_containedNineSlice == null)
            {
                _containedNineSlice = (ContainedNineSliceType)this.RenderableComponent;
            }
            return _containedNineSlice;
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
    // Bucket 1 (#2908) decision: the XNA-typed BlendState surface stays a permanent
    // #if XNALIKE gate. The backend-neutral Blend? property below is the unified API;
    // exposing the Microsoft.Xna.Framework BlendState type cannot be done on Raylib/Skia.
    // Mirrors SpriteRuntime, which made the same call.
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
#endif

    /// <summary>
    /// The Gum-specific Blend mode for the nine-slice. Null means "use the renderer's current
    /// blend mode" (typically alpha blending).
    /// </summary>
    public Gum.RenderingLibrary.Blend? Blend
    {
        get
        {
#if XNALIKE
            return Gum.RenderingLibrary.BlendExtensions.ToBlend(ContainedNineSlice.BlendState);
#else
            return ContainedNineSlice.Blend;
#endif
        }
        set
        {
#if XNALIKE
            if (value.HasValue)
            {
                BlendState = value.Value.ToBlendState().ToXNA();
            }
            // NotifyPropertyChanged handled by BlendState:
#else
            ContainedNineSlice.Blend = value;
            NotifyPropertyChanged();
#endif
        }
    }

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
        get => ContainedNineSlice.Color.ToUserColor();
        set
        {
            ContainedNineSlice.Color = value.ToContainerColor();
            NotifyPropertyChanged();
        }
    }

    #endregion

    #region Animation

    /// <summary>
    /// Whether the nine-slice should actively advance its animation chain.
    /// </summary>
    public bool Animate
    {
        get => ContainedNineSlice.AnimationLogic.Animate;
        set
        {
            ContainedNineSlice.AnimationLogic.Animate = value;
            NotifyPropertyChanged();
        }
    }

    /// <summary>
    /// The name of the currently active animation chain.
    /// </summary>
    public string CurrentChainName
    {
        get => ContainedNineSlice.AnimationLogic.CurrentChainName;
        set
        {
            ContainedNineSlice.AnimationLogic.CurrentChainName = value;
            NotifyPropertyChanged();
        }
    }

    /// <summary>
    /// The list of animation chains available to this nine-slice.
    /// </summary>
    public AnimationChainList AnimationChains
    {
        get => ContainedNineSlice.AnimationLogic.AnimationChains;
        set
        {
            ContainedNineSlice.AnimationLogic.AnimationChains = value;
            if (ContainedNineSlice.AnimationLogic.UpdateToCurrentAnimationFrame())
            {
                UpdateTextureValuesFrom(ContainedNineSlice);
            }
            NotifyPropertyChanged();
        }
    }

    /// <summary>
    /// The speed multiplier for animation playback (1.0 is normal speed).
    /// </summary>
    public float AnimationSpeed
    {
        get => ContainedNineSlice.AnimationLogic.AnimationSpeed;
        set
        {
            ContainedNineSlice.AnimationLogic.AnimationSpeed = value;
            NotifyPropertyChanged();
        }
    }

    #endregion

    #region Source File / Texture

    // Bucket 1 (#2908) decision: this obsolete shim stays XNALIKE-only and is intentionally
    // NOT widened to Raylib/Skia. Obsolete APIs are deprecated paths we want consumers off of;
    // adding them to backends that never had them spreads a dead surface to new code. Replace
    // usage with Texture / SourceFileName instead.
#if XNALIKE
    [Obsolete("Use Texture to set Texture2D, or use SourceFileName to set the file")]
    public Texture2D? SourceFile
    {
        get => Texture;
        set => Texture = value;
    }
#endif

    // Bucket 4 (#2908) decision: the XNA NineSlice renderable keeps nine separate Sprites,
    // each able to hold its OWN texture (the old "nine separate image files" model — the
    // _tl/_t/_tr/... suffixed files loaded via SetTexturesUsingPattern). That per-slice
    // texture model is a legacy approach we no longer support and was never carried to the
    // single-texture Raylib/Skia/Sokol renderables. We are NOT teaching those backends about
    // per-slice textures, nor adding a single-texture facade to the XNA renderable, so this
    // stays a permanent #if XNALIKE gate: on XNA we read the shared texture back through
    // TopLeftTexture and broadcast writes to all nine slices via SetSingleTexture, while every
    // other backend has a single Texture property. The branch is the model mismatch, not drift.
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
            if (ContainedNineSlice.AnimationLogic.UpdateToCurrentAnimationFrame())
            {
                UpdateTextureValuesFrom(ContainedNineSlice);
            }
        }
    }

    #endregion

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
            NotifyPropertyChanged();
        }
    }

#if XNALIKE || SKIA || RAYLIB
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
            _containedNineSlice = new ContainedNineSliceType();
            SetContainedObject(_containedNineSlice);

            // All backends default to 100x100 (#2908). Previously only the non-XNALIKE
            // branch set this, leaving XNALIKE NineSlices at the GraphicalUiElement
            // base default (32x32) and diverging from Raylib/Skia.
            Width = 100;
            Height = 100;

#if XNALIKE
            // Per-instance texture-coordinate defaults are an XNA-only legacy feature
            // (see the Static Defaults region) with no backend-neutral equivalent.
            this.TextureLeft = DefaultTextureLeft;
            this.TextureTop = DefaultTextureTop;
            this.TextureWidth = DefaultTextureWidth;
            this.TextureHeight = DefaultTextureHeight;

            this.TextureAddress = DefaultTextureAddress;
#endif
        }
    }
}
