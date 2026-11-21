# describe

```
gumcli describe <project.gumj> [<element>] [--instance <name>] [--json]
```

Lists the variables a Screen, Component or standard element can set with [edit](edit.md)'s `setVariable`, with each variable's type, followed by the element's states and categories. `setVariable` accepts exactly the names `describe` lists. After the categories, it also reads the element back: its base type, behaviors, instances, exposed variables and variable references, so you can confirm what an `edit` did without opening the element's file. Without an `<element>`, it lists the project's elements instead.

{% hint style="info" %}
Available in December 2026, or now if building Gum from source.
{% endhint %}

## Options

- `<project.gumj>`: Path to the `.gumj` or `.gumx` project file
- `<element>`: The Screen, Component or standard element, including its folder, such as `Controls/Counter` or `Text`. Leave it out to list the project's elements.
- `--instance <name>`: List the variables of this instance instead of the element's own. Each name is written `InstanceName.VariableName`, as `setVariable` takes it.
- `--json`: Output the description as a JSON document

## Output

Each variable shows its name and type. A Forms control also lists its behavior properties, such as `Minimum` and `Value` on a Slider. An enum variable, such as `ChildrenLayout`, and a state variable, such as `SizeState`, also list the values they accept.

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

The element's details follow, each shown only when the element has any. An instance marked `inherited` comes from the element's base type, and `in` names its parent. `Exposed` maps each exposed name to the instance variable it stands for. `References` shows the variable reference lines per instance, with the state in brackets when the lines are not in the Default state.

```
Base type: Panel
Behaviors: ButtonBehavior
Instances:
  Title (Text) inherited
  Container (Container)
  Label (Text) in Container
Exposed:
  LabelText -> Label.Text
References:
  Label: Text = Title.Text
```

With `--instance`, the details are left out, since they belong to the element.

## Listing Elements

Without an element, `describe` lists the project's Screens, Components, standard elements and behaviors, with a component's or screen's base type in parentheses. Behaviors are only listed; `describe` cannot describe one yet, so `describe <behavior>` reports that the element was not found.

```
Screens:
  Game
Components:
  Hud (Panel)
  Panel
Standards:
  Container
  Text
Behaviors:
  ButtonBehavior
```

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
  ],
  "baseType": "Panel",
  "behaviors": [ "ButtonBehavior" ],
  "instances": [
    { "name": "Label", "type": "Text", "parent": "Container", "definedByBase": false, "locked": false }
  ],
  "exposedVariables": [
    { "name": "LabelText", "variable": "Label.Text" }
  ],
  "variableReferences": [
    { "instance": "Label", "state": "Default", "category": null, "lines": [ "Text = Title.Text" ] }
  ]
}
```

- `baseType`, `behaviors`, `instances`, `exposedVariables` and `variableReferences` are written only when no `--instance` is given. `baseType` is `null` when the element has none, `parent` is `null` for an instance without a parent, and a reference's `instance` is `null` for lines on the element itself.
- Listing elements writes `{ "elements": [ { "name": "Hud", "kind": "component", "baseType": "Panel" } ] }`, where `kind` is `screen`, `component`, `standard` or `behavior`.
- `instance` is the `--instance` name, or `null`.
- `type` is the element whose variables are listed: the element itself, or the instance's type.
- `variables` is sorted by name. `values` appears only on a variable that accepts a fixed set of values.

## Examples

```
gumcli describe MyProject/MyProject.gumj
gumcli describe MyProject/MyProject.gumj Hud
gumcli describe MyProject/MyProject.gumj Hud --instance Counter1 --json
```

## Exit Codes

| Code | Meaning |
|------|---------|
| 0 | The description was printed |
| 2 | The project could not be loaded, it has no such element or instance, or `--instance` was given without an element |
