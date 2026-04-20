using System;

namespace Gum.Input;

public class TouchCollection
{
    public int Count => 0;

    public TouchLocation this[int index]
    {
        get
        {
            if (index < 0 || index >= Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            return new TouchLocation();
        }
        set
        {
            throw new NotSupportedException();
        }
    }
}
