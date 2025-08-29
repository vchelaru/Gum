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

```csharp
GraphicalUiElement screenRuntime;

protected override void Initialize()
{
    GumUI.Initialize(this, "GumProject/GumProject.gumx");
    GumUI.LoadAnimations();

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



