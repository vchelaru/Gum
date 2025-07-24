using Gum.Wireframe;
using SkiaGum.Renderables;
using SkiaSharp.Skottie;
using System;

namespace SkiaGum.GueDeriving;

public class LottieAnimationRuntime : BindableGue
{
    //protected override RenderableBase ContainedRenderable => ContainedLottieAnimation;

    LottieAnimation mContainedLottieAnimation;
    LottieAnimation ContainedLottieAnimation
    {
        get
        {
            if (mContainedLottieAnimation == null)
            {
                mContainedLottieAnimation = this.RenderableComponent as LottieAnimation;
            }
            return mContainedLottieAnimation;
        }
    }

    string sourceFile;
    public string SourceFile
    {
        // eventually we may want to store this off somehow
        get => sourceFile;
        set
        {
            if (sourceFile != value)
            {
                sourceFile = value;
                var loaderManager = global::RenderingLibrary.Content.LoaderManager.Self;
                var contentLoader = loaderManager.ContentLoader;
                var animation = contentLoader.LoadContent<Animation>(value);
                Animation = animation;
            }
        }
    }

    public Animation Animation
    {
        get => ContainedLottieAnimation.Animation;
        set => ContainedLottieAnimation.Animation = value;
    }

    public bool Loops
    {
        get => ContainedLottieAnimation.Loops;
        set => ContainedLottieAnimation.Loops = value;
    }

    //public bool IsDimmed
    //{
    //    get => ContainedCircle.IsDimmed;
    //    set => ContainedCircle.IsDimmed = value;
    //}

    public void Restart() => ContainedLottieAnimation.TimeAnimationStarted = DateTime.Now;

    public LottieAnimationRuntime(bool fullInstantiation = true)
    {
        if (fullInstantiation)
        {
            SetContainedObject(new LottieAnimation());
            //this.Color = SKColors.White;
            this.Visible = true;
            Width = 100;
            Height = 100;
        }
    }

    public override GraphicalUiElement Clone()
    {
        var toReturn = (LottieAnimationRuntime)base.Clone();

        toReturn.mContainedLottieAnimation = null;

        return toReturn;
    }
}
