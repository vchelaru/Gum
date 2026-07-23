using Gum.Graphics.Animation;
using Gum.RenderingLibrary;
using Gum.Wireframe;
using RenderingLibrary;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#if RAYLIB
using Gum.Renderables;
using RenderingLibrary.Graphics;
using Color = Raylib_cs.Color;
using Rectangle = Raylib_cs.Rectangle;
using Texture2D = Raylib_cs.Texture2D;
using ContainedSpriteType = Gum.Renderables.Sprite;
#elif SKIA
using SkiaGum.Renderables;
using Color = SkiaSharp.SKColor;
using Rectangle = SkiaSharp.SKRect;
using Texture2D = SkiaSharp.SKBitmap;
using ContainedSpriteType = SkiaGum.Renderables.Sprite;
#else
using RenderingLibrary.Graphics;
using Color = Microsoft.Xna.Framework.Color;
using Rectangle = Microsoft.Xna.Framework.Rectangle;
using Texture2D = Microsoft.Xna.Framework.Graphics.Texture2D;
using ContainedSpriteType = global::RenderingLibrary.Graphics.Sprite;
#endif

#if FRB
namespace MonoGameGum.GueDeriving;
#else
namespace Gum.GueDeriving;
#endif

/// <summary>
/// A GraphicalUiElement that specifically wraps a Sprite renderable.
/// Provides a unified API for common Sprite properties across different rendering platforms.
/// </summary>
public class SpriteRuntime : GraphicalUiElement
#if XNALIKE
    , IRenderTargetTextureReferencer
#endif
{
    #region Contained Sprite
    ContainedSpriteType _containedSprite;
    ContainedSpriteType ContainedSprite
    {
        get
        {
            if (_containedSprite == null)
            {
                _containedSprite = (ContainedSpriteType)this.RenderableComponent;
            }
            return _containedSprite;
        }
    }

    #endregion

    #region Color/Blend

    /// <summary>
    /// The alpha (transparency) of the sprite, from 0 (invisible) to 255 (opaque).
    /// </summary>
    public int Alpha
    {
        get => ContainedSprite.Alpha;
        set
        {
            ContainedSprite.Alpha = value;
            NotifyPropertyChanged();
        }
    }

    /// <summary>
    /// The red component of the sprite's color, from 0 to 255.
    /// </summary>
    public int Red
    {
        get => ContainedSprite.Red;
        set
        {
            ContainedSprite.Red = value;
            NotifyPropertyChanged();
        }
    }

    /// <summary>
    /// The green component of the sprite's color, from 0 to 255.
    /// </summary>
    public int Green
    {
        get => ContainedSprite.Green;
        set
        {
            ContainedSprite.Green = value;
            NotifyPropertyChanged();
        }
    }

    /// <summary>
    /// The blue component of the sprite's color, from 0 to 255.
    /// </summary>
    public int Blue
    {
        get => ContainedSprite.Blue;
        set
        {
            ContainedSprite.Blue = value;
            NotifyPropertyChanged();
        }
    }

    /// <summary>
    /// The color of the sprite, mapping to the platform-specific color type.
    /// </summary>
    public Color Color
    {
        get
        {
#if RAYLIB || SKIA
            return ContainedSprite.Color;
#else
            return global::RenderingLibrary.Graphics.XNAExtensions.ToXNA(ContainedSprite.Color);
#endif
        }
        set
        {
#if RAYLIB || SKIA
            ContainedSprite.Color = value;
#else
            ContainedSprite.Color = global::RenderingLibrary.Graphics.XNAExtensions.ToSystemDrawing(value);
#endif
            NotifyPropertyChanged();
        }
    }

#if XNALIKE
    /// <summary>
    /// The XNA/MonoGame BlendState used for rendering the sprite.
    /// </summary>
    public Microsoft.Xna.Framework.Graphics.BlendState BlendState
    {
        get => ContainedSprite.BlendState.ToXNA();
        set
        {
            ContainedSprite.BlendState = value.ToGum();
            NotifyPropertyChanged();
            NotifyPropertyChanged(nameof(Blend));
        }
    }
#endif

    /// <summary>
    /// The Gum-specific Blend mode for the sprite. Null means "use the renderer's current
    /// blend mode" (typically alpha blending).
    /// </summary>
    public Gum.RenderingLibrary.Blend? Blend
    {
        get
        {
#if XNALIKE
            return Gum.RenderingLibrary.BlendExtensions.ToBlend(ContainedSprite.BlendState);
#else
            return ContainedSprite.Blend;
#endif
        }
        set
        {
#if XNALIKE
            if (value.HasValue)
            {
                BlendState = value.Value.ToBlendState().ToXNA();
            }
#else
            ContainedSprite.Blend = value;
            NotifyPropertyChanged();
#endif
        }
    }

    #endregion

    /// <summary>
    /// Whether the sprite should be flipped vertically during rendering.
    /// </summary>
    public bool FlipVertical
    {
        get => ContainedSprite.FlipVertical;
        set
        {
            ContainedSprite.FlipVertical = value;
            NotifyPropertyChanged();
        }
    }

#if !SKIA
    /// <summary>
    /// The source rectangle in the texture to render, using platform-specific Rectangle type.
    /// Setting this also updates TextureLeft, TextureTop, TextureWidth, and TextureHeight.
    /// </summary>
    public Rectangle SourceRectangle
    {
        get => new Rectangle(
#if RAYLIB
            TextureLeft, TextureTop, TextureWidth, TextureHeight
#else
            (int)TextureLeft, (int)TextureTop, (int)TextureWidth, (int)TextureHeight
#endif
            );
        set
        {
            TextureLeft = (int)value.X;
            TextureTop = (int)value.Y;
            TextureWidth = (int)value.Width;
            TextureHeight = (int)value.Height;
        }
    }
#endif

    #region AnimationChain

    /// <summary>
    /// Whether the sprite should actively advance its animation chain.
    /// </summary>
    public bool Animate
    {
        get => ContainedSprite.AnimationLogic.Animate;
        set => ContainedSprite.AnimationLogic.Animate = value;
    }

    /// <summary>
    /// The name of the currently active animation chain.
    /// </summary>
    public string? CurrentChainName
    {
        get => ContainedSprite.AnimationLogic.CurrentChainName;
        set => ContainedSprite.AnimationLogic.CurrentChainName = value;
    }

    /// <summary>
    /// The list of animation chains available to this sprite.
    /// </summary>
    public AnimationChainList? AnimationChains
    {
        get => ContainedSprite.AnimationLogic.AnimationChains;
        set
        {
            ContainedSprite.AnimationLogic.AnimationChains = value;
            if (ContainedSprite.AnimationLogic.UpdateToCurrentAnimationFrame())
            {
                UpdateTextureValuesFrom(ContainedSprite);
            }
        }
    }

    /// <summary>
    /// The index of the current frame in the active animation chain.
    /// </summary>
    public int AnimationChainFrameIndex
    {
        get => ContainedSprite.AnimationLogic.CurrentFrameIndex;
        set => ContainedSprite.AnimationLogic.CurrentFrameIndex = value;
    }

    /// <summary>
    /// The current playback time (in seconds) within the active animation chain.
    /// </summary>
    public double AnimationChainTime
    {
        get => ContainedSprite.AnimationLogic.TimeIntoAnimation;
        set => ContainedSprite.AnimationLogic.TimeIntoAnimation = value;
    }

    /// <summary>
    /// The speed multiplier for animation playback (1.0 is normal speed).
    /// </summary>
    public float AnimationChainSpeed
    {
        get => ContainedSprite.AnimationLogic.AnimationSpeed;
        set => ContainedSprite.AnimationLogic.AnimationSpeed = value;
    }

    /// <summary>
    /// Whether the current animation chain should restart after finishing.
    /// </summary>
    public bool IsAnimationChainLooping
    {
        get => ContainedSprite.AnimationLogic.IsAnimationChainLooping;
        set => ContainedSprite.AnimationLogic.IsAnimationChainLooping = value;
    }

    /// <summary>
    /// Triggered when the current animation chain completes a full cycle.
    /// </summary>
    public event Action AnimationChainCycled
    {
        add => ContainedSprite.AnimationLogic.AnimationChainCycled += value;
        remove => ContainedSprite.AnimationLogic.AnimationChainCycled -= value;
    }

    #endregion

    #region Source File/Texture

    // The #if gate on this obsolete Texture2D shim reflects its historical footprint and is
    // intentionally not widened. Obsolete APIs are deprecated paths; spreading them to backends
    // that never had them just plants a dead surface in new code. (Skia keeps a separate string
    // SourceFile below — that one is the live API there, not the obsolete shim.) See NineSliceRuntime.
#if !SKIA
    /// <summary>
    /// Obsolete. Use Texture instead.
    /// </summary>
    [Obsolete("Use Texture")]
    public Texture2D? SourceFile
    {
        get => this.Texture;
        set => this.Texture = value;
    }
#else
    /// <summary>
    /// The file path to the texture. Setting this will load the texture via the LoaderManager.
    /// </summary>
    public string? SourceFile
    {
        // eventually we may want to store this off somehow
        get => null;
        set
        {
            if (string.IsNullOrEmpty(value))
            {
                Texture = null;
            }
            else
            {
                var loaderManager = global::RenderingLibrary.Content.LoaderManager.Self;
                var contentLoader = loaderManager.ContentLoader;
                var image = contentLoader.LoadContent<SkiaSharp.SKBitmap>(value);
                Texture = image;
            }
        }
    }
#endif

    /// <summary>
    /// The underlying texture used by the sprite.
    /// </summary>
    public Texture2D? Texture
    {
        get => ContainedSprite.Texture;
        set
        {
            var isUsingPercentage = WidthUnits == Gum.DataTypes.DimensionUnitType.PercentageOfSourceFile || 
                                    HeightUnits == Gum.DataTypes.DimensionUnitType.PercentageOfSourceFile;

            int widthBefore = -1, heightBefore = -1;

            if (isUsingPercentage && ContainedSprite.Texture != null)
            {
#if RAYLIB
                widthBefore = ContainedSprite.Texture.Value.Width;
                heightBefore = ContainedSprite.Texture.Value.Height;
#else
                widthBefore = ContainedSprite.Texture.Width;
                heightBefore = ContainedSprite.Texture.Height;
#endif
            }

            ContainedSprite.Texture = value;

            if (isUsingPercentage)
            {
                int widthAfter = -1, heightAfter = -1;
                if (value != null)
                {
#if RAYLIB
                    widthAfter = value.Value.Width;
                    heightAfter = value.Value.Height;
#else
                    widthAfter = value.Width;
                    heightAfter = value.Height;
#endif
                }

                if (widthBefore != widthAfter || heightBefore != heightAfter)
                {
                    UpdateLayout();
                }
            }

#if RAYLIB || SKIA
            NotifyPropertyChanged();
#endif
        }
    }

#if SKIA
    /// <summary>
    /// The underlying SKImage used by the sprite.
    /// </summary>
    public SkiaSharp.SKImage? Image
    {
        get => ContainedSprite.Image;
        set => ContainedSprite.Image = value;
    }
#endif

    /// <summary>
    /// Sets the texture via file name and updates animation frames if necessary.
    /// </summary>
    public string SourceFileName
    {
        set
        {
            base.SetProperty("SourceFile", value);
            if (ContainedSprite.AnimationLogic.UpdateToCurrentAnimationFrame())
            {
                UpdateTextureValuesFrom(ContainedSprite);
            }
        }
    }

#if XNALIKE || RAYLIB
    /// <summary>
    /// The source render-target container whose baked texture this sprite displays, in place of
    /// a directly-assigned <see cref="Texture"/>.
    /// </summary>
    public IRenderableIpso? RenderTargetTextureSource
    {
        get => ContainedSprite.RenderTargetTextureSource;
        set
        {
            if (ContainedSprite.RenderTargetTextureSource != value)
            {
                ContainedSprite.RenderTargetTextureSource = value;
                UpdateLayout();
            }
        }
    }
#endif

#if XNALIKE
    // The XNA Renderer's render-target detection walk (CollectReferencedRenderTargets) tests for
    // IRenderTargetTextureReferencer, so a nested SpriteRuntime must implement it. Raylib's Renderer
    // resolves the source via concrete Gum.Renderables.Sprite type checks instead, so it doesn't need
    // this interface. Both backends pull the baked target via Renderer.TryGetBakedRenderTargetFor.
    IRenderableIpso? IRenderTargetTextureReferencer.RenderTargetTextureSource =>
        ContainedSprite.RenderTargetTextureSource;
#endif


    #endregion

    /// <summary>
    /// Creates a new SpriteRuntime instance.
    /// </summary>
    /// <param name="fullInstantiation">Whether to create the underlying renderable.</param>
    /// <param name="tryCreateFormsObject">Whether to attempt creating a corresponding Forms object.</param>
    public SpriteRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true)
    {
        if (fullInstantiation)
        {
#if RAYLIB || SKIA
            _containedSprite = new ContainedSpriteType();
#else
            _containedSprite = new global::RenderingLibrary.Graphics.Sprite(null);
#endif
            SetContainedObject(_containedSprite);
            Width = 100;
            Height = 100;
            WidthUnits = Gum.DataTypes.DimensionUnitType.PercentageOfSourceFile;
            HeightUnits = Gum.DataTypes.DimensionUnitType.PercentageOfSourceFile;
        }
    }

    /// <inheritdoc/>
    public override GraphicalUiElement Clone()
    {
        var toReturn = (SpriteRuntime)base.Clone();
        toReturn._containedSprite = null;
        return toReturn;
    }

    /// <inheritdoc cref="GraphicalUiElement.AddToManagers()"/>
    [Obsolete("Use the AddToRoot extension method instead (e.g. mySprite.AddToRoot()).")]
    public new void AddToManagers() => base.AddToManagers(SystemManagers.Default, layer: null);

}
