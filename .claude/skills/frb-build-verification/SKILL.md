---
name: frb-build-verification
description: Verify FlatRedBall (FRB1) still compiles after editing Gum source it shares. Triggers: changes under GumCommon/ or MonoGameGum/Forms/, projitems sync, the FRB compile constant, CS0246/CS0103 from FRB. Only when a FlatRedBall checkout sits beside the Gum repo.
---

# FRB Build Verification

FRB1 (FlatRedBall) compiles Gum **source** (not the DLLs) under a `net6.0` target with the `FRB` constant defined, via shared `.projitems`. A change that builds fine in the Gum solutions can still break FRB1 — so when you touch shared source, build an FRB canary.

The projitems-sync rules (which files FRB1 pulls, and what to update when you add/rename/delete) live in `.claude/agents/coder.md` — read that, don't duplicate it here. This skill is only about **running the build**.

## Precondition — skip entirely if absent

This is only doable when a **FlatRedBall checkout exists as a sibling of the Gum repo** (i.e. `<gum-repo>/../FlatRedBall/`). The cross-repo csproj imports are sibling-relative (`..\..\..\..\FlatRedBall\…`), so without that layout the build can't resolve. If the sibling is absent, **skip FRB verification and say so** — it is not a failure; the maintainer/CI covers it. Do not hardcode an absolute path; check for the sibling.

## Must run from the PRIMARY checkout, never a worktree

The sibling-relative imports are computed from the csproj location, and the FlatRedBall-side csprojs pull Gum source by relative path into the **primary** Gum checkout (`…\Gum\MonoGameGum\…`). A worktree under `.claude/worktrees/<branch>/` is both nested too deep (so `..\..\..\..\FlatRedBall` resolves to nothing — `MSB4019`) and not the path FRB compiles from. To FRB-verify a branch, **check that branch out in the primary Gum checkout** and build there. Worktrees cannot FRB-verify.

## Canaries

Pick by what you changed. Build from the primary Gum checkout (first row) / the sibling FlatRedBall repo (second row).

| Changed source | Build target | Covers |
|---|---|---|
| `GumCommon/` (anything in `GumCoreShared.projitems`) | `GumCore/GumCoreXnaPc/GumCore.DesktopGlNet6/GumCore.DesktopGlNet6.csproj` | GumCommon shared into FRB |
| `MonoGameGum/Forms/` (in `FlatRedBall.Forms.Shared.projitems`) | `<FlatRedBall>/Engines/Forms/FlatRedBall.Forms/FlatRedBall.Forms.DesktopGlNet6/FlatRedBall.Forms.DesktopGlNet6.csproj` | Forms shared into FRB |

`GueDeriving/*Runtime`, `MonoGameGum/Renderables/`, `MonoGameGum/ExtensionMethods/`, `MonoGameGum/Input/` (Cursor, Keyboard, gamepad drivers), and the `Forms/DefaultVisuals/` runtimes are **not** compiled by FRB1 (it generates its own) — changing only those needs no FRB build.

**Don't infer this from the directory path.** `FormsUtilities.cs` lives under `MonoGameGum/Forms/` but is not `Include`d in `FlatRedBall.Forms.Shared.projitems`, so it needs no canary despite the directory match — even though it has its own live `#if FRB` branches (those serve other consumers of the file, not FRB). `grep` the exact filename against the projitems file in the table above before requiring a canary; if it's not `Include`d there, FRB doesn't compile it.

## Interpreting results — baseline first

`main` is sometimes already red under FRB (e.g. a shared file gained a member that lives behind `#if !FRB`, or the FRB-side projitems is out of sync). So **build the base branch first to capture the baseline errors, then build your branch and attribute only the *new* errors**. A pre-existing error that survives on both is not yours to fix here (flag it separately).

Common genuinely-new breaks: a shared method/property guarded by `#if !FRB` referenced from un-guarded code (`CS0103`/`CS0246`), a `.cs` file added/renamed/deleted without updating the FRB-side projitems, or a `net7.0+` BCL API used in shared source without an `#if NET7_0_OR_GREATER` gate (FRB multi-targets down to `net6.0`).
