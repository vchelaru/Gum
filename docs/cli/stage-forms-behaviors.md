# stage-forms-behaviors

```
gumcli stage-forms-behaviors <project.gumx> <destination>
```

Writes the behaviors of a Forms theme project into a single folder as standalone `.behx` files. A theme can share a behavior file with other themes or override part of it, so copying the theme's folder does not produce the behaviors the theme actually uses. This command follows those links and overrides and writes the final result.

This command is for people who build Forms themes. Theme authors run it as a build step to stage a theme's behaviors for the Gum tool's theme folder.

## Options

- `<project.gumx>`: Path to the theme's `.gumx` project file
- `<destination>`: Folder to write the `.behx` files into. Gum creates it if it does not exist.

## Examples

```
gumcli stage-forms-behaviors MyTheme/MyTheme.gumx Staged/Behaviors
```

Output on success:

```
Staged 12 behavior(s) to Staged/Behaviors
```

## Exit Codes

| Code | Meaning |
|------|---------|
| 0 | The behaviors were written |
| 2 | The project file was not found or staging failed |
