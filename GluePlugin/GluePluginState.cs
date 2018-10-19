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
    public class GluePluginState : Singleton<GluePluginState>
    {
        public FilePath GlueProjectFilePath { get; set; }
        public GlueProjectSave GlueProject { get; set;}
    }
}
