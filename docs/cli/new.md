# new

```
gumcli new [<path>] [--template <name>] [--platform <name>] [--no-restore]
```

Creates a new Gum project. The path is optional — when omitted, a `GumProject` subdirectory is created in the current directory.

Add `--platform` to also create a runnable game project for MonoGame, KNI, raylib, or Stride. Without it, only a Gum project is created.

## Options

- `<path>` *(optional)* — Path for the new project. Pass a `.gumj` (JSON) or `.gumx` (XML) extension to choose the format. If no extension is given, creates `<path>/<name>.gumj` inside a new folder named `<name>`. If omitted, the project is created at `./GumProject/GumProject.gumj`. With `--platform`, the path names the game project folder instead, and defaults to `MyGumGame`.
- `--template` / `-t` — Template to use. Default: `forms`.
- `--platform` / `-p` *(optional)*: Creates a full game project, not just a Gum project. Accepted values: `monogame`, `kni`, `raylib`, `stride`.
- `--no-restore`: With `--platform`, skips the `dotnet restore` that otherwise runs after the project is created.

## Templates

### `forms` (default)

Creates a project pre-populated with the full Forms UI control set:

- All Forms behaviors (Button, CheckBox, ComboBox, ListBox, Slider, TextBox, etc.)
- All Forms components and element variants
- Standard elements and StandardGraphics assets
- Demo and keyboard screens
- `UISpriteSheet.png` and `ProjectCodeSettings.codsj`

### `empty`

Creates a minimal project with only the standard elements:

- Subfolders: Screens, Components, Standards, Behaviors
- 8 standard elements (Circle, Component, Container, NineSlice, Polygon, Rectangle, Sprite, Text)
- `ExampleSpriteFrame.png` (default NineSlice texture)

## Creating a Game Project

```
gumcli new MyGame --platform monogame
dotnet run --project MyGame/MyGame.csproj
```

The first command creates a MonoGame game that references Gum through NuGet, with a Gum project inside it. The second runs it, showing a window with a clickable button.

`gumcli new MyGame --platform monogame` creates the following files:

```
MyGame/
  MyGame.csproj
  Program.cs
  Game1.cs
  Content/GumProject/
    GumProject.gumj
    ProjectCodeSettings.codsj
```

`Game1.cs` initializes Gum, loads the Gum project, and adds a test button. KNI creates the same files. raylib and Stride have no `Game1.cs` because everything is in `Program.cs`. The Stride project targets `net10.0-windows7.0` for GPU-accelerated rendering, so it builds only on Windows.

The Gum project uses the `forms` template unless you pass `-t empty`. `ProjectCodeSettings.codsj` already points at `MyGame.csproj` and uses the **Gum Forms** Output Library, so open `Content/GumProject/GumProject.gumj` in the Gum tool, add a screen, and run [`codegen`](codegen.md) to generate classes into the game project.

`gumcli` runs `dotnet restore` after creating the files. [`codegen`](codegen.md) reads the installed Gum version from the restored package to decide which code to generate, so restore must finish before the first code generation. With `--no-restore`, run `dotnet restore` yourself first. If the restore fails (for example when offline), `gumcli` prints a warning and still exits with code 0.

{% hint style="info" %}
`--platform` is available in November 2026, or now if building Gum from source.
{% endhint %}

## Examples

```
gumcli new
gumcli new MyProject
gumcli new path/to/MyProject.gumj
gumcli new MyProject --template forms
gumcli new MyProject -t empty
gumcli new MyGame --platform monogame
gumcli new MyGame -p kni
gumcli new MyGame -p raylib -t empty
gumcli new MyGame -p monogame --no-restore
```

Output on success:

```
Created project: /full/path/to/MyProject/MyProject.gumj
```

With `--platform`, the output names the game project and the Gum project:

```
Created project: /full/path/to/MyGame/MyGame.csproj
Created Gum project: /full/path/to/MyGame/Content/GumProject/GumProject.gumj
```

## Notes

- Exits with code 2 if the project file already exists (including when invoked with no path and a `GumProject/GumProject.gumj` is already present in the current directory).
- Exits with code 2 if an unknown template name is given.
- Exits with code 2 if an unknown platform name is given. FNA is not offered because it is not published to NuGet.
- With `--platform`, exits with code 2 if `<path>` ends in `.gumj` or `.gumx`, because the path names a folder. It also exits with code 2 for a project name that is not a valid C# project name or that matches a Gum runtime assembly name such as `MonoGameGum`.
