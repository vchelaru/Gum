---
name: gumcli
description: Driving the gumcli command-line tool to scaffold, generate code, validate, and screenshot a Gum project. Triggers: gumcli, codegen, gumcli check, screenshot, verifying generated UI, edit-validate-verify loop.
---

# gumcli

`gumcli` is a cross-platform .NET tool for working with Gum projects **without**
the editor. It is the primary way an AI assistant acts on a Gum project and
verifies its own work: machine-readable output on stdout, meaningful exit codes,
fully headless. Full reference: <https://docs.flatredball.com/gum/cli>.

## Install

```
dotnet tool install -g GumCli
```

## The edit → validate → verify loop

After **any** change to a Gum project (by hand, by tool, or by an assistant),
run this loop. Skipping validation is the most common way agent-authored UI
breaks silently.

### 1. Generate code (project + codegen mode only)

```
gumcli codegen-init  MyProject.gumx     # once: writes code-gen settings
gumcli codegen       MyProject.gumx     # after any element edit: emits typed C#
```

Regenerate after editing elements so the generated classes match the XML. If you
rename or delete an element, delete its stale generated `.cs` file before
regenerating.

### 2. Validate — always

```
gumcli check MyProject.gumx            # human-readable
gumcli check MyProject.gumx --json     # machine-readable
```

`check` catches the mistakes that are easy to make and invisible otherwise —
most importantly **misspelled XML element names that Gum drops without error**
(see **gum-file-format**). Exit code `0` = clean, `1` = errors found, `2` =
project could not be loaded / bad arguments. Branch on the exit code; do not
parse prose. Run `check` after every hand edit before trusting the file.

### 3. Verify visually

Compiling is not proof the UI looks right. Render it and inspect:

```
gumcli screenshot MyProject.gumx ScreenOrComponentName --output out.png
gumcli svg        MyProject.gumx ScreenOrComponentName --output out.svg
```

Open the image to confirm layout matches intent. With a multimodal model, the
assistant can look at the PNG directly.

## Other commands

| Command | Purpose |
|---------|---------|
| `gumcli new <path> [--template forms\|empty]` | Scaffold a new project. `forms` (default) includes the built-in Forms controls. |
| `gumcli fonts <project.gumx>` | Generate missing bitmap font files (`.fnt`+`.png`). **Windows-only.** |
| `gumcli pack <project.gumx>` | Bundle a project into a single file. |

## Notes

- **stdout is data, stderr is chatter.** Consume results (`--json`, paths) from
  stdout; banners and progress go to stderr.
- Pair `gumcli` with the MCP documentation server so the assistant can look up a
  property while it drives the project. See
  <https://docs.flatredball.com/gum/ai/gumcli-for-agents>.
