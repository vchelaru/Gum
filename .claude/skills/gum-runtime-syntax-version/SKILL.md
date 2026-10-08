---
name: gum-runtime-syntax-version
description: The integer version stamped on Gum runtime assemblies via GumSyntaxVersionAttribute, used by the tool's codegen to gate emitted code. Triggers when bumping the runtime syntax version, touching AssemblyAttributes.cs in GumCommon/MonoGameGum/RaylibGum/SkiaGum, or changing SyntaxVersionDetectionService.
---

# Gum Runtime Syntax Version

Not the same thing as `.gumx` file format versioning (see `gum-project-versioning`). This is an **assembly-level** integer stamped on each runtime DLL that tells the Gum tool's codegen which conventions / namespaces / role interfaces the consumer's runtime supports.

## Where it lives

- Attribute type: `GumDataTypes/GumSyntaxVersionAttribute.cs`.
- Stamped via `AssemblyAttributes.cs` in each runtime project:
  - `GumCommon/AssemblyAttributes.cs`
  - `MonoGameGum/AssemblyAttributes.cs` (KniGum and FnaGum csprojs glob `..\**\*.cs`, so they inherit this stamp automatically)
  - `Runtimes/RaylibGum/AssemblyAttributes.cs`
  - `Runtimes/SkiaGum/AssemblyAttributes.cs`
- Detection (tool side): `Tools/Gum.ProjectServices/CodeGeneration/SyntaxVersionDetectionService.cs`.
- Public docs / version table (the version history lives here, not in this skill):
  `docs/gum-tool/upgrading/syntax-versions.md` — published at
  https://docs.flatredball.com/gum/gum-tool/upgrading/syntax-versions

## How detection works

A manual `SyntaxVersion` in `.codsj` wins. Otherwise reads the consumer's `.csproj` (`CodeProjectCsprojLocator` prefers `Assembly-CSharp.csproj`, then the shortest name):
1. If `ProjectReference` → finds `MonoGameGum`/`RaylibGum`/`SkiaGum`/`KniGum`/`FnaGum`, opens that project's `AssemblyAttributes.cs`, regex-parses the version.
2. Else if `PackageReference` → locates the DLL in the NuGet cache (`NUGET_PACKAGES` if set, else `~/.nuget/packages`), reads the attribute via `MetadataLoadContext`. A floating `Version="*"` / `"2026.*"` resolves to the highest restored stable version in the cache, so the package must be restored before detection works.
3. Else if a `<Reference>` has a `<HintPath>` to a runtime DLL (how Unity references Gum) → reads that DLL's attribute.

A new runtime must be added to `GumRuntimeNames` and the package list in `SyntaxVersionDetectionService`. A runtime missing from them falls back to version 0 without any error, and codegen emits legacy namespaces that do not compile.

When none of the three finds a version, the fallback depends on the csproj. No Gum runtime referenced at all resolves to `SyntaxVersionDetectionService.LatestSyntaxVersion` (the stamp on GumCommon), so new projects get current namespaces. A runtime that is referenced but unreadable (package not restored, no attribute) resolves to 0, because an old build lacks the attribute.

**GumCommon is not on the detection scan list** — stamping it is for assembly-metadata consistency, not for codegen detection.

## When to bump

When the runtime surface that codegen cares about changes in a way that requires the tool to emit different code (renamed/removed role interfaces, new runtime types, namespace changes). Bump all four assemblies in lock step and add a row to the version table.

## Gate on a safe floor, not the exact release that added the API

A codegen check like `context.ResolvedSyntaxVersion >= N` doesn't need to match the version that
introduced the target API — it only needs to guarantee the API exists. If an API shipped mid-cycle
inside an already-stamped version (so some assemblies at that version have it and some don't), gate
on the *next* bumped version instead. The cost is early adopters keep seeing the old code path a
little longer; the alternative — gating loosely to minimize that — risks emitting code that fails to
compile against older runtimes still reporting that version. See `AddFindByNameAssignment` in
`CodeGenerator.cs`, which gates `FindFormsControl<T>` on `ResolvedSyntaxVersion >= 1` even though the
method shipped mid-version-0.

## When NOT to bump

Pure renderable / Forms / sample changes that the codegen doesn't pattern-match against. The version is for **codegen gates**, not a general changelog.

## Instance-Member Pattern vs Extension-Method Shims

When migrating a static extension to an instance method on `GraphicalUiElement` (or another GumCommon type), the instance method **entirely eliminates** the need for namespace-migration shims:

- Extension methods require a `using` directive in scope; instance methods need nothing.
- Two extensions with identical signatures cause CS0121 ambiguity when both namespaces are imported — instance methods sidestep this entirely (they always win over extensions).
- No `[Obsolete]` spam: the old extension is deleted; call sites resolve to the instance method automatically.

This pattern is viable wherever a GumCommon seam (like `IGumService.Default`) can dispatch the work. Applied to `AddToRoot` / `RemoveFromRoot` at syntax version 3: the per-platform extension classes were deleted and the instance methods dispatch via `IGumService.Default`. See issue #3119.
