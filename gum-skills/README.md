# gum-skills

Skills for developers building game UI **with** Gum.

Drop the subfolders you want into your project's `.claude/skills/` (or your
user-global `~/.claude/skills/`) to give an AI assistant such as Claude Code
durable, concept-level context on Gum — file formats, layout, Forms controls,
and the `gumcli` command-line tool. A skill loads automatically when its
`description` matches what you are doing, so the assistant stops guessing at
Gum's conventions and starts following them.

These are **3rd-party skills**: they describe how to *use* Gum from a game
project. Skills for working *on* Gum itself live in the repo's own
`/.claude/skills/` folder and are not meant for distribution — do not add
engine-internal material here.

## Getting started

Copy `gum-overview` first — it is the single entry point the other skills
assume — then add the topical skills that match your work:

| Skill | Use it for |
|-------|-----------|
| `gum-overview` | What Gum is, project file types, usage modes, project wiring. **Start here.** |
| `gumcli` | Scaffolding, code generation, validation, and screenshots from the command line. |
| `gum-file-format` | Reading and safely hand-editing `.gumx`/`.gusx`/`.gucx`/`.gutx` XML. |
| `gum-forms-controls` | Buttons, text boxes, lists, panels, and the state/category styling system. |
| `gum-layout` | Positioning and sizing — units, anchor/dock, stacking. |

A good default set is `gum-overview` + `gumcli` + whichever of `gum-layout`,
`gum-forms-controls`, and `gum-file-format` your task touches.

These skills pair with two other AI aids Gum ships: the **MCP documentation
server** (live, authoritative doc lookups) and **`gumcli`** (act on a project
and verify the result). See <https://docs.flatredball.com/gum/ai> for the full
picture.

## Authoring a new skill

Each skill is a folder containing a single `SKILL.md`. The file starts with
YAML frontmatter and uses `##` sections for the body:

```markdown
---
name: gum-example
description: One line telling the assistant WHEN this skill is relevant. Triggers: distinctive words, file types, symbols.
---

# Title

## Section
Concise, high-signal guidance. Link to the docs for depth.
```

Guidelines:

- **`description` is a trigger, not a summary.** Keep it to one line. Lead with
  the topic, then list the distinctive words, file extensions, or symbols that
  should activate it. It is loaded on every session, so keep it short.
- **Link to the docs; don't restate them.** Point at
  <https://docs.flatredball.com/gum> for anything that lives there. A skill is a
  map and a list of gotchas, not a copy of the manual.
- **Keep it short and self-contained.** The reader does not have Gum's source
  tree — favor concrete rules and small examples over "see this class."
- **Add a `references/` subfolder** only when a skill needs bulky supporting
  material (long tables, extended examples) that would bloat `SKILL.md`.

When a skill becomes broadly useful, propose adding it here by opening an issue
or PR on the [Gum repository](https://github.com/vchelaru/Gum).
