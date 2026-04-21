using Gum.DataTypes;
using Gum.Graphics.Animation;
using Gum.Wireframe;
using SkiaGum.Renderables;
using SkiaSharp;
using System;
using static System.Net.Mime.MediaTypeNames;

namespace SkiaGum.GueDeriving;

public class SpriteRuntime : InteractiveGue
{
    Sprite mContainedSprite;
    Sprite ContainedSprite
    {
        get
        {
            if (mContainedSprite == null)
            {
                mContainedSprite = (Sprite)this.RenderableComponent;
            }
            return mContainedSprite;
        }
    }

    public SKBitmap? Texture
    {
        get => ContainedSprite.Texture;
        set => ContainedSprite.Texture = value;
    }

    public SKImage? Image
    {
        get => ContainedSprite.Image;
        set => ContainedSprite.Image = value;
    }

    public SKColor Color
    {
        get => ContainedSprite.Color;
        set => ContainedSprite.Color = value;
    }

    public string SourceFile
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

    public bool Animate
    {
        get => ContainedSprite.AnimationLogic.Animate;
        set => ContainedSprite.AnimationLogic.Animate = value;
    }

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

    public string? CurrentChainName
    {
        get => ContainedSprite.AnimationLogic.CurrentChainName;
        set => ContainedSprite.AnimationLogic.CurrentChainName = value;
    }

    public int AnimationChainFrameIndex
    {
        get => ContainedSprite.AnimationLogic.CurrentFrameIndex;
        set => ContainedSprite.AnimationLogic.CurrentFrameIndex = value;
    }

    public double AnimationChainTime
    {
        get => ContainedSprite.AnimationLogic.TimeIntoAnimation;
        set => ContainedSprite.AnimationLogic.TimeIntoAnimation = value;
    }

    public float AnimationChainSpeed
    {
        get => ContainedSprite.AnimationLogic.AnimationSpeed;
        set => ContainedSprite.AnimationLogic.AnimationSpeed = value;
    }

    public bool IsAnimationChainLooping
    {
        get => ContainedSprite.AnimationLogic.IsAnimationChainLooping;
        set => ContainedSprite.AnimationLogic.IsAnimationChainLooping = value;
    }

    public event Action AnimationChainCycled
    {
        add => ContainedSprite.AnimationLogic.AnimationChainCycled += value;
        remove => ContainedSprite.AnimationLogic.AnimationChainCycled -= value;
    }

    public SpriteRuntime(bool fullInstantiation = true)
    {
        if (fullInstantiation)
        {
            mContainedSprite = new Sprite();
            SetContainedObject(mContainedSprite);

            WidthUnits = DimensionUnitType.PercentageOfSourceFile;
            HeightUnits = DimensionUnitType.PercentageOfSourceFile;
            Width = 100;
            Height = 100;
        }
    }

    public override GraphicalUiElement Clone()
    {
        var toReturn = (SpriteRuntime)base.Clone();

        toReturn.mContainedSprite = null;

        return toReturn;
    }
}
