# ThirdParty

Vendored builds of third-party dependencies that need a local patch not yet available upstream. Not a general vendoring convention — only exists because a specific fix was needed faster than the upstream project's release cadence allows. Prefer the real NuGet package whenever possible; only add here when a patch is required and blocked upstream.

## Gum.Topten.RichTextKit

Gum's fork of [Topten.RichTextKit](https://github.com/toptensoftware/RichTextKit) 0.4.167 with one patch: round stroke joins so `Style.HaloWidth` outlines don't spike at acute glyph corners ([issue #113](https://github.com/toptensoftware/RichTextKit/issues/113), [fix #114](https://github.com/toptensoftware/RichTextKit/pull/114), merged upstream but not yet released). SkiaGum, SilkNetGum and StrideGum reference it; keep all three on the same version.

It is published to nuget.org as `Gum.Topten.RichTextKit` so Gum.SkiaSharp consumers can restore it. The assembly inside is still `Topten.RichTextKit.dll`, so the source code is unchanged. `nuget-local/` holds the same `.nupkg` (see `/NuGet.config`) so the repo builds without depending on nuget.org having it.

To re-pack after a new patch: bump the version in `Gum.Topten.RichTextKit/Gum.Topten.RichTextKit.nuspec`, put the rebuilt `lib/netstandard2.0/Topten.RichTextKit.{dll,pdb,xml}` and `nuget-icon.png` next to the nuspec, and pack it with a `NoBuild` csproj whose `NuspecFile` points at it. Then publish the `.nupkg` to nuget.org and replace the copy in `nuget-local/`.

Switch back to the real package once upstream releases the fix: #5499 lists the steps.
