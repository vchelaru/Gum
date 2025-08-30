# Button

## Introduction

Button can be customized through its ButtonVisual class. This page discusses how to customize the various parts of a ButtonVisual

## Customizing Background

ButtonVisual includes a `Background` property of type `NineSliceRuntime`. This instance can be modified to change the button's appearance.

For example, the following code shows how to change the displayed texture.

```csharp
var buttonVisual = (ButtonVisual)button.Visual;
var background = buttonVisual.Background;
background.Texture = YourLoadedTexture;
background.TextureAddress = TextureAddress.EntireTexture;
```

Alternatively, you may want to use a sprite sheet texture (texture that has multiple individual parts). In this case, you can customize the coordinates by using custom texture address, as shown in the following code:

```csharp
background.Texture = YourLoadedTexture;
// This tells the NineSlice to use the assigned texture coordiantes
background.TextureAddress = Gum.Managers.TextureAddress.Custom;
// This is in pixels
background.TextureLeft = 438;
background.TextureTop = 231;
background.TextureWidth = 41;
background.TextureHeight = 42;
```

## Replacing Background

The default `Background` is of type NineSlice. This can be replaced with other runtime objects.

The following code shows how to replace the NineSlice background with a SpriteRuntime.

```csharp
var button = new Button();
button.AddToRoot();

var visual = (ButtonVisual)button.Visual;

// remove the existing background:
visual.Children.Remove(visual.Background);

// create a new SpriteRuntime:
var newBackground = new SpriteRuntime();
// insert it at index 0 so it is behind the existing Text instance
visual.Children.Insert(0, newBackground);
// Match the convention for backgrounds:
newBackground.Name = "Background";
// Assumes that SpriteTexture is a valid Texture2D
newBackground.Texture = SpriteTexture;

// Normally the Button's size is not controlled by its background.
// In this case we're using a Sprite which is sized based on its texture.
// Therefore, let's make the button size itself according to its background:
button.Dock(Dock.SizeToChildren);

// finally, let's modify the states so that the button gets darker or lighter on
// highlight and push:
visual.States.Enabled.Clear();
visual.States.Enabled.Apply = () =>
{
    newBackground.Color = Color.White;
};

visual.States.Highlighted.Clear();
visual.States.Highlighted.Apply = () =>
{
    newBackground.Color = Color.White;
};


visual.States.Pushed.Clear();
visual.States.Pushed.Apply = () =>
{
    newBackground.Color = Color.Gray;
};

```
