using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;

namespace RenderingLibrary.Graphics
{
    public class SpriteManager
    {
        #region Fields

        static SpriteManager mSelf;

        List<Sprite> mSprites = new List<Sprite>();

        #endregion

        #region Properties

        public SystemManagers Managers
        {
            get;
            set;
        }

        TextManager TextManager
        {
            get
            {
                if (Managers == null)
                {
                    return TextManager.Self;
                }
                else
                {
                    return Managers.TextManager;
                }
            }
        }

        Renderer Renderer
        {
            get
            {
                if (Managers == null)
                {
                    return Renderer.Self;
                }
                else
                {
                    return Managers.Renderer;
                }
            }
        }

        public static SpriteManager Self
        {
            get
            {
                if (mSelf == null)
                {
                    mSelf = new SpriteManager();
                }
                return mSelf;
            }
        }

        #endregion

        public void Add(Sprite sprite, Layer layer = null)
        {
            mSprites.Add(sprite);
#if !TEST
                        
            if (layer == null)
            {
                layer = Renderer.LayersWritable[0];
            }

            layer.Add(sprite);
#endif
        }

        
        public void Remove(Sprite sprite)
        {
            mSprites.Remove(sprite);
            Renderer.RemoveRenderable(sprite);
        }

        public void Activity(double currentTime)
        {
            foreach (Sprite s in mSprites)
            {
                if (s.Animation != null && s.Animate)
                {
                    s.AnimationActivity(currentTime);
                    s.Texture = s.Animation.CurrentTexture;
                }
            }
        }
    }
}
