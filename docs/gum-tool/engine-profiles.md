# Engine Profiles

## Introduction

An engine profile limits what a project can use, so Gum can act as an editor for an engine that supports only part of what Gum offers. For example, a level editor for a game that only draws sprites can hide every other element type and every variable the game does not read.

A profile is a `.gumengine` file that a project points to. Several projects can share one profile.

{% hint style="warning" %}
Engine profiles are highly experimental. The file format and the behavior described here may change without notice.
{% endhint %}

{% hint style="info" %}
Available in December 2026, or now if building Gum from source.
{% endhint %}

## Trying the Sample

The `Samples/SpriteLevelEditor` folder in the Gum repository is a project restricted to Sprites. Open `SpriteLevelEditor.gumx` and notice that:

* The **Standard** folder in the tree offers only **Sprite**.
* The tree has no **Components** or **Behaviors** folders.
* A selected Sprite shows only `X`, `Y`, `Width`, and `SourceFile` in the **Variables** tab.
* Selecting a Sprite draws handles only for the variables that are shown.

## The .gumengine File

A `.gumengine` file is JSON. This is the file used by the sample:

```json
{
  "supportedStandardTypes": [ "Sprite" ],
  "hideVariablesByDefault": true,
  "variableRules": [
    { "pattern": "Sprite.X", "action": "Show" },
    { "pattern": "Sprite.Y", "action": "Show" },
    { "pattern": "Sprite.Width", "action": "Show" },
    { "pattern": "Sprite.SourceFile", "action": "Show" }
  ],
  "disableComponents": true,
  "disableBehaviors": true
}
```

* `supportedStandardTypes` lists the standard element types the project can use. Types that are not listed are hidden from the tool.
* `hideVariablesByDefault` hides every variable that no rule shows. When it is `false`, every variable is shown unless a rule hides it.
* `variableRules` is an ordered list of rules. Each rule has a `pattern` and an `action` of `Show` or `Hide`. When more than one rule matches a variable, the last one wins.
* `disableComponents` and `disableBehaviors` remove components and behaviors from the project.

A `pattern` is either a qualified name such as `Sprite.X` or a plain name such as `X`, which matches the variable on every type. A `*` matches any characters, so `Sprite.Source*` matches every variable on a Sprite that starts with `Source`.

Variables that Gum always needs are never hidden: `Name`, `BaseType`, `Locked`, `DefaultChildContainer`, states, and custom variables you add yourself. `Parent` can be hidden, and hiding it also turns off parenting, so elements cannot be dragged into other elements.

## Using a Profile in a Project

To use a profile, select **Edit** -> **Settings...**, pick **General** under **Project**, and use **Browse** on the **Engine profile** row in the **Engine** section. Gum saves the path relative to the project file. **Clear** removes it, which makes the project unrestricted again.

Gum applies the profile as soon as you pick it, and reloads it when the `.gumengine` file changes on disk, so you can edit the file while the project is open.

## What Hidden Means

Hidden content stays in your files. Gum does not delete it, so a project can move to a less restricted profile and find everything intact. While content is hidden:

* The tool does not show it, and does not offer to create it.
* The Editor tab does not draw hidden values. A rotated Sprite draws unrotated if `Rotation` is hidden.
* Code generation skips it. Instances of a hidden type, and components that are based on a hidden type, produce no code.
* Gum does not warn about it.

Anything based on a hidden element is hidden too. A component based on a type the profile does not support disappears from the project, along with every instance of it.
