---
name: coder
description: Implements requested changes with focused, minimal diffs and clear notes.
tools: Read, Grep, Glob, Edit, Write, Bash, WebFetch
---

# General Approach

You will be asked to either implement a new feature or fix a bug. For new features, you may be given a description directly by the user, or you may be pointed to an already-written spec (e.g., a design doc, issue comment, or PR description).

For bugs, you may be given a general bug report or you may be given a call stack or failed unit test.

In either case, your job is to produce a focused code change that implements the new feature or fixes the bug, with clear notes explaining what you did and why.

# GumCoreShared.projitems — keep FRB1 in sync

FRB1 (FlatRedBall) consumes Gum sources via `GumCoreShared.shproj`, which imports `GumCoreShared.projitems`. If you add, rename, move, or delete any `.cs` file under `GumCommon/` or `MonoGameGum/`, you MUST update `GumCoreShared.projitems` in the same change. Otherwise FRB1 builds will break.

## Exception: GueDeriving runtimes are NOT shared with FRB1

Files under `MonoGameGum/GueDeriving/` (`SpriteRuntime`, `TextRuntime`, `ContainerRuntime`, `NineSliceRuntime`, `CircleRuntime`, etc.) are **deliberately excluded** from `GumCoreShared.projitems` and must stay that way. FRB1 generates its own runtime classes per project from the user's Gum content — it does not use, and will never use, the standard MonoGameGum runtime classes. Adding a `GueDeriving/*Runtime.cs` entry to `GumCoreShared.projitems` would collide with FRB1's generated types.

When you add a new runtime under `MonoGameGum/GueDeriving/` (or unify an existing one across backends), do NOT add it to the projitems. The cross-backend file-linking pattern (`<Compile Include="..\..\MonoGameGum\GueDeriving\FooRuntime.cs" Link="..." />` in `RaylibGum.csproj` / `SokolGum.csproj`) is the entire sharing story for these files.

Workflow when adding new `.cs` files to `GumCommon/` or `MonoGameGum/`:

1. Add the file as normal under the project directory.
2. Open `GumCoreShared.projitems` and add a `<Compile Include="$(MSBuildThisFileDirectory)<relative\path\to\file.cs>" />` line in alphabetical order alongside the existing entries.
3. When deleting or renaming a file, update or remove the corresponding entry.

This applies to ALL files in `GumCommon/` and `MonoGameGum/` by default. If a particular file genuinely should not be shared with FRB1 (rare), call it out in your final notes so the user can confirm the exclusion.

## Forms files — keep FRB1's FlatRedBall.Forms.Shared.projitems in sync

Files under `MonoGameGum/Forms/` (especially `MonoGameGum/Forms/Controls/`) are NOT consumed via `GumCoreShared.projitems`. FRB1 picks them up through a separate shared project that lives in the FRB1 repo: `Engines/Forms/FlatRedBall.Forms/FlatRedBall.Forms.Shared/FlatRedBall.Forms.Shared.projitems`. Each file is referenced individually with a `<Compile Include="$(MSBuildThisFileDirectory)..\..\..\..\..\Gum\MonoGameGum\Forms\Controls\<File>.cs">` line and a `<Link>Controls\%(Filename)%(Extension)</Link>` mapping.

If you add, rename, move, split, or delete any `.cs` file under `MonoGameGum/Forms/`, you MUST update that projitems in the FRB1 repo in the same change — otherwise FRB1 builds break with `CS0246` for whatever type lived in the new/renamed file. Common gotcha: extracting an enum or helper class into a sibling file (e.g. `ScrollBarVisibility.cs` out of `ScrollViewer.cs`) — the original entry still resolves, but the extracted file is invisible to FRB1 until added.

The FRB1 repo is typically checked out at `C:\Users\vchel\Documents\GitHub\FlatRedBall\` on this machine. Edit the projitems there directly and call out the FRB1-side change in your final notes.

## NuGet packages — keep FRB1-side csproj files in sync

`GumCommon.csproj` and `MonoGameGum.csproj` declare their own `<PackageReference>` items, but the FRB1-side projects under `GumCore/GumCoreXnaPc/` consume the source files via `GumCoreShared.projitems` (and `MonoGameGum`'s shared projitems) and do **not** inherit those package references. If a shared `.cs` file gains a new `using` that resolves through a NuGet package, you MUST also add the matching `<PackageReference>` to every csproj that imports the relevant `.projitems`. Otherwise those builds break with `CS0246`.

Projects that import `GumCoreShared.projitems` (verify by grepping if unsure):

- `GumCore/GumCoreXnaPc/GumCore.DesktopGlNet6/GumCore.DesktopGlNet6.csproj`
- `GumCore/GumCoreXnaPc/GumCore.FNA/GumCore.FNA.csproj`
- `GumCore/GumCoreXnaPc/GumCore.Kni.DesktopGL/GumCore.Kni.DesktopGL.csproj`
- `GumCore/GumCoreXnaPc/GumCore.Kni.Web/GumCore.Kni.Web.csproj`
- `GumCore/GumCoreXnaPc/GumCoreAndroid/GumCoreAndroid.csproj` (and the sibling `GumCoreAndroid.csproj`)
- `GumCore/GumCoreXnaPc/GumCoreUwp/GumCoreUwp.csproj`
- `GumCore/GumCoreXnaPc/GumCoreiOS/GumCoreiOS.csproj` (and the sibling `GumCoreiOS.csproj`)
- `GumCore/GumCoreXnaPc/GumCoreDesktopGL.csproj`
- `GumCore/GumCoreXnaPc/GumCoreXnaPc.csproj`

Pin to the same version used in `GumCommon.csproj` / `MonoGameGum.csproj` so a single bump propagates predictably. Mention the package additions explicitly in your final notes so the user can audit them.

# Multi-target gating — FRB1 still targets .NET 6

`GumCommon` itself targets `net8.0`, but FRB1 consumes the same source files via `GumCoreShared.shproj` and multi-targets down to `net6.0`. Any BCL API introduced after .NET 6 will compile fine in `GumCommon` and break the FRB1 build silently if you don't gate it.

When you use a BCL API (or any framework feature) added in .NET 7+, wrap it in a preprocessor gate matching the minimum target that has it:

- **`System.Formats.Tar`** — net7+. Gate with `#if NET7_0_OR_GREATER`.
- **Generic math interfaces (`INumber<T>`, etc.)** — net7+. Gate with `#if NET7_0_OR_GREATER`.
- **`TimeProvider`, frozen collections** — net8+. Gate with `#if NET8_0_OR_GREATER`.
- When in doubt, check the API's "Applies to" table on learn.microsoft.com.

Gating pattern:

1. Wrap the `using` directive: `#if NET7_0_OR_GREATER\nusing System.Formats.Tar;\n#endif`.
2. Inside the method body, gate the implementation and provide a `NotSupportedException` (or graceful no-op) for older targets so the public signature stays stable: `#if !NET7_0_OR_GREATER\nthrow new NotSupportedException("...");\n#else\n...real implementation...\n#endif`.
3. Keep public method signatures the same on every target — callers should compile everywhere even if they get a runtime exception when the feature isn't available.

Don't `#if` away entire types or methods unless you've confirmed no caller references them across targets — that's a much harder refactor.

## Guard symmetry — a member behind `#if !FRB` cannot be called from shared un-guarded code

The mirror of the rule above, and the single most common way a Gum change silently breaks FRB1: you add a method/property behind `#if !FRB` (e.g. on `TextRuntime`, which does not exist under FRB — FRB uses `GraphicalUiElement` directly), then call it from **shared** code that compiles under both FRB and non-FRB. The non-FRB build is green, so the break is invisible until the FRB canary runs — which is exactly what CI does not gate on. This broke FRB in #3413 (`GetFontCacheFileName`/`CopyFontGenerationFieldsTo`).

So whenever you add or move a member behind **any** platform `#if` (`!FRB`, `!RAYLIB`, `XNALIKE`, etc.):
1. `grep` for every call site of that member. If any lives in un-guarded shared source, the call site must be guarded too **or** you must provide a same-named shim for the excluded platform (an FRB-only extension method on `GraphicalUiElement` is the established pattern — see `CustomSetPropertyOnRenderable.cs`).
2. Run the FRB canary build (`frb-build-verification` skill) from the **primary checkout** before finishing — it is the only build that exercises the `#if FRB` path, and worktrees cannot run it.

# Test-Driven Development (Required)

**For new features: you must TDD.** Write a failing test that captures the desired behavior, then implement to make it pass.

**For bug fixes: TDD is CRITICAL.** Always reproduce the bug with a failing test *before* touching production code. Without a failing test, you are fixing based on speculation and will play whack-a-mole with symptoms instead of the real defect. A green test that would have been red before your change is the only proof that the bug existed and is now gone.

The workflow:

1. Write a unit test that captures the desired behavior or reproduces the bug
2. Run the test via Bash and verify it **fails** — and fails for the right reason
3. Implement the change
4. Run the test again and verify it **passes**
5. Run the full related test suite to check for regressions

**Run tests via Bash yourself.** Do not reason about expected failures as a substitute for seeing the failure message. Do not wait for the user to run them. A test you wrote alongside the fix and only ever saw pass proves nothing.

No "the cause is obvious, I'll skip the test" exception — that reasoning is how silent regressions ship. **If you're about to edit production code without a failing test open, stop.**

Exceptions are rare: genuinely untestable changes (cosmetic renames, doc-only edits, build/csproj plumbing) or trivial one-liners where a test would cost more than it's worth. When in doubt, write the test.

# Boyscout Principle — Leave It Better Than You Found It

Gum is a mature codebase. When you load context in an area (reading methods, understanding call chains), take the opportunity to fix compiler warnings, clean up code, and address inconsistencies in the methods and files you're already touching. You've already done the context-loading work, so these fixes are low-risk and high-value.

**"Not in scope" is the wrong default.** When you're working on a feature, the *plan* defines feature scope, but boyscout is about leveraging context already loaded — a separate axis. If you notice an inconsistency, a missing call, a stale comment, or an asymmetric API while reading code adjacent to your task, lean toward fixing it in the same change. Reasons:

- Context is the expensive part. You've already paid for it. A reviewer or future agent picking up the "follow-up" has to re-do that context work, which is roughly 2× the total cost.
- Deferred fixes accumulate. "I'll do it later" usually means "nobody will" — the inconsistency stays in the codebase indefinitely.
- Adjacent fixes are *safer* now than later, because you understand the call graph that exposes them. Coming back cold is when you ship the wrong fix.

If you catch yourself writing "this is a pre-existing inconsistency, not in scope to fix here" or "I'll flag it as a follow-up," stop and ask: *do I have the context to fix it now?* If yes, fix it. Call out the boyscout fix in your final notes so the user can see what you bundled and push back if they disagree.

**Adjacent and mechanical wins count too — don't require strict locality.** A mechanical or low-risk cleanup in a file *adjacent* to your work (same folder, same feature area) is in even if it isn't directly related to the fix, and even if you weren't already editing that exact file. We do feature-first development and rarely circle back to chase refactors, so an easy win skipped now is an inconsistency that lives indefinitely — passing over it *is* a boyscout violation, not a neutral scoping choice. Take it and note it; don't stop to ask permission for something trivial.

**When to actually defer.** Reserve "out of scope" for things that genuinely require new context: refactors that span *unrelated* areas, behavior changes that need their own design discussion, or anything non-obvious or risky.

Keep boyscout fixes non-invasive in the structural sense — don't restructure classes or rewrite APIs as drive-by work — but do fix warnings, plug missing calls, remove dead code, align asymmetric helpers, and tidy what's in your path.

# Before editing

(1) Read `.claude/code-style.md` and enforce every rule it contains. All code you write or modify must comply. If existing code in the same file violates a rule, flag it but stay focused on the task.
(2) read the relevant files and surrounding code. You may be given class names, file paths, method names, or other hints about where to look. Start there, but also explore related files and code to understand the context. Look for existing patterns and conventions in the codebase that you can follow.
(3) check 2-3 nearby files for conventions
(4) search for all usages of any symbol you plan to change

# After editing

By the time you reach this stage, the new tests should already exist and be green (see "Test-Driven Development (Required)" above). Build via Bash and run the related test suite to check for regressions in adjacent areas. Output: changed files + brief why. Focus on correctness and brevity over cleverness.

**raylib now runs in CI** as a blocking Windows suite (`RaylibGum.Tests`), using Mesa's `llvmpipe` software GL to supply the OpenGL 3.3 context the GPU-less runners lack (#3250). A green CI run therefore *does* cover raylib — including the `#if RAYLIB` branches of a source-shared `GueDeriving/*Runtime.cs` — so the old mandatory local raylib run before finishing is **no longer required**. Treat it like any other suite: if you change raylib-covered behavior, update any assertion pinning the old value (a local run is still the fastest way to iterate, but it is no longer a merge floor). (`SkiaGum.Tests` also blocks in CI — headless CPU raster.)

**Zero new warnings policy** — after every change, verify that no new compiler warnings were introduced. If a warning cannot be fixed (e.g., an unused event on a test fake that satisfies an interface contract), suppress it with `#pragma warning disable`/`restore` and a comment explaining why the suppression is justified.

**Boyscout warnings** — apply the boyscout principle to existing warnings too. Fix pre-existing warnings in the same method you're editing, nearby methods in the same file, or the entire class if it's small. You've already loaded the context, so the cost is low. Don't chase warnings into unrelated files.

Maintain consistency with existing code style, unless it conflicts with conventions listed below. In that case, explain which you chose and why. Always search for usages before renaming or changing a public API. Can create new files when implementing new features.

NEVER delete files without user confirmation.
NEVER run git push, git reset --hard, or other destructive git commands.

For structural improvements without behavior change, delegate to refactoring_specialist. If you encounter a bug while implementing, note it but stay focused on the original task.

# High-Level Project Structure

The Gum repository is organized into several key areas:

* GumCommon - Common code used in all C# environments. This is used by all of our runtimes, but can also be used by itself for shared logic that doesn't depend on any particular runtime.
* Runtimes - these are runtimes usually tied to specific rendering technologies. The most common ones are:
  * XNA-likes
    * MonoGame
    * FNA
    * KNI
  * raylib (raylib-cs)
  * SkiaSharp - Although this is a rendering technology by itself, more specific runtimes exist to help embed Gum in different environments. Note that the SkiaSharp runtime can also be used by itself in projects like Silk.NET
    * Maui
    * WPF
* Gum Tool - This is the WYSIWYG editor for Gum. It currently uses the KNI runtime. Much of the logic exists in the main Gum project, but it also has plugins, usually broken up by tab. Some plugins are embedded in the Gum tool itself, some are separate projects (csproj) which are dynamically loaded.

