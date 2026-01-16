using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gum.Wireframe
{
    #region Interfaces

    // additional interfaces, added here to make it easier to manage multiple projects.
    public interface IManagedObject
    {
        void AddToManagers();
        void RemoveFromManagers();
    }

    #endregion
}
