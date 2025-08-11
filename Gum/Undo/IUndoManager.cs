using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gum.Undo;
public interface IUndoManager
{
    UndoLock RequestLock();
}
