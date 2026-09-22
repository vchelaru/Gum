namespace Gum.ProjectServices;

/// <summary>
/// Merges the Forms template's Components, Standards, Behaviors, Fonts, and UISpriteSheet into
/// an already-existing Gum project, skipping anything the project already references.
/// </summary>
public interface IAddFormsToProjectService
{
    /// <summary>
    /// Loads the project at <paramref name="projectFilePath"/> and adds any Forms template
    /// component/standard/behavior it doesn't already reference, saving the result.
    /// </summary>
    /// <param name="projectFilePath">Absolute path to an existing .gumx or .gumj project.</param>
    AddFormsResult AddFormsTo(string projectFilePath);
}
