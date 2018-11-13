using FlatRedBall.Glue.SaveClasses;
using Gum.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToolsUtilities;

namespace GluePlugin
{
    public enum InitializationState
    {
        NotStarted,
        Initializing,
        Initialized
    }

    public class GluePluginState : Singleton<GluePluginState>
    {
        public FilePath GlueProjectFilePath { get; set; }
        public GlueProjectSave GlueProject { get; set;}
        public FilePath CsprojFilePath { get; set; }
        public string ProjectRootNamespace { get; set; }

        public InitializationState InitializationState { get; set; }
    }
}
