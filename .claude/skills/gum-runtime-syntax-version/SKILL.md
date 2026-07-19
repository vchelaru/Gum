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

## This repo's CodeGenerator.cs is not the only reader

FlatRedBall's Glue tool generates its own `GumRuntimes/*.Generated.cs` files (e.g. `ArcRuntime.Generated.cs`) via a codegen implementation in the FlatRedBall repo, independent of `Tools/Gum.ProjectServices/CodeGeneration/CodeGenerator.cs` — `GumSyntaxVersionAttribute` exists precisely so external codegens like Glue's can read runtime capability independently. A bug in one of those generated files is not automatically a bug in this repo's codegen; check which codegen actually produced the file before assuming the fix belongs here.

## How detection works

Reads the consumer's `.csproj`:
1. If `ProjectReference` → finds `MonoGameGum`/`RaylibGum`/`SkiaGum`/`KniGum`/`FnaGum`, opens that project's `AssemblyAttributes.cs`, regex-parses the version.
2. Else if `PackageReference` → locates the DLL in the NuGet cache, reads the attribute via `MetadataLoadContext`.
3. Else manual override from `.codsj`'s `SyntaxVersion` field.

**GumCommon is not on the detection scan list** — stamping it is for assembly-metadata consistency, not for codegen detection.

## When to bump

When the runtime surface that codegen cares about changes in a way that requires the tool to emit different code (renamed/removed role interfaces, new runtime types, namespace changes). Bump all four assemblies in lock step and add a row to the version table.

## When NOT to bump

Pure renderable / Forms / sample changes that the codegen doesn't pattern-match against. The version is for **codegen gates**, not a general changelog.

## Instance-Member Pattern vs Extension-Method Shims

When migrating a static extension to an instance method on `GraphicalUiElement` (or another GumCommon type), the instance method **entirely eliminates** the need for namespace-migration shims:

- Extension methods require a `using` directive in scope; instance methods need nothing.
- Two extensions with identical signatures cause CS0121 ambiguity when both namespaces are imported — instance methods sidestep this entirely (they always win over extensions).
- No `[Obsolete]` spam: the old extension is deleted; call sites resolve to the instance method automatically.

This pattern is viable wherever a GumCommon seam (like `IGumService.Default`) can dispatch the work. Applied to `AddToRoot` / `RemoveFromRoot` at syntax version 3: the per-platform extension classes were deleted and the instance methods dispatch via `IGumService.Default`. See issue #3119.
