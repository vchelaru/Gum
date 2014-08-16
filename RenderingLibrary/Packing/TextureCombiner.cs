using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RenderingLibrary.Packing
{
    public class TextureCombiner
    {
        public void Combine(List<Texture2D> textures, SpriteBatch spriteBatch, RenderTarget2D target, out Dictionary<Texture2D, Microsoft.Xna.Framework.Rectangle> mapping)
        {
            mapping = new Dictionary<Texture2D, Microsoft.Xna.Framework.Rectangle>();

            textures.Sort((a, b) => System.Math.Max(a.Width, a.Height).CompareTo(System.Math.Max(b.Width, b.Height)));

            RectangleTree rectangleTree = new RectangleTree();

            for(int i = 0; i < textures.Count; i++)
            {
                rectangleTree.TryInsert(i, textures[i].Width, textures[i].Height);
            }

            spriteBatch.GraphicsDevice.SetRenderTarget(target);


            spriteBatch.Begin();

            foreach(var rectangle in rectangleTree.ClosedNodes)
            {
                var texture = textures[rectangle.ID];

                spriteBatch.Draw(texture, new Microsoft.Xna.Framework.Vector2(rectangle.X, rectangle.Y), Microsoft.Xna.Framework.Color.White);

                var rectangleForMapping = new Microsoft.Xna.Framework.Rectangle(rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height);

                mapping.Add(texture, rectangleForMapping);
            }

            spriteBatch.End();


        }

    }
}
