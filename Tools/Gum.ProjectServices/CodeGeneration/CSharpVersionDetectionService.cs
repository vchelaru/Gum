using System;
using System.IO;
using System.Text.RegularExpressions;

namespace Gum.ProjectServices.CodeGeneration;

/// <inheritdoc/>
public class CSharpVersionDetectionService : ICSharpVersionDetectionService
{
    // Codegen asks once per generated file; the .csproj is only re-read when it changes.
    private string? _cachedCsprojPath;
    private DateTime _cachedWriteTimeUtc;
    private int? _cachedVersion;

    /// <inheritdoc/>
    public int? Detect(CodeOutputProjectSettings settings, string? projectDirectory)
    {
        string? csprojPath = CodeProjectCsprojLocator.FindCsproj(settings, projectDirectory);
        if (csprojPath == null)
        {
            return null;
        }

        try
        {
            DateTime writeTimeUtc = File.GetLastWriteTimeUtc(csprojPath);
            if (csprojPath != _cachedCsprojPath || writeTimeUtc != _cachedWriteTimeUtc)
            {
                _cachedVersion = ParseCSharpLanguageVersion(File.ReadAllText(csprojPath));
                _cachedCsprojPath = csprojPath;
                _cachedWriteTimeUtc = writeTimeUtc;
            }
            return _cachedVersion;
        }
        catch
        {
            return null;
        }
    }

    internal static int? ParseCSharpLanguageVersion(string csprojContents)
    {
        // An explicit <LangVersion> wins. Only a numeric one ("9.0", "9", "7.3") caps the version;
        // latest/latestMajor/preview/default mean the newest the compiler knows.
        Match langVersion = Regex.Match(csprojContents, @"<LangVersion>\s*([^<]*?)\s*</LangVersion>", RegexOptions.IgnoreCase);
        if (langVersion.Success)
        {
            Match major = Regex.Match(langVersion.Groups[1].Value, @"^(\d+)(?:\.\d+)?$");
            return major.Success ? int.Parse(major.Groups[1].Value) : null;
        }

        // Otherwise the compiler's default for the target framework. Multi-targeting compiles
        // with each framework's default, so the lowest one decides.
        Match targetFrameworks = Regex.Match(csprojContents, @"<TargetFrameworks?>\s*([^<]*?)\s*</TargetFrameworks?>", RegexOptions.IgnoreCase);
        if (targetFrameworks.Success)
        {
            int? lowest = null;
            foreach (string tfm in targetFrameworks.Groups[1].Value.Split(';', StringSplitOptions.RemoveEmptyEntries))
            {
                int? version = GetDefaultCSharpVersion(tfm.Trim());
                if (version == null)
                {
                    // Unrecognized, or an MSBuild property like $(Frameworks), so don't guess.
                    return null;
                }
                lowest = lowest == null ? version : Math.Min(lowest.Value, version.Value);
            }
            return lowest;
        }

        // Old-style (non-SDK) projects target .NET Framework, which defaults to C# 7.3.
        if (Regex.IsMatch(csprojContents, @"<TargetFrameworkVersion>\s*v4", RegexOptions.IgnoreCase))
        {
            return 7;
        }

        return null;
    }

    private static int? GetDefaultCSharpVersion(string targetFramework)
    {
        // net5.0 -> C# 9, net6.0 -> C# 10, ...: each .NET release bumps the default by one.
        Match modern = Regex.Match(targetFramework, @"^net(\d+)\.\d+", RegexOptions.IgnoreCase);
        if (modern.Success)
        {
            int netVersion = int.Parse(modern.Groups[1].Value);
            if (netVersion >= 5)
            {
                return netVersion + 4;
            }
        }

        if (Regex.IsMatch(targetFramework, @"^netstandard2\.1$|^netcoreapp3\.", RegexOptions.IgnoreCase))
        {
            return 8;
        }

        if (Regex.IsMatch(targetFramework, @"^netstandard|^netcoreapp|^net\d{2,3}$|^net4", RegexOptions.IgnoreCase))
        {
            return 7;
        }

        return null;
    }
}
