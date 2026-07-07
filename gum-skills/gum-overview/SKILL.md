---
name: gum-overview
description: What Gum is and how a Gum project is wired — file types, Forms vs raw visuals, the three usage modes, project setup, and the content-copy rule. Start here. Triggers: adding Gum, GumService, .gumx, "how do I use Gum".
---

# Gum Overview

Gum is a UI framework and WYSIWYG editor for C# games. The **tool** (a desktop
editor) authors UI; the **runtime** libraries load and render it at game time on
MonoGame, KNI, FNA, raylib, and SkiaSharp. This skill is the entry point the
other `gum-*` skills assume. Full docs: <https://docs.flatredball.com/gum>.

## Two visual layers: Forms vs raw visuals

- **Forms** — WPF-like interactive controls (`Button`, `TextBox`, `ListBox`,
  `CheckBox`, `StackPanel`, …). Use these for anything the player interacts with.
  See **gum-forms-controls**.
- **Raw visuals** — the drawing primitives Forms are built from
  (`TextRuntime`, `ColoredRectangleRuntime`, `SpriteRuntime`, `NineSliceRuntime`,
  `ContainerRuntime`). Use these for non-interactive HUD/decoration.

Both are laid out by the same unit system — see **gum-layout**.

## Project file types

| Extension | Contains |
|-----------|----------|
| `.gumx` | The project. Lists references to the elements below; loaded first. |
| `.gusx` | A **Screen** (a top-level UI page). |
| `.gucx` | A **Component** (a reusable element, e.g. a custom button). |
| `.gutx` | A **Standard element** (project defaults for `Text`, `Sprite`, `Container`, …). |
| `.behx` | A **Behavior** (a contract components can implement). |

These are XML. To read or hand-edit them safely, see **gum-file-format**.

## Three usage modes

Pick one up front — it shapes how code references UI:

1. **Code-only** — no `.gumx`. Build UI entirely in C# (`new Button()`, etc.).
   Simplest to start; no editor.
2. **Project + dynamic** — load a `.gumx` at runtime and find elements by string
   name (`GetGraphicalUiElementByName("Title")`). Visual editing, no codegen.
3. **Project + codegen** — load a `.gumx` **and** generate strongly-typed C#
   classes for each screen/component (via `gumcli codegen`), so UI is referenced
   by property instead of magic string. Recommended for larger projects. See
   **gumcli**.

## Wiring Gum into a game (MonoGame)

Install the runtime NuGet package for your platform — `Gum.MonoGame`,
`Gum.KNI`, or `Gum.FNA` — then drive the service each frame:

```csharp
using Gum;
using Gum.Forms;
using Gum.Forms.Controls;

GumService GumUI => GumService.Default;

// Initialize(): code-only. Initialize(this, "GumProject/GumProject.gumx"): loads a project.
protected override void Initialize()      { GumUI.Initialize(this); base.Initialize(); }
protected override void Update(GameTime t){ GumUI.Update(t); base.Update(t); }
protected override void Draw(GameTime t)  { GumUI.Draw(); base.Draw(t); }
```

Exact package IDs, other platforms, and optional add-ons (shapes, dynamic fonts,
expressions) are in the setup docs:
<https://docs.flatredball.com/gum/code/getting-started/setup>.

## Content-copy rule (do not use the MonoGame Content Pipeline)

Gum content files (`.gumx`, `.gusx`, `.gucx`, `.gutx`, `.png`, `.fnt`) are
plain files loaded from disk. They must be copied to the output directory with a
**wildcard `None` include**, *not* added to `Content.mgcb` / the MGCB pipeline:

```xml
<ItemGroup>
  <None Update="Content\**\*.*">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </None>
</ItemGroup>
```

AI assistants default to `Content.mgcb` because that is the dominant MonoGame
pattern — for Gum it is **wrong** and the project will fail to find its UI at
runtime. Fonts and images used by Gum go through this same `None` copy, not MGCB.
Details: <https://docs.flatredball.com/gum/code/files-and-fonts/file-loading>.

## States and categories (brief)

Elements can define named **states** grouped into **categories** (e.g. a
`ButtonCategory` with `Enabled`/`Highlighted`/`Pushed` states). Applying a state
sets a batch of variables at once; Forms controls switch states automatically on
interaction. You do not need to build these from scratch to use Gum — recognize
them when you see them. See **gum-forms-controls** for how controls use them, and
<https://docs.flatredball.com/gum/gum-tool/gum-elements/states>.
