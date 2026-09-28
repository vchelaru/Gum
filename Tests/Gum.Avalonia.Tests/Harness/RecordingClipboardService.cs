using Gum.Avalonia.Services;
using Gum.Services;

namespace Gum.Avalonia.Tests.Harness;

/// <summary>
/// The test container's <see cref="IClipboardService"/>: the head's own service, which writes to the
/// main window's clipboard, plus a record of the last text copied. The headless app has no main
/// window, so the record is the only place a test can read what the tool copied.
/// </summary>
internal sealed class RecordingClipboardService : IClipboardService
{
    private readonly AvaloniaClipboardService _inner = new AvaloniaClipboardService();

    /// <summary>The text of the last copy, or null when nothing was copied since <see cref="Clear"/>.</summary>
    public string? LastText { get; private set; }

    /// <inheritdoc/>
    public void SetText(string text)
    {
        LastText = text;
        _inner.SetText(text);
    }

    /// <summary>Forgets the last copy.</summary>
    public void Clear() => LastText = null;
}
