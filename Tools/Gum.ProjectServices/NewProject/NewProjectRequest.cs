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

    private NewProjectPackageChannel _packageChannel;

    /// <summary>
    /// Which Gum packages the code project uses: stable NuGet (the default), preview (prerelease) NuGet, or the local Gum
    /// checkout. Preview and local source need a platform.
    /// </summary>
    public NewProjectPackageChannel PackageChannel
    {
        get => _packageChannel;
        init => _packageChannel = value;
    }

    /// <summary>
    /// Shorthand for <see cref="PackageChannel"/> being <see cref="NewProjectPackageChannel.LocalSource"/>: reference the Gum
    /// checkout's runtime project instead of the NuGet package. Needs a platform. Setting it to <see langword="false"/> changes nothing.
    /// </summary>
    public bool SourceLinked
    {
        get => _packageChannel == NewProjectPackageChannel.LocalSource;
        init
        {
            if (value)
            {
                _packageChannel = NewProjectPackageChannel.LocalSource;
            }
        }
    }

    /// <summary>Run <c>dotnet restore</c> on the code project after creating it. Only used with a platform.</summary>
    public bool Restore { get; init; } = true;

    /// <summary>
    /// Also add the NuGet add-ons the setup docs recommend for the platform (<see cref="RecommendedPackages"/>) to the code
    /// project, before the restore. Needs a platform. With <see cref="SourceLinked"/> the add-ons are linked as projects of the checkout
    /// (<see cref="RecommendedPackages.ForSourceLinked"/>) while the files are written. The command line is
    /// <c>gumcli new ... &amp;&amp; dotnet add "&lt;csproj&gt;" package ...</c> (<c>reference</c> when source-linked); <c>gumcli new</c> itself has no option for it.
    /// </summary>
    public bool IncludeRecommendedPackages { get; init; }

    /// <summary>
    /// Also generate code for the whole new project (<c>gumcli codegen "&lt;gumj&gt;"</c>) after the restore, and
    /// then have the host game create the demo screen's generated class instead of loading the screen at runtime. Needs a platform;
    /// does nothing without the Forms template (nothing to generate). <c>gumcli new</c> has no option for it: it is its own command in the chain.
    /// </summary>
    public bool GenerateCode { get; init; }

    /// <summary>
    /// For a Gum-only project whose <see cref="Path"/> has no project extension: create a <c>.gumx</c> (XML)
    /// project instead of the default <c>.gumj</c> (JSON). A path ending in <c>.gumx</c>/<c>.gumj</c> decides by itself.
    /// </summary>
    public bool UseXmlFormat { get; init; }
}
