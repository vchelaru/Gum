# edit

```
gumcli edit <project.gumj> <operations.json> [--json]
```

Applies a list of changes from a JSON file to a project and saves it in the same format the **Gum** tool writes. Each change goes through the same code the tool runs when you make that change by hand, so a misspelled variable name fails instead of writing a file Gum cannot use.

{% hint style="info" %}
Available in December 2026, or now if building Gum from source.
{% endhint %}

## Options

- `<project.gumj>`: Path to the `.gumj` or `.gumx` project file
- `<operations.json>`: Path to the operations file
- `--json`: Report new errors as a JSON array, in the same format as [check](check.md)

## The Operations File

```json
{
  "version": 1,
  "operations": [
    { "op": "addComponent", "name": "Hud" },
    { "op": "addInstance", "element": "Hud", "name": "Score", "type": "Text" }
  ]
}
```

`version` is the file format version, which is `1`. Each entry in `operations` names its operation in `op` and runs in order. An unknown `version`, `op` or field is an error.

`element` names a Screen or Component, including its folder, such as `Controls/Counter`.

### addScreen and addComponent

```json
{ "op": "addScreen", "name": "GameScreen" }
{ "op": "addComponent", "name": "Controls/Counter", "baseType": "Text" }
```

`baseType` is optional and defaults to `Container`.

### addInstance

```json
{ "op": "addInstance", "element": "Hud", "name": "Counter1", "type": "Controls/Counter", "parent": "Stack" }
```

`parent` is optional and names another instance in the same element.

### addVariable

```json
{ "op": "addVariable", "element": "Hud", "name": "Score", "type": "int", "value": 0 }
```

Adds a new variable to a Screen or Component, like **Add Variable** in the **Variables** tab. `type` is `float`, `int`, `string` or `bool`. `value` is optional.

### setVariable

```json
{ "op": "setVariable", "element": "Hud", "name": "Counter1.Text", "value": "0" }
{ "op": "setVariable", "element": "Hud", "category": "Size", "state": "Big", "name": "Width", "value": 200 }
```

Sets a variable the element already has. An instance's variable is written `InstanceName.VariableName`. Without `state`, the value goes in the default state. `state` alone names an uncategorized state, and `category` with `state` names a state in a category.

Enum values such as `ChildrenLayout` take the value's name, for example `"TopToBottomStack"`. A category's state variable, such as `SizeState`, takes the state's name.

{% hint style="warning" %}
`setVariable` only sets variables the element's type defines. To create a new variable, use `addVariable`.
{% endhint %}

### addCategory and addState

```json
{ "op": "addCategory", "element": "Hud", "name": "Size" }
{ "op": "addState", "element": "Hud", "category": "Size", "name": "Big" }
```

`category` is optional on `addState`. Without it, the state is uncategorized.

### exposeVariable

```json
{ "op": "exposeVariable", "element": "Hud", "instance": "Counter1", "variable": "Text", "name": "Counter1Text" }
```

Exposes `Counter1.Text` on `Hud` as `Counter1Text`, so instances of `Hud` can set it.

## All or Nothing

`edit` writes nothing unless every operation applies and the edit adds no errors. It runs the same check as [check](check.md) before and after the operations. Errors the project already had do not block the edit, and only errors the edit adds are reported, in the same format as `check`.

## Examples

```
gumcli edit MyProject/MyProject.gumj changes.json
gumcli edit MyProject/MyProject.gumj changes.json --json
```

Output on success:

```
No errors found.
Saved /full/path/to/MyProject/MyProject.gumj
```

An operation that cannot apply prints its position in the list to stderr:

```
error: operations[3] (setVariable): Hud has no variable named Widht.
```

## Exit Codes

| Code | Meaning |
|------|---------|
| 0 | The project was saved |
| 1 | An operation could not apply, the edit added errors, or saving failed |
| 2 | The project could not be loaded, or the operations file is not valid |
