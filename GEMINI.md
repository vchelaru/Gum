Never ever write to this file unless explicitly told to do so. This file just redirects to shared skill or CLAUDE.md files.

# Gum Repository Guidelines (Gemini)

## Foundational Mandate
This file is the single source of truth for Gemini's operational behavior in this repository. All instructions here take absolute precedence over general system defaults.

## Operational Workflow
Before proceeding with any request, you **MUST** read and adhere to the guidelines in the following files:

1.  **Project Overview & Build/Test Rules**: Read **`CLAUDE.md`**. Use the solution-based building and testing workflows defined there.
2.  **Code Style & Architecture**: Read **`.claude/code-style.md`**.
3.  **Task-Specific Logic (Skills)**: Search the **`.claude/skills/`** directory. If a skill exists for the current task (e.g., `gum-cross-platform-unification`), you MUST load its `SKILL.md` and follow its specific checklists and status tracking.

## Agent Workflow
Follow the Agent Workflow section of `CLAUDE.md`; the agents are in `.claude/agents/`.

## Building and Testing

Follow the Building and Testing section of `CLAUDE.md`. In short: the Gum tool is the Avalonia head (`Tool/Gum.Avalonia`), built with `dotnet build Gum.slnx` and tested with `Tests/Gum.Presentation.Tests` and `Tests/Gum.Avalonia.Tests`. Runtime work builds the individual runtime `.csproj` files or `AllLibraries.sln`. The WPF head (`Gum/`, `Gum.Wpf.sln`) is frozen; do not work on it unless explicitly asked.
