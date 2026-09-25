# Texture Edge Bleed

Living doc. History of Gum's load-time texture edge bleed, which textures it touches, and the open
problems. Add a dated entry under **Log** when something new turns up.

## What it does

`ContentLoader.ApplyEdgeBleed` (`RenderingLibrary/Content/ContentLoader.cs`) runs
`TextureEdgeBleed.Bleed` on textures Gum loads. It copies neighbor RGB into fully transparent (A == 0)
texels and rewrites the texture in place with `SetData`. `Renderer.BleedTransparentTextureEdgesOnLoad`
turns it off; it defaults to true.

It applies to every texture Gum creates from a file or URL: sprites, NineSlices, and all font atlases
(KernSmith and BMFont `.fnt` pages go through the same `LoadContent<Texture2D>` path). Since
2026-09-25 it skips textures returned by `XnaContentManager`.

## Why it exists

[#3691](https://github.com/vchelaru/Gum/issues/3691): standalone MonoGame Gum draws with straight alpha
(`Renderer.NormalBlendState = NonPremultiplied`). Under Linear filtering, a transparent texel stored as
black gets interpolated into the visible edge, giving text a dark fringe. KernSmith atlases store
transparent texels as black, so fonts showed it most.

The issue listed two fixes: bleed at bake time in KernSmith, or move to a premultiplied pipeline. What
shipped was a third option, a runtime bleed on every file-loaded texture, on by default.

## Problems with the runtime approach

- It assumes straight alpha. On a premultiplied texture it writes RGB > A, which `AlphaBlend` draws as
  visible fringes or boxes.
- It mutates the texture in place, so anyone else holding the same instance sees changed pixels.
- It costs a full `GetData`/`SetData` round trip per texture load.
- It treats the symptom. The black transparent texels come from the atlas baker.

## Open questions

- **Fonts through the ContentManager fallback.** A `.fnt` page whose PNG is missing on disk falls back
  to `XnaContentManager` (the `catch` in `LoadTextureFromFile`). Those atlases no longer get the bleed.
  A pipeline-built atlas with `PremultiplyAlpha=False` and black transparent texels would fringe again
  under Linear filtering. Not observed; unverified.
- **Gum caches ContentManager textures as its own disposables.** `LoadTexture2D` adds the
  ContentManager's instance to `LoaderManager`'s cache, so `LoaderManager.DisposeAndClear` may dispose a
  texture the game still uses. Unverified.
- **`LoadFromContentManager` renames the shared texture** (`texture.Name = fileNameStandardized`).
  Harmless so far, but it is another write to an object Gum doesn't own.
- **Long-term fix.** Bleed at bake time in KernSmith (existing `FontCache` atlases would need
  regenerating), or premultiply on load and draw with `AlphaBlend`. Either would let the runtime bleed go.

## Log

- **2026-09-25.** A user's game (MonoGame, pipeline textures built with `PremultiplyAlpha=True`,
  `TextureFormat=Color`) showed artifacts under `AlphaBlend` once Gum screens were constructed, and
  looked fine under `NonPremultiplied`. A Gum component referenced `..\sheet.png`; the raw PNG wasn't in
  the output folder, so Gum fell back to `XnaContentManager.Load("sheet")`, got the game's own cached
  instance, and bled it. The `SurfaceFormat.Color` guard didn't exclude it because the pipeline output
  was `Color`. Fix: skip the bleed for ContentManager textures
  (`ContentManagerTexture_IsNotBled_EvenWhenEdgeBleedEnabled` in `LinearFilterEdgeDarkeningTests`).
  Workaround for older versions: `Renderer.BleedTransparentTextureEdgesOnLoad = false` before
  `GumService.Default.Initialize`.
