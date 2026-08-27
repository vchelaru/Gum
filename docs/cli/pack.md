# pack

```
gumcli pack <project.gumx> [-o <path>] [--include <categories>]
```

Loads a `.gumx` project, walks its dependencies, and writes a single-file `.gumpkg` bundle (tar + brotli) containing the requested file categories. Use this when you want to ship a single file with your game instead of a folder of loose `.gumx`/`.gusx`/`.gucx`/`.gutx` files plus textures and fonts.

{% hint style="info" %}
The Gum WYSIWYG editor still saves loose files. `.gumpkg` is purely a packaging format produced by `gumcli pack` — there is no "Save as bundle" option in the editor.
{% endhint %}

## Options

- `<project>` — Path to the `.gumx` project file (positional argument)
- `-o, --output <path>` — Output path. Defaults to `<ProjectName>.gumpkg` next to the `.gumx`.
- `--include <categories>` — Comma-separated list of file categories to include. Defaults to `core,fontcache,external`. Valid values:
  - `core` — the `.gumx` plus all `.gusx`, `.gucx`, `.gutx`, and `.behx` files referenced by the project
  - `fontcache` — generated bitmap font files under `FontCache/` (`.fnt` + `.png` pages)
  - `external` — files referenced by the project but outside Core/FontCache, such as sprite source `.png` textures, `CustomFontFile` paths, and a `Font` value that points at a project-relative `.ttf` file rather than a system font family name (e.g. `Fonts/MyFont.ttf` vs. `Arial`)

{% hint style="info" %}
**Shipping September 2026:** Packing a `.ttf` referenced through the `Font` property (not just `CustomFontFile`) will ship in the September release, or now if building Gum from source. Before this, only `CustomFontFile` paths were bundled as external font files.
{% endhint %}

## Examples

Pack with default categories (everything):

```
gumcli pack MyProject/MyProject.gumx
```

Pack to a specific output path:

```
gumcli pack MyProject/MyProject.gumx -o build/MyProject.gumpkg
```

Omit the font cache (e.g. when your build pipeline regenerates bitmap fonts via `gumcli fonts`):

```
gumcli pack MyProject/MyProject.gumx --include core,external
```

## Output

The command prints per-category file counts together with uncompressed and compressed byte sizes plus the overall compression ratio, for example:

```
Packed 83 files into C:\Games\MyProject\MyProject.gumpkg
  Core:          42
  FontCache:     18
  External:      23
Uncompressed:    2003532 bytes
Compressed:      541210 bytes
Ratio:           27.0%
```

## Loading a `.gumpkg` at runtime

In MonoGame, load the project the same way you would a loose project:

```csharp
GumService.Default.Initialize(graphics, gumProjectFile: "MyProject/MyProject.gumx");
```

If a sibling `.gumpkg` is found and the loose `.gumx` is **not** present, the loader transparently switches to bundle mode and serves all element, texture, and font reads from the bundle.

That includes a `.ttf` the project references through `Font` or `CustomFontFile`. Runtime font generation (KernSmith on MonoGame, KNI, FNA, and raylib, or SkiaSharp's own rasterizer) reads the font out of the bundle, so a project that rasterizes its fonts at runtime runs from a `.gumpkg` alone with no loose files and no `FontCache/` folder.

{% hint style="info" %}
**Shipping September 2026:** Reading a bundled `.ttf` at runtime ships in the September release, or now if building Gum from source. Before this, the font was packed into the `.gumpkg` but the runtime looked for it on disk and fell back to the default font.
{% endhint %}

**Loose wins when both exist.** This is intentional — during development you keep the loose files (and hot reload) working, and in a published build you ship only the `.gumpkg`.

{% hint style="warning" %}
The bundle loader requires .NET 7 or greater (it uses `System.Formats.Tar`). On older targets the `.gumpkg` is ignored and the loader falls back to loose-file resolution.
{% endhint %}

## Exit Codes

| Code | Meaning |
|------|---------|
| 0 | Bundle written successfully |
| 1 | One or more dependency files were missing on disk |
| 2 | Project failed to load, project file not found or unreadable, or an invalid `--include` value was supplied |
