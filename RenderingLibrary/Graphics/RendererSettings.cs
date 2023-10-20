using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RenderingLibrary.Graphics
{
    public static class RendererSettings
    {
        public static bool UseCustomEffectRendering { get; set; } = false;
        public static bool UseBasicEffectRendering { get; set; } = true;
        public static bool UsingEffect => UseCustomEffectRendering || UseBasicEffectRendering; 
    }
}
