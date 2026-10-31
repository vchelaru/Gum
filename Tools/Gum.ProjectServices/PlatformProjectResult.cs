namespace Gum.ProjectServices;

/// <summary>
/// Outcome of <see cref="IPlatformProjectScaffolder.Create"/>.
/// </summary>
public class PlatformProjectResult
{
    /// <summary>Gets whether the host project and Gum project were created.</summary>
    public bool Success { get; init; }

    /// <summary>Gets the human-readable failure reason when <see cref="Success"/> is <see langword="false"/>.</summary>
    public string ErrorMessage { get; init; } = "";

    /// <summary>Gets the full path of the generated host .csproj.</summary>
    public string CsprojPath { get; init; } = "";

    /// <summary>Gets the full path of the generated Gum project file (.gumj, or .gumx for the XML format).</summary>
    public string GumProjectPath { get; init; } = "";

    /// <summary>The demo screen the host game shows, or <see langword="null"/> when there is none.</summary>
    public DemoScreenInfo? DemoScreen { get; init; }
}
