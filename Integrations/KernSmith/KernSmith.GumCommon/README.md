# KernSmith.GumCommon

Shared integration logic that bridges KernSmith bitmap font generation with Gum's BmfcSave font descriptor.

## Overview

This package provides the common mapping layer used by all platform-specific Gum integration packages (`KernSmith.MonoGameGum`, `KernSmith.FnaGum`, `KernSmith.KniGum`). It translates Gum's `BmfcSave` font configuration into KernSmith's `FontGeneratorOptions` and drives the font generation pipeline.

By isolating the shared logic here, each platform package only needs to handle framework-specific concerns like texture creation.

**Target**: `net8.0`

This integration is maintained in the [Gum repository](https://github.com/vchelaru/Gum) and depends on the [KernSmith](https://github.com/kaltinril/KernSmith) bitmap-font rasterizer (consumed as a NuGet package).

## Default "Arial"

If building "Arial" fails (Linux without the Microsoft core fonts, the browser), the package registers its bundled Liberation Sans Regular (SIL OFL 1.1, license in the package) under "Arial" and retries once, so default text stays sharp when zoomed. An installed Arial, or one you registered with `RegisterFont`, is never replaced. Bold and italic are synthesized by KernSmith from the regular face (bold is wider and heavier, italic is slanted), so no separate bold or italic files are needed.
