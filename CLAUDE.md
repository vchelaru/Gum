# Gum Repository Guidelines

## What Is This?

This project (Gum) provides UI solutions for game developers using C#. It includes:
* A Common library which can run in any C# environment for layout and some UI control logic
* Runtime libraries for various platforms including MonoGame, KNI, and FNA. Also SkiaSharp and raylib.
* A tool also called Gum or Gum UI or Gum UI tool which is a WYSIWYG editor for game UI

## Project Direction

High-level direction for Gum — vision, roadmap, open strategic questions, and decision records (ADRs) — lives in `Direction/` at the repo root. **For any discussion of Gum's high-level goals, roadmap, or strategic decisions, read `Direction/README.md` first**, then the specific file for the topic. This is the strategy layer (*what* and *why*); it is separate from the operational guidance in this file and in the skills/agents (*how*), and from the published user-facing docs in `docs/`.

**The Gum tool is the Avalonia head (`Tool/Gum.Avalonia`); WPF (`Gum/`, `Gum.Wpf.sln`) is frozen and no longer ships.** Per ADR-0017 (`Direction/decisions/0017-commit-to-avalonia-full-cutover.md`) the release packages only the Avalonia head, for Windows, macOS and Linux; the last WPF release was September 2, 2026. No new tool features or refactors go into the WPF head. On an ambiguous task ("implement this feature," "fix this bug" with no head named), assume Avalonia. Touch WPF only when the user explicitly names it. Logic shared by both heads lives in `Tools/Gum.Presentation` and the `Tool/*.Core` projects; a change there must keep `Gum.Wpf.sln` building (CI builds it on Windows). Deleting the WPF projects is the remaining half of phase 120 (`Direction/avalonia-migration/phase-120-cutover.md`), a separate PR; until it lands both heads exist in the repo, but only one gets new work.

## Agent Workflow

For every task, read the guidelines in the matching agent file under `.claude/agents/` and follow them yourself, in your own context, before proceeding — do not dispatch an actual subagent for this by default. Spinning up a subagent costs real tokens: a fresh agent has none of the context already built up in this conversation and has to independently re-read files, re-run builds, and re-explore the codebase to reconstruct it, on top of whatever the task itself costs. Reserve `Agent`-tool dispatch for two cases only: **(1)** the work is genuinely parallelizable (independent pieces that can run concurrently), or **(2)** the user explicitly asks for delegation/a subagent.

**Re-read the agent file at the start of each new task — not once per session.** Long sessions drift; reloading the discipline keeps it active. Inline coding without re-reading the agent file first is not an option, even for "small" follow-ups in the same conversation.

Also load any skill whose trigger matches the area you're working in — before reading code, designing a fix, or making changes. "I'm only investigating, not editing yet" is not a reason to skip; the skill exists to inform the investigation, not just the keystrokes. The only time it's acceptable to skip is for a trivial single-file lookup that won't influence any recommendation or change.

**This check re-runs per new file path, not once per task.** Matching skills against the files identified at task start (e.g. the source file a bug lives in) does not cover a different file touched later in the same task — a sample or test project pulled in only for manual verification, a config file edited in passing, a doc updated alongside. Each new file gets its own trigger-match against the skill list before it's edited, even when the task's "main" skill is already loaded.

**It also re-runs on a task-type shift, not just a new file path.** Moving from implementing/verifying into diagnosing an unexpected result is its own trigger — re-scan the skill list before investigating.

Available agents:
- **refactoring-specialist** — Refactoring and improving code structure
- **docs-writer** — Writing or updating documentation
- **product-manager** — Breaking down tasks and tracking progress
- **security-auditor** — Security reviews and vulnerability assessments

Select the agent that best matches the task at hand. For tasks that span multiple concerns (e.g., implement a feature and write tests), invoke the relevant agents in sequence.

General implementation work (new features, bug fixes, unit tests) has no dedicated agent file — follow this file and whatever skills its triggers pull in directly (`tdd` for test discipline, `code-style.md` for style, `refactoring-direction` before touching a static singleton, etc.).

**Work in a fresh worktree, not the primary checkout**, unless the user explicitly says to work in place — the current branch may already have unrelated in-progress work on it.

**Boyscout principle:** while you're already reading a method or file for the task at hand, fix compiler warnings, dead code, and small inconsistencies you notice along the way — the context is already loaded, so it's cheap now and expensive later. Call out what you bundled in your final notes. Don't restructure classes or chase warnings into unrelated files as drive-by work.

**Reviewing changes before merge is not a dedicated agent.** Use the `/code-review` skill at **low or medium effort** (e.g. `Skill({skill: "code-review", args: "medium"})`) for routine pre-commit review — it covers correctness bugs *and* quality/refactoring cleanups in one pass, inline, no subagents. The coder writes its own unit tests; the `tdd` skill owns test discipline and the testability gate.

**Do not invoke `/code-review` bare (no `args`) and do not self-route to the workflow-backed/"ultra" review.** Invoking the skill with no `args` has been observed to respond by *telling you* to call `Workflow({name: "code-review", args: "high"})` — a fan-out of ~10+ subagents that can burn 500k+ tokens per run. That response is the skill's own suggestion, not user opt-in, and following it anyway is the exact mistake that burned ~590k tokens twice on 2026-07-09 (issues #3581 and #3586) before the user caught it and asked for this rule to be written down. The Workflow-backed/"ultra" path is only for when the user explicitly asked for it in that turn — "ultracode" in their message, ultracode on for the session, or the user directly asking for a multi-agent/deep/ultra review — never as your own default for a pre-commit check, and never just because the skill's own output recommended it. If a change seems to genuinely warrant the heavier pass, ask the user first instead of routing to it yourself.

## Improving Guidance Files Alongside Work

When a task surfaces an improvement to a checked-in guidance file — a skill (`.claude/skills/`), an agent (`.claude/agents/`), `CLAUDE.md`, or `code-style.md` — include that change in the **same PR** as the work that motivated it. Do not ask whether it is okay, and do not split it into a separate PR. These files are shared repo artifacts, and bundling keeps the improvement next to the change that prompted it (and its rationale). The most common case: you hit a confusing pattern or recurring mistake, fix it, and add a rule to the relevant skill so it does not recur — both belong in one PR.

Edit these files **in the worktree**, never the primary checkout — an edit in the primary checkout never reaches the branch, and the PR ships without it.

## Building and Testing

**Pre-push verification is `pwsh Tools/verify.ps1`, nothing more.** It runs only the test classes the branch added or changed, builds the changed source projects, and reports errors, test summaries, and warnings on changed lines. Do not run whole test projects, `Gum.Wpf.sln`, every runtime, or the FRB canary locally as a routine pass; CI runs that matrix on every push and is the gate. Build a specific extra target only when the change gives a concrete reason (e.g. a new `#if FRB` member reached from shared code).

**A red CI check on an open PR is diagnosed and fixed, not rerun-and-walked-away-from — even when the failing job looks unrelated to your diff.** Read the job log first: a genuine infra flake (a download's 5xx/timeout, a runner provisioning error) shows that signature and a rerun is the fix; a test assertion or exception is a real bug and gets fixed in the same PR — or filed as its own issue if genuinely out of scope, per "Everything is either fixed or filed" in the personal issue-workflow doc — before moving on. Rerunning a real test failure without fixing it just hands the same flake to the next PR.

Pick the right build target based on what you're working on:

* **Tool work — the Avalonia head is the tool** (`Tool/Gum.Avalonia`, see Project Direction above). `dotnet build Gum.slnx` builds the head, its plugins and `Gum.Avalonia.Tests`; `dotnet build Tool/Gum.Avalonia/Gum.Avalonia.csproj` builds the head alone. The head is plain `net10.0` with no WPF/WinForms anywhere in its reference graph (`Direction/avalonia-migration/`); the neutral plugin projects (`Gum/ConvertToJsonPlugin`, `EventOutputPlugin`, `GumFormsPlugin`, `ImportFromGumxPlugin`, `PerformanceMeasurementPlugin`, `SvgPlugin/SkiaPlugin`) copy themselves into the head's `Plugins/` folder by post-build and fall back to a repo-relative path when `$(SolutionDir)` is empty, so building them by csproj is fine. Tests: `dotnet test Tests/Gum.Presentation.Tests` covers the shared tool logic (`Tools/Gum.Presentation`, `DataUi.Core`, the `Tool/*.Core` plugin cores, the neutral plugins) and `dotnet test Tests/Gum.Avalonia.Tests` covers the head (headless, runs on any OS); new tool tests go in one of those two, never in `GumToolUnitTests`. The head composes `AddGumCore()` from `Gum.Presentation` plus `AddGumAvalonia()`; a service that only the WPF head implements must be added to `HeadProvidedContracts` and given an Avalonia implementation, or the head fails to compose. It may be launched unattended for verification: `dotnet run --project Tool/Gum.Avalonia -- --exit-after 8 --screenshot <path>.png` (this is the one GUI launch that is allowed; it exits by itself). Add a project path to open it, `--select Element[#Instance]` to select something once it loads, `--theme light|dark` to show a theme variant without saving it, and **always** `--user-data <temp folder>` so the run's last-project/recent-list writes stay out of the user's real settings (`%APPDATA%\Gum`, the same folder the WPF head used, so recent projects carry over). Opening a project can re-save it (legacy version upgrade), so point it at a copy, never at `Samples/`. Comparing the two heads side by side (screenshot driver, UI Automation probes, Linux run recipe) is `Tools/ParityShots/README.md`.
* **Frozen WPF head (`Gum/`, `Gum.Wpf.sln`; `GumFull.sln` is the WPF tool plus the CLI)** — only when the task explicitly targets WPF. Its plugin projects (`EditorTabPlugin_XNA`, `TextureCoordinateSelectionPlugin`, `StateAnimationPlugin`, `CodeOutputPlugin`) use `$(SolutionDir)` in post-build scripts, which is undefined when building a `.csproj` directly, so build them via the solution or plugin output silently breaks. Its tests are `Tool/Tests/GumToolUnitTests` (see below). CI builds `Gum.Wpf.sln` on Windows, so a change to shared code must keep it green even though it no longer ships.
* **Runtime/library work (`AllLibraries.sln` OR individual csprojs)** — runtime projects (`GumCommon`, `MonoGameGum`, `KniGum`, `FnaGum`, `SkiaGum`, `RaylibGum`, and their test projects including `MonoGameGum.Tests`) have no `$(SolutionDir)`-dependent post-builds. Building the relevant individual `.csproj` is fine and is usually faster than building the whole solution. Use `AllLibraries.sln` when a change spans many runtime projects or you want a single command to verify them all.

Examples:
* Tool build: `dotnet build Gum.slnx`
* Tool test (focused): `dotnet test Tests/Gum.Presentation.Tests/Gum.Presentation.Tests.csproj --filter "TestClassName"`
* Runtime test (focused): `dotnet test MonoGameGum.Tests/MonoGameGum.Tests.csproj --filter "TestClassName"`
* Runtime build (broad): `dotnet build AllLibraries.sln`

If a runtime change is in `GumCommon` and you've already built `MonoGameGum.Tests`, that pulls in `GumCommon` and `MonoGameGum` transitively — no need to also build the solution.

**Don't initialize the FNA submodule — or build `AllLibraries.sln`, which pulls it in — unless your change actually touches FNA code.** `fna` is the repo's only submodule, and a fresh clone/worktree leaves it uninitialized; initializing it triggers a large recursive clone (FNA → SDL/FAudio/FNA3D/…) that can cost many minutes of wall-clock for near-zero added signal. For runtime/library changes, build the individual csprojs instead: `MonoGameGum.Tests` (covers `GumCommon` + `MonoGameGum`), plus `KniGum`, `RaylibGum`, and `SkiaGum`/`SkiaGum.Wpf` as relevant (none need submodules). `FnaGum` is `XNALIKE` — the same compile family as MonoGame/KNI, so if those build it almost certainly does — which rarely justifies a submodule clone for a typical `GumCommon` change. Build `AllLibraries.sln` (after `fna` is initialized) only when the change genuinely spans FNA.

**SokolGum is in no solution and is not built by CI.** Sokol.NET is not a submodule, so `Runtimes/Sokol`, `Runtimes/SokolGum`, `Tests/SokolGum.Tests`, and `Samples/SokolGum*` build only after cloning it by hand — see `Runtimes/SokolGum/README.md`. A sweep across every runtime should still update SokolGum's source, but report the Sokol side as unverified rather than claiming it builds.

**Zero new warnings** after every change — verify via the build output; suppress with a comment only when unavoidable. **Never launch Visual Studio, a sample `.exe`, `dotnet run`, or any GUI app** — verify with `dotnet build`/`dotnet test` only, manual/visual testing is the user's step.

**Animation editor work: dogfood it headlessly.** `Tests/Gum.Avalonia.Tests/Animations/README.md` describes the harness that drives the real Animations tab with simulated input (clicks, drags, keys, scripted dialogs, pixel reads) and the find-a-bug, pin-it, fix-it loop built on it. Read it before changing the animation editor; it covers only that tab.

**Running focused WPF-head unit tests (`GumToolUnitTests`, frozen head only).** Building this project triggers the WPF plugin projects' post-build copy, which uses `$(SolutionDir)`. To run the csproj directly, supply it — with **backslashes** (forward slashes break the `copy`/`md` steps):

```
dotnet test Tool/Tests/GumToolUnitTests/GumToolUnitTests.csproj -p:SolutionDir='C:\path\to\repo\' --filter "ClassName"
```

If the Gum tool is **running**, it locks `Gum/bin/Debug/Plugins/*` and that copy fails with "Access denied". Add `-p:BuildProjectReferences=false` to run the tests against the already-built `Gum.dll` without re-copying plugins (rebuild tool source separately first if you changed it). This avoids having to close the user's running tool.

## Code Style

See `.claude/code-style.md` for all code style rules. Read that file before writing or editing any code.

## Static Singletons in the Tool

The Gum tool has been progressively migrated to constructor-injected services (`ISelectedState`, `IDialogService`, `IUndoManager`, `PluginManager`, etc.). When editing tool code, prefer the injected service over the static singleton if both exist (e.g. use the injected `_pluginManager` rather than `PluginManager.Self`).

**Load the `refactoring-direction` skill before any refactoring** (and before draining a singleton). It owns the static-singleton rules: drain a blocking singleton on the spot — in the same PR — rather than deferring or asking about phase timing; how to break the DI construction cycle the `Self`+`Initialize`+`Locator` pattern hides (inject the back-edge as `Lazy<T>`); and the **sanctioned exceptions that must never be drained** — `ObjectFinder.Self` and the `RenderingLibrary`/`InputLibrary` runtime singletons (`Renderer.Self`, `Cursor.Self`, etc.).

When draining a **plugin** ctor (`Locator`/`.Self` → `[ImportingConstructor]`), `AllPluginsCompositionTests` guards that every plugin still composes via MEF. If the drain bridges a *new* core service in `PluginManager.AddCoreExports`, mirror that type into `PluginBridgedServiceTypes.All` or the test goes red; a head-only service goes in that head's `IPluginHostConfiguration.AddHeadExports` instead — see the `gum-tool-plugins` skill.

## Searching C# Code

There is **no Roslyn/LSP semantic search in this environment** — the local LSP plugin was unreliable (slow workspace-load races on this large repo) and has been disabled. Use `Grep` for searches, and delegate broad "who references/calls/implements this" sweeps to the `Explore` agent, which reads excerpts and can distinguish definitions from uses.

Because grep matches **text, not symbols**, classify hits before drawing conclusions — a name match doesn't tell you whether it's a definition, a registered service, a plugin, a view, or a converter. Trusting raw match counts has produced wrong conclusions before (e.g. naming a class as a refactor target when it had zero *real* call sites). Read the surrounding code at each hit, and distinguish declarations (`class Foo`, the `Foo(` ctor, `: Foo` bases) from usages, rather than counting matches.

## Investigating Third-Party Libraries

**Never decompile DLLs or NuGet assemblies** (no `dotnet-ildasm`, `ilspycmd`, ILSpy, dnSpy, etc.) to inspect third-party code. If you need to know the API surface of a library:
1. Check the library's GitHub repo or published docs.
2. Read how Gum already calls it (the call sites in this repo are usually enough).
3. Ask the user.

Decompilation is a last resort and requires explicit user permission.
