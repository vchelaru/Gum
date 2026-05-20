---
name: gum-shapes-xnb-packaging
description: How Gum.Shapes.MonoGame / Gum.Shapes.KNI ship platform-specific apos-shapes.xnb. Triggers: editing Runtimes/GumShapes/MonoGameGumShapes.csproj, Runtimes/GumShapes/KniGumShapes.csproj, anything under Runtimes/GumShapes/buildTransitive/, or shipping/republishing those packages.
---

# Gum.Shapes XNB Packaging

The two shapes packages (`Gum.Shapes.MonoGame`, `Gum.Shapes.KNI`) ship a pre-built `apos-shapes.xnb` per supported graphics backend so consumers don't run the upstream Apos.Shapes shader through MGCB themselves. The same files have to reach two completely different consumer types, and that fork is where this subsystem keeps producing footguns. PR #2853 + issue #2873 are the canonical incident — read those before changing anything in this area.

## Two consumer paths, one set of XNBs

Each shapes csproj must serve:

1. **NuGet consumers** — pick the platform-correct XNB through `buildTransitive/Gum.Shapes.{MonoGame,KNI}.props`, which has `<ItemGroup Condition="'$(MonoGamePlatform)' == '<Platform>'">` blocks that inject a `<Content>` referencing `$(MSBuildThisFileDirectory)<Platform>/apos-shapes.xnb`. The XNBs ship inside the `.nupkg` under `buildTransitive/.../<Platform>/`.
2. **ProjectReference consumers** — developers building against Gum source. `buildTransitive/` does **not** flow through `<ProjectReference>`, so the .props is invisible to them. They get only DesktopGL, unconditionally, via a `<Content Include="..." CopyToOutputDirectory="PreserveNewest" />` item directly in the shapes csproj. WindowsDX / Windows / BlazorGL ProjectReference users override locally; the inline comment in the csproj documents that.

Upstream's own buildTransitive (`Apos.Shapes` / `Apos.Shapes.KNI`) is suppressed at the package boundary via `PrivateAssets="contentfiles;analyzers;build;buildTransitive"` on its `<PackageReference>`. `compile` and `runtime` are deliberately left out so the upstream DLL still flows.

## The landmine — same identity in both `<Content>` and `<None>`

The DesktopGL XNB has to be present in the on-disk file AND in the .nupkg. The natural-but-wrong way to express that is two items:

```xml
<Content Include=".../DesktopGL/apos-shapes.xnb" CopyToOutputDirectory="PreserveNewest" Pack="false" />
<None    Include=".../DesktopGL/apos-shapes.xnb" Pack="true" PackagePath="buildTransitive/.../DesktopGL/" />
```

NuGet pack resolves the same identity across item types and the `Pack="false"` wins — the XNB **silently drops out of the package**. Per-package CI doesn't notice. WindowsDX / Windows / BlazorGL XNBs, which only have a `<None>` entry, ship correctly, so the failure is also platform-selective. That regression shipped as `Gum.Shapes.MonoGame 2026.5.20.1-preview.3` (issue #2873) — all DesktopGL consumers hit `MSB3030: Could not copy ... apos-shapes.xnb because it was not found`.

The fix is to **never have the same file path appear twice across `<Content>` and `<None>`**. The DesktopGL XNB is expressed as one `<Content>` item that does both jobs:

```xml
<Content Include=".../DesktopGL/apos-shapes.xnb"
         Link="Content/apos-shapes.xnb"
         CopyToOutputDirectory="PreserveNewest"
         Pack="true"
         PackagePath="buildTransitive/.../DesktopGL/" />
```

Single source of truth per file = no collision possible. The other-platform XNBs stay as `<None Pack="true">` (no ProjectReference copy is desired for them).

## The safety net — `VerifyShapesNupkg` target

Both csprojs declare a `RoslynCodeTaskFactory` inline task `VerifyShapesNupkgEntries` and a `<Target Name="VerifyShapesNupkg" AfterTargets="Pack">`. The target opens the produced `.nupkg` as a ZIP and asserts every required entry (props + every platform XNB) is present. If any is missing the build emits an error and exits non-zero — `dotnet pack` cannot silently produce a broken shapes package.

If you find yourself adding a new platform XNB, two edits are required and both must land in the same change:

1. Add the file under `Runtimes/GumShapes/buildTransitive/.../NewPlatform/apos-shapes.xnb` and a matching conditional in the relevant `Gum.Shapes.*.props`.
2. Add the entry to the `ExpectedEntries` attribute of `VerifyShapesNupkg`. If you forget step 2, the safety net stops covering that platform — defeats the point of the target.

## Don't trust local pack output without checking

The verification target fails the build (non-zero exit) when an expected entry is missing, but pack still leaves the broken `.nupkg` on disk — there's no clean-up step. Before publishing, **check the exit code** of `dotnet pack` (`echo $?` / `$LASTEXITCODE`) and as a belt-and-suspenders manual check run `unzip -l <nupkg> | grep xnb` so you can see all expected entries by eye.

## Related

- `gum-cross-platform-unification` — explains the Apos.Shapes ↔ SkiaGum shape-runtime source-sharing pattern. The csproj that links those shared sources is the same one this skill is about.
- `gum-project-versioning` — version bumps land in both shapes csprojs in lockstep with `Gum.MonoGame` / `Gum.KNI`.
