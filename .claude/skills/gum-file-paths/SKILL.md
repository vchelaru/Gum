---
name: gum-file-paths
description: ToolsUtilities.FilePath comparison and normalization semantics, plus cross-platform file-move traps. Triggers: FilePath, Standardized, FullPath, comparing a computed path to one from Directory.EnumerateFiles, case-only renames.
---

# Gum File Path Semantics

`ToolsUtilities.FilePath` is the repo's path type. Each difference below from raw-string behavior fails
silently — wrong result, no exception.

## `==` is case-insensitive; `FullPath` is case-preserving

`FilePath` equality compares `Standardized`, which is lowercased. `FullPath` keeps the original casing.
Any logic about a case-only rename (`Foo` → `foo`) must compare `FullPath` ordinally — `oldPath == newPath`
reports "nothing changed" for exactly the rename it needs to detect.

## `Standardized` normalizes to `Path.DirectorySeparatorChar`, not `/`

`FileManager.RemoveDotDotSlash` does the conversion. A hardcoded `Contains("/bin/")` check therefore
matches on Unix and is dead on Windows. This bites production code, not just test assertions.

## `..` is collapsed, `.` is not

A `CodeProjectRoot` of `"./"` yields paths like `C:\Proj\.\Screens\X.cs`, which never compare equal to
what `Directory.EnumerateFiles` returns. Any code diffing computed paths against enumerated ones must
run both sides through `Path.GetFullPath` first. `CodeGenerationFileLocationsService` output is the
usual source of the computed side.

## `File.Move` on a case-only rename is platform-divergent

Windows handles it directly; .NET pre-checks the destination on Unix and throws "already exists" on a
case-insensitive macOS volume. Move through a temp name in two steps. A single-step move passes on
Windows and fails only on the macOS CI leg, so a local run does not prove it.

## Related

- `gum-unit-tests` — writing path assertions that hold on both Windows and Unix CI legs.
