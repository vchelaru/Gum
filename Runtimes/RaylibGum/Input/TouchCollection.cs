using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RaylibGum.Input;
public class TouchCollection
{
    public int Count => 0;


    public TouchLocation this[int index]
    {
        get
        {
            if (index < 0 || index >= Count)
            {
                throw new ArgumentOutOfRangeException("index");
            }

            // todo:
            return new TouchLocation();

        }
        set
        {
            throw new NotSupportedException();
        }
    }
}
