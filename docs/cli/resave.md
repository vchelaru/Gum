# resave

```
gumcli resave <project.gumj> [--raw]
```

Loads a project and saves it again without any edits. The command saves the project file and every Screen, Component, and Standard element. Compare the files before and after with your version control to see whether a load and save changes your project.

Use it in CI or in a scripted sweep over many projects to catch serialization changes before they reach users.

## Options

- `<project.gumj>`: Path to the `.gumj` or `.gumx` project file
- `--raw`: Skip the step the Gum tool runs when it opens a project, which adds any missing default values to the Standard elements. With `--raw` only the file readers and writers run, so the saved files should match the originals exactly.

Without `--raw`, the result is what opening and saving the project in the Gum tool would write.

## Examples

```
gumcli resave MyProject/MyProject.gumj
gumcli resave MyProject/MyProject.gumj --raw
```

Output on success:

```
Saved /full/path/to/MyProject/MyProject.gumj
```

Problems found while loading print to stderr as `warning:` lines and do not stop the save.

{% hint style="warning" %}
`resave` overwrites the project files in place. Run it on a project under version control or on a copy.
{% endhint %}

## Exit Codes

| Code | Meaning |
|------|---------|
| 0 | The project was saved |
| 1 | Saving failed |
| 2 | The project file was not found or could not be loaded |
