namespace Gum.Logic;

/// <summary>
/// Creates a new project and populates it with a usable starting point. Used both by the
/// File menu's New Project command and by startup when there is no project to reopen, so a
/// first-time user lands in the same place either way.
/// </summary>
public interface INewProjectLogic
{
    /// <summary>
    /// Replaces the loaded project with a new one, prompting for a save location and (unless the
    /// user opts out) importing the default Forms theme and adding a starting screen. A cancelled
    /// prompt leaves an empty, unsaved project.
    /// </summary>
    void CreateNewProject();
}
