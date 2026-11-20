# describe

```
gumcli describe <project.gumj> <element> [--instance <name>] [--json]
```

Lists the variables a Screen, Component or standard element can set with [edit](edit.md)'s `setVariable`, with each variable's type, followed by the element's states and categories. `setVariable` accepts exactly the names `describe` lists.

{% hint style="info" %}
Available in December 2026, or now if building Gum from source.
{% endhint %}

## Options

- `<project.gumj>`: Path to the `.gumj` or `.gumx` project file
- `<element>`: The Screen, Component or standard element, including its folder, such as `Controls/Counter` or `Text`
- `--instance <name>`: List the variables of this instance instead of the element's own. Each name is written `InstanceName.VariableName`, as `setVariable` takes it.
- `--json`: Output the description as a JSON document

## Output

Each variable shows its name and type. An enum variable, such as `ChildrenLayout`, and a state variable, such as `SizeState`, also list the values they accept.

```
Hud
Variables:
  ChildrenLayout  ChildrenLayout (Regular, TopToBottomStack, LeftToRightStack, AutoGridHorizontal, AutoGridVertical)
  Score           int
  SizeState       Size (Big, Normal)
  State           State (Default)
  Width           float
States: Default
Categories:
  Size: Big, Normal
```

The states and categories are the element's own, which `setVariable` and `clearVariable` name with `state` and `category`. With `--instance` they are still the element's, since the instance's values are set in the element's states.

## JSON Output

```json
{
  "element": "Hud",
  "instance": null,
  "type": "Hud",
  "variables": [
    { "name": "Score", "type": "int" },
    { "name": "SizeState", "type": "Size", "values": [ "Big", "Normal" ] }
  ],
  "states": [ "Default" ],
  "categories": [
    { "name": "Size", "states": [ "Big", "Normal" ] }
  ]
}
```

- `instance` is the `--instance` name, or `null`.
- `type` is the element whose variables are listed: the element itself, or the instance's type.
- `variables` is sorted by name. `values` appears only on a variable that accepts a fixed set of values.

## Examples

```
gumcli describe MyProject/MyProject.gumj Hud
gumcli describe MyProject/MyProject.gumj Hud --instance Counter1 --json
```

## Exit Codes

| Code | Meaning |
|------|---------|
| 0 | The description was printed |
| 2 | The project could not be loaded, or it has no such element or instance |
