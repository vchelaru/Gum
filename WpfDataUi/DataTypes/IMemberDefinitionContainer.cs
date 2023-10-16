using System.Collections.Generic;

namespace WpfDataUi.DataTypes
{
    public interface IMemberDefinitionContainer
    {
        IEnumerable<IMemberDefinition> Members { get;}
    }
}
