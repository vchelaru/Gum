---
name: gum-tool-logging
description: Diagnostic logging in the Gum tool via Gum.Diagnostics.ToolLog. Triggers: adding logging to hunt a bug, ToolLog, tool.log, TextureDiagnostics, "add logs", reading a log the user reproduced into.
---

# Gum Tool Logging

All tool diagnostics go through `Gum.Diagnostics.ToolLog` (`Tools/Gum.Presentation/Diagnostics/ToolLog.cs`). Never add a second logger. `Wireframe/TextureDiagnostics.cs` is an example of a thin helper that builds on it.

## Rules (from the user)

- Debug builds only. Release writes nothing and allocates nothing.
- One file per tool session, named by the time the app started, so history is kept. The user never clears or deletes a log; the agent reads the newest one.
- The agent deletes logs older than a month when it reads logs. The tool does not prune them.
- Logging grows over time: add stages and events whenever they make the picture clearer. Start lines with a searchable prefix (`texture.drop`) and rename prefixes freely.
- Never log per frame. Logging in shared runtime code (`GumCommon`, `RenderingLibrary`, `MonoGameGum`, other runtimes) is temporary and comes out once the bug is found; tool-side logging stays.

## Where the files are

`%TEMP%\Gum\Logs\tool-yyyyMMdd-HHmmss.log`, one per session (`ToolLog.DefaultPath`); read the newest. `GUM_TOOL_LOG` overrides the path. `GumPreview` has its own log (`PreviewDebugLog`, `preview.log` plus `.previous`) that shares `DiagnosticLogFile`; tests are in `AlwaysOnDebugLogTests`.
