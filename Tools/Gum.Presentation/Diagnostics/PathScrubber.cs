using System;
using System.Text.RegularExpressions;

namespace Gum.Diagnostics;

/// <summary>
/// Removes the user's name and home-folder path from text before it goes into a diagnostics file.
/// The home folder, and any other <c>C:\Users\x</c>, <c>/Users/x</c> or <c>/home/x</c> path, become
/// <see cref="Placeholder"/>, and so does the bare user name when it stands as a whole word (so a user
/// named "sam" leaves "sample" alone).
/// </summary>
public sealed class PathScrubber
{
    /// <summary>What a removed name or path is replaced with.</summary>
    public const string Placeholder = "%USER%";

    private const string NameBoundaryBefore = @"(?<![\p{L}\p{N}_])";
    private const string NameBoundaryAfter = @"(?![\p{L}\p{N}_])";
    private const string OtherHomePaths = @"(?:[A-Za-z]:[\\/]+Users[\\/]+|/Users/|/home/)[^\\/\s""'<>|:*?]+";

    private readonly Regex? _homeFolder;
    private readonly Regex _otherHomePaths;
    private readonly Regex? _userName;

    /// <param name="userName">The account name, for example <see cref="Environment.UserName"/>. Null or empty to skip.</param>
    /// <param name="homeFolder">The home folder, for example the user profile. Null or empty to skip.</param>
    public PathScrubber(string? userName, string? homeFolder)
    {
        RegexOptions options = RegexOptions.IgnoreCase | RegexOptions.CultureInvariant;

        string? trimmedHome = homeFolder?.TrimEnd('\\', '/');
        if (!string.IsNullOrEmpty(trimmedHome))
        {
            string[] parts = trimmedHome.Split(new[] { '\\', '/' }, StringSplitOptions.None);
            string pattern = string.Join(@"[\\/]+", Array.ConvertAll(parts, Regex.Escape));
            _homeFolder = new Regex(pattern, options);
        }

        _otherHomePaths = new Regex(OtherHomePaths, options);

        if (!string.IsNullOrEmpty(userName))
        {
            _userName = new Regex(NameBoundaryBefore + Regex.Escape(userName) + NameBoundaryAfter, options);
        }
    }

    /// <summary>Returns <paramref name="text"/> with the user's name and home paths replaced.</summary>
    public string Scrub(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return text;
        }

        string result = _homeFolder?.Replace(text, Placeholder) ?? text;
        result = _otherHomePaths.Replace(result, Placeholder);
        if (_userName != null)
        {
            result = _userName.Replace(result, Placeholder);
        }
        return result;
    }
}
