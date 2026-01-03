using Gum.Wireframe;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RaylibGum.Input;
public class Keyboard : IInputReceiverKeyboard
{
    public bool IsShiftDown => false;

    public bool IsCtrlDown => false;

    public bool IsAltDown => false;

    IEnumerable<int> IInputReceiverKeyboard.KeysTyped
    {
        get
        {
            return Enumerable.Empty<int>();
        }
    }

    public void Activity(float gameTime)
    {
        // todo:
    }

    public string GetStringTyped()
    {
        return string.Empty;
    }

    string IInputReceiverKeyboard.GetStringTyped()
    {
        throw new NotImplementedException();
    }
}
