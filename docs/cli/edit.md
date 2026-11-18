# edit

```
gumcli edit <project.gumj> <operations.json> [--json]
gumcli edit --schema [--json]
```

Applies a list of changes from a JSON file to a project and saves it in the same format the **Gum** tool writes. Each change goes through the same code the tool runs when you make that change by hand, so a misspelled variable name fails instead of writing a file Gum cannot use.

{% hint style="info" %}
Available in December 2026, or now if building Gum from source.
{% endhint %}

## Options

- `<project.gumj>`: Path to the `.gumj` or `.gumx` project file
- `<operations.json>`: Path to the operations file
- `--json`: Report new errors as a JSON array, in the same format as [check](check.md)
- `--schema`: Print every operation with its required and optional fields and the JSON type each field takes (`string`, `bool`, or `any` for a field such as `value` that takes any JSON value) instead of editing. It takes no project or operations file. With `--json` it prints `{ "version": 1, "operations": [ { "op": "setVariable", "required": [ { "name": "element", "type": "string" }, ... ], "optional": [ ... ] } ] }`.

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

`baseType` applies only to `addComponent`. It is optional and defaults to `Container`.

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

Sets a variable the element already has. [describe](describe.md) lists the names it accepts. An instance's variable is written `InstanceName.VariableName`. Without `state`, the value goes in the default state. `state` alone names an uncategorized state, and `category` with `state` names a state in a category.

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

Exposes `Counter1.Text` on `Hud` as `Counter1Text`, so instances of `Hud` can set it. The op fails if `name` is already a variable or property of `Hud`, such as `Width` or `Name`, ignoring case.

## Removing

{% hint style="info" %}
Available in December 2026, or now if building Gum from source.
{% endhint %}

The remove operations take out what the tool's delete commands take out, including the values other elements set for what is removed. A remove the tool refuses fails the edit with the tool's message.

### removeScreen and removeComponent

```json
{ "op": "removeScreen", "name": "Game" }
{ "op": "removeComponent", "name": "Controls/Hud" }
```

Removes the element from the project and deletes its file, like the tool's delete with **Delete file** checked. `name` includes the folder. A component that another element still uses, as an instance or as its base type, cannot be removed: the edit fails and reports what uses it.

### removeInstance

```json
{ "op": "removeInstance", "element": "Hud", "name": "Panel" }
```

Removes the instance and every variable that sets or names it. Its children move to the instance's own parent, like the tool's default **Delete only parent(s)**. An instance defined in a base element cannot be removed.

### removeState and removeCategory

```json
{ "op": "removeState", "element": "Hud", "category": "Size", "name": "Big" }
{ "op": "removeCategory", "element": "Hud", "name": "Mood" }
```

`category` is optional on `removeState`. Variables that set the removed state, in this element or others, are removed so they fall back to their inherited value. The default state, and a state or category a behavior needs, cannot be removed.

### removeVariable

```json
{ "op": "removeVariable", "element": "Hud", "name": "Score" }
```

Removes a variable added with `addVariable`, and the values instances set for it. A variable used by a variable reference cannot be removed.

### unexposeVariable

```json
{ "op": "unexposeVariable", "element": "Hud", "name": "Counter1Text" }
```

Un-exposes the variable `Hud` exposes as `Counter1Text`, like the tool's **Un-expose Variable** with **Un-expose and clear values**. Gum removes the values other elements set for `Counter1Text`, and `Counter1.Text` keeps its own value. The op fails if a variable reference uses `Counter1Text`.

### clearVariable

```json
{ "op": "clearVariable", "element": "Hud", "name": "Width" }
{ "op": "clearVariable", "element": "Hud", "category": "Size", "state": "Big", "name": "Width" }
```

Removes a set value so the variable falls back to its inherited value, like **Make Default** in the **Variables** tab. `name`, `state` and `category` work as in `setVariable`. An exposed variable stays exposed. A variable added with `addVariable` goes back to its type's zero value, such as `0` or `false`. In a category state the variable takes the default state's value instead, since every state in a category sets it.

## Creating a Component From an Instance

{% hint style="info" %}
Available in December 2026, or now if building Gum from source.
{% endhint %}

### extractToComponent

```json
{ "op": "extractToComponent", "element": "Hud", "instance": "Panel", "name": "Controls/PanelComponent", "replace": true }
```

Makes a new component from `Panel`, like **Create Component** in the tool's right-click menu. The component's base type is `Panel`'s type, the instances attached to `Panel` become its instances, and the variables `Panel` sets, other than its position, become the component's. The attached instances keep their positions, which were already relative to `Panel`.

`replace` is the tool's checkbox. With `true`, `Panel` and the instances attached to it leave `Hud`, and an instance of the new component named `Panel` takes its place, with the same position and draw order. Values other states set on `Panel` stay on the new instance. Values, variable references and exposed variables that named one of the attached instances are removed, and a removed variable reference leaves its last value behind. With `false`, `Hud` does not change.

## Variable References

{% hint style="info" %}
Available in December 2026, or now if building Gum from source.
{% endhint %}

### setVariableReferences

```json
{ "op": "setVariableReferences", "element": "SegmentBar", "instance": "Total", "lines": [ "Width = TotalHealth * 4" ] }
{ "op": "setVariableReferences", "element": "SegmentBar", "lines": [ "Height = Total.Width" ] }
```

Replaces the variable references of `Total`, or of `SegmentBar` itself when `instance` is left out, like typing them in the **Variable References** box. Gum checks each line the way the tool does and writes the values the lines set, so `Total.Width` becomes `40` when `TotalHealth` is `10`. Gum also updates other states and elements whose references read a changed value. On a Forms control instance, the control's own values win over a line that sets its visual state, as in the tool: on a button whose `IsEnabled` is at its default, a line that sets `ButtonCategoryState` stores no value. A line that does not parse, names a variable that does not exist, or has a right side of the wrong type fails the edit with the line and the reason. `state` and `category` work as in `setVariable`. An empty `lines` list removes the references and leaves the values they set.

## Project Properties

{% hint style="info" %}
Available in December 2026, or now if building Gum from source.
{% endhint %}

### setProjectProperty

```json
{ "op": "setProjectProperty", "name": "DefaultCanvasWidth", "value": 256 }
{ "op": "setProjectProperty", "name": "DefaultCanvasHeight", "value": 224 }
{ "op": "setProjectProperty", "name": "TextureFilter", "value": "Point" }
```

Sets a project property, like the **Canvas** and **General** pages of the tool's **Project Properties**. `DefaultCanvasWidth` and `DefaultCanvasHeight` set the canvas size and take a positive whole number. `TextureFilter` takes `"Point"` or `"Linear"`. Any other name fails the edit, and the message lists the names it accepts.

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
