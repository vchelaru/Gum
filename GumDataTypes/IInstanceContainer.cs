using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gum.DataTypes
{
    public interface IInstanceContainer
    {
        string Name { get; }
        IEnumerable<InstanceSave> Instances { get; }
    }
}
