# SkiaGum.Standalone

Shared **source** for the render-only `Gum.GumService` used by SkiaSharp host
environments — WPF, MAUI, Silk.NET, and bring-your-own-canvas standalone apps.

## Why this is source-only (no .csproj yet)

`Gum.SkiaSharp` (the `SkiaGum` core library) is **rendering/layout only** — it does
not contain a `GumService` (see issue #3218). The render-only service was evicted from
core and relocated here. It is `namespace Gum` / type `GumService`, mirroring the
game-host service in `MonoGameGum/GumService.cs`, so user code is portable across hosts.

Each host lib / sample / tool that needs the service **file-links** `GumService.cs`:

```xml
<Compile Include="..\SkiaGum.Standalone\GumService.cs" Link="GumService.cs" />
```

This mirrors how MonoGame and raylib each compile their own copy of the game-host
`GumService.cs` rather than referencing a shared `GumService.dll`: one `Gum.GumService`
per host assembly, never two in one app.

A `Gum.SkiaSharp.Standalone` NuGet package (which would compile this file) is deferred
until a concrete bring-your-own-canvas consumer needs it (YAGNI). Until then the file
has no owning project and is reached only through the links above.

See `.claude/designs/runtime-unification/GumServiceHostModel.md` and issues
#3218 (this restructure) / #2738 (the Silk.NET runtime that follows).
