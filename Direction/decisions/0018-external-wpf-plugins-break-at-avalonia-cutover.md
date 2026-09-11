# 0018. External WPF plugins break at the Avalonia cutover, with notice

- **Status:** Accepted
- **Date:** 2026-09-10
- **Deciders:** Jeremy Swartwood (charged by Victor Chelaru with the migration), Claude

## Context

ADR-0017 commits the Gum tool to a full Avalonia cutover. Phase 40 of the migration plan
(`Direction/avalonia-migration/phase-40-plugin-panel-contract.md`) asked for one explicit decision
before the later phases consume the plugin contract: what happens to third-party plugins that were
written against the WPF tool.

The plugin contract has two halves. The MEF composition, the event surface on `PluginBase`, and the
service imports are already framework-neutral and live in `Gum.Presentation`. The other half was
WPF by construction: `WpfPluginBase.AddMenuItem` returned a `System.Windows.Controls.MenuItem`,
tabs were handed over as `FrameworkElement`s, and the delete-options dialog events carried a WPF
`Window`. Every plugin in this repository is first-party. We know of no external plugin that is
maintained outside the repository, but the docs under `docs/gum-tool/plugins/` have described how
to write one since the XNA days, so we cannot rule them out.

The alternatives considered:

1. **A compatibility shim** that keeps WPF plugins loading inside the Avalonia tool. This requires
   a WPF host inside the Avalonia process, which does not exist on macOS or Linux and would pull
   `UseWPF` back into the tool graph on Windows. It contradicts rule (4) of the plan's done
   criteria (no Windows-only framework in the shipped tool graph).
2. **A long dual-head period** where both the WPF and Avalonia tools ship and external authors
   migrate at leisure. This doubles the release surface for every phase after 40 and delays the
   cutover indefinitely; the stalled `avalonia-port` branch is what indefinite parallel work looks
   like.
3. **Break at cutover, with notice.** Publish the neutral contract now, keep an obsolete WPF shim
   in the WPF tool until cutover, and treat any external breakage as a documented, dated change.

## Decision

Option 3. Concretely:

- `PluginBase.AddMenuEntry(Action click, params string[] path)` is the one way to add a menu item.
  It returns a `MenuItemModel` and works under both heads. Tabs are handed over as either a control
  the head can show or a ViewModel the head resolves to a view (`ITabManager.AddControl(object, …)`
  already takes `object`). Dialogs go through `IDialogService`.
- The WPF tool keeps `WpfPluginBase.AddMenuItem` as an `[Obsolete]` shim over the model until the
  cutover release so an external WPF plugin still loads there with a compiler warning.
- The Avalonia head refuses to load any plugin assembly that references `PresentationFramework`,
  `PresentationCore`, `WindowsBase`, `System.Windows.Forms`, or `System.Xaml`, and reports the
  reason in the Output tab and the plugin scan report instead of failing composition.
- The last WPF release stays downloadable after cutover.
- The change is announced in the release notes one release before cutover, in the plugin docs
  (`docs/gum-tool/plugins/`), and on Discord. The draft is
  `Direction/avalonia-migration/plugin-compatibility-notice.md`.

## Consequences

- No WPF type reaches a plugin through the contract any more. The only WPF-coupled member left on
  `WpfPluginBase` is the delete-options dialog event pair, which phase 80 replaces.
- The eleven in-repo plugin projects migrate on the schedule in the phase 40 audit table. The two
  that already built without WPF (`ConvertToJsonPlugin`, `EventOutputPlugin`) now target plain
  `net10.0`, carry the cross-platform banned-API guard, and load in the Avalonia head today.
- An external author who does not migrate loses their plugin at cutover. That is the accepted
  cost; the notice period and the obsolete shim are the mitigation.
