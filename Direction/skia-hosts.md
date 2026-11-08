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
| 1 | Scaffold for host registration | **Check done (#7743); no scaffold.** A new host is still registered by hand in four places (`AllLibraries.sln`, `Tests/CiTestProjects.slnf`, a step in `.github/workflows/build-and-test.yaml`, a test project derived from `Tests/SkiaHost.TestsCommon`), but `SkiaHostRegistrationTests` in `Tests/Gum.RepoHygiene.Tests` now finds every host by convention and fails with a numbered list of what to add where. Extended (#7747) to also check the docs page, its card, the `docs/SUMMARY.md` entry, the root README row, `Samples/<Host>`, and the `windows-test-groups.txt` line; `KniSkiaGum` is deliberately exempt from the four docs checks. A script that performs the edits is still open. |
| 10 | Shared host contract tests | **Done (#7749).** `Tests/SkiaHost.TestsCommon` has `GumServiceContractTestsBase`, `InputSubscriptionTestsBase`, `CursorTestsBase` and `GamePadDriverTestsBase`; a host supplies a small rig (`IHostInputRig`, `IHostCursorRig`, `IHostGamePadRig`) and a thin derived class per base. Verified by mutation in all four hosts. The clipboard round trip and the Silk/Stride keyboard behavior moved to `ClipboardTestsBase` and `PolledKeyboardTestsBase` (#7751); the clipboard expected value is a fresh Guid so a no-op `SetText` cannot pass. |
| 11 | One platform identity table | **Done (#7748).** `Tools/Gum.ProjectServices/HostPlatformInfo.cs` feeds `ReservedProjectNames`, `GetGumRuntime` and `GumPackageNames`; `HostPlatformInfoDriftTests` fails if a `Runtimes/<X>Gum` host or packable runtime is missing from it. Fixed: UnityGum and 11 other runtime assembly names were not reserved, so a new project with that name could overwrite the runtime DLL. |
| 2 | Init-order hook | **Done (#7741).** The base owns the order: a host calls `GumServiceSkiaInteractiveBase.InitializeWithDevices(..., attachDevices)` and assigns its device fields and `Clipboard` inside the callback. Remaining gap: the inherited public `Initialize(SKCanvas, int, int, ...)` on Silk and Stride still skips the callback, so calling it directly gives null devices. |
| 3 | Key translation helper | **Done (#7744).** `GumCommon/Input/TablePolledKeyboard<TDeviceKey>` takes a Gum-key table and one device query; Silk and Stride use it. The ~100-entry tables stay (they are the data), so the line savings are small. The value is `KeyTableCoverage` in `Tests/SkiaHost.TestsCommon`, which names every `Keys` value a host neither maps nor lists as intentionally unmapped. KniSkia (direct cast) and Unity (push model) do not use it. |
| 4 | Engine-surface lifetime | **Done with 7 (#7745).** See `SkiaTargetHolder` under 7. The KniSkiaGum samples' `Game1.cs` still manage their own `SkiaRenderTarget2D` and do not resize; they were not converted. |
| 5 | Gamepad mapping | `GamePadDriver` in Silk, Stride and KNI differs only by the device read and the Y-axis flip. Low value: the field mapping is the work. |
| 6 | csproj boilerplate | Long repeated comments and a per-package collision-guard targets file. Cosmetic. |
| 7 | Canvas capture and target holder | Every host that owns an SGR target creates it, swaps `SystemManagers.Canvas` when the target is recreated on resize, and captures the canvas, which is readable only between `Begin` and `End` (Stride does a throwaway `Begin`/`EndWithoutDrawing`). **Done (#7745).** `Runtimes/SkiaGum/Helpers/SkiaTargetHolder.cs` (`ISkiaHostTarget` plus `SkiaTargetHolder<TTarget>`: `Create`, `EnsureSize`, `BeginFrame`) owns create, dispose-and-recreate on resize, canvas apply and the resize callback. A new host implements `ISkiaHostTarget` on a wrapper around its SGR `SkiaRenderTarget2D` and passes a create delegate; StrideGum and the Unity `GumRenderer` use it. It references no engine or SGR package and has no CPU path. Gap: if the create delegate throws during a resize, the old target is already disposed and `Target` points at it. |
| 8 | SGR facade per engine | The three Stride SGR packages declare identical type names with no shared interface, so StrideGum carries extern aliases plus a wrapper per package (about 150 lines). An interface or facade in SGR would remove them. Work in the SGR repo. |
| 9 | Off-screen GPU smoke helper | **Done for Stride D3D11 and D3D12 (#7752).** `Tests/StrideGum.GpuTests.D3D11` and `.D3D12` (shared source in `Tests/StrideGum.GpuTests.Shared`) create a Stride device with no window on the WARP software D3D device (`STRIDE_GRAPHICS_SOFTWARE_RENDERING=1`, set in test code), render through `GumService.InitializeGpu` and `DrawStrideFrame(Texture, GraphicsContext)`, read pixels back from the GPU texture, and check a resize. They run in the Windows `headless` stream, so `fast-ci` keeps them; a missing device fails loudly. Not covered: Vulkan (SGR runs it on lavapipe in its own CI), and the registration of `GumSceneRenderer` into the compositor (covered by `GumSceneRendererRegistrationTests`). Unity has a player `--smoke-test`; KniSkia has none. |

Still open, in order of value: Vulkan in the Stride smoke test if it works window-less, a smoke test for Unity-free hosts such as KniSkia, the registration script (rest of 1), a `DisconnectGamePad` helper on the interactive base, 8 when SGR can take it, then 5 and 6 only if touched anyway. `SkiaTargetHolder.EnsureSize`'s canvas apply is redundant for Stride today (BeginFrame re-applies it) but not for a host that calls `EnsureSize` without `BeginFrame`. Dropped: sharing `GumSyntaxVersion` through one linked file, because the tool's detection reads each runtime's own `AssemblyAttributes.cs` by path (see the gum-runtime-syntax-version skill). Stride and Unity ignore resize cursors (`ApplyHostCustomCursor` exists only in Silk and KniSkia); it is optional and the device enums differ, so there is no shared mapping. Not worth doing: building the key table from enum names, generating the collision-guard targets, trimming csproj comments.

## Current hosts

| Host | Canvas source | Notes |
|---|---|---|
| `KniSkiaGum` | SGR `SkiaRenderTarget2D` (KNI) | Mirrors `KniGum`/`MonoGameGum` API: `Initialize(game)`, `GameTime`. The reference for an engine that has a native Gum runtime. |
| `StrideGum` | SGR Stride packages | Registers a scene renderer into Stride's `GraphicsCompositor`; the caller never calls Update/Draw. |
| `UnityGum` | SGR Unity package | Push model: the host pushes input each frame (`GumRenderer`, `GumInput`). Experimental, Unity 6+. |
| `SilkNetGum` | Its own GL `GRContext` | See "Silk.NET" below. |

`StrideGum` and `UnityGum` no longer have a CPU path. An unsupported graphics API throws a
`NotSupportedException` that names it; plain `net10.0` Stride supports Vulkan only. Stride has been built
but not run on a GPU; Unity's player smoke test passes on D3D11.

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
  Recorded the GPU-only rule and the candidates above.
- 2026-11-08: Removed the CPU path from `UnityGum` (#7738) and `StrideGum` (#7739). Stride now uses SGR
  0.20.0-beta for D3D11, D3D12 and Vulkan; the `gumcli new` Stride template targets `net10.0-windows7.0`.
  Duplication removed: both CPU surface-and-upload paths. Added candidates 7 to 9 from the Stride and Unity
  agents' reports. A macOS CI failure in `NewProjectRecommendedPackagesTests` (real `dotnet add package`,
  empty output) passed on the next commit; the test now reports the exit code and error when it recurs.
- 2026-11-08: Candidate 2 landed (#7741): `InitializeWithDevices` in the shared base owns the
  "assign devices, then `InitializeDefaults`" order for Silk, Stride and KniSkia. Duplication removed: three
  "store fields, assign clipboard, call base" blocks and KniSkia's separate early visuals-version call. The
  base now checks the visuals version before disposing old input.
- 2026-11-08: Three more improvements landed while waiting for the next engine's SGR adapter: the
  registration check (#7743), `TablePolledKeyboard` with key-table coverage (#7744), and `SkiaTargetHolder`
  (#7745). Duplication removed: Stride's and Unity's separate target create/resize/canvas blocks, and a few
  lines of key-table plumbing per host. The Unity sample's smoke test passed on D3D11 at 640x360 and after a
  resize to 800x450; Stride is covered by builds and unit tests only. This goes beyond one improvement per
  host on purpose; the rule still applies once an engine is being added.
- 2026-11-08: A second review found more shared work; landed #7747 (wider registration check), #7748 (identity
  table plus the UnityGum reserved-name fix) and #7749 (shared contract test bases, about 500 lines of host
  tests now supplied as small rigs). Their own mutation checks and verifiers found no weakened coverage.
  KniSkiaGum is deliberately kept out of the docs for now.
- 2026-11-08: Landed #7751 (clipboard and keyboard test bases) and #7752 (window-less Stride GPU smoke tests on
  D3D11 and D3D12; Stride is now run on a device, not only built). Dropped the shared syntax-version file
  (detection reads `AssemblyAttributes.cs` by path).
