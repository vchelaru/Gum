# add-forms

```
gumcli add-forms <project.gumj>
```

Adds the Forms controls to a Gum project that already exists. Use it to add Button, CheckBox, ListBox, TextBox, and the rest of the Forms set to a project you created with the `empty` template or that predates Forms.

The command merges in the Forms components, standard elements, behaviors, fonts, and `UISpriteSheet.png`. Anything the project already references by name is skipped, so running the command on a project that has some Forms controls does not overwrite them.

## Options

- `<project.gumj>`: Path to the `.gumj` or `.gumx` project file

## Examples

```
gumcli add-forms MyProject/MyProject.gumj
```

Output on success:

```
Added 45 component(s), 0 standard(s), and 33 behavior(s) to: /full/path/to/MyProject/MyProject.gumj
```

To start a new project with Forms already in it, use the default `forms` template with [new](new.md) instead.

## Exit Codes

| Code | Meaning |
|------|---------|
| 0 | Forms controls were added |
| 2 | The path is not a `.gumx` or `.gumj` file, the project was not found, or the merge failed |
