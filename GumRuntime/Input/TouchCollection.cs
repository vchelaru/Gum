using System;
using System.Numerics;

namespace Gum.Input;

/// <summary>
/// The fingers on a touch screen for the current frame. Empty on platforms or frames without touch.
/// </summary>
public class TouchCollection
{
    static readonly TouchCollection Empty = new TouchCollection();

    readonly TouchLocation[] _touches;

    public TouchCollection()
    {
        _touches = Array.Empty<TouchLocation>();
    }

    public TouchCollection(TouchLocation[] touches)
    {
        _touches = touches ?? throw new ArgumentNullException(nameof(touches));
    }

    /// <summary>
    /// Builds a collection from a host's indexed touch points. Takes the reads as delegates so a host
    /// can adapt its own touch API and tests can supply touches without touch hardware.
    /// </summary>
    internal static TouchCollection Create(int count, Func<int, int> getId, Func<int, Vector2> getPosition)
    {
        if (count <= 0)
        {
            return Empty;
        }

        var touches = new TouchLocation[count];
        for (int i = 0; i < count; i++)
        {
            touches[i] = new TouchLocation(getId(i), getPosition(i));
        }
        return new TouchCollection(touches);
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
