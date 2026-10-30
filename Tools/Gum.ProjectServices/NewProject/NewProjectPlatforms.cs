using System;
using System.Collections.Generic;
using System.Linq;

namespace Gum.ProjectServices;

/// <summary>
/// The platform names <c>gumcli new --platform</c> accepts, shared with the New Project dialog.
/// </summary>
public static class NewProjectPlatforms
{
    /// <summary>
    /// Every accepted name. An alias follows its platform's main name and is not listed in <see cref="MainNames"/>.
    /// </summary>
    public static IReadOnlyList<(string Name, HostPlatform Platform)> All { get; } = new (string, HostPlatform)[]
    {
        ("monogame", HostPlatform.MonoGame),
        ("kni", HostPlatform.Kni),
        ("kniskia", HostPlatform.KniSkia),
        ("raylib", HostPlatform.Raylib),
        ("stride", HostPlatform.Stride),
        ("silknet", HostPlatform.SilkNet),
        ("silk.net", HostPlatform.SilkNet)
    };

    /// <summary>One name per platform, in display order.</summary>
    public static IReadOnlyList<(string Name, HostPlatform Platform)> MainNames { get; } =
        All.GroupBy(entry => entry.Platform).Select(group => group.First()).ToList();

    /// <summary>The main names joined as <c>monogame, kni, ...</c>, for messages.</summary>
    public static string ValidNames => string.Join(", ", MainNames.Select(entry => entry.Name));

    /// <summary>Finds the platform for a name, ignoring case.</summary>
    public static bool TryParse(string name, out HostPlatform platform)
    {
        foreach ((string entryName, HostPlatform entryPlatform) in All)
        {
            if (string.Equals(entryName, name, StringComparison.OrdinalIgnoreCase))
            {
                platform = entryPlatform;
                return true;
            }
        }

        platform = default;
        return false;
    }
}
