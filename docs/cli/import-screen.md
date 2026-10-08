# import-screen

```
gumcli import-screen <project.gumj> <screen.gusx> [--subfolder <name>] [--assets <folder>]
```

Imports a Screen file into an existing project. The command adds the Screen to the project, writes its file under `Screens/`, and optionally copies the images and fonts the Screen needs. It performs the same merge as **Content > Import > HTML** in the Gum tool, without opening the tool, so a script can test or repeat the import.

If the project already has a Screen or Component with the same name, the command gives the imported Screen a unique name instead of replacing the existing one.

## Options

- `<project.gumj>`: Path to the `.gumj` or `.gumx` project file
- `<screen.gusx>`: Path to the `.gusx` or `.gusj` Screen file to import
- `--subfolder`: Import the Screen into `Screens/<name>/` instead of `Screens/`
- `--assets`: A folder containing `Images/`, `Fonts/`, and `FontCache/` subfolders to copy into the project. Without this option, no files are copied.

## Examples

```
gumcli import-screen MyProject/MyProject.gumj Exported/HomePage.gusx
gumcli import-screen MyProject/MyProject.gumj Exported/HomePage.gusx --subfolder Imported
gumcli import-screen MyProject/MyProject.gumj Exported/HomePage.gusx --assets Exported/Assets
```

Output on success:

```
Imported screen "HomePage".
```

## Exit Codes

| Code | Meaning |
|------|---------|
| 0 | The Screen was imported |
| 1 | A Screen or Component with the same name already exists and could not be renamed |
| 2 | The Screen file was not found or could not be read, or the project could not be loaded |
