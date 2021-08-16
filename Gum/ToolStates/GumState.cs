using Gum.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gum.ToolStates
{
    public class GumState : Singleton<GumState>
    {
        public ProjectState ProjectState => ProjectState.Self;
        public ISelectedState SelectedState => Gum.ToolStates.SelectedState.Self;
    }
}
