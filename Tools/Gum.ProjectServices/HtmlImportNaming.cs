using System;
using System.Text.RegularExpressions;

namespace Gum.ProjectServices;

/// <summary>
/// Pure naming/conflict-resolution helpers for importing a Screen into a project. Shared by
/// HtmlToGumPlugin's Content → Import → HTML… merge step and gumcli import-screen so the two
/// import paths qualify/uniquify names identically.
/// </summary>
public static class HtmlImportNaming
{
    private static readonly Regex UrlPattern = new(@"^https?://", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>Whether <paramref name="source"/> is a remote http(s) URL rather than a local file path.</summary>
    public static bool IsUrl(string source) => UrlPattern.IsMatch(source ?? "");

    /// <summary>Prepends "https://" to <paramref name="source"/> if it doesn't already start with http(s)://, so a bare host like "example.com" is treated as a URL.</summary>
    public static string NormalizeUrl(string source)
    {
        var trimmed = (source ?? "").Trim();
        return trimmed.Length == 0 || IsUrl(trimmed) ? trimmed : $"https://{trimmed}";
    }

    /// <summary>Prefixes <paramref name="screenName"/> with <paramref name="destinationSubfolder"/>, matching the "Screens/{subfolder}/{name}" convention used by the .gumx importer.</summary>
    public static string QualifyScreenName(string screenName, string? destinationSubfolder) =>
        string.IsNullOrWhiteSpace(destinationSubfolder)
            ? screenName
            : $"{destinationSubfolder.TrimEnd('/')}/{screenName}";

    /// <summary>
    /// Returns <paramref name="desiredScreenName"/> if its qualified name is free, otherwise appends
    /// "_2", "_3", ... until <paramref name="nameExists"/> (checked against the qualified name) returns false.
    /// </summary>
    public static string ResolveUniqueScreenName(
        string desiredScreenName, string? destinationSubfolder, Func<string, bool> nameExists)
    {
        if (!nameExists(QualifyScreenName(desiredScreenName, destinationSubfolder)))
        {
            return desiredScreenName;
        }

        int suffix = 2;
        string candidate;
        do
        {
            candidate = $"{desiredScreenName}_{suffix}";
            suffix++;
        } while (nameExists(QualifyScreenName(candidate, destinationSubfolder)));

        return candidate;
    }
}
