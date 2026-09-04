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

Pass the bundle to `Initialize` in place of the `.gumx`:

```csharp
// Initialize
GumService.Default.Initialize(graphics, gumProjectFile: "MyProject.gumpkg");
```

The extension you pass decides how Gum loads the project. A path ending in `.gumx` (or `.gumj`) reads loose files, and a path ending in `.gumpkg` serves every element, texture, and font read from inside the bundle. Gum does not look for a sibling file of the other kind, so a `.gumpkg` sitting next to your loose project stays unused until you name it.

That includes a `.ttf` the project references through `Font` or `CustomFontFile`. Runtime font generation (KernSmith on MonoGame, KNI, FNA, and raylib, or SkiaSharp's own rasterizer) reads the font out of the bundle, so a project that rasterizes its fonts at runtime runs from a `.gumpkg` alone with no loose files and no `FontCache/` folder.

{% hint style="info" %}
**Shipping September 2026:** Reading a bundled `.ttf` at runtime ships in the September release, or now if building Gum from source. Before this, the font was packed into the `.gumpkg` but the runtime looked for it on disk and fell back to the default font.
{% endhint %}

Because the choice is just the string you pass, a game can keep loose files while developing, where [hot reload](../code/debugging/hot-reload.md) works, and load the bundle in a published build. How it decides between the two paths is up to you.

{% hint style="warning" %}
The bundle loader requires .NET 7 or greater (it uses `System.Formats.Tar`). On older targets, passing a `.gumpkg` path throws.
{% endhint %}

## Packing From Your Build

Running `gumcli pack` by hand before every release is easy to forget, and a stale `.gumpkg` looks exactly like a fresh one. An MSBuild target packs the project as part of the build instead, so the bundle is always as new as the files it came from.

### Make GumCli available to the build

Install **GumCli** as a *local* tool so the version is recorded in source control and build machines do not need a global install. Run this once in your repository root:

```
dotnet new tool-manifest
dotnet tool install GumCli
```

This creates `.config/dotnet-tools.json`, which you check in. On a fresh clone or a build server, `dotnet tool restore` installs the recorded version, and you invoke the tool as `dotnet gumcli`.

If you would rather install **GumCli** globally (`dotnet tool install -g GumCli`), invoke it as plain `gumcli` in the examples below, and make sure every build machine has it installed.

### Add the target

Add the following to your game's `.csproj`, adjusting the paths to match your project:

```xml
<ItemGroup>
    <GumSourceFile Include="GumProject\**\*.*" />
</ItemGroup>

<Target Name="PackGumProject"
        AfterTargets="Build"
        Condition="'$(Configuration)' == 'Release'"
        Inputs="@(GumSourceFile)"
        Outputs="$(OutDir)MyProject.gumpkg">
    <Exec Command="dotnet tool restore" />
    <Exec Command="dotnet gumcli pack &quot;GumProject\MyProject.gumx&quot; -o &quot;$(OutDir)MyProject.gumpkg&quot;"
          WorkingDirectory="$(MSBuildProjectDirectory)" />
</Target>
```

What each piece does:

* `AfterTargets="Build"` runs the pack once the normal build finishes, so the bundle lands in the same output folder as your game.
* `Condition` limits packing to `Release` builds. Debug builds keep loading loose files, which is what hot reload needs.
* `Inputs` and `Outputs` make the target incremental. MSBuild skips it when the bundle is newer than every file in the Gum project, so an unchanged project does not pay for a repack on every build.
* `WorkingDirectory` lets you write the project path relative to the `.csproj` instead of spelling out an absolute path.

`gumcli pack` returns a non-zero exit code when a referenced file is missing or the project fails to load, and `Exec` turns that into a build failure. A project with a broken reference fails the build instead of shipping a bundle with holes in it.

### Keep loose files out of a release build

The loose `.gumx` and its element files are still copied to the output folder by the `CopyToOutputDirectory` entry you added when [setting up the project](../code/getting-started/setup/loading-a-gum-project-.gumx.md). Add a condition so a `Release` build ships only the bundle:

```xml
<ItemGroup Condition="'$(Configuration)' != 'Release'">
    <None Update="GumProject\**\*.*">
        <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </None>
</ItemGroup>
```

Each configuration then ships only the files it loads, so make sure the path your game passes to `Initialize` matches the configuration it was built in.

### Packing on publish instead

If you only want a bundle in published output, target `Publish` and write into the publish folder:

```xml
<Target Name="PackGumProject" AfterTargets="Publish">
    <Exec Command="dotnet gumcli pack &quot;GumProject\MyProject.gumx&quot; -o &quot;$(PublishDir)MyProject.gumpkg&quot;"
          WorkingDirectory="$(MSBuildProjectDirectory)" />
</Target>
```

### Regenerating fonts first

If your build also generates bitmap fonts, run [`gumcli fonts`](fonts.md) before packing so the `FontCache` files exist when `pack` looks for them:

```xml
<Exec Command="dotnet gumcli fonts &quot;GumProject\MyProject.gumx&quot;"
      WorkingDirectory="$(MSBuildProjectDirectory)" />
```

{% hint style="info" %}
`gumcli fonts` is Windows only, since it drives `bmfont.exe`. On a Linux or macOS build agent, either commit the generated `FontCache` files to source control, or pack with `--include core,external` and let the runtime rasterize fonts from a `.ttf`.
{% endhint %}

## Exit Codes

| Code | Meaning |
|------|---------|
| 0 | Bundle written successfully |
| 1 | One or more dependency files were missing on disk |
| 2 | Project failed to load, project file not found or unreadable, or an invalid `--include` value was supplied |
