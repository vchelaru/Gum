---
name: gum-tool-animations
description: How the Gum tool authors state-based animations — StateAnimationPlugin, Animations tab, .ganx sidecars. Triggers: Animations tab, StateAnimationPlugin, ElementAnimationsSave, AnimationSave, AnimatedStateSave, .ganx, named events.
---

# Gum Tool Animations

State-based animation authoring in the **editor**: timeline animations that interpolate
between named **States** over time. NOT the same as runtime AnimationChains (see Landmines).

## Where it lives
- Whole feature is a MEF plugin under `Gum/StateAnimationPlugin/`; entry
  `MainStateAnimationPlugin.cs`. Tab is hidden until View ▸ View Animations.
- View `Views/MainWindow.xaml(.cs)`; VMs `ElementAnimationsViewModel` →
  `AnimationViewModel` → `AnimatedKeyframeViewModel`.

## Data model & serialization
- Persisted as a **per-element `.ganx` sidecar**: `<ElementName>Animations.ganx` next to
  the element's `.gucx/.gusx` (path: `Managers/AnimationFilePathService.cs`). NOT embedded
  in the element file or `.gumx`. Load/save: `Managers/AnimationCollectionViewModelManager.cs`.
- Save classes live in `GumDataTypes/SaveClasses/*`, namespace `Gum.StateAnimation.SaveClasses`.
  `ElementAnimationsSave` → `List<AnimationSave>`; each `AnimationSave` holds **three parallel
  keyframe lists**: state keyframes, sub-animation refs, named events.
- A state keyframe binds to a state **by name string**, `"Category/State"` convention
  (uncategorized = no slash). Resolution: `AnimationViewModel.GetStateFromCategorizedName`.
- Keyframes are **cumulative** — each combines with prior ones; preview rebuilds via
  `RefreshCumulativeStates`.

## Named events — FRB-only
"Add Named Event" authors a `NamedEventSave` (name + time). It round-trips to `.ganx`, but
**Gum's own runtime never dispatches it** — only FlatRedBall consumes named events. Skip in
non-FRB docs.

## Runtime relationship (do NOT conflate)
- **State animations (this skill)** — `.ganx`, interpolate between states. Runtime driver:
  `GumCommon/Runtime/AnimationController.cs` + `AnimationRuntime.cs` (undocumented elsewhere).
- **AnimationChains** — `.achx`, sprite-sheet flipbook on Sprite/NineSlice; see
  [[gum-runtime-animation-chains]]. **Different concept, different file, different runtime.**

## Landmines
- **"Keyframe" is a union of 3 things.** `AnimatedKeyframeViewModel` is a state keyframe OR
  sub-animation ref OR named event, discriminated by which string is non-empty (StateName →
  state, else AnimationName → sub-anim, else event). No type enum; fans out into 3 lists on save.
- **Misleading names on `AnimationSave`.** `States` = state *keyframes* (not element states);
  `Animations` = sub-animation *keyframes* (not child animations).
- **Interpolation types come from a NuGet package, not Gum.** `InterpolationType`/`Easing`
  (state keyframes only) are `FlatRedBall.Glue.StateInterpolation` types from the standalone
  **`FlatRedBall.InterpolationCore`** package — an easing/tweening library under the FlatRedBall
  brand, NOT the FRB engine. The animation data model serializes these into `.ganx`.
- **Sidecar must follow the element.** Element rename/duplicate/delete must move the `.ganx`;
  wired via RenameManager/DuplicateService/ElementDeleteService in `AssignEvents`.
- **Save is whitelist-filtered.** `HandleDataChange` only re-saves on specific property
  changes; a new persisted keyframe field won't save unless added there.

## Docs
User-facing content today is a 4-part tutorial under
`docs/gum-tool/tutorials-and-examples/animation-tutorials/`. Issue #480 wants a *reference*
section on the Animations tab itself (data model, named events, the tab UI), not tutorial-style.
