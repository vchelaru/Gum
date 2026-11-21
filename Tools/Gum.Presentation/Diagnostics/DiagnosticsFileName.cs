using System;
using System.IO;

namespace Gum.Diagnostics;

/// <summary>Picks the diagnostics zip's name: <c>GumDiagnostics-yyyy-MM-dd.zip</c>, then <c>-2</c>, <c>-3</c> on a collision.</summary>
public static class DiagnosticsFileName
{
    /// <summary>The first name for <paramref name="date"/> that does not exist in <paramref name="folder"/>.</summary>
    public static string Next(string folder, DateTime date)
    {
        string baseName = "GumDiagnostics-" + date.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
        string name = baseName + ".zip";
        for (int suffix = 2; File.Exists(Path.Combine(folder, name)); suffix++)
        {
            name = baseName + "-" + suffix + ".zip";
        }
        return name;
    }
}
