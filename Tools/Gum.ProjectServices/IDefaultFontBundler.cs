namespace Gum.ProjectServices;

/// <summary>
/// Copies the bundled default Text font (Liberation Sans, SIL OFL 1.1) into a project's
/// <c>Fonts</c> folder. Every "create a new project" entry point (<see cref="ProjectCreator"/>,
/// the Gum tool's File → New) needs this: <see cref="Managers.StandardElementsManager"/>'s Text
/// standard points its Font default at this bundled file instead of a system font name (e.g.
/// "Arial"), because BlazorGL/WASM has no OS font store for KernSmith to resolve a system font
/// name against (#4276). Liberation Sans is metrically compatible with Arial and free to
/// redistribute, so the license file travels alongside it.
/// </summary>
public interface IDefaultFontBundler
{
    /// <summary>
    /// Extracts the bundled font and its license into <paramref name="projectDirectory"/>/Fonts.
    /// </summary>
    void CopyTo(string projectDirectory);
}
