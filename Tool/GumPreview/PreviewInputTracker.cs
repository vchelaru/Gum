namespace GumPreview;

/// <summary>What <see cref="PreviewInputTracker"/> needs to know about the focused text box, as plain values.</summary>
public readonly record struct TextBoxSnapshot(
    string Name, string Text, bool Wrap, int SelectionStart, int SelectionLength, int CaretIndex, int InstanceCount, int VisibleInstanceCount);

/// <summary>
/// Turns per-frame samples of the mouse and the focused text box into log lines, writing only when
/// something changed since the last sample. Only created when logging is on.
/// </summary>
public sealed class PreviewInputTracker
{
    private readonly PreviewDebugLog _log;
    private bool _wasDown;
    private int _lastX;
    private int _lastY;
    private TextBoxSnapshot? _last;

    public PreviewInputTracker(PreviewDebugLog log)
    {
        _log = log;
    }

    /// <summary>Logs a press, each move while held (a drag), and the release.</summary>
    public void ObserveMouse(int x, int y, bool isDown)
    {
        if (isDown && !_wasDown)
        {
            _log.LogMouse("down", x, y);
        }
        else if (isDown && (x != _lastX || y != _lastY))
        {
            _log.LogMouse("drag", x, y);
        }
        else if (!isDown && _wasDown)
        {
            _log.LogMouse("up", x, y);
        }
        _wasDown = isDown;
        _lastX = x;
        _lastY = y;
    }

    /// <summary>Logs a changed selection, and the highlight instance count and visibility after a text, wrap or instance change. Null when no text box has focus.</summary>
    public void ObserveTextBox(TextBoxSnapshot? snapshot)
    {
        if (snapshot is not TextBoxSnapshot current)
        {
            _last = null;
            return;
        }

        TextBoxSnapshot? previous = _last?.Name == current.Name ? _last : null;
        _last = current;

        if (previous == null
            || previous.Value.SelectionStart != current.SelectionStart
            || previous.Value.SelectionLength != current.SelectionLength
            || previous.Value.CaretIndex != current.CaretIndex)
        {
            _log.LogSelection(current.Name, current.SelectionStart, current.SelectionLength, current.CaretIndex);
        }

        if (previous == null
            || previous.Value.Text != current.Text
            || previous.Value.Wrap != current.Wrap
            || previous.Value.InstanceCount != current.InstanceCount
            || previous.Value.VisibleInstanceCount != current.VisibleInstanceCount)
        {
            _log.LogSelectionInstances(current.Name, current.Wrap, current.Text.Length, current.InstanceCount, current.VisibleInstanceCount);
        }
    }
}
