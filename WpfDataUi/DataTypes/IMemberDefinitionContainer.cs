using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WpfDataUi.DataTypes
{
    public interface IMemberDefinitionContainer
    {
        IEnumerable<IMemberDefinition> Members { get;}
    }
}
