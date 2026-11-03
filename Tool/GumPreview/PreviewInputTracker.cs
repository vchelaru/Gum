namespace GumPreview;

/// <summary>What <see cref="PreviewInputTracker"/> needs to know about the focused text box, as plain values.</summary>
public readonly record struct TextBoxSnapshot(
    string Name, string Text, bool Wrap, int SelectionStart, int SelectionLength, int CaretIndex, int InstanceCount, int VisibleInstanceCount,
    System.Func<System.Collections.Generic.IReadOnlyList<string>>? Geometry = null);

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
    private readonly System.Func<long> _nowMilliseconds;
    private readonly System.Action<string>? _requestFrame;
    private long _lastMoveMilliseconds = long.MinValue / 2;
    private int _loggedMoveX = int.MinValue;
    private int _loggedMoveY = int.MinValue;

    /// <param name="nowMilliseconds">A monotonic clock, injectable for tests.</param>
    /// <param name="requestFrame">Asks for a screenshot (with the reason) when the highlight instances change.</param>
    public PreviewInputTracker(PreviewDebugLog log, System.Func<long>? nowMilliseconds = null, System.Action<string>? requestFrame = null)
    {
        _log = log;
        _nowMilliseconds = nowMilliseconds ?? (() => System.Environment.TickCount64);
        _requestFrame = requestFrame;
    }

    /// <summary>The shortest time between two logged cursor moves.</summary>
    public const long MoveIntervalMilliseconds = 50;

    /// <summary>
    /// Logs a press, each move while held (a drag), the release, and (at most every
    /// <see cref="MoveIntervalMilliseconds"/>, only when the position changed) a move with no button down.
    /// <paramref name="over"/> names the control under the cursor; it is only called when a move is logged.
    /// </summary>
    public void ObserveMouse(int x, int y, bool isDown, System.Func<string?>? over = null)
    {
        if (!isDown && !_wasDown && (x != _loggedMoveX || y != _loggedMoveY))
        {
            long now = _nowMilliseconds();
            if (now - _lastMoveMilliseconds >= MoveIntervalMilliseconds)
            {
                _lastMoveMilliseconds = now;
                _loggedMoveX = x;
                _loggedMoveY = y;
                _log.Write("mouse", $"move {x},{y} over={over?.Invoke() ?? "(none)"}");
            }
        }

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

        bool logged = false;
        if (previous == null
            || previous.Value.SelectionStart != current.SelectionStart
            || previous.Value.SelectionLength != current.SelectionLength
            || previous.Value.CaretIndex != current.CaretIndex)
        {
            _log.LogSelection(current.Name, current.SelectionStart, current.SelectionLength, current.CaretIndex);
            logged = true;
        }

        if (previous == null
            || previous.Value.Text != current.Text
            || previous.Value.Wrap != current.Wrap
            || previous.Value.InstanceCount != current.InstanceCount
            || previous.Value.VisibleInstanceCount != current.VisibleInstanceCount)
        {
            _log.LogSelectionInstances(current.Name, current.Wrap, current.Text.Length, current.InstanceCount, current.VisibleInstanceCount);
            logged = true;
            _requestFrame?.Invoke($"selection-instances count={current.InstanceCount}");
        }

        if (logged && current.Geometry != null)
        {
            _log.LogGeometry(current.Name, current.Geometry());
        }
    }
}
