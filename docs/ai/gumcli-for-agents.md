# GumCli for Agents

[GumCli](../cli/README.md) (`gumcli`) is a cross-platform command-line tool for working with Gum projects without opening the editor. Because it is scriptable, has machine-readable output, and reports meaningful exit codes, it is the primary way an AI agent acts on a Gum project and verifies its own work.

This page is a recipe-oriented tour from an agent-workflow angle. For the full reference on each command, follow the links into the [CLI](../cli/README.md) section.

## Installation

`gumcli` installs as a .NET global tool:

```
dotnet tool install -g GumCli
```

## Why it works well for agents

* **Machine-readable output** — command results (JSON, file paths, summaries) go to **stdout**, while banners and progress chatter go to **stderr**. An agent can consume stdout directly, for example `gumcli check MyProject.gumj --json`.
* **Meaningful exit codes** — `0` for success, `1` for errors found, `2` for a project that could not be loaded or invalid arguments. An agent can branch on the exit code instead of parsing prose.
* **No editor required** — everything runs headless, so it fits an automated edit → validate → verify loop.

## The agent loop

A typical agent workflow for building or modifying a Gum UI follows four stages.

### 1. Edit the project

Make changes with [`edit`](../cli/edit.md) instead of writing `.gusx` or `.gucx` XML by hand. `edit` adds screens, components, instances, states and categories, and sets variables through the same code the **Gum** tool uses, then saves in the tool's format. `setVariable` fails on a variable name the element does not have, so a misspelled name is caught right away. Run [`describe`](../cli/describe.md) before writing `setVariable` operations to get the exact names, types and allowed values, and `edit --schema` to list every operation's fields. Use `addVariable` when the element needs a new variable. `edit` also removes instances, states, categories and variables, and clears set values, taking out the values other elements set for them just as the tool does.

{% hint style="info" %}
Available in December 2026, or now if building Gum from source.
{% endhint %}

### 2. Generate code

If the project uses code generation, initialize the settings once, then generate strongly-typed C# for the project's screens and components. This eliminates magic strings and lets the agent reference UI elements by name.

* [`codegen-init`](../cli/codegen-init.md) — write the code-generation settings into the project.
* [`codegen`](../cli/codegen.md) — generate C# for the project's elements. Pass `--prune` to also delete generated files left behind by elements that were deleted or renamed.

### 3. Validate

After editing a project by hand or with other tools, validate it before trusting it (`edit` already refuses changes that add errors). `check` catches the silent mistakes that are easy for an agent to make, such as misspelled element names that Gum would otherwise drop without error.

* [`check`](../cli/check.md) — validate a project and report errors. A non-zero exit code tells the agent to stop and fix the problem before continuing.

### 4. Verify visually

Generated code that compiles is not proof the UI looks right. Rendering the result to an image gives the agent something concrete to inspect — and, with a multimodal model, to actually "see."

* [`screenshot`](../cli/screenshot.md) — render a Screen or Component to a PNG. An agent can open the PNG to confirm the layout matches intent.
* [`diff-screenshots`](../cli/diff-screenshots.md) — render every Screen and Component via both `monogame` and `raylib` and report any mismatch, so a raylib-specific rendering bug doesn't slip through if the project targets both.
* [`svg`](../cli/svg.md) — render to SVG when a vector format is preferred.

## Supporting commands

* [`new`](../cli/new.md) — scaffold a new Gum project.
* [`fonts`](../cli/fonts.md) — generate missing bitmap font files referenced by the project.
* [`pack`](../cli/pack.md) — bundle a project into a single `.gumpkg` file.

{% hint style="info" %}
Pair `gumcli` with the [MCP Documentation Server](mcp-server.md) so your assistant can look up how a command or property works while it drives the project, and with the [AI Skills](ai-skills.md) so it understands the file formats it is editing.
{% endhint %}
