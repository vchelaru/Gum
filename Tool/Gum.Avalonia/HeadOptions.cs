using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace Gum.Avalonia;

/// <summary>
/// Options the Avalonia head reads from its own command line: <c>--exit-after seconds</c> and
/// <c>--screenshot path.png</c> for unattended verification runs. Everything else is left for
/// <see cref="Gum.CommandLine.CommandLineManager"/>, which reads the process command line itself.
/// </summary>
public sealed class HeadOptions
{
    /// <summary>Creates the options.</summary>
    public HeadOptions(double? exitAfterSeconds, string? screenshotPath)
    {
        ExitAfterSeconds = exitAfterSeconds;
        ScreenshotPath = screenshotPath;
    }

    /// <summary>When set, the app closes itself this many seconds after the main window opens.</summary>
    public double? ExitAfterSeconds { get; }

    /// <summary>When set, the main window is rendered to this PNG just before exiting.</summary>
    public string? ScreenshotPath { get; }

    /// <summary>Parses the two head-owned flags and ignores everything else.</summary>
    public static HeadOptions Parse(IReadOnlyList<string> args)
    {
        double? exitAfter = null;
        string? screenshot = null;
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
        }
        return new HeadOptions(exitAfter, screenshot);
    }
}
