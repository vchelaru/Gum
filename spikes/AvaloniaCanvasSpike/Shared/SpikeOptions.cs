namespace AvaloniaCanvasSpike;

/// <summary>
/// Command-line options: <c>[project.gumx] [ElementName] [--exit-after seconds] [--screenshot path.png]</c>.
/// </summary>
public sealed class SpikeOptions
{
    /// <summary>Path of the sample project used when none is given on the command line.</summary>
    public const string DefaultProjectRelativePath =
        "Samples/MonoGameGumFromFile/MonoGameGumFromFile/Content/GumProject.gumx";

    /// <summary>Creates options from parsed values.</summary>
    public SpikeOptions(string backendName, string projectPath, string? elementName, double? exitAfterSeconds, string? screenshotPath)
    {
        BackendName = backendName;
        ProjectPath = projectPath;
        ElementName = elementName;
        ExitAfterSeconds = exitAfterSeconds;
        ScreenshotPath = screenshotPath;
    }

    /// <summary>Which XNA-family backend this build uses, for the window title.</summary>
    public string BackendName { get; }

    /// <summary>Absolute path of the .gumx/.gumj to load.</summary>
    public string ProjectPath { get; }

    /// <summary>Screen or component to show; the project's first screen when null.</summary>
    public string? ElementName { get; }

    /// <summary>When set, the app closes itself this many seconds after the window opens.</summary>
    public double? ExitAfterSeconds { get; }

    /// <summary>When set, the window is rendered to this PNG just before exiting.</summary>
    public string? ScreenshotPath { get; }

    /// <summary>
    /// Parses the command line. Without a project argument, walks up from the executable to
    /// the repo root and uses <see cref="DefaultProjectRelativePath"/>.
    /// </summary>
    public static SpikeOptions Parse(string backendName, string[] args)
    {
        List<string> positional = new List<string>();
        double? exitAfter = null;
        string? screenshot = null;

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--exit-after" when i + 1 < args.Length:
                    exitAfter = double.Parse(args[++i], System.Globalization.CultureInfo.InvariantCulture);
                    break;
                case "--screenshot" when i + 1 < args.Length:
                    screenshot = Path.GetFullPath(args[++i]);
                    break;
                default:
                    positional.Add(args[i]);
                    break;
            }
        }

        string? projectPath = positional.Count > 0 ? Path.GetFullPath(positional[0]) : FindDefaultProject();
        if (projectPath == null || !File.Exists(projectPath))
        {
            throw new FileNotFoundException(
                "Pass a .gumx path as the first argument, or run from inside the Gum repo so the sample project can be found.",
                projectPath);
        }

        string? elementName = positional.Count > 1 ? positional[1] : null;
        return new SpikeOptions(backendName, projectPath, elementName, exitAfter, screenshot);
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
