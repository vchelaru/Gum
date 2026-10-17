using System;
using System.Collections.Generic;
using WpfDataUi.DataTypes;

namespace CodeOutputPlugin;

/// <summary>
/// The content of the Code generation page of the Settings window: the project-wide code generation
/// rows, built by the same <see cref="CodeOutputSettingsMembers"/> the Code tab uses for the selected
/// element. An edit made here raises the members' <see cref="CodeOutputSettingsMembers.SettingsChanged"/>,
/// which the Code Output plugin already saves and regenerates on.
/// </summary>
public class CodeGenerationSettingsViewModel
{
    private readonly CodeOutputSettingsMembers _members;

    /// <summary>Creates the page content over the plugin's settings members.</summary>
    public CodeGenerationSettingsViewModel(CodeOutputSettingsMembers members)
    {
        _members = members;
        _members.RebuildRequested += (_, _) => Reloaded?.Invoke();
    }

    /// <summary>Raised when the view should rebuild its rows from <see cref="BuildCategories"/>.</summary>
    public event Action? Reloaded;

    /// <summary>The rows the page shows: the project-wide category.</summary>
    public List<MemberCategory> BuildCategories() => new List<MemberCategory> { _members.BuildProjectCategory() };

    /// <summary>Asks the view to rebuild its rows, as when the page is opened.</summary>
    public void Reload() => Reloaded?.Invoke();
}
