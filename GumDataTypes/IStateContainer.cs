using Gum.DataTypes.Variables;
using System.Collections.Generic;

namespace Gum.DataTypes
{
    public interface IStateContainer
    {
        IList<StateSave> UncategorizedStates
        {
            get;
        }

        IEnumerable<StateSave> AllStates
        {
            get;
        }

        IList<StateSaveCategory> Categories
        {
            get;
        }
    }
}
