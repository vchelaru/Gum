using System;

namespace WpfDataUi.DataTypes
{
    public interface IMemberDefinition
    {
        string Name { get;  }
        string Category { get;  }
        Type MemberType { get;  }
    }
}
