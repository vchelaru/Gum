using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gum.DataTypes.Variables;

namespace Gum.Undo
{
    public class StateSaveMemento : StateSave
    {
        public string Container { get; set; }
    }
}
