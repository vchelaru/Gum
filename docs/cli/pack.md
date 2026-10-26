# pack

```
gumcli pack <project.gumj> [-o <path>] [--include <categories>] [--shake [--keep <names>] [--roots <file>] [--roots-assembly <dll>]]
```

Loads a `.gumj` or `.gumx` project, walks its dependencies, and writes a single-file `.gumpkg` bundle (tar + brotli) containing the requested file categories. Use this when you want to ship a single file with your game instead of a folder of loose project and element files plus textures and fonts.

{% hint style="info" %}
The Gum WYSIWYG editor still saves loose files. `.gumpkg` is purely a packaging format produced by `gumcli pack` — there is no "Save as bundle" option in the editor.
{% endhint %}

## Options

- `<project>`: Path to the `.gumj` or `.gumx` project file (positional argument)
- `-o, --output <path>`: Output path. Defaults to `<ProjectName>.gumpkg` next to the project file.
- `--include <categories>`: Comma-separated list of file categories to include. Defaults to `core,fontcache,external`. Valid values:
  - `core`: the project file plus all screen, component, standard element, and behavior files referenced by the project
  - `fontcache`: generated bitmap font files under `FontCache/` (`.fnt` + `.png` pages)
  - `external`: files referenced by the project but outside Core/FontCache, such as sprite source `.png` textures, `CustomFontFile` paths, and a `Font` value that points at a project-relative `.ttf` file rather than a system font family name (e.g. `Fonts/MyFont.ttf` vs. `Arial`)
- `--shake`: Leave out every Screen and Component that nothing in the project reaches, along with the files only they use. Off by default. See [Leaving Out Unused Elements](pack.md#leaving-out-unused-elements).
- `--keep <names>`: With `--shake`, element names to keep, comma-separated or repeated.
- `--roots <file>`: With `--shake`, a text file of element names to keep, one per line. A line starting with `#` is a comment.
- `--roots-assembly <dll>` — With `--shake`, your built game assembly. Pack keeps every element your game's code uses, as recorded by the Gum source generator.

{% hint style="info" %}
**Shipping September 2026:** Packing a `.ttf` referenced through the `Font` property (not just `CustomFontFile`) will ship in the September release, or now if building Gum from source. Before this, only `CustomFontFile` paths were bundled as external font files.
{% endhint %}

## Examples

Pack with default categories (everything):

```
gumcli pack GumProject/GumProject.gumj
```

Pack to a specific output path:

```
gumcli pack GumProject/GumProject.gumj -o build/GumProject.gumpkg
```

Omit the font cache (e.g. when your build pipeline regenerates bitmap fonts via `gumcli fonts`):

```
gumcli pack GumProject/GumProject.gumj --include core,external
```

## Output

The command prints per-category file counts together with uncompressed and compressed byte sizes plus the overall compression ratio, for example:

```
Packed 83 files into C:\Games\MyGame\GumProject.gumpkg
  Core:          42
  FontCache:     18
  External:      23
Uncompressed:    2003532 bytes
Compressed:      541210 bytes
Ratio:           27.0%
```

## Leaving Out Unused Elements

By default, `pack` bundles every element in the project. A project built from the Forms template carries dozens of components that a game may never show, and loading them all costs startup time. `--shake` leaves those elements out:

```
gumcli pack GumProject/GumProject.gumj --shake
```

{% hint style="info" %}
Available in November 2026, or now if building Gum from source.
{% endhint %}

Pack starts from every Screen and keeps each element it can reach from there:

* the element's base type
* the type of every instance in it
* any element its variable references read, such as a `Styles` component
* the components Gum Forms creates on its own: the Tooltip component for any control, the ListBoxItem component for a ListBox or ComboBox, and the MenuItem and submenu popup components for a Menu or MenuItem

Standard elements and behaviors are always kept. Textures, fonts, and animation files are bundled only when a kept element uses them.

The output starts with a line that counts the kept elements and names the ones left out:

```
Kept 12 of 64 Screens and Components; trimmed 52: Controls/ButtonClose, Controls/ButtonDeny, ...
Packed 41 files into C:\Games\MyGame\GumProject.gumpkg
```

### Keeping Elements Created From Code

An element that only your code creates, such as a card component added to the screen at runtime, is not reachable from any Screen. Name it so pack keeps it, in one of three ways.

`--keep` takes names on the command line. A name includes its folder, as it appears in the Gum tool:

```
gumcli pack GumProject/GumProject.gumj --shake --keep CardGum,Controls/Toast
```

`--roots` reads names from a text file, one per line, which suits a list you check in with your project:

```
# Elements created only from code
CardGum
Controls/Toast
```

`--roots-assembly` reads the names from your built game assembly. The Gum NuGet packages include a source generator that records every element your hand-written code uses: creating a generated class, naming it as a type, or passing a string literal to an `ObjectFinder` lookup such as `GetElementSave("CardGum")`. Code generation adds the link between a class and its element at [syntax version 7](../gum-tool/upgrading/syntax-versions.md), so generate your code with a current runtime first.

```
gumcli pack GumProject/GumProject.gumj --shake --roots-assembly bin/Release/net8.0/MyGame.dll
```

The generator cannot read a name that is not a string literal, such as `GetElementSave(elementName)`. It reports warning `GUM004` at that line, and you add the name with `--keep` or `--roots`.

You can combine all three options. Pack stops with exit code 2 when a name matches no element in the project, so pack catches a misspelled name before it reaches your game.

### When a Trimmed Element Is Requested

The bundle records which elements were left out. When generated code asks for one of them, Gum throws an exception that names the element and the fix:

```
'CardGum' was trimmed by gumcli pack --shake because nothing in the project reaches it. Pack with --keep CardGum, or list it in the --roots file.
```

A variable reference that reads a trimmed element is not applied. Gum reports an error that names the reference and the element to keep, the same way it reports [other references that cannot be applied](../code/styling/runtime-variable-references.md#casting-and-type-considerations).

In either case, add the element with `--keep`, `--roots`, or `--roots-assembly` and pack again. `ObjectFinder.Self.GetElementSave` still returns `null` for a trimmed element, the same as for a name that does not exist, so code that looks up elements by name should check the result.

To rule out shaking while you track down a problem, pack without `--shake` or load the loose project files.

## Loading a `.gumpkg` at runtime

Pass the bundle to `Initialize` in place of the `.gumj`:

```csharp
// Initialize
GumService.Default.Initialize(graphics, gumProjectFile: "GumProject.gumpkg");
```

The extension you pass decides how Gum loads the project. A path ending in `.gumj` (or `.gumx`) reads loose files, and a path ending in `.gumpkg` serves every element, texture, and font read from inside the bundle. Gum does not look for a sibling file of the other kind, so a `.gumpkg` sitting next to your loose project is ignored unless you pass its path to `Initialize`.

A bundle path resolves from the same starting folder as a loose project path. On MonoGame, KNI, and FNA that folder is your game's `Content` folder, so `"GumProject/GumProject.gumj"` loads `Content/GumProject/GumProject.gumj` and the sample above loads `Content/GumProject.gumpkg`. On raylib the path starts at the folder holding the executable, so a project loaded as `"resources/GumProject/GumProject.gumj"` becomes `"resources/GumProject.gumpkg"`.

{% hint style="warning" %}
A bundle keeps the file name of the project it holds. Gum reads a `.gumpkg` by looking inside it for a project file of the same name, so `GumProject.gumpkg` must hold `GumProject.gumj`, and loading a renamed bundle throws. The comparison is exact, including capitalization, on every platform. `-o` names the file to write, so point it at a different folder rather than a different name.
{% endhint %}

{% hint style="info" %}
Bundle loading is available on MonoGame, KNI, FNA, and raylib. SkiaGum and Silk.NET load loose project files only, so pass them a `.gumj` or `.gumx` path.
{% endhint %}

That includes a `.ttf` the project references through `Font` or `CustomFontFile`. Runtime font generation (KernSmith on MonoGame, KNI, FNA, and raylib) reads the font out of the bundle, so a project that rasterizes its fonts at runtime runs from a `.gumpkg` alone with no loose files and no `FontCache/` folder.

{% hint style="info" %}
**Shipping September 2026:** Reading a bundled `.ttf` at runtime ships in the September release, or now if building Gum from source. Before this, the font was packed into the `.gumpkg` but the runtime looked for it on disk and fell back to the default font.
{% endhint %}

Because the choice is just the string you pass, a game can keep loose files while developing, where [hot reload](../code/debugging/hot-reload.md) works, and load the bundle in a published build. How it decides between the two paths is up to you.

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
    <GumSourceFile Include="Content\GumProject\**\*.*" />
</ItemGroup>

<Target Name="PackGumProject"
        AfterTargets="Build"
        Condition="'$(Configuration)' == 'Release'"
        Inputs="@(GumSourceFile)"
        Outputs="$(OutDir)Content\GumProject.gumpkg">
    <Exec Command="dotnet tool restore" />
    <MakeDir Directories="$(OutDir)Content" />
    <Exec Command="dotnet gumcli pack &quot;Content\GumProject\GumProject.gumj&quot; -o &quot;$(OutDir)Content\GumProject.gumpkg&quot;"
          WorkingDirectory="$(MSBuildProjectDirectory)" />
</Target>
```

The paths above follow the MonoGame, KNI, and FNA layout, where the Gum project lives in `Content\GumProject` and the game loads it as `"GumProject.gumpkg"`. On raylib, swap `Content` for `resources` in both the item and the output path, and load the bundle as `"resources/GumProject.gumpkg"`.

What each piece does:

* `AfterTargets="Build"` runs the pack once the normal build finishes, so the bundle lands in the build output alongside the rest of your content. `dotnet publish` does not carry it any further, so see [Packing for publish](pack.md#packing-for-publish) if you publish.
* `Condition` limits packing to `Release` builds, since Debug builds keep loading loose files, which is what hot reload needs. Your game has to ask for the bundle only in the configuration that produces one, or it looks for a `.gumpkg` that was never written.
* `Inputs` and `Outputs` make the target incremental. MSBuild skips it when the bundle is newer than every file in the Gum project, so an unchanged project does not pay for a repack on every build.
* `MakeDir` creates the content folder, because `pack` writes the bundle but does not create the folder holding it.
* `WorkingDirectory` lets you write the project path relative to the `.csproj` instead of spelling out an absolute path.

`gumcli pack` returns a non-zero exit code when a referenced file is missing or the project fails to load, and `Exec` turns that into a build failure. A project with a broken reference fails the build instead of shipping a bundle with holes in it.

### Keep loose files out of a release build

Every configuration copies the loose `.gumj` and its element files to the output folder, through the `CopyToOutputDirectory` entry you added when [setting up the project](../code/getting-started/setup/loading-a-gum-project-.gumx.md). Add a condition to that entry so a `Release` build ships only the bundle:

```xml
<ItemGroup Condition="'$(Configuration)' != 'Release'">
    <None Update="Content\GumProject\**\*.*">
        <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </None>
</ItemGroup>
```

Each configuration then ships only the files it loads.

### Packing for publish

A bundle written into the build output is not copied to the publish folder, so a game you ship with `dotnet publish` wants its own target:

```xml
<Target Name="PackGumProjectForPublish" AfterTargets="Publish">
    <Exec Command="dotnet tool restore" />
    <MakeDir Directories="$(PublishDir)Content" />
    <Exec Command="dotnet gumcli pack &quot;Content\GumProject\GumProject.gumj&quot; -o &quot;$(PublishDir)Content\GumProject.gumpkg&quot;"
          WorkingDirectory="$(MSBuildProjectDirectory)" />
</Target>
```

### Regenerating fonts first

If your build also generates bitmap fonts, run [`gumcli fonts`](fonts.md) before packing so the `FontCache` files exist when `pack` looks for them:

```xml
<Exec Command="dotnet gumcli fonts &quot;Content\GumProject\GumProject.gumj&quot;"
      WorkingDirectory="$(MSBuildProjectDirectory)" />
```

{% hint style="info" %}
`gumcli fonts` runs on any operating system. A project that uses **BMFont** gets its fonts from KernSmith on a Linux or macOS build agent, since BMFont runs `bmfont.exe`, which only exists on Windows. To get the same font files on every agent, switch the project to KernSmith, commit the generated `FontCache` files to source control, or pack with `--include core,external` and let the runtime rasterize fonts from a `.ttf`. See [fonts](fonts.md) for details.
{% endhint %}

## Exit Codes

| Code | Meaning |
|------|---------|
| 0 | Bundle written successfully |
| 1 | One or more dependency files were missing on disk |
| 2 | Project failed to load, project file not found or unreadable, an invalid `--include` value was supplied, `--keep`, `--roots`, or `--roots-assembly` was used without `--shake`, a roots file or assembly was not found, or a kept name matches no element in the project |
