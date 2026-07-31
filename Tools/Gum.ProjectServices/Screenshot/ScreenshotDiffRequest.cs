namespace Gum.ProjectServices.Screenshot;

/// <summary>
/// Parameters for diffing every Screen and Component in a project across two rendering backends.
/// </summary>
public class ScreenshotDiffRequest
{
    /// <summary>
    /// Absolute path to the .gumx project file.
    /// </summary>
    public required string ProjectPath { get; init; }

    /// <summary>
    /// First rendering backend (e.g. <c>MonoGameScreenshotService</c>).
    /// </summary>
    public required IScreenshotService BackendA { get; init; }

    /// <summary>
    /// Second rendering backend (e.g. <c>RaylibScreenshotService</c>).
    /// </summary>
    public required IScreenshotService BackendB { get; init; }

    /// <summary>
    /// Directory each backend's rendered PNGs are written to, under a per-backend subfolder.
    /// Defaults to a new temp directory when not specified.
    /// </summary>
    public string? OutputDirectory { get; init; }

    /// <summary>
    /// Maximum per-channel pixel difference (0-255) still considered a match. Absorbs
    /// antialiasing/hinting drift between renderers.
    /// </summary>
    public byte Tolerance { get; init; } = 2;
}
