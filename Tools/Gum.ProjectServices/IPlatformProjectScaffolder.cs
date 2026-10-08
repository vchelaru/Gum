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
    /// <c>ProjectCodeSettings.codsj</c> for the platform. Gum is referenced through NuGet.
    /// </summary>
    /// <param name="projectDirectory">The folder for the new project. Its name becomes the project name.</param>
    /// <param name="platform">The runtime to target.</param>
    /// <param name="includeFormsTemplate">Whether the Gum project gets the Forms controls template or only the standard elements.</param>
    PlatformProjectResult Create(string projectDirectory, HostPlatform platform, bool includeFormsTemplate);
}
