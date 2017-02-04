using Gum.DataTypes.Variables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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

        IEnumerable<StateSaveCategory> Categories
        {
            get;
        }
    }

    public interface IStateCategoryListContainer
    {
        List<StateSaveCategory> Categories
        {
            get;
        }
    }
}
