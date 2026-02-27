# Mixing Gum and 3D

## Introduction

Gum draw calls can be performed at any point in your game's Draw method. You can have Gum draw above or below 3D objects by moving the `GumUI.Draw()` call.

## XNA-Likes (MonoGame, FNA, KNI) and Draw

The `GumUI.Draw()` call internally uses a `SpriteBatch` to draw all UI. SpriteBatch can modify render states in a way that can break 3D rendering, such as by drawing Model instances, or the various G`raphicsDevice` draw calls.

You can verify that these state changes are breaking your 3D rendering by temporarily commenting-out `GumUI.Draw()`. If this is in fact the problem, you can restore the GraphicsDevice settings after the draw.

An older article by Shawn Hargreaves states:

> So exactly which states does SpriteBatch change? Here's the complete list:
>
> ```
>     GraphicsDevice.BlendState = BlendState.AlphaBlend;
>     GraphicsDevice.DepthStencilState = DepthStencilState.None;
>     GraphicsDevice.RasterizerState = RasterizerState.CullCounterClockwise;
>     GraphicsDevice.SamplerStates[0] = SamplerState.LinearClamp;
> ```
>
> SpriteBatch also modifies the vertex buffer, index buffer, and applies its own effect onto the GraphicsDevice.
>
> Before you draw anything in 3D you will probably want to reset these states:
>
> ```
>     GraphicsDevice.BlendState = BlendState.Opaque;
>     GraphicsDevice.DepthStencilState = DepthStencilState.Default;
> ```
>
> Depending on your 3D content, you may also want to set:
>
> ```
>     GraphicsDevice.SamplerStates[0] = SamplerState.LinearWrap;
> ```

For more information see this article: [https://shawnhargreaves.com/blog/spritebatch-and-renderstates-in-xna-game-studio-4-0.html](https://shawnhargreaves.com/blog/spritebatch-and-renderstates-in-xna-game-studio-4-0.html)

