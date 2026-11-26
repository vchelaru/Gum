using System;
using System.Text.RegularExpressions;
using Gum.Diagnostics;

namespace Gum.Avalonia.Diagnostics.CrashReporting;

/// <summary>
/// Cleans text sent with a crash report: the open project's folder becomes <see cref="ProjectPlaceholder"/>,
/// in either separator style and any casing, then the diagnostics file's <see cref="PathScrubber"/>
/// removes the user's name and home paths.
/// </summary>
public class CrashReportScrubber
{
    /// <summary>Replaces the project folder.</summary>
    public const string ProjectPlaceholder = "<project>";

    private readonly PathScrubber _userScrubber;
    private readonly Func<string?> _projectDirectory;

    /// <param name="userScrubber">Removes the user's name and home paths.</param>
    /// <param name="projectDirectory">The open project's folder, read at each call; null when none is open.</param>
    public CrashReportScrubber(PathScrubber userScrubber, Func<string?> projectDirectory)
    {
        _userScrubber = userScrubber;
        _projectDirectory = projectDirectory;
    }

    /// <summary>The text with the project folder and the user's name and home paths replaced; null stays null.</summary>
    public string? Scrub(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return text;
        }

        // The project usually sits inside the home folder, so it goes first or it would read %USER%\...
        return _userScrubber.Scrub(ReplaceProject(text, _projectDirectory()));
    }

    private static string ReplaceProject(string text, string? directory)
    {
        string trimmed = directory?.TrimEnd('/', '\\') ?? "";
        // A root ("/", "C:") would replace far more than a folder.
        if (trimmed.Length < 3)
        {
            return text;
        }

        string[] parts = trimmed.Split('/', '\\');
        // Either separator between the parts, and only a whole folder name, so D:\Game does not match D:\Games.
        string pattern = string.Join(@"[\\/]+", Array.ConvertAll(parts, Regex.Escape)) + @"(?![\w.-])";
        return Regex.Replace(text, pattern, ProjectPlaceholder, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    }
}
