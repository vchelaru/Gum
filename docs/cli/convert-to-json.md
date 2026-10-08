# convert-to-json

```
gumcli convert-to-json <project.gumx>
```

Writes a JSON copy of an XML project. The command creates a `.gumj` file next to the `.gumx`, plus a JSON file next to every Screen, Component, Standard element, Behavior, and animation file the project references. The existing XML files are not changed.

Convert a project when your game publishes with Native AOT or trimming (`PublishTrimmed`), since those builds need the JSON format. The Gum tool has the same conversion in its menus.

## Options

- `<project.gumx>`: Path to the `.gumx` project file

## Examples

```
gumcli convert-to-json MyProject/MyProject.gumx
```

Output on success:

```
Wrote /full/path/to/MyProject/MyProject.gumj
Converted 3 screen(s), 12 component(s), 8 standard element(s), 10 behavior(s), 2 animation(s) - 35 file(s) total.
```

After converting, load the `.gumj` file in your game and delete the XML files once you no longer need them.

## Exit Codes

| Code | Meaning |
|------|---------|
| 0 | The JSON files were written |
| 2 | The project could not be loaded or converted |
