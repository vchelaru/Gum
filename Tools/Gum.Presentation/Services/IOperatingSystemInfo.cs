using System;

namespace Gum.Services;

/// <summary>
/// The operating system the tool is running on, behind an interface so platform-dependent
/// defaults (macOS keyboard conventions) can be tested on any OS.
/// </summary>
public interface IOperatingSystemInfo
{
    /// <summary>Whether the tool is running on macOS.</summary>
    bool IsMacOS { get; }
}

/// <inheritdoc/>
public class OperatingSystemInfo : IOperatingSystemInfo
{
    /// <inheritdoc/>
    public bool IsMacOS => OperatingSystem.IsMacOS();
}
