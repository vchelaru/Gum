using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace GumPreview;

/// <summary>
/// GumPreview's command line. The tool's launcher passes <c>--project</c>, <c>--element</c>,
/// <c>--selection-file</c> and <c>--content-root</c> (PreviewProcessStartInfoBuilder in
/// Tool/EditorTabPlugin.Core); <c>--exit-after</c> and <c>--screenshot</c> are for unattended runs
/// such as the release package smoke test (#5471), and <c>--focus</c>/<c>--type</c> put a control
/// into a runtime-only state before that capture.
/// </summary>
public sealed class PreviewOptions
{
    /// <summary>Usage text printed with <see cref="Error"/>.</summary>
    public const string Usage =
        "Usage: GumPreview --project <path to .gumx/.gumj> --element <ScreenOrComponentName> [--selection-file <path>] [--content-root <path>] [--exit-after <seconds> [--screenshot <path.png>]] [--focus <InstanceName> [--type <text>]]";

    private PreviewOptions()
    {
    }

    /// <summary>The .gumx or .gumj to load.</summary>
    public string? ProjectPath { get; private set; }

    /// <summary>The screen or component to show.</summary>
    public string? ElementName { get; private set; }

    /// <summary>The file the tool rewrites to hand over new selections.</summary>
    public string? SelectionFilePath { get; private set; }

    /// <summary>Where relative content resolves from, when the project is a converted copy elsewhere.</summary>
    public string? ContentRootDirectory { get; private set; }

    /// <summary>
    /// When set, the run is unattended: it exits once the project has loaded and a frame has been
    /// drawn, or with exit code 1 if that has not happened within this many seconds.
    /// </summary>
    public double? ExitAfterSeconds { get; private set; }

    /// <summary>With <see cref="ExitAfterSeconds"/>, the absolute path of the PNG the first frame is written to.</summary>
    public string? ScreenshotPath { get; private set; }

    /// <summary>The Forms control to focus once the element is shown, so its focused look can be captured.</summary>
    public string? FocusName { get; private set; }

    /// <summary>With <see cref="FocusName"/>, text entered into that text box as if typed.</summary>
    public string? TypedText { get; private set; }

    /// <summary>Why the command line is unusable, or null when it is usable.</summary>
    public string? Error { get; private set; }

    /// <summary>Parses GumPreview's arguments. Unknown arguments are ignored.</summary>
    public static PreviewOptions Parse(IReadOnlyList<string> args)
    {
        PreviewOptions options = new PreviewOptions();
        string? exitAfterText = null;
        for (int i = 0; i < args.Count; i++)
        {
            bool hasValue = i + 1 < args.Count;
            if (args[i] == "--project" && hasValue)
            {
                options.ProjectPath = args[++i];
            }
            else if (args[i] == "--element" && hasValue)
            {
                options.ElementName = args[++i];
            }
            else if (args[i] == "--selection-file" && hasValue)
            {
                options.SelectionFilePath = args[++i];
            }
            else if (args[i] == "--content-root" && hasValue)
            {
                options.ContentRootDirectory = args[++i];
            }
            else if (args[i] == "--exit-after" && hasValue)
            {
                exitAfterText = args[++i];
            }
            else if (args[i] == "--screenshot" && hasValue)
            {
                options.ScreenshotPath = Path.GetFullPath(args[++i]);
            }
            else if (args[i] == "--focus" && hasValue)
            {
                options.FocusName = args[++i];
            }
            else if (args[i] == "--type" && hasValue)
            {
                options.TypedText = args[++i];
            }
        }

        if (string.IsNullOrEmpty(options.ProjectPath) || string.IsNullOrEmpty(options.ElementName))
        {
            options.Error = "--project and --element are required.";
        }
        else if (options.TypedText != null && options.FocusName == null)
        {
            options.Error = "--type needs --focus to name the text box to type into.";
        }
        else if (exitAfterText != null)
        {
            if (double.TryParse(exitAfterText, NumberStyles.Float, CultureInfo.InvariantCulture, out double seconds) && seconds > 0)
            {
                options.ExitAfterSeconds = seconds;
            }
            else
            {
                options.Error = $"--exit-after needs a positive number of seconds, not '{exitAfterText}'.";
            }
        }
        else if (options.ScreenshotPath != null)
        {
            options.Error = "--screenshot needs --exit-after.";
        }
        return options;
    }
}
