using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Gum.Services;

/// <summary>
/// The version the tool shows for itself (the About dialog). The WPF head stamps
/// <c>AssemblyMetadata("BuildVersion", ...)</c>, which the release job rewrites with the release
/// date; the Avalonia head is published with <c>-p:Version</c>, which lands in the informational
/// version. Either is read, so both heads report the same release version.
/// </summary>
public static class ToolVersion
{
    /// <summary>The text shown when no version is stamped at all.</summary>
    public const string Unknown = "unknown";

    /// <summary>The version stamped on <paramref name="assembly"/> (normally the entry assembly).</summary>
    public static string Describe(Assembly? assembly) =>
        Describe(assembly?.GetCustomAttributes() ?? Enumerable.Empty<Attribute>());

    /// <summary>
    /// Picks the version out of assembly attributes: the <c>BuildVersion</c> metadata first, else
    /// the informational version without its <c>+</c> source-revision suffix, else <see cref="Unknown"/>.
    /// </summary>
    public static string Describe(IEnumerable<Attribute> attributes)
    {
        List<Attribute> all = attributes.ToList();

        string? buildVersion = all.OfType<AssemblyMetadataAttribute>()
            .FirstOrDefault(attribute => attribute.Key == "BuildVersion")?.Value;
        if (!string.IsNullOrWhiteSpace(buildVersion))
        {
            return buildVersion;
        }

        string? informational = all.OfType<AssemblyInformationalVersionAttribute>().FirstOrDefault()?.InformationalVersion;
        if (!string.IsNullOrWhiteSpace(informational))
        {
            int revision = informational.IndexOf('+');
            return revision >= 0 ? informational.Substring(0, revision) : informational;
        }

        return Unknown;
    }
}
