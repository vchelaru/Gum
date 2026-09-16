using System;

namespace Gum.Services;

/// <summary>
/// The operating system the tool is running on, behind an interface so platform-dependent
/// defaults (macOS keyboard conventions, Windows-only font generators) can be tested on any OS.
/// </summary>
public interface IOperatingSystemInfo
{
    /// <summary>Whether the tool is running on macOS.</summary>
    bool IsMacOS { get; }

    /// <summary>Whether the tool is running on Windows.</summary>
    bool IsWindows { get; }

    /// <summary>The OS name as shown to the user: "Windows", "macOS" or "Linux".</summary>
    string DisplayName { get; }
}

/// <inheritdoc/>
public class OperatingSystemInfo : IOperatingSystemInfo
{
    /// <inheritdoc/>
    public bool IsMacOS => OperatingSystem.IsMacOS();

    /// <inheritdoc/>
    public bool IsWindows => OperatingSystem.IsWindows();

    /// <inheritdoc/>
    public string DisplayName => IsWindows ? "Windows" : IsMacOS ? "macOS" : "Linux";
}
