using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Gum.CommandLine;
using Gum.Startup.SingleInstance;

namespace Gum.Avalonia;

/// <summary>
/// Options the Avalonia head reads from its own command line for unattended verification runs:
/// <c>--exit-after seconds</c>, <c>--screenshot path.png</c>, <c>--select Element[#Instance]</c>,
/// <c>--theme light|dark</c>, <c>--user-data folder</c>, <c>--zoom-to-fit</c>, <c>--no-handoff</c> and
/// <c>--await-open-requests count</c>. Everything else is left for
/// <see cref="Gum.CommandLine.CommandLineManager"/>, which reads the process command line itself.
/// </summary>
public sealed class HeadOptions
{
    /// <summary>Creates the options.</summary>
    public HeadOptions(double? exitAfterSeconds, string? screenshotPath, string? selectPath = null, string? theme = null, string? userDataFolder = null,
        bool zoomToFit = false, string? startupFileError = null, OpenRequest? handoffRequest = null, bool noHandoff = false,
        int? awaitOpenRequests = null)
    {
        HandoffRequest = handoffRequest;
        NoHandoff = noHandoff;
        AwaitOpenRequests = awaitOpenRequests;
        StartupFileError = startupFileError;
        ExitAfterSeconds = exitAfterSeconds;
        ScreenshotPath = screenshotPath;
        SelectPath = selectPath;
        Theme = theme;
        UserDataFolder = userDataFolder;
        ZoomToFit = zoomToFit;
    }

    /// <summary>
    /// When set, the run is unattended: the app closes itself once the project has loaded and the
    /// canvas has drawn (see <see cref="Diagnostics.UnattendedRun"/>), and fails with a nonzero exit
    /// code if that hasn't happened this many seconds after the main window opens. It is killed if
    /// it is still running <see cref="Diagnostics.UnattendedExitDeadline.GracePeriod"/> after that
    /// many seconds from launch.
    /// </summary>
    public double? ExitAfterSeconds { get; }

    /// <summary>
    /// When set with <see cref="ExitAfterSeconds"/>, the main window is rendered to this PNG once
    /// the run is ready, just before exiting.
    /// </summary>
    public string? ScreenshotPath { get; }

    /// <summary>
    /// When set with <see cref="ExitAfterSeconds"/>, once startup has finished the canvas zooms and
    /// scrolls so the selected element and its visible descendants fit the Editor view, at no more
    /// than 100%, before the screenshot. The applied camera is reported on stdout.
    /// </summary>
    public bool ZoomToFit { get; }

    /// <summary>
    /// When set, the element (and optionally the instance, after a <c>#</c>) selected once the
    /// project has loaded, so a run can show the panels that follow the selection.
    /// </summary>
    public string? SelectPath { get; }

    /// <summary>
    /// When set to "light" or "dark", the theme variant shown for this run only; the saved theme
    /// setting is left unchanged.
    /// </summary>
    public string? Theme { get; }

    /// <summary>
    /// When set, the folder that holds this run's per-user files (GeneralSettings.xml and the rest)
    /// in place of the user's own, so an unattended run leaves the user's last project and recent
    /// list alone.
    /// </summary>
    public string? UserDataFolder { get; }

    /// <summary>
    /// Why the project or element file named on the command line cannot open, or null when it can
    /// (or none is named). Loading a missing one shows a modal error that nobody answers in an
    /// unattended run, so the run would sit until <c>--exit-after</c> (#7374); a path that is no Gum
    /// file would open an empty tool and still report success.
    /// </summary>
    public string? StartupFileError { get; }

    /// <summary>
    /// The project, and the element when an element file is named, that this launch asks for; null
    /// when no usable project file is on the command line. A second launch sends it to the Gum that
    /// already owns the project instead of opening another window (#8076).
    /// </summary>
    public OpenRequest? HandoffRequest { get; }

    /// <summary>
    /// True when this launch claims the project it ends up with open so later launches can reach it:
    /// not started with <c>--no-handoff</c>, and not an unattended run unless
    /// <see cref="AwaitOpenRequests"/> asks for it.
    /// </summary>
    public bool TakesPartInHandoff => !NoHandoff && (ExitAfterSeconds == null || AwaitOpenRequests != null);

    /// <summary>
    /// <c>--no-handoff</c>: start a new Gum even when another one owns the project, and claim nothing.
    /// </summary>
    public bool NoHandoff { get; }

    /// <summary>
    /// <c>--await-open-requests N</c>: an unattended run (<see cref="ExitAfterSeconds"/>) does not
    /// capture and exit until N requests from later launches have been handled. Lets a script drive
    /// the handoff without a visible window.
    /// </summary>
    public int? AwaitOpenRequests { get; }

    /// <summary>Parses the head-owned flags and ignores everything else.</summary>
    public static HeadOptions Parse(IReadOnlyList<string> args)
    {
        double? exitAfter = null;
        string? screenshot = null;
        string? select = null;
        string? theme = null;
        string? userData = null;
        bool zoomToFit = false;
        bool noHandoff = false;
        int? awaitOpenRequests = null;
        List<string> positionals = new List<string>();
        bool followsUnknownFlag = false;
        for (int i = 0; i < args.Count; i++)
        {
            bool isValueOfUnknownFlag = followsUnknownFlag;
            followsUnknownFlag = false;
            if (args[i] == "--exit-after" && i + 1 < args.Count)
            {
                exitAfter = double.Parse(args[++i], CultureInfo.InvariantCulture);
            }
            else if (args[i] == "--screenshot" && i + 1 < args.Count)
            {
                screenshot = Path.GetFullPath(args[++i]);
            }
            else if (args[i] == "--select" && i + 1 < args.Count)
            {
                select = args[++i];
            }
            else if (args[i] == "--theme" && i + 1 < args.Count)
            {
                theme = args[++i];
            }
            else if (args[i] == "--zoom-to-fit")
            {
                zoomToFit = true;
            }
            else if (args[i] == "--no-handoff")
            {
                noHandoff = true;
            }
            else if (args[i] == "--await-open-requests" && i + 1 < args.Count)
            {
                awaitOpenRequests = int.Parse(args[++i], CultureInfo.InvariantCulture);
            }
            else if (args[i] == "--user-data" && i + 1 < args.Count)
            {
                userData = Path.GetFullPath(args[++i]);
            }
            else if (args[i].StartsWith("-"))
            {
                // Not ours; the next argument may be its value (--project x, --parent-pid 5), which is not a startup file.
                followsUnknownFlag = true;
            }
            else if (!isValueOfUnknownFlag || CommandLineManager.TryResolveStartupFile(args[i], out _, out _))
            {
                positionals.Add(args[i]);
            }
        }
        string? startupFileError = FindStartupFileError(positionals);
        return new HeadOptions(exitAfter, screenshot, select, theme, userData, zoomToFit, startupFileError,
            startupFileError == null ? FindHandoffRequest(positionals) : null, noHandoff, awaitOpenRequests);
    }

    // The last explicit project file names the project; otherwise the project beside the last element
    // file does. The last element file names the element, as CommandLineManager reads it.
    private static OpenRequest? FindHandoffRequest(IReadOnlyList<string> positionals)
    {
        string? project = null;
        string? elementFile = null;
        foreach (string positional in positionals)
        {
            if (!CommandLineManager.TryResolveStartupFile(positional, out string? resolvedProject, out string? elementName))
            {
                continue;
            }

            if (elementName == null)
            {
                project = resolvedProject;
            }
            else
            {
                elementFile = positional;
                project ??= resolvedProject;
            }
        }

        if (project == null)
        {
            return null;
        }

        string projectPath = Path.GetFullPath(project);
        string? element = elementFile == null ? null : InstanceIdentity.ElementNameFor(projectPath, Path.GetFullPath(elementFile));
        return new OpenRequest(projectPath, element);
    }

    // The head and CommandLineManager read the same arguments; the last project or element file wins there.
    private static string? FindStartupFileError(IReadOnlyList<string> positionals)
    {
        string? lastStartupFile = null;
        foreach (string positional in positionals)
        {
            if (CommandLineManager.TryResolveStartupFile(positional, out _, out _))
            {
                lastStartupFile = positional;
            }
            else
            {
                return positional + " is not a Gum project file";
            }
        }

        if (lastStartupFile != null && !File.Exists(lastStartupFile))
        {
            return "The file does not exist: " + lastStartupFile;
        }

        return null;
    }
}
