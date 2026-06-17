# Gum Repository Guidelines

## What Is This?

This project (Gum) provides UI solutions for game developers using C#. It includes:
* A Common library which can run in any C# environment for layout and some UI control logic
* Runtime libraries for various platforms including MonoGame, KNI, and FNA. Also SkiaSharp and raylib.
* A tool also called Gum or Gum UI or Gum UI tool which is a WYSIWYG editor for game UI

## Agent Workflow

For every task, invoke the appropriate agent from `.claude/agents/` before proceeding. The agent's instructions provide guidelines for how the task should be performed. Before doing any work, announce which agent you are using such as "Invoking coder agent for this task..."

**Re-read the agent file at the start of each new task — not once per session.** Long sessions drift; reloading the discipline keeps it active. Inline coding without re-reading the agent file first is not an option, even for "small" follow-ups in the same conversation.

Also load any skill whose trigger matches the area you're working in — before reading code, designing a fix, or making changes. "I'm only investigating, not editing yet" is not a reason to skip; the skill exists to inform the investigation, not just the keystrokes. The only time it's acceptable to skip is for a trivial single-file lookup that won't influence any recommendation or change.

Available agents:
- **coder** — Writing or modifying code and unit tests for new features or bugs
- **qa** — Testing, reviewing changes, and verifying correctness
- **refactoring-specialist** — Refactoring and improving code structure
- **docs-writer** — Writing or updating documentation
- **product-manager** — Breaking down tasks and tracking progress
- **security-auditor** — Security reviews and vulnerability assessments

Select the agent that best matches the task at hand. For tasks that span multiple concerns (e.g., implement a feature and write tests), invoke the relevant agents in sequence.

## Improving Guidance Files Alongside Work

When a task surfaces an improvement to a checked-in guidance file — a skill (`.claude/skills/`), an agent (`.claude/agents/`), `CLAUDE.md`, or `code-style.md` — include that change in the **same PR** as the work that motivated it. Do not ask whether it is okay, and do not split it into a separate PR. These files are shared repo artifacts, and bundling keeps the improvement next to the change that prompted it (and its rationale). The most common case: you hit a confusing pattern or recurring mistake, fix it, and add a rule to the relevant skill so it does not recur — both belong in one PR.

## Building and Testing

Pick the right build target based on what you're working on:

* **Tool work (`GumFull.sln`)** — anything under `Tool/`, `Gum/`, plugins, or tool-side code **must** be built via the solution. Plugin projects use `$(SolutionDir)` in post-build scripts, which is undefined when building a `.csproj` directly, so building individual tool csprojs silently breaks plugin output.
* **Runtime/library work (`AllLibraries.sln` OR individual csprojs)** — runtime projects (`GumCommon`, `MonoGameGum`, `KniGum`, `FnaGum`, `SkiaGum`, `RaylibGum`, and their test projects including `MonoGameGum.Tests`) have no `$(SolutionDir)`-dependent post-builds. Building the relevant individual `.csproj` is fine and is usually faster than building the whole solution. Use `AllLibraries.sln` when a change spans many runtime projects or you want a single command to verify them all.

Examples:
* Tool build: `dotnet build GumFull.sln`
* Runtime test (focused): `dotnet test MonoGameGum.Tests/MonoGameGum.Tests.csproj --filter "TestClassName"`
* Runtime build (broad): `dotnet build AllLibraries.sln`

If a runtime change is in `GumCommon` and you've already built `MonoGameGum.Tests`, that pulls in `GumCommon` and `MonoGameGum` transitively — no need to also build the solution.

**Running focused tool unit tests (`GumToolUnitTests`).** Building this project triggers the plugin projects' post-build copy, which uses `$(SolutionDir)`. To run the csproj directly, supply it — with **backslashes** (forward slashes break the `copy`/`md` steps):

```
dotnet test Tool/Tests/GumToolUnitTests/GumToolUnitTests.csproj -p:SolutionDir='C:\path\to\repo\' --filter "ClassName"
```

If the Gum tool is **running**, it locks `Gum/bin/Debug/Plugins/*` and that copy fails with "Access denied". Add `-p:BuildProjectReferences=false` to run the tests against the already-built `Gum.dll` without re-copying plugins (rebuild tool source separately first if you changed it). This avoids having to close the user's running tool.

## Code Style

See `.claude/code-style.md` for all code style rules. Read that file before writing or editing any code.

## Static Singletons in the Tool

The Gum tool has been progressively migrated to constructor-injected services (`ISelectedState`, `IDialogService`, `IUndoManager`, `PluginManager`, etc.). When editing tool code, prefer the injected service over the static singleton if both exist (e.g. use the injected `_pluginManager` rather than `PluginManager.Self`).

**`ObjectFinder.Self` is the exception.** It is intentionally still a static singleton across the entire codebase (tool, runtimes, tests). There is no `IObjectFinder` interface, and replacing it is a project-wide refactor — do not attempt it as drive-by cleanup. Calls to `ObjectFinder.Self.GetBaseElements(...)`, `ObjectFinder.Self.GetDefaultChildName(...)`, etc. are acceptable in any context.

## Investigating Third-Party Libraries

**Never decompile DLLs or NuGet assemblies** (no `dotnet-ildasm`, `ilspycmd`, ILSpy, dnSpy, etc.) to inspect third-party code. If you need to know the API surface of a library:
1. Check the library's GitHub repo or published docs.
2. Read how Gum already calls it (the call sites in this repo are usually enough).
3. Ask the user.

Decompilation is a last resort and requires explicit user permission.
