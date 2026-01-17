using Gum.DataTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gum.Plugins;

public interface IPluginManager
{
    // todo - interface this...
    void InstanceReordered(InstanceSave instance);

    bool GetIfExtensionIsValid(string extension, ElementSave parentElement, InstanceSave instance, string changedMember);
}
