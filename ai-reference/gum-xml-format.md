# Gum XML File Format Reference

## File Types

| Extension | Type | XML root element |
|-----------|------|-----------------|
| `.gumx` | Project | `GumProjectSave` |
| `.gusx` | Screen | `ScreenSave` |
| `.gucx` | Component | `ComponentSave` |
| `.gutx` | Standard Element | `StandardElementSave` |
| `.behx` | Behavior | `BehaviorSave` |
| `.ganx` | Animation | `ElementAnimationsSave` |

## Variable Structure

Every variable inside a `<State>` block looks like:

```xml
<Variable>
  <Type>DimensionUnitType</Type>
  <Name>WidthUnits</Name>
  <Value xsi:type="xsd:int">2</Value>
  <Category>Dimensions</Category>
  <SetsValue>true</SetsValue>
</Variable>
```

The `<Type>` field determines how to interpret `<Value>`. Enum types always serialize as integers. **Omitting a variable uses the parent/default value — you only need to include variables that differ from defaults.**

## Integer-Encoded Enums

### HorizontalAlignment
Used by: `XOrigin` (all elements), `HorizontalAlignment` (Text)

| Value | Name |
|-------|------|
| 0 | Left |
| 1 | Center |
| 2 | Right |

### VerticalAlignment
Used by: `YOrigin` (all elements), `VerticalAlignment` (Text)

| Value | Name |
|-------|------|
| 0 | Top |
| 1 | Center |
| 2 | Bottom |
| 3 | TextBaseline |

### PositionUnitType
Used by: `XUnits`, `YUnits` (all elements)

X-axis values (use for XUnits):

| Value | Name | Meaning |
|-------|------|---------|
| 0 | PixelsFromLeft | Pixels from parent's left edge |
| 2 | PercentageWidth | % of parent's width (0–100) |
| 4 | PixelsFromRight | Pixels from parent's right edge |
| 6 | PixelsFromCenterX | Pixels from parent's horizontal center |

Y-axis values (use for YUnits):

| Value | Name | Meaning |
|-------|------|---------|
| 1 | PixelsFromTop | Pixels from parent's top edge |
| 3 | PercentageHeight | % of parent's height (0–100) |
| 5 | PixelsFromBottom | Pixels from parent's bottom edge |
| 7 | PixelsFromCenterY | Pixels from parent's vertical center |
| 8 | PixelsFromCenterYInverted | Inverted (obsolete) |
| 9 | PixelsFromBaseline | Pixels from parent's text baseline |

**Default:** XUnits=0 (PixelsFromLeft), YUnits=1 (PixelsFromTop)

### DimensionUnitType
Used by: `WidthUnits`, `HeightUnits` (all elements)

| Value | Name | Meaning |
|-------|------|---------|
| 0 | Absolute | Fixed pixels |
| 1 | PercentageOfParent | % of parent's dimension (100 = 100%) |
| 2 | RelativeToParent | Pixels offset from parent's size (0 = same as parent) |
| 3 | PercentageOfSourceFile | % of source texture size |
| 4 | RelativeToChildren | Pixels padding beyond children's bounds |
| 5 | PercentageOfOtherDimension | % of the other dimension (e.g. Width = % of Height) |
| 6 | MaintainFileAspectRatio | Matches aspect ratio of source file |
| 7 | Ratio | Proportional share of remaining parent space |
| 8 | AbsoluteMultipliedByFontScale | Absolute × device font scale |
| 9 | ScreenPixel | Screen pixels (unaffected by camera zoom) |

**Default:** 0 (Absolute). Text defaults to 4 (RelativeToChildren) for both Width and Height.

### ChildrenLayout
Used by: `ChildrenLayout` (Container)

| Value | Name |
|-------|------|
| 0 | Regular |
| 1 | TopToBottomStack |
| 2 | LeftToRightStack |
| 3 | AutoGridHorizontal |
| 4 | AutoGridVertical |

### Blend
Used by: `Blend` (Container, Sprite, NineSlice)

| Value | Name |
|-------|------|
| 0 | Normal |
| 1 | Additive |
| 2 | Replace |
| 3 | SubtractAlpha |
| 4 | ReplaceAlpha |
| 5 | MinAlpha |

### TextureAddress
Used by: `TextureAddress` (Sprite, NineSlice)

| Value | Name |
|-------|------|
| 0 | EntireTexture |
| 1 | Custom |
| 2 | DimensionsBased |

### TextOverflowHorizontalMode
Used by: `TextOverflowHorizontalMode` (Text)

| Value | Name |
|-------|------|
| 0 | TruncateWord |
| 1 | EllipsisLetter |

### TextOverflowVerticalMode
Used by: `TextOverflowVerticalMode` (Text)

| Value | Name |
|-------|------|
| 0 | SpillOver |
| 1 | TruncateLine |

## State Category Values (Strings)

State category assignments use **string names**, not integers:

```xml
<Variable>
  <Type>ColorCategory</Type>
  <Name>ColorCategoryState</Name>
  <Value xsi:type="xsd:string">Primary</Value>
  <SetsValue>true</SetsValue>
</Variable>
```

The valid string values depend on which states are defined in your project's standard elements. Common ones in the Forms template:

- **ColorCategory:** `Primary`, `PrimaryDark`, `PrimaryLight`, `White`, `Black`, `Gray`, `DarkGray`, `LightGray`, `Success`, `Warning`, `Danger`, `Accent`
- **StyleCategory:** `Tiny`, `Small`, `Normal`, `Emphasis`, `Strong`, `H3`, `H2`, `H1`, `Title`

## Common Pitfalls for AI-Generated Files

1. **Wrong XML tag names cause silent data loss.** `XmlSerializer` silently drops unknown tags. Use `<Instance>` not `<InstanceSave>`, use `<State>` not `<States>`. Run `gumcli check` after writing any file.

2. **Use integers for enums, not strings.** `<Value xsi:type="xsd:int">1</Value>` is correct. `<Value xsi:type="xsd:string">Center</Value>` for an enum property will be silently ignored.

3. **XUnits and YUnits share one enum but use different value ranges.** XUnits uses even-indexed values (0, 2, 4, 6); YUnits uses odd-indexed values (1, 3, 5, 7) for the corresponding positions.

4. **Only set variables that differ from defaults.** Including every possible variable is valid but verbose; omitting unchanged variables is preferred.

5. **Font files must match the naming convention.** The font file for a Text instance using `Font=Arial`, `FontSize=18` is `Font18Arial.fnt` (+ `Font18Arial_0.png`). Never write `.fnt` files manually — run `gumcli fonts` to generate them.
