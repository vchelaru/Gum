using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WpfDataUi.DataTypes
{
    public interface IMemberDefinition
    {
        string Name { get;  }
        string Category { get;  }
        Type MemberType { get;  }
    }
}
