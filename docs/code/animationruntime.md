# Animation

## Introduction

`AnimationRuntime` is a class containing a list of keyframes (of type KeyframeRuntimeInstance). `AnimationRuntime` instances can be used to modify variables on a `GraphicalUiElement` over time .

AnimationRuntime instances can be created from animations defined in the Gum tool or by hand in code. For information on how to create an animation in the Gum Tool, see the [Animation Tutorials](../gum-tool/tutorials-and-examples/animation-tutorials/) section.

{% hint style="warning" %}
As of April 2025 animation support at runtime is considered a preview feature and the syntax for working with animations is likely to change in response to community feedback.
{% endhint %}

## Code Example - Loading Animations from Gum Project

Animations defined in the Gum tool can be loaded at runtime. To load and play an animation, the following calls are needed:

1. Call `GumService.LoadAnimations` . This loads all animation files for all screens and components in the current Gum project.
2. Obtain an `AnimationRuntime` instance from your `GraphicalUiElement` . This could be a screen or component.
3. Call `PlayAnimation` to begin the animation.

Animations can be played on an entire screen, entire component, or an individual instance within a screen or component.&#x20;

For this example, the project has a screen called AnimatedScreen which contains an animation named SlideOnAndOff.

<figure><img src="../.gitbook/assets/23_04 51 29.png" alt=""><figcaption></figcaption></figure>

AnimatedScreen also contains an instance of a component named PleaseWaitPopup which has its own animation called Spinning. Note that the Spinning animation is marked as repeating, so once it starts it will play until it is stopped in code.

<figure><img src="../.gitbook/assets/23_04 53 37.png" alt=""><figcaption></figcaption></figure>

The following code shows how to load all animations, create the AnimatedScreen, and play animations according to keyboard commands.

{% tabs %}
{% tab title="Using Generated Code" %}
<pre class="language-cs"><code class="lang-cs">using Gum.Wireframe; // for PlayAnimation extension method

public class Game1 : Game
{
    private GraphicsDeviceManager _graphics;
    GumService GumUI => GumService.Default;
    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
    }

<strong>    AnimatedScreen _animatedScreen;
</strong>
    protected override void Initialize()
    {
        GumUI.Initialize(this, "GumProject/GumProject.gumx");
        GumUI.LoadAnimations();

<strong>        _animatedScreen = new AnimatedScreen();
</strong><strong>        _animatedScreen.AddToRoot();
</strong>
        base.Initialize();
    }

    protected override void Update(GameTime gameTime)
    {
        GumUI.Update(gameTime);

<strong>        var keyboard = GumUI.Keyboard;
</strong><strong>        if(keyboard.KeyPushed(Keys.Space))
</strong><strong>        {
</strong><strong>            _animatedScreen.Visual.PlayAnimation("SlideOnAndOff");
</strong><strong>        }
</strong><strong>        if(keyboard.KeyPushed(Keys.Escape))
</strong><strong>        {
</strong><strong>            var popup = _animatedScreen
</strong><strong>                .PleaseWaitPopupInstance.Visual;
</strong><strong>
</strong><strong>            if(popup.Visible)
</strong><strong>            {
</strong><strong>                popup.StopAnimation();
</strong><strong>                popup.Visible = false;
</strong><strong>            }
</strong><strong>            else
</strong><strong>            {
</strong><strong>                popup.Visible = true;
</strong><strong>                popup.PlayAnimation("Spinning");
</strong><strong>            }
</strong><strong>        }
</strong>
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);
        GumUI.Draw();
        base.Draw(gameTime);
    }
}
</code></pre>
{% endtab %}

{% tab title="No Generated Code" %}
<pre class="language-csharp"><code class="lang-csharp">using Gum.Wireframe; // for PlayAnimation extension method
using MonoGameGum; // needed for ToGraphicalUiElement() extension method

public class Game1 : Game
{
    private GraphicsDeviceManager _graphics;
    GumService GumUI => GumService.Default;
    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
    }

<strong>    GraphicalUiElement _animatedScreen;
</strong>
    protected override void Initialize()
    {
        GumUI.Initialize(this, "GumProject/GumProject.gumx");
<strong>        GumUI.LoadAnimations();
</strong>
<strong>        _animatedScreen = Gum.Managers.ObjectFinder.Self
</strong><strong>            .GetScreen("AnimatedScreen")
</strong><strong>            .ToGraphicalUiElement();
</strong><strong>        _animatedScreen.AddToRoot();
</strong>
        base.Initialize();
    }

    protected override void Update(GameTime gameTime)
    {
        GumUI.Update(gameTime);

<strong>        var keyboard = GumUI.Keyboard;
</strong><strong>        if(keyboard.KeyPushed(Keys.Space))
</strong><strong>        {
</strong><strong>            _animatedScreen.PlayAnimation("SlideOnAndOff");
</strong><strong>        }
</strong><strong>        if(keyboard.KeyPushed(Keys.Escape))
</strong><strong>        {
</strong><strong>            var popup = _animatedScreen
</strong><strong>                .GetGraphicalUiElementByName("PleaseWaitPopupInstance");
</strong><strong>
</strong><strong>            if(popup.Visible)
</strong><strong>            {
</strong><strong>                popup.StopAnimation();
</strong><strong>                popup.Visible = false;
</strong><strong>            }
</strong><strong>            else
</strong><strong>            {
</strong><strong>                popup.Visible = true;
</strong><strong>                popup.PlayAnimation("Spinning");
</strong><strong>            }
</strong><strong>        }
</strong>
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);
        GumUI.Draw();
        base.Draw(gameTime);
    }
}
</code></pre>
{% endtab %}
{% endtabs %}

<figure><img src="../.gitbook/assets/23_05 20 06.gif" alt=""><figcaption></figcaption></figure>

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
    GumUI.Initialize(this, Gum.Forms.DefaultVisualsVersion.V3);

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

<figure><img src="../.gitbook/assets/06_11 02 23.gif" alt=""><figcaption><p>Button reacting to hover</p></figcaption></figure>

## Playing Multiple Animations on the Same Instance

{% hint style="warning" %}
The following code requires December 2025 or newer of the Gum runtimes. You can also link against source to get the latest code.
{% endhint %}

The `PlayAnimation` method performs the following logic:

1. Internally stores the animation that is played
2. Internally resets the time on the animation back to 0

Each instance can store a separate animation and time, allowing a single animation to be played on multiple instances.

Since each instance only stores one value for the current animation and time, an instance cannot play multiple animations at one time.

{% hint style="info" %}
Future versions of Gum may change this behavior, allowing `PlayAnimation` to stack multiple animations.
{% endhint %}

To play multiple animations, we can keep track of each animation time and apply it manually in our update function.

The following code assumes that `Animation1` and `Animation2` are valid animations. The following code also assumes that `MyInstance` is a valid visual instance.

```csharp
double animation1Time = 0;
double animation2Time = 0;

protected override void Update(GameTime gameTime)
{
    GumUI.Update(gameTime);

    var keyboard = GumUI.Keyboard;

    animation1Time += gameTime.ElapsedGameTime.TotalSeconds;
    animation2Time += gameTime.ElapsedGameTime.TotalSeconds;

    if(keyboard.KeyPushed(Keys.Space))
    {
        // Restart both animations when space is pressed
        animation1Time = 0;
        animation2Time = 0;
    }

    var animation1State = Animation1.GetStateToSet(animation1Time, MyInstance);
    MyInstance.ApplyState(animation1State);

    var animation2State = Animation2.GetStateToSet(animation2Time, MyInstance);
    MyInstance.ApplyState(animation2State);

    base.Update(gameTime);
}

```
