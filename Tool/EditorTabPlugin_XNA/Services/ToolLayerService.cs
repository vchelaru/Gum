using RenderingLibrary;
using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gum.Managers
{
    public class ToolLayerService : Singleton<ToolLayerService>
    {

        public Layer TopLayer { get; private set; }

        public void Initialize()
        {
            TopLayer = SystemManagers.Default.Renderer.AddLayer();
        }

        public void Activity()
        {
            // just in case another plugin adds more layers, keep this one on top:
            if(SystemManagers.Default.Renderer.Layers.Last() != TopLayer)
            {
                SystemManagers.Default.Renderer.RemoveLayer(TopLayer);
                SystemManagers.Default.Renderer.AddLayer(TopLayer);
            }
        }
    }
}
