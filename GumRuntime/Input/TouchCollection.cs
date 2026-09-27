using System;

namespace Gum.Input;

/// <summary>
/// The fingers on a touch screen for the current frame. Empty on platforms or frames without touch.
/// </summary>
public class TouchCollection
{
    readonly TouchLocation[] _touches;

    public TouchCollection()
    {
        _touches = Array.Empty<TouchLocation>();
    }

    public TouchCollection(TouchLocation[] touches)
    {
        _touches = touches ?? throw new ArgumentNullException(nameof(touches));
    }

    public int Count => _touches.Length;

    public TouchLocation this[int index]
    {
        get
        {
            if (index < 0 || index >= Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            return _touches[index];
        }
        set
        {
            throw new NotSupportedException();
        }
    }
}
