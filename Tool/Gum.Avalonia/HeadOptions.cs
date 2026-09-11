using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace Gum.Avalonia;

/// <summary>
/// Options the Avalonia head reads from its own command line for unattended verification runs:
/// <c>--exit-after seconds</c>, <c>--screenshot path.png</c>, <c>--select Element[#Instance]</c>, and
/// <c>--theme light|dark</c>. Everything else is left for
/// <see cref="Gum.CommandLine.CommandLineManager"/>, which reads the process command line itself.
/// </summary>
public sealed class HeadOptions
{
    /// <summary>Creates the options.</summary>
    public HeadOptions(double? exitAfterSeconds, string? screenshotPath, string? selectPath = null, string? theme = null)
    {
        ExitAfterSeconds = exitAfterSeconds;
        ScreenshotPath = screenshotPath;
        SelectPath = selectPath;
        Theme = theme;
    }

    /// <summary>When set, the app closes itself this many seconds after the main window opens.</summary>
    public double? ExitAfterSeconds { get; }

    /// <summary>When set, the main window is rendered to this PNG just before exiting.</summary>
    public string? ScreenshotPath { get; }

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

    /// <summary>Parses the head-owned flags and ignores everything else.</summary>
    public static HeadOptions Parse(IReadOnlyList<string> args)
    {
        double? exitAfter = null;
        string? screenshot = null;
        string? select = null;
        string? theme = null;
        for (int i = 0; i < args.Count; i++)
        {
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
        }
        return new HeadOptions(exitAfter, screenshot, select, theme);
    }
}
