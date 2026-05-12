# Upgrading Apos.Shapes

This folder exists for one purpose: to compile the `apos-shapes.xnb` shader
binaries that the `Gum.Shapes.MonoGame` and `Gum.Shapes.KNI` NuGet packages
ship (produced by `MonoGameGumShapes.csproj` and `KniGumShapes.csproj`). Consumers receive those pre-compiled XNBs via the
`buildTransitive` props files, so they don't have to run the MGCB content
pipeline themselves.

The five `XnbBuilder*` projects in this folder are **never executed** —
`Program.cs` is a single `return;`. They exist solely so `dotnet build` runs
the MonoGame / KNI content pipeline against the `Apos.Shapes` (or
`Apos.Shapes.KNI`) NuGet's `.fx` source and writes the resulting XNB into the
project's `bin/.../Content/` folder.

## When you must regenerate the XNBs

Whenever the `Apos.Shapes` / `Apos.Shapes.KNI` package version is bumped. A
new package release may carry a new `.fx` source, which compiles to a
different binary, and the shipped XNB must match the runtime assembly the
consumer ends up with.

There is a DEBUG-only runtime guard that catches version drift:
`Runtimes/GumShapes/Renderables/ShapeRenderer.cs` holds a
`CompiledAgainstAposShapesVersion` constant and throws on mismatch when
`ShapeRenderer.Initialize` runs. RELEASE builds skip the check, so a missed
upgrade can silently ship broken shapes — do not rely on consumers hitting
the throw.

## What gets bumped

Seven `PackageReference` lines plus one C# constant must move together. Use:

```
git grep -nE "Apos\.Shapes(\.KNI)?.*Version="
```

That should list exactly these files:

| File | Package | What it controls |
|---|---|---|
| `Runtimes/GumShapes/XnbBuilder/XnbBuilderMonoGameDesktopGL/XnbBuilderMonoGameDesktopGL.csproj` | `Apos.Shapes` | Builds the MonoGame DesktopGL XNB |
| `Runtimes/GumShapes/XnbBuilder/XnbBuilderMonoGameWindowsDX/XnbBuilderMonoGameWindowsDX.csproj` | `Apos.Shapes` | Builds the MonoGame WindowsDX XNB |
| `Runtimes/GumShapes/XnbBuilder/XnbBuilderDesktopGL/XnbBuilderDesktopGL.csproj` | `Apos.Shapes.KNI` | Builds the KNI DesktopGL XNB |
| `Runtimes/GumShapes/XnbBuilder/XnbBuilderDirectX/XnbBuilderDirectX.csproj` | `Apos.Shapes.KNI` | Builds the KNI Windows XNB |
| `Runtimes/GumShapes/XnbBuilder/XnbBuilderBlazorGL/XnbBuilderBlazorGL.csproj` | `Apos.Shapes.KNI` | Builds the KNI BlazorGL XNB |
| `Runtimes/GumShapes/MonoGameGumShapes.csproj` | `Apos.Shapes` | The version consumers pull at runtime (ships as `Gum.Shapes.MonoGame`) |
| `Runtimes/GumShapes/KniGumShapes.csproj` | `Apos.Shapes.KNI` | The version consumers pull at runtime (ships as `Gum.Shapes.KNI`) |

Plus this C# constant:

- `Runtimes/GumShapes/Renderables/ShapeRenderer.cs` — `CompiledAgainstAposShapesVersion`

## Where each XNB has to land

The `buildTransitive/Gum.Shapes.MonoGame.props` and
`buildTransitive/Gum.Shapes.KNI.props` files include these XNBs by literal
path. If a destination changes you must update the props file too — but for a
normal version bump, the destinations stay the same:

| Builder project | Build output | Ships to |
|---|---|---|
| `XnbBuilderMonoGameDesktopGL` | `bin/<config>/net8.0/Content/apos-shapes.xnb` | `Runtimes/GumShapes/buildTransitive/MonoGame/Content/DesktopGL/apos-shapes.xnb` |
| `XnbBuilderMonoGameWindowsDX` | `bin/<config>/net8.0-windows/Content/apos-shapes.xnb` | `Runtimes/GumShapes/buildTransitive/MonoGame/Content/WindowsDX/apos-shapes.xnb` |
| `XnbBuilderDesktopGL` | `bin/<config>/net8.0/Content/apos-shapes.xnb` | `Runtimes/GumShapes/buildTransitive/Content/DesktopGL/apos-shapes.xnb` |
| `XnbBuilderDirectX` | `bin/<config>/net8.0-windows/Content/apos-shapes.xnb` | `Runtimes/GumShapes/buildTransitive/Content/Windows/apos-shapes.xnb` |
| `XnbBuilderBlazorGL` | `wwwroot/Content/apos-shapes.xnb` (Release only emits to wwwroot; Debug also emits to `bin/Debug/net8.0/Content/`) | `Runtimes/GumShapes/buildTransitive/Content/BlazorGL/apos-shapes.xnb` |

## Step-by-step upgrade

All commands assume the repo root as the working directory and PowerShell as
the shell. Replace `0.6.9` with the new Apos.Shapes version everywhere.

### 1. Bump every reference

Edit the seven `.csproj` files and the one `.cs` constant listed above so
they all carry the new version. After editing, re-run the grep:

```
git grep -nE "Apos\.Shapes(\.KNI)?.*Version="
```

Every result must show the new version. No occurrence of the old version
should remain.

### 2. Rebuild the XNBs

This step requires **Windows** — `XnbBuilderMonoGameWindowsDX` and
`XnbBuilderDirectX` invoke `fxc.exe` through MGCB, which is only available
on Windows. On Linux/macOS you can rebuild the three OpenGL/Blazor XNBs but
must do the two Windows XNBs on a Windows machine; otherwise the bump is
incomplete. (Apos.Shapes itself ships only an OpenGL `.fx` on non-Windows
content pipelines — see
`docs/code/standard-visuals/shapes-apos.shapes.md`.)

Clean once to make sure stale XNBs are not reused, then build all five:

```powershell
dotnet build Runtimes/GumShapes/XnbBuilder/XnbBuilderMonoGameDesktopGL/XnbBuilderMonoGameDesktopGL.csproj -c Release
dotnet build Runtimes/GumShapes/XnbBuilder/XnbBuilderMonoGameWindowsDX/XnbBuilderMonoGameWindowsDX.csproj -c Release
dotnet build Runtimes/GumShapes/XnbBuilder/XnbBuilderDesktopGL/XnbBuilderDesktopGL.csproj    -c Release
dotnet build Runtimes/GumShapes/XnbBuilder/XnbBuilderDirectX/XnbBuilderDirectX.csproj        -c Release
dotnet build Runtimes/GumShapes/XnbBuilder/XnbBuilderBlazorGL/XnbBuilderBlazorGL.csproj      -c Release
```

If any build fails, fix it before continuing. Common causes: a content
pipeline reference inside the NuGet was renamed (the project file may need a
matching `PackageReference` adjustment) or `fxc.exe` is missing from `PATH`
(install the Windows 10 SDK).

### 3. Copy each XNB into `buildTransitive`

The build configuration above is `Release`, so the source paths use
`bin/Release/...`:

```powershell
Copy-Item Runtimes/GumShapes/XnbBuilder/XnbBuilderMonoGameDesktopGL/bin/Release/net8.0/Content/apos-shapes.xnb         Runtimes/GumShapes/buildTransitive/MonoGame/Content/DesktopGL/apos-shapes.xnb -Force
Copy-Item Runtimes/GumShapes/XnbBuilder/XnbBuilderMonoGameWindowsDX/bin/Release/net8.0-windows/Content/apos-shapes.xnb Runtimes/GumShapes/buildTransitive/MonoGame/Content/WindowsDX/apos-shapes.xnb -Force
Copy-Item Runtimes/GumShapes/XnbBuilder/XnbBuilderDesktopGL/bin/Release/net8.0/Content/apos-shapes.xnb                 Runtimes/GumShapes/buildTransitive/Content/DesktopGL/apos-shapes.xnb -Force
Copy-Item Runtimes/GumShapes/XnbBuilder/XnbBuilderDirectX/bin/Release/net8.0-windows/Content/apos-shapes.xnb           Runtimes/GumShapes/buildTransitive/Content/Windows/apos-shapes.xnb -Force
Copy-Item Runtimes/GumShapes/XnbBuilder/XnbBuilderBlazorGL/wwwroot/Content/apos-shapes.xnb                              Runtimes/GumShapes/buildTransitive/Content/BlazorGL/apos-shapes.xnb -Force
```

### 4. Verify the diff

```
git diff --stat
```

The diff should contain exactly:
- The seven `.csproj` version bumps
- The `ShapeRenderer.cs` constant update
- The five `apos-shapes.xnb` binaries under `buildTransitive/`

Nothing else. If you see unrelated files, back them out before committing.

### 5. Smoke-test

```
dotnet test MonoGameGum.Tests/MonoGameGum.Tests.csproj
```

The DEBUG-time `ValidateAposShapesVersion` guard runs whenever
`ShapeRenderer.Initialize` is called, so any version-mismatch bug surfaces
here. For deeper confidence, run a sample that uses shapes (e.g. the
MonoGameGum sample with `AposShapeRuntime`) on each platform that has a
shipping XNB.

## Related code

- `Runtimes/GumShapes/Renderables/ShapeRenderer.cs` — the DEBUG version check
  and the `CompiledAgainstAposShapesVersion` constant.
- `Runtimes/GumShapes/buildTransitive/Gum.Shapes.MonoGame.props` and
  `Gum.Shapes.KNI.props` — wire the shipped XNBs into consumer builds and
  set `SkipAposShapeContent=true` so Apos.Shapes' own content pipeline is
  bypassed on the platforms we ship pre-built XNBs for.
- `docs/code/standard-visuals/shapes-apos.shapes.md` — consumer-side install
  guide and the note about the Linux KNI shader limitation.
