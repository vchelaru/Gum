# ThirdParty

Vendored builds of third-party dependencies that need a local patch not yet available upstream. Not a general vendoring convention — only exists because a specific fix was needed faster than the upstream project's release cadence allows. Prefer the real NuGet package whenever possible; only add here when a patch is required and blocked upstream.

## nuget-local/

A local folder-based NuGet feed (see `/NuGet.config`). Each `.nupkg` here is a source-identical rebuild of a real package, patched, with only the version bumped (`<original>-gumfix.N`) so it can't be confused with the real release.

| Package | Why vendored | Revert when |
|---|---|---|
| `Topten.RichTextKit.0.4.167-gumfix.1` | `Style.HaloWidth` spikes at acute glyph corners (missing `StrokeJoin = Round`), needed clean for SkiaGum's per-run outline tag. Issue: [toptensoftware/RichTextKit#113](https://github.com/toptensoftware/RichTextKit/issues/113). Fix: [#114](https://github.com/toptensoftware/RichTextKit/pull/114). Referenced by both `Runtimes/SkiaGum/SkiaGum.csproj` and `Runtimes/SilkNetGum/SilkNetGum.csproj` — the latter links SkiaGum's `Renderables/*.cs` source directly but has its own separate `PackageReference`, so it needs the same version or it silently runs the shared `Text.cs` against the unpatched halo behavior. | PR #114 ships in a real release — bump both csproj's `Topten.RichTextKit` references back to the real version and delete this package. |

To rebuild a vendored package after re-patching the fork: `dotnet pack <csproj> -c Release -p:TargetFrameworks=<tfm> -p:TtsInheritDoc=false -p:TtsCodeSign=false -p:Version=<version>-gumfix.N -o ThirdParty/nuget-local` (adjust build-tool skip flags per package; RichTextKit's are its own `buildtools` submodule quirks, not a general rule).
