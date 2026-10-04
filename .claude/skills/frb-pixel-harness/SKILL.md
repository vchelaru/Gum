---
name: frb-pixel-harness
description: Pixel tests that render Gum through a real FlatRedBall (FRB1) game. Triggers: premultiplied alpha, Blend/BlendState changes, render-target bake or composite, Tests/FlatRedBall.GumRendering.Tests, BlendMathTests.
---

# FRB Pixel Harness

`Tests/FlatRedBall.GumRendering.Tests` draws Gum through FRB's own `GumIdb` and checks single pixels against arithmetic. It exists because `MonoGameGum.IntegrationTests` cannot model FRB's premultiplied pipeline (`NormalBlendState == AlphaBlend`). Run it after changing blend states, `Sprite.Render`, or the render-target bake/composite in `Renderer.cs`. For compile-only checks see `frb-build-verification`.

Run it by path: `dotnet test Tests/FlatRedBall.GumRendering.Tests`. It is in no solution, and the build skips itself with a warning when `../FlatRedBall` is missing.

| File | Purpose |
|---|---|
| `Harness/FrbGumHost.cs` | One real FRB `Game` per process; `Render(...)` draws elements and returns the backbuffer |
| `BlendMathTests.cs` | Solid colors at a known alpha, expected value computed in the test |

## Gotchas

- `FrbGumHost.Initialize` copies the hooks Glue generates into `Game1.Initialize` (`GumGame1CodeGenerator.cs` in the FRB repo). When Glue's generated setup changes, update the host to match.
- FRB state is static, so there is exactly one host per process. Touch `FrbGumHost.Instance` before anything that uses `SystemManagers.Default`.
- Sprite textures must go through `TextureContentLoader.MakePremultiplied`, as real FRB loads do.
- Inside `namespace FlatRedBall.*`, `Gum.` and `Sprite` resolve to FRB types. Use `global::Gum...` and `RenderingLibrary.Graphics.Sprite`.
- The `FRB` compile constant makes `ToBlendState` return the premultiplied blend states; a plain `dotnet test` of `MonoGameGum.Tests` never exercises that path.
- A `Skip`ped test documents a known FRB bug; remove the `Skip` in the same change that fixes it.
