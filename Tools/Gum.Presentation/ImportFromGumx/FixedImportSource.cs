using System;

namespace ImportFromGumxPlugin.ViewModels;

/// <summary>
/// Opens the Import from .gumx dialog on a source the caller already chose, so the user only picks
/// which items to import (Add Forms uses this for a theme's own project). Items the project does not
/// have yet start checked; items it already has are marked and start unchecked.
/// </summary>
/// <param name="GumxPath">The source project. Its elements keep their names; no destination subfolder is added.</param>
/// <param name="Title">The dialog title.</param>
/// <param name="IncludeStandards">Whether the source's Standards are offered.</param>
/// <param name="BeforeProjectSave">Runs after a successful import, just before the project is saved and reloaded.</param>
/// <param name="PreselectScreens">Whether screens the project does not have yet start checked like components do.</param>
public record FixedImportSource(
    string GumxPath,
    string Title,
    bool IncludeStandards,
    Action? BeforeProjectSave,
    bool PreselectScreens = false);
