using System.Windows.Forms;

namespace Gum.Services;

/// <inheritdoc cref="IClipboardService"/>
public class ClipboardService : IClipboardService
{
    public void SetText(string text) => Clipboard.SetText(text);
}
