---
name: gum-source-linked-project
description: Creating a new MonoGame project that references Gum via ProjectReference to source instead of NuGet, for manually running an unreleased runtime change. Triggers: "linked to Gum source", "link to source", scratch test project, manual verification of a MonoGameGum/GumCommon/RenderingLibrary change.
---

# Gum Source-Linked Project

For manually running a MonoGameGum/GumCommon change in a real game (see the `tdd` skill for when a unit test isn't enough). Two of Gum's own samples are already working reference projects for this exact pattern — start from one of them rather than guessing:

- **Code-only** (no `.gumx`, UI built via C#): `Samples/MonoGameGumInCode/MonoGameGumInCode/MonoGameGumInCode.csproj`
- **File-based** (loads a real `.gumx` from `Content/`): `Samples/GumFormsSample/MonoGameGumFormsSample/MonoGameGumFormsSample.csproj`

## Fastest path: `dotnet new mgdesktopgl`, then rewire

1. `dotnet new install MonoGame.Templates.CSharp` — not bundled with the SDK.
2. `dotnet new mgdesktopgl -o <dir> -n <ProjectName>`.
3. In the generated `.csproj`: set `<TargetFramework>net8.0</TargetFramework>` (the template defaults to the newest installed SDK, which can be ahead of Gum's `net8.0`) and pin `MonoGame.Framework.DesktopGL`/`MonoGame.Content.Builder.Task` to `3.8.4.1` — the exact version every Gum sample uses — instead of the template's floating `3.8.*`.
4. Add `<ProjectReference Include="..\..\Gum\MonoGameGum\MonoGameGum.csproj" />` (path relative to wherever the new project sits).
5. In `Game1.cs`: `GumService.Default.Initialize(this)` in `Initialize()` for code-only defaults, `GumUI.Update(gameTime)` in `Update`, `GumUI.Draw()` in `Draw`. For a `.gumx`-loading project instead, pass the project's relative path as `Initialize`'s second argument and mirror the `Content\<ProjectFolder>\**\*.*` `CopyToOutputDirectory` item group from `MonoGameGumFormsSample.csproj`.

Verified: this recipe builds clean (0 new errors) against Gum's current source tree.
