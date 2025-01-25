# AnimateSelf

### Introduction

The AnimateSelf method performs .achx animation logic, advancing the displayed frame on the calling GraphicalUiElement and its children. This call is recursive, so it is typically only called on the root GraphicalUiElement (such as the current Screen).

## Code Example

The following shows how to call AnimateSelf on a GraphicalUiElement. Typically this is called every frame in activity. For example, this code shows how to animate a GraphicalUiElement in a MonoGame project:

```csharp
protected override void Update(GameTime gameTime)
{
    SystemManagers.Default.Activity(gameTime.TotalGameTime.TotalSeconds);
    currentScreenGue.AnimateSelf(gameTime.ElapsedGameTime.TotalSeconds);
    ...
```

{% hint style="info" %}
AnimateSelf is called automatically in FlatRedBall projects in generated code.
{% endhint %}
