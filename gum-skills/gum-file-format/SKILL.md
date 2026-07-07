---
name: gum-file-format
description: Reading and safely hand-editing Gum's XML files — enum-to-int mappings for Units/Origins/ChildrenLayout, qualified InstanceName.Property variables, and why hand-edited files silently break. Triggers: editing .gusx/.gucx/.gutx by hand, WidthUnits/XOrigin values, gumcli check errors.
---

# Gum File Format

Gum projects are XML, serialized with .NET's `XmlSerializer`. Prefer editing
them with the Gum tool or `gumcli`; when you must read or hand-edit, this skill
covers the structure and the traps. **Always run `gumcli check` after a hand
edit** — several failure modes below are silent otherwise (see **gumcli**).

## File → root element

| Extension | Root element |
|-----------|--------------|
| `.gumx` (project) | `<GumProjectSave>` — lists element references, loaded first |
| `.gusx` (screen) | `<ScreenSave>` |
| `.gucx` (component) | `<ComponentSave>` |
| `.gutx` (standard element) | `<StandardElementSave>` |
| `.behx` (behavior) | `<BehaviorSave>` |

## Element structure

Every file needs the complete root tag **with both XML-schema namespaces** —
the `xsi:`/`xsd:` prefixes used on every `<Value>` are undefined without them.
The element has a `<Name>`, an optional `<BaseType>` (what it inherits from,
e.g. `Container`), one or more `<State>`s (the first is `Default`), and a list
of `<Instance>`s (child objects). A state holds `<Variable>` entries. This is a
complete, valid screen (it passes `gumcli check`):

```xml
<?xml version="1.0" encoding="utf-8"?>
<ScreenSave xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema">
  <Name>MainMenu</Name>
  <State>
    <Name>Default</Name>
    <Variable Type="ChildrenLayout" Name="MenuStack.ChildrenLayout"><Value xsi:type="xsd:int">1</Value></Variable>
    <Variable Type="string" Name="Title.Parent"><Value xsi:type="xsd:string">MenuStack</Value></Variable>
    <Variable Type="string" Name="Title.Text"><Value xsi:type="xsd:string">Main Menu</Value></Variable>
  </State>
  <Instance><Name>MenuStack</Name><BaseType>Container</BaseType></Instance>
  <Instance><Name>Title</Name><BaseType>Text</BaseType></Instance>
</ScreenSave>
```

**`<Name>`** is the element's name — path-qualified for an element in a
subfolder (`Elements/Divider`), otherwise bare (`MainMenu`). It must match the
reference to it in the `.gumx`.

**Qualified variable names.** A variable named `MenuStack.ChildrenLayout` sets
the `ChildrenLayout` of the instance named `MenuStack`. No dot means an
element-level value. The name before the dot must match an `<Instance>`'s
`<Name>`.

**Parenting instances.** Instances are siblings by default — all direct children
of the element root. To nest one inside another, give the child a `Parent`
variable whose value is the parent instance's name (`Title.Parent = MenuStack`
above puts `Title` inside `MenuStack`, so the container's stacking layout
arranges it). Without it, a stacking container has nothing to lay out.

## Landmine: enum values are stored as integers

For an enum-typed variable, the `Type` attribute holds the enum name but the
`<Value>` holds its **underlying int** (`xsi:type="xsd:int"`), not the name.
`WidthUnits` = `2` means `RelativeToParent`. You must know the mappings to read
or write these correctly:

### WidthUnits / HeightUnits — `DimensionUnitType`

| Int | Name | Int | Name |
|----|------|----|------|
| 0 | Absolute | 6 | MaintainFileAspectRatio |
| 1 | PercentageOfParent | 7 | Ratio |
| 2 | RelativeToParent | 8 | AbsoluteMultipliedByFontScale |
| 3 | PercentageOfSourceFile | 9 | ScreenPixel |
| 4 | RelativeToChildren | 10 | RelativeToMaxParentOrChildren |
| 5 | PercentageOfOtherDimension | | |

### XUnits / YUnits — `PositionUnitType`

One shared enum; some values apply to the X axis, some to Y. **This is not the
`GeneralUnitType` enum you use in C# code** — on disk `XUnits`/`YUnits` are
`PositionUnitType`. Do not copy int values between the two.

| Int | Name | Axis | Int | Name | Axis |
|----|------|:----:|----|------|:----:|
| 0 | PixelsFromLeft | X | 5 | PixelsFromBottom | Y |
| 1 | PixelsFromTop | Y | 6 | PixelsFromCenterX | X |
| 2 | PercentageWidth | X | 7 | PixelsFromCenterY | Y |
| 3 | PercentageHeight | Y | 8 | PixelsFromCenterYInverted | Y |
| 4 | PixelsFromRight | X | 9 | PixelsFromBaseline | Y |

### XOrigin, and Text `HorizontalAlignment` — `HorizontalAlignment`

`0` = Left, `1` = Center, `2` = Right.

### YOrigin, and Text `VerticalAlignment` — `VerticalAlignment`

`0` = Top, `1` = Center, `2` = Bottom, `3` = TextBaseline.

### ChildrenLayout (on a container) — `Type="ChildrenLayout"`

`0` = Regular, `1` = TopToBottomStack, `2` = LeftToRightStack,
`3` = AutoGridHorizontal, `4` = AutoGridVertical.

## Landmine: two on-disk variable shapes exist

Gum has written variables two ways over time — a compact **attribute form**
(current tool output) and an expanded **element form** (older files). Both load:

```xml
<!-- attribute form -->
<Variable Type="DimensionUnitType" Name="WidthUnits"><Value xsi:type="xsd:int">0</Value></Variable>
<!-- element form -->
<Variable><Type>DimensionUnitType</Type><Name>WidthUnits</Name><Value xsi:type="xsd:int">0</Value><SetsValue>true</SetsValue></Variable>
```

Do not hand-craft variables from scratch by guessing the shape. Copy the shape
of an existing variable in the same file, or better, let the tool/`gumcli`
write them and edit values only.

## Landmine: wrong element names are dropped silently

`XmlSerializer` ignores XML elements it does not recognize — it does not error.
So `<States>` instead of `<State>`, or `<InstanceSave>` instead of `<Instance>`,
loads as an **empty** element with no warning, and your UI is silently missing
content. `gumcli check` is built to catch exactly this. Run it after every hand
edit.

## Bottom line

Read these files freely; the tables above let you understand and sanity-check
them. But to *write* them, prefer the Gum tool or `gumcli`, and validate with
`gumcli check` before trusting the result. Data-model reference:
<https://docs.flatredball.com/gum/gum-tool/gum-elements>.
