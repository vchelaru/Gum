---
name: gum-bundle
description: The .gumpkg bundle (format, gumcli pack, raw textures, loading). Triggers: gumpkg, GumBundleWriter, GumBundleLoader, GumProjectDependencyWalker, GumRawTexture, gumcli pack, packing a new file type.
---

# .gumpkg Bundles

A `.gumpkg` is one file holding a project's loose files. Code lives in `GumCommon/Bundle/`; `gumcli pack` is `Tools/Gum.Cli/Commands/PackCommand.cs`. See [gum-cli](../gum-cli/SKILL.md) for the command.

## Shape

- Header is the 4 bytes `GUMP` plus a version byte (`GumBundleFormat`). Version 2 is a zip with one Deflate entry per file; version 1 (brotli tar) is still read.
- Each entry is keyed by the file's original project-relative path, so references in the project resolve unchanged.

## Pack flow

1. `GumProjectDependencyWalker.Walk` lists every file the project references (Core, FontCache, External). It is the one place that decides what a project depends on, so a new file type must be recognized there first.
2. `PackCommand` rewrites PNG entries: it decodes them with StbImageSharp and stores raw RGBA (`GumRawTexture`, optionally premultiplied with `--premultiplied-alpha`) under the same path. A runtime tells a raw texture from a PNG by `GumRawTexture.IsRawTexture`, so the texture upload skips decoding.
3. `GumBundleLoader` picks bundle or loose mode from the project's extension (`.gumpkg` versus `.gumx`/`.gumj`). Bundle entries reach the runtime through `FileManager`'s host stream hook, which wins over loose files.

## Landmines

- A new source file type needs three changes: the walker (to list it), `PackCommand` (to convert it), and each runtime's content loader (`RenderingLibrary/Content/ContentLoader.cs`, plus the Skia and Raylib loaders that have their own). Missing any one makes the packed project fail only at runtime.
- Entries are keyed by original path, so one file used as two different things would need two entries under one key. The format has no way to do that today.
- How animation chain files (`.achx`/`.achj`) are packed and loaded has not been traced here.
