---
name: gum-project-versioning
description: Reference guide for Gum's .gumx schema versioning and migration strategy. Load this when changing the shape of GumProjectSave, ElementSave, or any other serialized save class — particularly when deciding whether a change needs a version bump, when adding a backward-compat shim, or when writing XmlSerializer-aware properties that must round-trip across tool versions.
---

# Gum Project Versioning Reference

## The GumxVersions enum

Located in `GumDataTypes/GumProjectSave.cs`. Currently two entries:

- `InitialVersion = 1` — verbose XML (names as child elements)
- `AttributeVersion = 2` — compact XML (names as attributes on `ElementReference` / `BehaviorReference`)

`NativeVersion` is the version that the current tool writes by default. A new `GumProjectSave` is constructed at `NativeVersion`.

## The three load-time behaviors

`ProjectManager.LoadProject` (~line 201 and ~288 per recent research) compares the file's `Version` against `NativeVersion`:

1. **File version > NativeVersion** → dialog: *"saved with a newer version of Gum... Please update Gum."* File is rejected.
2. **File version < AttributeVersion** → Output-tab warning with an upgrade docs link (`https://docs.flatredball.com/gum/gum-tool/upgrading/upgrading-file-gumx-version`). File still opens and works.
3. **File version == NativeVersion** → normal load.

## Save doesn't auto-bump

This is unusual. Most authoring tools silently upgrade on save. Gum does not — a file loaded as v1 stays v1 after the user re-saves it. Upgrading is a user-initiated action following the docs link. Don't "fix" this without discussion.

## XmlSerializer tolerance as the migration mechanism

There is no explicit migration pass for v1 → v2. Instead:

- `GumFileSerializer` (in `GumDataTypesNet6/`) builds separate serializers (`GetCompactSerializer`, `GetLegacyInstancesCompactSerializer`, `GetGumProjectCompactSerializer`) using `XmlAttributeOverrides` to handle the attribute-vs-element shape differences.
- Unknown XML elements are silently ignored by `XmlSerializer`, so adding a new element to a serialized class is inherently forward-compatible: old tools just skip it.

This means **most schema changes don't require a version bump.**

## Bump-vs-don't-bump decision framework

- **Adding a new optional field** → **don't bump.** Old tools ignore it.
- **Adding a new field while replacing/renaming an old one** → **don't bump**, if you keep the old field as a backward-compat shim (an always-serialized view over the new one so old tools still read something sensible).
- **Removing a field with no shim, or making a breaking semantic change** → **bump.** Accept the forward-compat barrier (old tools refuse the file).
- **Bundle multiple schema changes into one bump** when possible.

The `LocalizationFile → LocalizationFiles` change in issue #2512 used the no-bump route: new `List<string>` property plus the legacy `string` property kept as a shim reading/writing index 0.

## Writing backward-compat shims — XmlSerializer gotchas

If you add a new property and keep the old one as a shim, know these:

1. **`[Obsolete]` alone silently drops the property from XML output.** XmlSerializer skips `[Obsolete]` members. If you need a shim to keep serializing, use `[EditorBrowsable(EditorBrowsableState.Never)]` plus an explicit `[XmlElement("OldName")]` attribute instead of `[Obsolete]`.

2. **`List<T>` appends; `T[]` replaces.** On deserialize, XmlSerializer calls `Add` on a `List<T>` property once per child element — so if the old element appears before the new one, both contribute and you end up with duplicates. Arrays are assigned via the setter, which replaces. If your deserialized shape might contain both old and new elements, serialize as `T[]` (with an `[XmlArray]` attribute) and expose a `[XmlIgnore] List<T>` that syncs.

3. **Declaration order affects which setter wins** when both old and new elements are present in XML. XmlSerializer applies properties in document order. If your shim and new property both set the same underlying state, putting the legacy property first (so the new property's setter fires last) is the intended round-trip shape.

4. **Always emit the legacy element** when the new one is populated. That way, an older tool opening a new file still reads *something* — typically the first entry of a list.

## Related skills

- **gum-tool-save-classes** — broader overview of the save/load data model, `ShouldSerialize*`, `[XmlIgnore]`, two-phase loading.
- **gum-localization** — localization-specific file loading, the place where `LocalizationFile(s)` is consumed.
- **gum-tool-codegen** — `.codsj` files have their own `Version` field (separate from `.gumx` versioning), migrated in `CodeOutputProjectSettingsManager.MigrateIfNeeded` on load. Don't auto-save — let the next user save persist the new version. Each version bump needs a comment explaining what it does and why.
