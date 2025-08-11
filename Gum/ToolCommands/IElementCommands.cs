using Gum.DataTypes;
using Gum.DataTypes.Variables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gum.ToolCommands;
public interface IElementCommands
{
    StateSaveCategory AddCategory(IStateContainer objectToAddTo, string name);
}
