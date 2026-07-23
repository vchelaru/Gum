namespace SokolGum.Helpers;

/// <summary>
/// Extension helpers for the Sokol <see cref="Color"/> type. Mirrors the surface of
/// <c>RaylibGum.Helpers.ColorExtensions</c>, <c>SkiaGum.Helpers.ColorExtensions</c>, and the
/// XNA-side <c>RenderingLibrary.Graphics.XNAExtensions</c> so shared runtime code can call the
/// same members on any backend.
/// </summary>
public static class ColorExtensions
{
    /// <summary>
    /// Converts a contained renderable's color to the user-facing color. On Sokol both are
    /// <see cref="Color"/>, so this is identity — it exists only so shared runtime source can
    /// call the same member name that needs a real conversion on the XNA backend.
    /// </summary>
    public static Color ToUserColor(this Color value) => value;

    /// <summary>
    /// Converts a user-facing color to the contained renderable's color. Identity on Sokol; see
    /// <see cref="ToUserColor(Color)"/>.
    /// </summary>
    public static Color ToContainerColor(this Color value) => value;
}
