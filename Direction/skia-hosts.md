# Skia hosts: adding a new platform

How Gum reaches a game engine through SkiaSharp, the rules every Skia host follows, and the standing
goal that each new host is easier to add than the last. Living: edit in place, append to the Log.
Operational detail (file names, props, partial methods) is in the skills
[`gum-runtime-topology`](../.claude/skills/gum-runtime-topology/SKILL.md) ("Adding a Skia-based host")
and [`skia-game-rendering`](../.claude/skills/skia-game-rendering/SKILL.md); this document is the
strategy and the ledger.

## The shape of a Skia host

Gum draws all UI through Skia on every host. A host has three parts:

| Part | Owner | Where |
|---|---|---|
| Layout, Forms, rendering of Gum elements to an `SKCanvas` | Shared Skia core | `Runtimes/SkiaGum` (`GumServiceSkiaBase`, `GumServiceSkiaInteractiveBase`) plus `GumCommon` |
| Getting an `SKCanvas` that lives on the engine's GPU | SkiaGameRendering (SGR) | Sibling repo `../SkiaGameRendering`, consumed as NuGet or UPM packages |
| Reading the engine's input and shaping the engine-facing API | The host | `Runtimes/<Engine>Gum` (cursor, keyboard, gamepad, `GumService`, clipboard) |

A host is therefore small: it writes device translation only. Shared host plumbing already in source:
`Runtimes/SkiaHost.props`, `SkiaInputHost.props`, `SkiaPackages.props`, `Runtimes/SkiaHost/GumService.SkiaHost.cs`,
`Cursor.SkiaHost.cs`, and `GumCommon/Input/PolledKeyboard.cs`.

## Rule 1: GPU only, no CPU rendering path

SGR's purpose is to keep Skia on the GPU with no CPU readback, and its `CLAUDE.md` forbids a CPU
rasterization or readback fallback for any backend or failure. Gum follows the same rule. A Skia host never
creates a CPU `SKSurface` and uploads pixels into an engine texture, and never offers a "force CPU" switch.

When an engine or graphics API has no SGR support, the options are the ones SGR names: fix it in SGR, narrow
the supported range, or do not ship that host yet. The host fails with one clear message naming the
unsupported API.

Why this is written down: a CPU path left in one host reads as a pattern, and agents copy patterns. Delete
dead fallbacks instead of documenting around them.

## Rule 2: every new host leaves the shared code better

Each new platform makes exactly one incremental improvement to the shared code, so the next platform is
easier to add. This is the host-level form of the repo rule that duplicated code shrinks whenever it is
touched, never by a dedicated sweep.

- Pick the improvement while adding the host, from the candidates below. Do not batch several.
- The improvement removes work the next host would otherwise repeat. A host that only copies an earlier
  host is a failure of this rule.
- Record it in the Log below and in the PR body, including what duplication was removed.

### Candidates (found by reading the four existing hosts; not all verified in depth)

| # | Candidate | What a new host repeats today |
|---|---|---|
| 1 | Scaffold for host registration | A new host is registered by hand in four places: `AllLibraries.sln`, `Tests/CiTestProjects.slnf`, a step in `.github/workflows/build-and-test.yaml`, and a test project derived from `Tests/SkiaHost.TestsCommon`. Missing one fails CI late. |
| 2 | Init-order hook | Silk, Stride and KniSkia each document "set device fields before `base.Initialize`, which calls back into `CreateHostCursor`/`CreateHostKeyboard`". A forgotten order gives a null device with no error. A `protected virtual` attach hook called by the base would remove the trap. |
| 3 | Key translation helper | Silk and Stride each hand-write about 105 entries mapping Gum keys to engine keys. KNI avoids it because Gum's `Keys` has the same names and values as XNA's. A small generic table-backed `PolledKeyboard` plus a test that flags unmapped keys would shrink a new host. |
| 4 | Engine-surface lifetime | Stride creates and recreates its SGR render target on resize and drives a clock. KniSkia gets the same from a sample's `SkiaRenderTarget2D`. Unity does its own. Once the CPU paths are gone, what remains duplicated across hosts (resize, per-frame Begin/Update/Draw/End) is the shared piece to extract. |
| 5 | Gamepad mapping | `GamePadDriver` in Silk, Stride and KNI differs only by the device read and the Y-axis flip. Low value: the field mapping is the work. |
| 6 | csproj boilerplate | Long repeated comments and a per-package collision-guard targets file. Cosmetic. |

Order of value, as of the last review: 1, 2 and 4 first; 3 next; 5 and 6 only if touched anyway.

## Current hosts

| Host | Canvas source | Notes |
|---|---|---|
| `KniSkiaGum` | SGR `SkiaRenderTarget2D` (KNI) | Mirrors `KniGum`/`MonoGameGum` API: `Initialize(game)`, `GameTime`. The reference for an engine that has a native Gum runtime. |
| `StrideGum` | SGR Stride packages | Registers a scene renderer into Stride's `GraphicsCompositor`; the caller never calls Update/Draw. |
| `UnityGum` | SGR Unity package | Push model: the host pushes input each frame (`GumRenderer`, `GumInput`). Experimental, Unity 6+. |
| `SilkNetGum` | Its own GL `GRContext` | See "Silk.NET" below. |

The CPU paths that `StrideGum` and `UnityGum` once carried are being removed; do not model a new host on
them if they are still in the tree.

### Silk.NET

`SilkNetGum` does not go through SGR. The caller owns the window and passes in an `SKCanvas`; the sample
(`Samples/SilkNetGum`) builds its own GL `GRContext`. SGR has no Silk.NET adapter. It may move to SGR
later, or it may stay as is: that is undecided and nobody asked for it. Its usage is small. Treat it as the
reference for "bring your own canvas" and do not spend improvement budget on it unless a new host needs the
same change.

## Next candidates

- **Godot.** SGR already has the adapter (Vulkan, D3D12, Metal, GL compatibility; render target only, no
  direct canvas). ADR-0020 scopes Godot in with no scheduled work, to be built when someone asks. When it is
  built, pair it with one improvement from the table above.
- Any other engine: start with its row in SGR's README table. If the row is missing, the first job is an SGR
  adapter, not a Gum workaround.

## Checklist for a new host

1. Find the engine in SGR's README table and read `docs/<engine>/quickstart.md`. No row means stop (Rule 1).
2. Read `Direction/decisions/0014` and `0020` for scope, and the "Skia hosts match their native counterparts
   in interface" guidance: same API shape as the engine's native Gum runtime where one exists.
3. Model on `Runtimes/KniSkiaGum` (polled engine) or `Runtimes/UnityGum` (push model). Do not copy a CPU path.
4. Write device translation only; reuse the shared props and partials.
5. Register the host in the four places in candidate 1.
6. Pick and land one improvement from Rule 2; log it.
7. Keep SkiaSharp on the single version SGR uses (`Runtimes/SkiaPackages.props`).

## Log

- 2026-11-08: Document created after a review of the Silk, Stride, KniSkia and Unity hosts and the SGR repo.
  Recorded the GPU-only rule and the candidates above. Stride and Unity CPU-path removal started in separate
  PRs.
