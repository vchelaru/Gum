# Parent

## Introduction

The Parent property get or sets the calling GraphicalUiElement's parent. If Parent is set to a non-null value, then the newly-set Parent's Children gets modified too. Therefore, it is possible to either add a child to its Parent's Children or set its Parent directly. You do not need to do both.

## Code Example - Adding a Child by Setting Parent

The following code shows how to add a SpriteRuntime to a parent `ContainerInstance`. It assumes that `ContainerInstance` is a valid GraphicalUiElement:

```csharp
var sprite = new SpriteRuntime();
// This automatically adds sprite as a child of ContainerRuntime
sprite.Parent = ContainerInstance;
```

Later, the sprite could be removed by setting its Parent to null:

```csharp
// This removes the Sprite from its Parent (ContainerInstance)
sprite.Parent = null;
```

Gum Forms objects can be added to or removed from parents by setting Parent. For example, the following code could be used to show a button in a popup and remove it when it has been clicked:

```csharp
var button = new Button();
button.Text = "Click Me!";
button.Parent = FrameworkElement.ModalRoot;
button.Click += (_,_) => button.Parent = null;
```
