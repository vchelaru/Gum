# Gum Repository Guidelines

## What Is This?

This project (Gum) provides UI solutions for game developers using C#. It includes:
* A Common library which can run in any C# environment for layout and some UI control logic
* Runtime libraries for various platforms including MonoGame, KNI, and FNA. Also SkiaSharp and raylib.
* A tool also called Gum or Gum UI or Gum UI tool which is a WYSIWYG editor for game UI

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

## Code Style

See `.claude/code-style.md` for all code style rules. Read that file before writing or editing any code.

## Investigating Third-Party Libraries

**Never decompile DLLs or NuGet assemblies** (no `dotnet-ildasm`, `ilspycmd`, ILSpy, dnSpy, etc.) to inspect third-party code. If you need to know the API surface of a library:
1. Check the library's GitHub repo or published docs.
2. Read how Gum already calls it (the call sites in this repo are usually enough).
3. Ask the user.

Decompilation is a last resort and requires explicit user permission.
