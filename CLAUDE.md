# Gum Repository Guidelines

## What Is This?

This project (Gum) provides UI solutions for game developers using C#. It includes:
* A Common library which can run in any C# environment for layout and some UI control logic
* Runtime libraries for various platforms including MonoGame, KNI, and FNA. Also SkiaSharp and raylib.
* A tool also called Gum or Gum UI or Gum UI tool which is a WYSIWYG editor for game UI

## Project Direction

High-level direction for Gum — vision, roadmap, open strategic questions, and decision records (ADRs) — lives in `Direction/` at the repo root. **For any discussion of Gum's high-level goals, roadmap, or strategic decisions, read `Direction/README.md` first**, then the specific file for the topic. This is the strategy layer (*what* and *why*); it is separate from the operational guidance in this file and in the skills/agents (*how*), and from the published user-facing docs in `docs/`.

## Agent Workflow

For every task, read the guidelines in the matching agent file under `.claude/agents/` and follow them yourself, in your own context, before proceeding — do not dispatch an actual subagent for this by default. Spinning up a subagent costs real tokens: a fresh agent has none of the context already built up in this conversation and has to independently re-read files, re-run builds, and re-explore the codebase to reconstruct it, on top of whatever the task itself costs. Reserve `Agent`-tool dispatch for two cases only: **(1)** the work is genuinely parallelizable (independent pieces that can run concurrently), or **(2)** the user explicitly asks for delegation/a subagent.

**Re-read the agent file at the start of each new task — not once per session.** Long sessions drift; reloading the discipline keeps it active. Inline coding without re-reading the agent file first is not an option, even for "small" follow-ups in the same conversation.

Also load any skill whose trigger matches the area you're working in — before reading code, designing a fix, or making changes. "I'm only investigating, not editing yet" is not a reason to skip; the skill exists to inform the investigation, not just the keystrokes. The only time it's acceptable to skip is for a trivial single-file lookup that won't influence any recommendation or change.

**This check re-runs per new file path, not once per task.** Matching skills against the files identified at task start (e.g. the source file a bug lives in) does not cover a different file touched later in the same task — a sample or test project pulled in only for manual verification, a config file edited in passing, a doc updated alongside. Each new file gets its own trigger-match against the skill list before it's edited, even when the task's "main" skill is already loaded.

Available agents:
- **coder** — Writing or modifying code and unit tests for new features or bugs
- **refactoring-specialist** — Refactoring and improving code structure
- **docs-writer** — Writing or updating documentation
- **product-manager** — Breaking down tasks and tracking progress
- **security-auditor** — Security reviews and vulnerability assessments

Select the agent that best matches the task at hand. For tasks that span multiple concerns (e.g., implement a feature and write tests), invoke the relevant agents in sequence.

**Reviewing changes before merge is not a dedicated agent.** Use the `/code-review` skill at **low or medium effort** (e.g. `Skill({skill: "code-review", args: "medium"})`) for routine pre-commit review — it covers correctness bugs *and* quality/refactoring cleanups in one pass, inline, no subagents. The coder writes its own unit tests; the `tdd` skill owns test discipline and the testability gate.

**Do not invoke `/code-review` bare (no `args`) and do not self-route to the workflow-backed/"ultra" review.** Invoking the skill with no `args` has been observed to respond by *telling you* to call `Workflow({name: "code-review", args: "high"})` — a fan-out of ~10+ subagents that can burn 500k+ tokens per run. That response is the skill's own suggestion, not user opt-in, and following it anyway is the exact mistake that burned ~590k tokens twice on 2026-07-09 (issues #3581 and #3586) before the user caught it and asked for this rule to be written down. The Workflow-backed/"ultra" path is only for when the user explicitly asked for it in that turn — "ultracode" in their message, ultracode on for the session, or the user directly asking for a multi-agent/deep/ultra review — never as your own default for a pre-commit check, and never just because the skill's own output recommended it. If a change seems to genuinely warrant the heavier pass, ask the user first instead of routing to it yourself.

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

**Don't initialize the FNA or Sokol submodules — or build `AllLibraries.sln`, which pulls them in — unless your change actually touches FNA or Sokol code.** A fresh clone/worktree leaves these submodules uninitialized; initializing them triggers a large recursive clone (FNA → SDL/FAudio/FNA3D/…) that can cost many minutes of wall-clock for near-zero added signal. For runtime/library changes, build the individual csprojs instead: `MonoGameGum.Tests` (covers `GumCommon` + `MonoGameGum`), plus `KniGum`, `RaylibGum`, and `SkiaGum`/`SkiaGum.Wpf` as relevant (none need submodules). `FnaGum` is `XNALIKE` — the same compile family as MonoGame/KNI, so if those build it almost certainly does — and Sokol is experimental; neither justifies a submodule clone for a typical `GumCommon` change. Build `AllLibraries.sln` (after the submodules are initialized) only when the change genuinely spans FNA/Sokol.

**Running focused tool unit tests (`GumToolUnitTests`).** Building this project triggers the plugin projects' post-build copy, which uses `$(SolutionDir)`. To run the csproj directly, supply it — with **backslashes** (forward slashes break the `copy`/`md` steps):

```
dotnet test Tool/Tests/GumToolUnitTests/GumToolUnitTests.csproj -p:SolutionDir='C:\path\to\repo\' --filter "ClassName"
```

If the Gum tool is **running**, it locks `Gum/bin/Debug/Plugins/*` and that copy fails with "Access denied". Add `-p:BuildProjectReferences=false` to run the tests against the already-built `Gum.dll` without re-copying plugins (rebuild tool source separately first if you changed it). This avoids having to close the user's running tool.

## Code Style

See `.claude/code-style.md` for all code style rules. Read that file before writing or editing any code.

## Static Singletons in the Tool

The Gum tool has been progressively migrated to constructor-injected services (`ISelectedState`, `IDialogService`, `IUndoManager`, `PluginManager`, etc.). When editing tool code, prefer the injected service over the static singleton if both exist (e.g. use the injected `_pluginManager` rather than `PluginManager.Self`).

**Load the `refactoring-direction` skill before any refactoring** (and before draining a singleton). It owns the static-singleton rules: drain a blocking singleton on the spot — in the same PR — rather than deferring or asking about phase timing; how to break the DI construction cycle the `Self`+`Initialize`+`Locator` pattern hides (inject the back-edge as `Lazy<T>`); and the **sanctioned exceptions that must never be drained** — `ObjectFinder.Self` and the `RenderingLibrary`/`InputLibrary` runtime singletons (`Renderer.Self`, `Cursor.Self`, etc.).

When draining a **plugin** ctor (`Locator`/`.Self` → `[ImportingConstructor]`), `AllPluginsCompositionTests` guards that every plugin still composes via MEF. If the drain bridges a *new* service in `PluginManager.LoadPlugins`, mirror that type into `PluginBridgedServiceTypes.All` or the test goes red — see the `gum-tool-plugins` skill.

## Searching C# Code

There is **no Roslyn/LSP semantic search in this environment** — the local LSP plugin was unreliable (slow workspace-load races on this large repo) and has been disabled. Use `Grep` for searches, and delegate broad "who references/calls/implements this" sweeps to the `Explore` agent, which reads excerpts and can distinguish definitions from uses.

Because grep matches **text, not symbols**, classify hits before drawing conclusions — a name match doesn't tell you whether it's a definition, a registered service, a plugin, a view, or a converter. Trusting raw match counts has produced wrong conclusions before (e.g. naming a class as a refactor target when it had zero *real* call sites). Read the surrounding code at each hit, and distinguish declarations (`class Foo`, the `Foo(` ctor, `: Foo` bases) from usages, rather than counting matches.

## Investigating Third-Party Libraries

**Never decompile DLLs or NuGet assemblies** (no `dotnet-ildasm`, `ilspycmd`, ILSpy, dnSpy, etc.) to inspect third-party code. If you need to know the API surface of a library:
1. Check the library's GitHub repo or published docs.
2. Read how Gum already calls it (the call sites in this repo are usually enough).
3. Ask the user.

Decompilation is a last resort and requires explicit user permission.
