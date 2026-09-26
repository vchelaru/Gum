using Gum.Wireframe;
using SkiaGum.Renderables;
using SkiaSharp.Skottie;
using System;

#if FRB
namespace SkiaGum.GueDeriving;
#else
namespace Gum.GueDeriving;
#endif

public class LottieAnimationRuntime : InteractiveGue
{
    //protected override RenderableBase ContainedRenderable => ContainedLottieAnimation;

    LottieAnimation? mContainedLottieAnimation;
    LottieAnimation ContainedLottieAnimation
    {
        get
        {
            mContainedLottieAnimation ??= this.RenderableComponent as LottieAnimation
                ?? throw new System.InvalidOperationException(
                    $"{nameof(LottieAnimationRuntime)} has no LottieAnimation renderable. Construct it with fullInstantiation: true or call SetContainedObject first.");
            return mContainedLottieAnimation;
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
                    Animation = null;
                }
                else
                {
                    var loaderManager = global::RenderingLibrary.Content.LoaderManager.Self;
                    Animation = loaderManager.LoadContent<Animation>(value);
                }
            }
        }
    }

    public Animation? Animation
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
