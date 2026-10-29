namespace RenderingLibrary.Graphics;

/// <summary>
/// The messages for a <c>SourceShaderFile</c> that no shader resolver handled (#7053). Shared by the
/// MonoGame/KNI/FNA/raylib and Skia dispatchers, which each pass the ShadowDusk package id for the
/// runtime they are compiled for. Nothing here depends on the file's extension.
/// </summary>
internal static class ShaderResolverRequirement
{
    /// <summary>
    /// The message for a file assigned while no resolver is registered.
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
    /// The text to append when a registered resolver returned nothing for the file, so it did not handle it.
    /// </summary>
    public static string BuildDeclinedHint(string packageId)
    {
        return "The registered shader resolver did not handle this file. " + BuildFix(packageId);
    }

    /// <summary>
    /// The fix, with the ordering that makes it work.
    /// </summary>
    public static string BuildFix(string packageId)
    {
        return $"Register a shader resolver, for example the {packageId} package: add it and call UseShadowDusk() on the GumService once at startup, before creating any element that uses a SourceShaderFile (a file assigned before a resolver exists fails right then, and calling UseShadowDusk() later does not retry it; for example call Initialize, then UseShadowDusk(), then ToGraphicalUiElement).";
    }
}
