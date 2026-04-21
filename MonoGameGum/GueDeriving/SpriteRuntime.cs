#if MONOGAME || FNA || KNI
#define XNALIKE
#endif

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
using Color = Raylib_cs.Color;
using Rectangle = Raylib_cs.Rectangle;
using Texture2D = Raylib_cs.Texture2D;
using ContainedSpriteType = Gum.Renderables.Sprite;
namespace Gum.GueDeriving;
#elif SKIA
using SkiaGum.Renderables;
using Color = SkiaSharp.SKColor;
using Rectangle = SkiaSharp.SKRect;
using Texture2D = SkiaSharp.SKBitmap;
using ContainedSpriteType = SkiaGum.Renderables.Sprite;
namespace SkiaGum.GueDeriving;
#else
using RenderingLibrary.Graphics;
using Color = Microsoft.Xna.Framework.Color;
using Rectangle = Microsoft.Xna.Framework.Rectangle;
using Texture2D = Microsoft.Xna.Framework.Graphics.Texture2D;
using ContainedSpriteType = RenderingLibrary.Graphics.Sprite;
namespace MonoGameGum.GueDeriving;
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
    ContainedSpriteType mContainedSprite;
    ContainedSpriteType ContainedSprite
    {
        get
        {
            if (mContainedSprite == null)
            {
                mContainedSprite = (ContainedSpriteType)this.RenderableComponent;
            }
            return mContainedSprite;
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
            return RenderingLibrary.Graphics.XNAExtensions.ToXNA(ContainedSprite.Color);
#endif
        }
        set
        {
#if RAYLIB || SKIA
            ContainedSprite.Color = value;
#else
            ContainedSprite.Color = RenderingLibrary.Graphics.XNAExtensions.ToSystemDrawing(value);
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

    /// <summary>
    /// The Gum-specific Blend mode for the sprite.
    /// </summary>
    public Gum.RenderingLibrary.Blend? Blend
    {
        get
        {
            return Gum.RenderingLibrary.BlendExtensions.ToBlend(ContainedSprite.BlendState);
        }
        set
        {
            if (value.HasValue)
            {
                BlendState = value.Value.ToBlendState().ToXNA();
            }
        }
    }
#endif

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
#if XNALIKE
        get => ContainedSprite.Animate;
        set => ContainedSprite.Animate = value;
#else
        get => ContainedSprite.AnimationLogic.Animate;
        set => ContainedSprite.AnimationLogic.Animate = value;
#endif
    }

    /// <summary>
    /// The name of the currently active animation chain.
    /// </summary>
    public string? CurrentChainName
    {
#if XNALIKE
        get => ContainedSprite.CurrentChainName;
        set => ContainedSprite.CurrentChainName = value;
#else
        get => ContainedSprite.AnimationLogic.CurrentChainName;
        set => ContainedSprite.AnimationLogic.CurrentChainName = value;
#endif
    }

    /// <summary>
    /// The list of animation chains available to this sprite.
    /// </summary>
    public AnimationChainList? AnimationChains
    {
#if XNALIKE
        get => ContainedSprite.AnimationChains;
        set
        {
            ContainedSprite.AnimationChains = value;
            if (ContainedSprite.UpdateToCurrentAnimationFrame())
            {
                UpdateTextureValuesFrom(ContainedSprite);
            }
        }
#else
        get => ContainedSprite.AnimationLogic.AnimationChains;
        set
        {
            ContainedSprite.AnimationLogic.AnimationChains = value;
            if (ContainedSprite.AnimationLogic.UpdateToCurrentAnimationFrame())
            {
                UpdateTextureValuesFrom(ContainedSprite);
            }
        }
#endif
    }

    /// <summary>
    /// The index of the current frame in the active animation chain.
    /// </summary>
    public int AnimationChainFrameIndex
    {
#if XNALIKE
        get => ContainedSprite.CurrentFrameIndex;
        set => ContainedSprite.CurrentFrameIndex = value;
#else
        get => ContainedSprite.AnimationLogic.CurrentFrameIndex;
        set => ContainedSprite.AnimationLogic.CurrentFrameIndex = value;
#endif
    }

    /// <summary>
    /// The current playback time (in seconds) within the active animation chain.
    /// </summary>
    public double AnimationChainTime
    {
#if XNALIKE
        get => ContainedSprite.TimeIntoAnimation;
        set => ContainedSprite.TimeIntoAnimation = value;
#else
        get => ContainedSprite.AnimationLogic.TimeIntoAnimation;
        set => ContainedSprite.AnimationLogic.TimeIntoAnimation = value;
#endif
    }

    /// <summary>
    /// The speed multiplier for animation playback (1.0 is normal speed).
    /// </summary>
    public float AnimationChainSpeed
    {
#if XNALIKE
        get => ContainedSprite.AnimationSpeed;
        set => ContainedSprite.AnimationSpeed = value;
#else
        get => ContainedSprite.AnimationLogic.AnimationSpeed;
        set => ContainedSprite.AnimationLogic.AnimationSpeed = value;
#endif
    }

    /// <summary>
    /// Whether the current animation chain should restart after finishing.
    /// </summary>
    public bool IsAnimationChainLooping
    {
#if XNALIKE
        get => ContainedSprite.IsAnimationChainLooping;
        set => ContainedSprite.IsAnimationChainLooping = value;
#else
        get => ContainedSprite.AnimationLogic.IsAnimationChainLooping;
        set => ContainedSprite.AnimationLogic.IsAnimationChainLooping = value;
#endif
    }

    /// <summary>
    /// Triggered when the current animation chain completes a full cycle.
    /// </summary>
    public event Action AnimationChainCycled
    {
        add 
        {
#if XNALIKE
            ContainedSprite.AnimationChainCycled += value;
#else
            ContainedSprite.AnimationLogic.AnimationChainCycled += value;
#endif
        }
        remove
        {
#if XNALIKE
            ContainedSprite.AnimationChainCycled -= value;
#else
            ContainedSprite.AnimationLogic.AnimationChainCycled -= value;
#endif
        }
    }

    #endregion

    #region Source File/Texture

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
#if XNALIKE
            if (ContainedSprite.UpdateToCurrentAnimationFrame())
            {
                UpdateTextureValuesFrom(ContainedSprite);
            }
#else
            if (ContainedSprite.AnimationLogic.UpdateToCurrentAnimationFrame())
            {
                UpdateTextureValuesFrom(ContainedSprite);
            }
#endif
        }
    }

#if XNALIKE
    /// <summary>
    /// The IRenderableIpso source used for RenderTarget rendering in XNA-based platforms.
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
            mContainedSprite = new ContainedSpriteType();
#else
            mContainedSprite = new RenderingLibrary.Graphics.Sprite(null);
#endif
            SetContainedObject(mContainedSprite);
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
        toReturn.mContainedSprite = null;
        return toReturn;
    }

    /// <inheritdoc cref="GraphicalUiElement.AddToManagers()"/>
    [Obsolete("Use the AddToRoot extension method instead (e.g. mySprite.AddToRoot()).")]
    public new void AddToManagers() => base.AddToManagers(SystemManagers.Default, layer: null);

}
