using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gum.DataTypes;

namespace Gum.ToolStates
{
    public class ProjectState
    {
        static ProjectState mSelf = new ProjectState();

        public static ProjectState Self
        {
            get
            {
                return mSelf;
            }
        }

        public ISelectedState Selected
        {
            get
            {
                return SelectedState.Self;
            }
        }

        public GumProjectSave GumProjectSave
        {
            get
            {
                return ProjectManager.Self.GumProjectSave;
            }
        }
    }
}
