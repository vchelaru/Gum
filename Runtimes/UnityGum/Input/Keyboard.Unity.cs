using System.Collections.Generic;
using GumKeys = Gum.Forms.Input.Keys;

namespace Gum.Input;

/// <summary>
/// Keyboard fed by the Unity host. Before each <c>GumService.Update</c> the host reports held keys with
/// <see cref="SetKeyDown"/> and typed characters with <see cref="AddTypedText"/>;
/// <see cref="PolledKeyboard"/> derives push and release edges and key repeat from that.
/// </summary>
public class Keyboard : PolledKeyboard
{
    private readonly HashSet<GumKeys> _pushedDown = new HashSet<GumKeys>();

    // Every key the host has reported, in first-reported order. PolledKeyboard polls only these.
    private readonly List<GumKeys> _reportedKeys = new List<GumKeys>();

    /// <summary>
    /// Reports whether <paramref name="key"/> is held. Takes effect at the next Activity.
    /// </summary>
    public void SetKeyDown(GumKeys key, bool isDown)
    {
        if (isDown)
        {
            if (_pushedDown.Add(key) && !_reportedKeys.Contains(key))
            {
                _reportedKeys.Add(key);
            }
        }
        else
        {
            _pushedDown.Remove(key);
        }
    }

    /// <summary>
    /// Adds a character the OS typed (including its own key repeat), which TextBox reads on the next
    /// Activity. Control characters 0-29 (Backspace, Enter, Ctrl+letter) are dropped, as MonoGame's
    /// keyboard does: TextBox handles those through the keys.
    /// </summary>
    public void AddTypedText(char character)
    {
        if (character > 29)
        {
            AppendTypedText(character);
        }
    }

    /// <inheritdoc/>
    protected override IEnumerable<GumKeys> SupportedKeys => _reportedKeys;

    /// <inheritdoc/>
    protected override bool IsDeviceKeyDown(GumKeys key) => _pushedDown.Contains(key);
}
