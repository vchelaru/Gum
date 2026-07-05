---
name: xnafiddle
description: XnaFiddle — Victor's site for XNA-syntax fiddles that run on web and export to a chosen runtime/platform, optionally linking engine source. Triggers: XnaFiddle, fiddle, cross-backend web repro, engine-source experiment, upstream KNI/MonoGame/FNA repro.
---

# XnaFiddle

**XnaFiddle is Victor's own site** (he maintains it) — so it's authoritative and feature-requestable, not a black-box third party. It lets you author a *fiddle*: a small project written in XNA syntax that compiles and runs in the browser.

## What it's for in Gum

Two jobs the checked-in `Samples/` projects don't cover as well:

1. **A source-linked, multi-platform engine experiment.** On **export** you choose the target runtime (**MonoGame / KNI / FNA**) and platforms (e.g. **desktop, web, Android** for KNI), and the export can **link against engine source** instead of the NuGet package — even emitting one `.slnx` with per-platform projects. This is what makes it possible to toggle an engine's build constants and prove *engine-side* behavior, not just observe the shipped build.
2. **A shareable public repro** for an upstream engine issue (KNI/MonoGame/FNA), so the maintainer can run it in a couple of clicks.

## Landmine — it is NOT a fixed prebuilt playground

The obvious assumption — "an online playground bakes in one engine, so you can't change the backend or its build" — is **wrong** for XnaFiddle. Because export chooses the runtime *and* can source-link, a fiddle can swap backends and run against engine source you control. Don't dismiss it as an investigation vehicle on the belief that it only observes a fixed build.

## Relationship to the local Gum samples

`Samples/KniGum*` (and the MonoGame/FNA equivalents) already reproduce across backends and link the **Gum runtime** from source, with the engine itself via NuGet — best when the question is "does Gum consume the shipped engine correctly." Reach for XnaFiddle when you instead need a self-contained **engine-source** experiment or a public repro: it's the stronger tool for *varying the engine*, weaker for exercising Gum's own runtime (a fiddle is engine-level XNA, not Gum-aware by default).
