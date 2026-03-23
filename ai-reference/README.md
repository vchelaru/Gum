# Gum AI Reference

These files are designed to be loaded into AI agent context to help agents generate correct Gum code. Drop the relevant files into your project's AI context (e.g., `.cursor/rules/`, `.claude/skills/`, or equivalent) so your agent can reference them when writing Gum UI code.

## Which Files to Use

### Code-Only (MonoGame)

For projects that create Gum UI entirely in C# code without the Gum tool:

| File | When to load |
|------|-------------|
| `gum-code-setup.md` | Always -- covers initialization, element creation, and parenting |
| `gum-code-layout.md` | When positioning or sizing elements -- covers Dock/Anchor, StackPanel, and the unit system |
| `gum-code-forms.md` | When using interactive controls -- covers Button, TextBox, ListBox, binding, etc. |
| `gum-code-styling.md` | When customizing appearance -- covers global theming, per-control colors, states, backgrounds |

Load all four for general UI work. They total ~520 lines and are designed to be loaded together.

### Gum Tool (XML-Based Projects)

For projects that use the Gum WYSIWYG editor and `.gumx` project files:

| File | When to load |
|------|-------------|
| `gum-xml-format.md` | When generating or editing `.gusx`, `.gucx`, or `.gutx` files |
| `gum-styles.md` | When working with the centralized design token / styles system |
| `gum-cli.md` | When using `gumcli` commands (check, codegen, fonts, etc.) |

## Additional Documentation

For deeper detail beyond what these reference files cover, agents with web access can fetch pages from the Gum documentation. Hub pages include subpage listings that are navigable without JavaScript:

- Controls: https://docs.flatredball.com/gum/code/controls
- Layout: https://docs.flatredball.com/gum/code/layout
- Getting started: https://docs.flatredball.com/gum/code/getting-started
