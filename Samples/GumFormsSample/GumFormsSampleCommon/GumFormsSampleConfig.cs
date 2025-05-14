using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GumFormsSample
{
    internal class GumFormsSampleConfig
    {
        public float Scale { get; } = 1f;
        public int Width => (int)(1024 * Scale);
        public int Height => (int)(768 * Scale);

        public void Apply(GraphicsDeviceManager graphics)
        {
            graphics.PreferredBackBufferWidth = Width;
            graphics.PreferredBackBufferHeight = Height;
            graphics.SupportedOrientations = DisplayOrientation.LandscapeLeft | DisplayOrientation.LandscapeRight;
#if ANDROID || iOS
            graphics.IsFullScreen = true;
#endif
        }
    }
}
