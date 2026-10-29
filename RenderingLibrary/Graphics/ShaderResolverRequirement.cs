using System;
using System.IO;

namespace RenderingLibrary.Graphics;

/// <summary>
/// Which <c>SourceShaderFile</c> extensions need a ShadowDusk package, and the message that names the
/// package when none is available (#7053). Shared by the MonoGame/KNI/FNA/raylib and Skia dispatchers,
/// which each pass the package id for the runtime they are compiled for.
/// </summary>
internal static class ShaderResolverRequirement
{
    /// <summary>
    /// Whether <paramref name="file"/> is a <c>.fx</c> or <c>.slang</c> file, which only the ShadowDusk
    /// package for the runtime can load. Skia compiles <c>.sksl</c> without it.
    /// </summary>
    public static bool NeedsShadowDusk(string? file)
    {
        if (string.IsNullOrEmpty(file))
        {
            return false;
        }

        string extension = Path.GetExtension(file);
        return string.Equals(extension, ".fx", StringComparison.OrdinalIgnoreCase)
            || string.Equals(extension, ".slang", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// The message for a <c>.fx</c> or <c>.slang</c> file assigned while no resolver is registered.
    /// </summary>
    public static string BuildNoResolverMessage(string file, object? tag, string packageId)
    {
        string message = "Error setting SourceShaderFile on Container";
        if (tag != null)
        {
            message += $" in {tag}";
        }
        message += $"\n{file}";
        message += "\nThe shader cannot load because no shader resolver is registered. " + BuildFix(packageId);
        return message;
    }

    /// <summary>
    /// The fix to append when a resolver is registered but returned nothing for a <c>.fx</c> or
    /// <c>.slang</c> file, so none in the chain handles them.
    /// </summary>
    public static string BuildFix(string packageId)
    {
        return $"Loading .fx and .slang files needs the {packageId} package: add it and call UseShadowDusk() on the GumService once at startup, before any element or screen that uses these files is created: a .fx or .slang file assigned before a resolver exists fails right then, and calling UseShadowDusk() later does not retry it (for example call Initialize, then UseShadowDusk(), then ToGraphicalUiElement).";
    }
}
