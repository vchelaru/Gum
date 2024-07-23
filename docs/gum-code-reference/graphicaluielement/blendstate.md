# BlendState

### Introduction

The BlendState property allows a GraphicalUiElement to blend with what is drawn before it. BlendStates can be used to control both color and alpha blending. Most games do not need to modify BlendState, and coverage of BlendStates is beyond the scope of this documentation.

The Gum BlendStates type mimics the XNA (MonoGame) BlendState. For more information see on BlendState, see the MonoGame BlendState API reference: [https://docs.monogame.net/api/Microsoft.Xna.Framework.Graphics.BlendState.html](https://docs.monogame.net/api/Microsoft.Xna.Framework.Graphics.BlendState.html)

For examples of custom BlendStates, see this post: [https://community.monogame.net/t/playing-with-blendstate-vs-photoshop-blend-modes/6827](https://community.monogame.net/t/playing-with-blendstate-vs-photoshop-blend-modes/6827)

### Example - BlendState for RenderTargets

By default Gum objects render directly to the back buffer, so the alpha value of the back buffer does not matter.

If you are rendering to a render target, then you may want to modify the blend state so that the object being renders adds to the alpha. The following code shows how to do this:

```csharp
// assuming MyColoredRectangle is a valid GraphicalUiElement
MyColoredRectangle.BlendState = Gum.BlendState.NonPremultipliedAddAlpha;
```
