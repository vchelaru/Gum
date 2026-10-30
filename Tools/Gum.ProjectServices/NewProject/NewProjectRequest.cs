namespace Gum.ProjectServices;

/// <summary>
/// What to create: the options of <c>gumcli new</c>, in the same terms the command line uses.
/// </summary>
public class NewProjectRequest
{
    /// <summary>
    /// The path as <c>gumcli new</c> reads it. Without a platform: a <c>.gumj</c>/<c>.gumx</c> file path, or a
    /// project name or folder (creates <c>&lt;path&gt;/&lt;name&gt;.gumj</c>); empty means <c>GumProject/GumProject.gumj</c>
    /// in the current folder. With a platform: the game folder (never a project file; empty means <c>MyGumGame</c>),
    /// holding the Gum project at <c>Content/GumProject/GumProject.gumj</c>. A dialog passes <c>Path.Combine(location, name)</c>.
    /// </summary>
    public string? Path { get; init; }

    /// <summary><c>forms</c> or <c>empty</c>.</summary>
    public string Template { get; init; } = "forms";

    /// <summary>A bundled Forms theme name, or <c>Standard</c> (the plain Forms controls). Needs the forms template.</summary>
    public string? Theme { get; init; }

    /// <summary>A name from <see cref="NewProjectPlatforms"/> to also create a code project, or <see langword="null"/> for the Gum project only.</summary>
    public string? Platform { get; init; }

    /// <summary>Reference the Gum checkout's runtime project instead of the NuGet package. Needs a platform.</summary>
    public bool SourceLinked { get; init; }

    /// <summary>Run <c>dotnet restore</c> on the code project after creating it. Only used with a platform.</summary>
    public bool Restore { get; init; } = true;

    /// <summary>
    /// For a Gum-only project whose <see cref="Path"/> has no project extension: create a <c>.gumx</c> (XML)
    /// project instead of the default <c>.gumj</c> (JSON). A path ending in <c>.gumx</c>/<c>.gumj</c> decides by itself.
    /// </summary>
    public bool UseXmlFormat { get; init; }
}
