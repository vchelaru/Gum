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
            if(mContainedSprite == null)
            {
                mContainedSprite = this.RenderableComponent as Sprite;
            }
            return mContainedSprite;
        }
    }

    public string SourceFile
    {
        // eventually we may want to store this off somehow
        get => null;
        set
        {
            if(string.IsNullOrEmpty(value))
            {
                Texture = null;
            }
            else
            {
                var loaderManager = global::RenderingLibrary.Content.LoaderManager.Self;
                var contentLoader = loaderManager.ContentLoader;
                var image = contentLoader.LoadContent<SKBitmap>(value);
                Texture = image;
            }
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

    #region AnimationChain

    public bool Animate
    {
        get => ContainedSprite.AnimationLogic.Animate;
        set => ContainedSprite.AnimationLogic.Animate = value;
    }

    public string? CurrentChainName
    {
        get => ContainedSprite.AnimationLogic.CurrentChainName;
        set => ContainedSprite.AnimationLogic.CurrentChainName = value;
    }

    public AnimationChainList? AnimationChains
    {
        get => ContainedSprite.AnimationLogic.AnimationChains;
        set
        {
            ContainedSprite.AnimationLogic.AnimationChains = value;
            ContainedSprite.AnimationLogic.UpdateToCurrentAnimationFrame();
        }
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

    #endregion

    public SpriteRuntime(bool fullInstantiaton = true)
    {
        if(fullInstantiaton)
        {
            SetContainedObject(new Sprite());

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
