using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameGum;
using RenderingLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GumFormsSample.Services
{
    internal class RenderService
    {
        public void Draw(GraphicsDevice graphicsDevice, RenderTarget2D renderTarget, SpriteBatch spriteBatch, GumFormsSampleConfig config)
        {
            graphicsDevice.SetRenderTarget(renderTarget);
            graphicsDevice.Clear(Color.CornflowerBlue);
            GumService.Default.Draw();
            graphicsDevice.SetRenderTarget(null);

            spriteBatch.Begin();
            spriteBatch.Draw(renderTarget, new Rectangle(0, 0, config.Width, config.Height), Color.White);
            spriteBatch.End();
        }
    }
}
