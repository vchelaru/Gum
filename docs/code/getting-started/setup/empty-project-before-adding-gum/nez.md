# Nez

## Introduction

Nez is a framework which adds additional functionality on top of MonoGame and FNA. Gum can be used with Nez since Nez ultimately exposes the MonoGame/FNA GraphicsDevice. Aside from setup, using Gum in Nez is identical to using Gum in any MonoGame project. Therefore, after setup you can reference the [MonoGame section](../../../monogame/) for usage details.

## Creating a New Project

Nez projects use a game class which must inherit from [Nez Core](https://github.com/prime31/Nez/blob/master/FAQs/Nez-Core.md). This class ultimately inherits from the standard MonoGame Game class, so you can include overrides for Update and Draw just like any standard MonoGame project.

A typical empty Nez game may look like is shown in the following code snippet:

```csharp
public class Game1 : Core
{
    protected override void Initialize()
    {
        base.Initialize();

        Scene = new BasicScene();
    }

    protected override void Update(GameTime gameTime)
    {
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        base.Draw(gameTime);
    }
}
```

Next, you can begin adding Gum to your project. For more information see the [Adding/Initializing Gum](../adding-initializing-gum/monogame-kni-fna/) page.
