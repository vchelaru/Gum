# KernSmith.GumCommon

Shared integration logic that bridges KernSmith bitmap font generation with Gum's BmfcSave font descriptor.

## Overview

This package provides the common mapping layer used by all platform-specific Gum integration packages (`KernSmith.MonoGameGum`, `KernSmith.FnaGum`, `KernSmith.KniGum`). It translates Gum's `BmfcSave` font configuration into KernSmith's `FontGeneratorOptions` and drives the font generation pipeline.

By isolating the shared logic here, each platform package only needs to handle framework-specific concerns like texture creation.

**Target**: `net8.0`

This integration is maintained in the [Gum repository](https://github.com/vchelaru/Gum) and depends on the [KernSmith](https://github.com/kaltinril/KernSmith) bitmap-font rasterizer (consumed as a NuGet package).
