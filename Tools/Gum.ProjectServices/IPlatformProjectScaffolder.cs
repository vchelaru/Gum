namespace Gum.ProjectServices;

/// <summary>
/// Creates a runnable host game project for a <see cref="HostPlatform"/> together with a Gum project
/// inside it, with code generation already pointed at the host project.
/// </summary>
public interface IPlatformProjectScaffolder
{
    /// <summary>
    /// Writes the host .csproj and startup code into <paramref name="projectDirectory"/> (named after the
    /// folder), creates a Gum project at <c>Content/GumProject/GumProject.gumj</c>, and writes
    /// <c>ProjectCodeSettings.codsj</c> for the platform. Gum is referenced through NuGet unless <paramref name="gumSourceDirectory"/> is set.
    /// </summary>
    /// <param name="projectDirectory">The folder for the new project. Its name becomes the project name.</param>
    /// <param name="platform">The runtime to target.</param>
    /// <param name="includeFormsTemplate">Whether the Gum project gets the Forms controls template or only the standard elements.</param>
    /// <param name="gumSourceDirectory">
    /// The root of a local Gum checkout. When set, the host references that checkout's runtime project
    /// instead of the NuGet package, and fails if the project is not there.
    /// </param>
    /// <param name="themeDirectory">
    /// A Forms theme folder (see <see cref="IFormsThemeLocator"/>) to create the Gum project from instead of the
    /// plain Forms template. Takes precedence over <paramref name="includeFormsTemplate"/>.
    /// </param>
    PlatformProjectResult Create(
        string projectDirectory, HostPlatform platform, bool includeFormsTemplate, string? gumSourceDirectory = null,
        string? themeDirectory = null);
}
