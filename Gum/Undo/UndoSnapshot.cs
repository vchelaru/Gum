using Gum.DataTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gum.Undo
{
    public class UndoSnapshot
    {
        public ElementSave Element;
        public string CategoryName;
        public string StateName;
    }
}
