using System;

namespace Gum.ProjectServices.CodeGeneration;

/// <summary>
/// Builds the warning shown when <c>gumcli</c> is older than the Gum runtime a project references.
/// <c>gumcli</c> is a separate dotnet tool, so updating a game's Gum packages does not update it,
/// and a stale tool silently generates code for an older runtime.
/// </summary>
public static class StaleCliWarning
{
    /// <summary>
    /// Returns the warning text when <paramref name="runtimePackageVersion"/> is newer than
    /// <paramref name="cliVersion"/>, or null when it is equal, older, or either version cannot be
    /// read. Packages and the CLI are published in the same wave with the same version, so a newer
    /// package proves the CLI is stale. Prerelease and build-metadata suffixes are ignored.
    /// </summary>
    public static string? GetMessage(string cliVersion, string? runtimePackageVersion)
    {
        if (!TryParse(cliVersion, out Version? cli) || !TryParse(runtimePackageVersion, out Version? runtime))
        {
            return null;
        }

        if (runtime <= cli)
        {
            return null;
        }

        return $"gumcli v{cli} is older than the Gum runtime in this project (v{runtime}). " +
            "Run: dotnet tool update -g GumCli";
    }

    // Reads the numeric part of a version such as "2026.10.1.1-preview.1+abc123" and pads it to
    // four components so "2026.10.1" and "2026.10.1.0" compare equal. Floating versions, ranges
    // and MSBuild properties do not parse.
    private static bool TryParse(string? version, out Version? parsed)
    {
        parsed = null;
        if (string.IsNullOrWhiteSpace(version))
        {
            return false;
        }

        int suffixIndex = version.IndexOfAny(new[] { '-', '+' });
        string numeric = suffixIndex < 0 ? version : version.Substring(0, suffixIndex);

        if (!Version.TryParse(numeric, out Version? result))
        {
            return false;
        }

        parsed = new Version(result.Major, result.Minor, Math.Max(result.Build, 0), Math.Max(result.Revision, 0));
        return true;
    }
}
