---
name: frb-build-verification
description: Verify FlatRedBall (FRB1) still compiles after editing Gum source it shares. Triggers: changes under GumCommon/ or MonoGameGum/Forms/, projitems sync, the FRB compile constant, CS0246/CS0103 from FRB. Only when a FlatRedBall checkout sits beside the Gum repo.
---

# FRB Build Verification

FRB1 (FlatRedBall) compiles Gum **source** (not the DLLs) under a `net6.0` target with the `FRB` constant defined, via shared `.projitems`. A change that builds fine in the Gum solutions can still break FRB1 — so when you touch shared source, build an FRB canary.

## What FRB1 pulls in, and what to keep in sync when you touch it

FRB1 consumes Gum source via two shared-source imports — update the matching one in the same change whenever you add, rename, move, or delete a `.cs` file:

- **`GumCommon/` or `MonoGameGum/`** → `GumCoreShared.projitems` (add a `<Compile Include="$(MSBuildThisFileDirectory)<relative\path>" />` line, alphabetically). **Exception:** `MonoGameGum/GueDeriving/*Runtime.cs` (SpriteRuntime, TextRuntime, etc.) is deliberately excluded — FRB1 generates its own runtime classes per project and never uses Gum's `GueDeriving` types. Cross-backend sharing for these files instead happens via `<Compile Include>`/`Link` in `RaylibGum.csproj`/`SokolGum.csproj`.
- **`MonoGameGum/Forms/` (esp. `Controls/`)** → `FlatRedBall.Forms.Shared.projitems` in the **FlatRedBall sibling repo** (typically `C:\Users\vchel\Documents\GitHub\FlatRedBall\`), one `<Compile Include>`+`<Link>` pair per file. A file split (e.g. extracting an enum into its own file) needs an entry added even though the original file's entry still resolves.

If a shared `.cs` file gains a `using` that resolves through a NuGet package, add the matching `<PackageReference>` (pinned to the same version as `GumCommon.csproj`/`MonoGameGum.csproj`) to every FRB1-side csproj that imports `GumCoreShared.projitems` (grep to find them if unsure — currently `GumCore.DesktopGlNet6`, `GumCore.FNA`, `GumCore.Kni.DesktopGL`, `GumCore.Kni.Web`, `GumCoreAndroid`, `GumCoreiOS`).

## Multi-target gating — FRB1 still targets net6.0

`GumCommon` targets `net8.0`, but FRB1 multi-targets the same shared source down to `net6.0`. A BCL API added after net6.0 compiles fine in `GumCommon` and breaks FRB1 silently. Gate it: `#if NET7_0_OR_GREATER`/`#if NET8_0_OR_GREATER` around the `using` and the implementation, with a `NotSupportedException` fallback for older targets so the public signature stays stable everywhere. `ToolsUtilities/ToolsUtilitiesStandard.csproj` and `GumDataTypes/GumDataTypesNet6.csproj` also multi-target `netstandard2.0` for the same shared files — net8.0 isn't the floor there either; build those two csproj directly to catch it (neither is in `GumFull.sln`/`AllLibraries.sln`).

## Guard symmetry — `#if !FRB` members can't be called from unguarded shared code

The most common way a Gum change silently breaks FRB1: a member behind `#if !FRB` (e.g. on `TextRuntime`, which doesn't exist under FRB) gets called from shared, unguarded code. The non-FRB build stays green, so the break is invisible until this skill's canary runs. Whenever you add or move a member behind any platform `#if` (`!FRB`, `!RAYLIB`, `XNALIKE`, etc.), grep every call site — if any lives in unguarded shared source, guard the call site too or provide a same-named shim for the excluded platform (an FRB-only extension method on `GraphicalUiElement` is the established pattern — see `CustomSetPropertyOnRenderable.cs`).

## Precondition — skip entirely if absent

This is only doable when a **FlatRedBall checkout exists as a sibling of the Gum repo** (i.e. `<gum-repo>/../FlatRedBall/`). The cross-repo csproj imports are sibling-relative (`..\..\..\..\FlatRedBall\…`), so without that layout the build can't resolve. If the sibling is absent, **skip FRB verification and say so** — it is not a failure; the maintainer/CI covers it. Do not hardcode an absolute path; check for the sibling.

Check the sibling's checked-out branch before trusting the canary — `git status`/`git branch --show-current` there. A stale or already-merged local branch doesn't error, it just silently compiles against old FRB-side source, so a clean canary result proves nothing. The sibling's default branch is `origin/NetStandard`, not `main`/`master`.

## Worktree must be a sibling of Gum, not nested under `.claude/worktrees/`

The sibling-relative imports (`..\..\..\..\FlatRedBall\…`) are computed from the csproj's location. A git worktree is a full checkout, so its internal directory structure mirrors the primary checkout at the same depth — the import resolves correctly as long as the worktree root itself sits at the same level as `Gum\` and `FlatRedBall\` (i.e. directly under the parent `GitHub\` folder). A worktree nested under `.claude/worktrees/<branch>/` is one level too deep, so the import resolves to nowhere (`MSB4019`).

Create issue worktrees as a sibling of the Gum repo (e.g. `<gum-repo>/../gum-wt-<branch>/`), not under `.claude/worktrees/`.

## Canaries

Pick by what you changed. **The "Lives in" column is the repo containing the `.csproj` file itself** — don't go hunting for it in the other repo. Both rows still need the FlatRedBall sibling present (row 1's target also pulls in some FlatRedBall-side `Embedded\*.cs` files), but only row 2's `.csproj` is physically located there.

| Changed source | Lives in | Build target (relative to that repo's root) | Covers |
|---|---|---|---|
| `GumCommon/` (anything in `GumCoreShared.projitems`) | **Gum repo** (this repo) | `GumCore/GumCoreXnaPc/GumCore.DesktopGlNet6/GumCore.DesktopGlNet6.csproj` | GumCommon shared into FRB |
| `MonoGameGum/Forms/` (in `FlatRedBall.Forms.Shared.projitems`) | **FlatRedBall sibling repo** | `Engines/Forms/FlatRedBall.Forms/FlatRedBall.Forms.DesktopGlNet6/FlatRedBall.Forms.DesktopGlNet6.csproj` | Forms shared into FRB |
| Types referenced by `Gum/SvgPlugin/SkiaInGumShared/` (e.g. renames/moves in `Runtimes/GumShapes/`, `Runtimes/SkiaGum/`) | **Gum repo** (this repo) | `Gum/SvgPlugin/SkiaInGumShared/SkiaInGum.csproj` | Legacy FRB Skia-adapter layer — references `GumCore.DesktopGlNet6.csproj` directly, so it also needs the FlatRedBall sibling even though its own `.csproj` lives here |

`GueDeriving/*Runtime`, `MonoGameGum/Renderables/`, `MonoGameGum/ExtensionMethods/`, `MonoGameGum/Input/` (Cursor, Keyboard, gamepad drivers), and the `Forms/DefaultVisuals/` runtimes are **not** compiled by FRB1 (it generates its own) — changing only those needs no FRB build.

**Don't infer this from the directory path.** `FormsUtilities.cs` lives under `MonoGameGum/Forms/` but is not `Include`d in `FlatRedBall.Forms.Shared.projitems`, so it needs no canary despite the directory match — even though it has its own live `#if FRB` branches (those serve other consumers of the file, not FRB). The inverse also happens: `Input/CursorExtensions.cs` lives under the normally-not-compiled `MonoGameGum/Input/`, but is `Include`d in `FlatRedBall.Forms.Shared.projitems` (it carries the `GetEventFailureReason` diagnostic, which Forms uses), so editing it *does* need the Forms canary. `grep` the exact filename against the projitems file in the table above before requiring a canary; if it's not `Include`d there, FRB doesn't compile it.

## Interpreting results — baseline first

`main` is sometimes already red under FRB (e.g. a shared file gained a member that lives behind `#if !FRB`, or the FRB-side projitems is out of sync). So **build the base branch first to capture the baseline errors, then build your branch and attribute only the *new* errors**. A pre-existing error that survives on both is not yours to fix here (flag it separately).

Common genuinely-new breaks: a shared method/property guarded by `#if !FRB` referenced from un-guarded code (`CS0103`/`CS0246`), a `.cs` file added/renamed/deleted without updating the FRB-side projitems, or a `net7.0+` BCL API used in shared source without an `#if NET7_0_OR_GREATER` gate (FRB multi-targets down to `net6.0`).
