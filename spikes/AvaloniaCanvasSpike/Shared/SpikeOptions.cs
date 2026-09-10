namespace AvaloniaCanvasSpike;

/// <summary>Command-line options: an optional project path and element name.</summary>
public sealed class SpikeOptions
{
    /// <summary>Path of the sample project used when none is given on the command line.</summary>
    public const string DefaultProjectRelativePath =
        "Samples/MonoGameGumFromFile/MonoGameGumFromFile/Content/GumProject.gumx";

    /// <summary>Creates options from the command line.</summary>
    public SpikeOptions(string backendName, string projectPath, string? elementName)
    {
        BackendName = backendName;
        ProjectPath = projectPath;
        ElementName = elementName;
    }

    /// <summary>Which XNA-family backend this build uses, for the window title.</summary>
    public string BackendName { get; }

    /// <summary>Absolute path of the .gumx/.gumj to load.</summary>
    public string ProjectPath { get; }

    /// <summary>Screen or component to show; the project's first screen when null.</summary>
    public string? ElementName { get; }

    /// <summary>
    /// Parses <c>[project.gumx] [ElementName]</c>. Without a project argument, walks up from the
    /// executable to the repo root and uses <see cref="DefaultProjectRelativePath"/>.
    /// </summary>
    public static SpikeOptions Parse(string backendName, string[] args)
    {
        string? projectPath = args.Length > 0 ? Path.GetFullPath(args[0]) : FindDefaultProject();
        if (projectPath == null || !File.Exists(projectPath))
        {
            throw new FileNotFoundException(
                "Pass a .gumx path as the first argument, or run from inside the Gum repo so the sample project can be found.",
                projectPath);
        }

        string? elementName = args.Length > 1 ? args[1] : null;
        return new SpikeOptions(backendName, projectPath, elementName);
    }

    private static string? FindDefaultProject()
    {
        DirectoryInfo? directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null)
        {
            string candidate = Path.Combine(directory.FullName, DefaultProjectRelativePath);
            if (File.Exists(candidate))
            {
                return candidate;
            }
            directory = directory.Parent;
        }
        return null;
    }
}
