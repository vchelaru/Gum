# 0017. Commit to a full Avalonia cutover for the Gum tool

- **Status:** Accepted
- **Date:** 2026-09-09
- **Deciders:** Victor Chelaru, Jeremy Swartwood, Claude

## Context

ADR-0003 (2026-06-20) deliberately left the cross-platform editor as an *unresolved bet*: decouple
UI from logic as no-regret groundwork, and defer the Avalonia swap to "a measured prototype at the
end." That was the right call in June, when the tool was a WPF + WinForms hybrid with ~475 static
`.Self` calls and two `WindowsFormsHost`s. Three things have changed since.

1. **The groundwork is largely done.** Between June and September 2026 the tool gained a headless
   `net8.0` `Gum.Presentation` assembly (ADR-0005) that now holds ~356 files and 939 tests; every
   `WindowsFormsHost` is gone (#4166, #4226, #4230); the WinForms render/input path is deleted
   (#4227); the element tree is a native WPF `TreeView` bound to a plain observable node model;
   the canvas renders to a texture and reads pixels back to CPU behind
   `IRenderTargetPixelBufferWriter` (#3829), so any framework that can blit a bitmap can host it.
   What remains WPF-coupled in `Gum/` is almost entirely the view layer (see
   `../avalonia-migration/foundation.md`). The "measured prototype" ADR-0003 wanted is now cheap,
   and the north-star test in `ui-decoupling-plan.md` ("could an Avalonia UI be built today with
   only new Views?") is close to a yes.
2. **The Wine path is a dead end.** The macOS diagnostics suite (branch
   `diagnostics/mac-wine-test-suite`) pinpointed the launch failure: the editor canvas asks KNI for
   a Direct3D feature-level 10.0 device, and Wine on macOS offers only 9.3; DXVK over MoltenVK was
   tried and ruled out. Mac and Linux users cannot run the tool today, and no Wine-side fix is
   available. Native cross-platform is the only route to that audience.
3. **The earlier direct-port attempt (branch `avalonia-port`, stalled 2026-06-09) failed for a
   process reason, not a technical one.** It carried the whole migration on one long-lived branch
   that fell ~1000 commits behind main, and it hit the "core services live in the `net8.0-windows`
   project" blocker that ADR-0005 has since resolved on main. Its phase docs remain useful analysis;
   its code does not.

## Decision

We will **make the Avalonia head the one and only Gum tool**, shipping natively on Windows, macOS,
and Linux, and **retire the WPF + WinForms implementation** at the end. This supersedes the
*deferral* in ADR-0003; the decoupling decision itself stands and is the foundation this builds on.

Execution rules, all binding:

- **Incremental on `main`, never a long-lived branch.** The Avalonia head lives in the repo beside
  the WPF head from the first PR. Every step lands as an ordinary PR to `main`. The WPF tool stays
  shippable until cutover. This is the process the June–September decoupling used and the process
  the stalled branch did not.
- **The canvas backend is spiked first, standalone, before shell work.** A KNI desktop-GL device
  rendering to a render target, read back to CPU, and presented in an Avalonia bitmap on macOS or
  Linux is the single novel risk. It gates everything after it. Fallback: render the editor canvas
  through SkiaGum (Gum's own Skia runtime), accepting a bitmap-font fidelity gap to close.
- **Compiler-enforced boundary, not convention.** The head targets plain `net8.0`; anything it
  references must too. That is the purity guard. No custom scanner.
- **ViewModels and services are reused unchanged (ADR-0004/0005).** Only views (AXAML), the
  framework-specific seam implementations, the property grid, and the plugin panel contract are
  rewritten. A logic rewrite discovered mid-port is a decoupling gap to fix on the WPF side first,
  not a fork.
- **Parity gate is byte-identical output.** Saved project files and generated code from the Avalonia
  head must match the WPF head byte for byte, on every OS, before cutover.

The plan, phase docs, and the inventory of already-finished work live in
`Direction/avalonia-migration/`.

## Consequences

- **Easier:** Mac and Linux users get a native editor; the tool's audience stops being OS-gated.
  The `net8.0` head makes every remaining WPF leak a compile error. Plugin authors get one
  cross-platform contract.
- **Harder / cost:** a large, multi-month lift dominated by the canvas backend, the property-grid
  re-author, ~83 XAML files to re-author as AXAML, and eleven plugin projects to de-WPF. Packaging
  gains macOS signing/notarization and Linux artifacts as permanent release-time obligations.
- **Breaking change at cutover:** external third-party plugins written against WPF (`WpfPluginBase`,
  `MenuItem`, `FrameworkElement` tabs) stop loading. The plan owns a compatibility decision for this
  (phase 40) and it must be communicated before cutover, not after.
- **Watch-out:** the stalled branch's lesson. If the head cannot build green on `main` in every PR,
  the approach has drifted; stop and fix the seam rather than accumulating a branch.

## Alternatives considered

- **Keep deferring (ADR-0003 as written).** Rejected: the groundwork has reached the point where
  deferral no longer buys information, and the Wine dead end removes the "Mac users can limp along"
  option.
- **Resurrect the `avalonia-port` branch.** Rejected: ~1000 commits behind; its Phase 8 extraction
  duplicates what `Gum.Presentation` already did more correctly. Salvage the docs, drop the code.
- **Fix the tool under Wine (Direct3D 9.3 path, DXVK, GPTK/CrossOver).** Rejected: measured dead end
  on vanilla Wine + MoltenVK; GPTK/CrossOver are third-party, paid, and still not native.
- **Multi-target the existing `Gum` project (`net8.0;net8.0-windows`) with `#if` guards.** Rejected:
  `UseWPF`/`UseWindowsForms` cannot be cleanly conditioned and the view code would need pervasive
  `#if`; a separate head is cleaner.
- **A web-hosted editor (KNI's Blazor/WASM target).** Not rejected forever, but out of scope: a
  desktop editor with file-system access is the product; a web target could be a later head on the
  same `Gum.Presentation`.
