---
name: skills-writer
description: Creates and updates skill files (.claude/skills/*/SKILL.md) by reading source code and condensing knowledge into concise reference guides. Use when asked to create a new skill, update an existing skill, or document a subsystem for Claude Code agent context.
---

# Skills Writer

## Process

1. Read all source files relevant to the skill topic.
2. Check `.claude/skills/` for existing skill files to match style and depth.
3. Distill non-obvious behaviors, relationships, and gotchas into SKILL.md. Obvious things (property names, method signatures) do not belong.

## Skill File Rules

- **Length**: Under 500 lines; aim for under 100 for focused topics.
- **Naming**: Kebab-case noun phrases for the directory (e.g., `gum-tool-undo`).
- **Frontmatter**: `name`, `description` (third person, specific — state what the skill covers AND when to load it).
- **Structure**: `##` sections. Tables for key-file maps and event/class lists. Prose for relationships and non-obvious behavior.
- **Progressive disclosure**: High-level architecture at the top. Link to separate detail files for advanced content (e.g., `[xnafiddle.md](xnafiddle.md)`) rather than inlining everything.

## What to Include

- Architecture: how major pieces fit together and why.
- Non-obvious gotchas: surprising behavior, ordering dependencies, naming mismatches.
- Key file map: table of file to purpose (one-liners only).
- Specific identifiers only when behavior is surprising or the name is misleading.

## What to Exclude

- Full class outlines or property lists — readable directly from source.
- Code examples unless a snippet captures an irreplaceable non-obvious pattern.
- Time-sensitive info (versions, dates, migration notes).
- Anything Claude already knows from general C# or .NET knowledge.

## Output

Write the skill file to `.claude/skills/<skill-name>/SKILL.md`. Create the directory if needed. Only create additional files if the content is large enough to warrant referenced detail files.
