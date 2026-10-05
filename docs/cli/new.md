# new

```
gumcli new [<path>] [--template <name>]
```

Creates a new Gum project. The path is optional — when omitted, a `GumProject` subdirectory is created in the current directory.

## Options

- `<path>` *(optional)* — Path for the new project. Pass a `.gumj` (JSON) or `.gumx` (XML) extension to choose the format. If no extension is given, creates `<path>/<name>.gumj` inside a new folder named `<name>`. If omitted, the project is created at `./GumProject/GumProject.gumj`.
- `--template` / `-t` — Template to use. Default: `forms`.

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

## Examples

```
gumcli new
gumcli new MyProject
gumcli new path/to/MyProject.gumj
gumcli new MyProject --template forms
gumcli new MyProject -t empty
```

Output on success:

```
Created project: /full/path/to/MyProject/MyProject.gumj
```

## Notes

- Exits with code 2 if the project file already exists (including when invoked with no path and a `GumProject/GumProject.gumj` is already present in the current directory).
- Exits with code 2 if an unknown template name is given.
