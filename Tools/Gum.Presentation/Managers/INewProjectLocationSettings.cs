namespace Gum.Managers;

/// <summary>
/// The folder the New Project dialog offers as the parent of the next project: the parent of the
/// project folder the last successfully created project used. Stored in the tool's user settings
/// (<c>GeneralSettings.xml</c>), so it is shared by every head. The live implementation is
/// <c>ProjectManager</c>, bridged via DI to the same singleton.
/// </summary>
public interface INewProjectLocationSettings
{
    /// <summary>The remembered parent folder, or null when none was ever saved. It may no longer exist.</summary>
    string? LastNewProjectParent { get; }

    /// <summary>Remembers <paramref name="parentFolder"/> and saves the user settings.</summary>
    void RememberNewProjectParent(string parentFolder);
}
