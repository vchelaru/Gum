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
For every task, invoke the appropriate agent from `.claude/agents/` before proceeding. The agent's instructions provide guidelines for how the task should be performed. Before doing any work, announce which agent you are using such as "Invoking coder agent for this task..."

Available agents:
- **coder** — Writing or modifying code and unit tests for new features or bugs
- **qa** — Testing, reviewing changes, and verifying correctness
- **refactoring-specialist** — Refactoring and improving code structure
- **docs-writer** — Writing or updating documentation
- **product-manager** — Breaking down tasks and tracking progress
- **security-auditor** — Security reviews and vulnerability assessments

Select the agent that best matches the task at hand. For tasks that span multiple concerns (e.g., implement a feature and write tests), invoke the relevant agents in sequence.

## Building and Testing

**Always build and test via a solution file**, not individual `.csproj` files. Plugin projects use `$(SolutionDir)` in post-build scripts, which is undefined when building a `.csproj` directly.

Pick the solution based on what you're working on:

* **`GumFull.sln`** — the Gum tool and all tool-related projects (WPF editor, plugins, `GumToolUnitTests`, `Gum.Cli.Tests`, etc.). Use this when working on anything under `Tool/`, `Gum/`, plugins, or tool-side code.
* **`AllLibraries.sln`** — all runtime-related projects (`GumCommon`, `MonoGameGum`, `KniGum`, `FnaGum`, `SkiaGum`, `RaylibGum`, and their test projects including `MonoGameGum.Tests`). Use this when working on runtime libraries, `GumCommon`, or anything a shipped game would reference.

Examples:
* Build: `dotnet build GumFull.sln` or `dotnet build AllLibraries.sln`
* Test: `dotnet test AllLibraries.sln --filter "TestClassName"`

If a change spans both (e.g. editing `GumCommon` which is linked into both), build both solutions.
