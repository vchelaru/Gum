# AnimationRuntime

## Introduction

`AnimationRuntime` is a class containing a list of keyframes (of type KeyframeRuntimeInstance). `AnimationRuntime` instances can be used to modify variables on a `GraphicalUiElement` over time .

AnimationRuntime instances can be created from animations defined in the Gum tool or by hand in code. For information on how to create an animation in the Gum Tool, see the [Animation Tutorials](../../gum-tool/tutorials-and-examples/animation-tutorials/) section.

{% hint style="warning" %}
As of April 2025 animation support at runtime is considered a preview feature and the syntax for working with animations is likely to change in response to community feedback.
{% endhint %}

## Code Example - Loading Animations from Gum Project

Animations defined in the Gum tool can be loaded at runtime. To load and play an animation, the following calls are needed:

1. Call `GumService.LoadAnimations`
2. Obtain an `AnimationRuntime` instance from your `GraphicalUiElement`
3. Call `ApplyAtTimeTo` to apply the animation at runtime.

The following code shows how to load the first screen in a Gum project and how to play its animation.

<pre class="language-csharp"><code class="lang-csharp">GraphicalUiElement screenRuntime;

protected override void Initialize()
{
    GumUI.Initialize(this, "GumProject/GumProject.gumx");
<strong>    GumUI.LoadAnimations();
</strong>
    var screen = ObjectFinder.Self.GumProjectSave.Screens.First();
    screenRuntime = screen.ToGraphicalUiElement();
    screenRuntime.AddToRoot();

    base.Initialize();
}

protected override void Update(GameTime gameTime)
{
    GumUI.Update(gameTime);

    if(someCondition)
    {
<strong>        var animation = screenRuntime.Animations[0];
</strong><strong>        // This sets the current animation which plays automatically
</strong><strong>        screenRuntime.PlayAnimation(animation);
</strong>    }

    base.Update(gameTime);
}
</code></pre>

Alternatively, you can explicitly apply an animation to a runtime object at a given time. This gives you more control over how animations play.

<pre class="language-csharp"><code class="lang-csharp">GraphicalUiElement screenRuntime;

protected override void Initialize()
{
    GumUI.Initialize(this, "GumProject/GumProject.gumx");
<strong>    GumUI.LoadAnimations();
</strong>
    var screen = ObjectFinder.Self.GumProjectSave.Screens.First();
    screenRuntime = screen.ToGraphicalUiElement();
    screenRuntime.AddToRoot();

    base.Initialize();
}

protected override void Update(GameTime gameTime)
{
    GumUI.Update(gameTime);

<strong>    var animation = screenRuntime.Animations[0];
</strong><strong>    animation.ApplyAtTimeTo(gameTime.TotalGameTime.TotalSeconds, screenRuntime);
</strong>
    base.Update(gameTime);
}

</code></pre>

## Code Example - Animations in Code-Only Projects

Animations can be defined and executed in a code-only environment. The steps for creating an animation in a code only project are:

1. Creating an object which will be animated (such as a Button).
2. Creating the states for the animation. For example, a Button may have a larger size when highlighted, and a smaller size when not highlighted.
3. Create keyframes which define the time when each state should display and how to interpolate (tween) between each of the keyframes.
4. Define the animation using the states (keyframes) defined earlier.
5. Play the animation in response to an event, such as a Button highlight.

```csharp
protected override void Initialize()
{
    GumUI.Initialize(this, Gum.Forms.DefaultVisualsVersion.V2);

    var button = new Button();
    button.AddToRoot();
    button.Anchor(Anchor.Center);
    var buttonVisual = (ButtonVisual)button.Visual;
    buttonVisual.Animations = new List<AnimationRuntime>();

    // we will grow/shrink the contained background in response to hovering:
    var largeState = new StateSave();
    largeState.Name = "Large";
    // can't use Apply since we need values to be interpolated:
    // buttonVisual background is already
    // using WidthUnits and HeightUnits 
    // of RelativeToParent, so we can just set the Width/Height:
    largeState.SetValue("Background.Width", 20f);
    largeState.SetValue("Background.Height", 20f);

    var regularState = new StateSave();
    regularState.Name = "Regular";
    regularState.SetValue("Background.Width", 0f);
    regularState.SetValue("Background.Height", 0f);

    var growAnimation = new AnimationRuntime();
    buttonVisual.Animations.Add(growAnimation);
    growAnimation.Name = "Grow";

    var firstGrowKeyframe = new KeyframeRuntime();
    growAnimation.Keyframes.Add(firstGrowKeyframe);
    firstGrowKeyframe.CachedCumulativeState = regularState;
    firstGrowKeyframe.Time = 0;
    firstGrowKeyframe.InterpolationType = FlatRedBall.Glue.StateInterpolation.InterpolationType.Elastic;
    firstGrowKeyframe.Easing = FlatRedBall.Glue.StateInterpolation.Easing.Out;

    var secondGrowKeyframe = new KeyframeRuntime();
    growAnimation.Keyframes.Add(secondGrowKeyframe);
    secondGrowKeyframe.CachedCumulativeState = largeState;
    secondGrowKeyframe.Time = 1;

    var shrinkAnimation = new AnimationRuntime();
    buttonVisual.Animations.Add(shrinkAnimation);
    shrinkAnimation.Name = "Shrink";

    var firstShrinkKeyframe = new KeyframeRuntime();
    shrinkAnimation.Keyframes.Add(firstShrinkKeyframe);
    firstShrinkKeyframe.CachedCumulativeState = largeState;
    firstShrinkKeyframe.Time = 0;
    firstShrinkKeyframe.InterpolationType = FlatRedBall.Glue.StateInterpolation.InterpolationType.Elastic;
    firstShrinkKeyframe.Easing = FlatRedBall.Glue.StateInterpolation.Easing.Out;

    var secondShrinkKeyframe = new KeyframeRuntime();
    shrinkAnimation.Keyframes.Add(secondShrinkKeyframe);
    secondShrinkKeyframe.CachedCumulativeState = regularState;
    secondShrinkKeyframe.Time = 1;

    buttonVisual.RollOn += (_,_) =>
    {
        buttonVisual.PlayAnimation(growAnimation);
    };

    buttonVisual.RollOff += (_,_) =>
    {
        buttonVisual.PlayAnimation(shrinkAnimation);
    };

    base.Initialize();
}

```

<figure><img src="../../.gitbook/assets/06_11 02 23.gif" alt=""><figcaption><p>Button reacting to hover</p></figcaption></figure>
