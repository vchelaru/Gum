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
        List<NineSlice> mNineSlices = new List<NineSlice>();
        List<InvisibleRenderable> invisibleRenderables = new List<InvisibleRenderable>();

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

        public IEnumerable<Sprite> Sprites
        {
            get
            {
                return mSprites;
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

        public void Add(InvisibleRenderable renderable, Layer layer = null)
        {
            invisibleRenderables.Add(renderable);
#if !TEST

            if (layer == null)
            {
                layer = Renderer.LayersWritable[0];
            }

            layer.Add(renderable);
#endif
        }

        public void Add(NineSlice nineSlice, Layer layer = null)
        {
            mNineSlices.Add(nineSlice);

#if !TEST
            if (layer == null)
            {
                layer = Renderer.LayersWritable[0];
            }
            layer.Add(nineSlice);
#endif
        }

        
        public void Remove(Sprite sprite)
        {
            mSprites.Remove(sprite);
            Renderer.RemoveRenderable(sprite);
        }

        public void Remove(InvisibleRenderable invisibleRenderable)
        {
            invisibleRenderables.Remove(invisibleRenderable);
            Renderer.RemoveRenderable(invisibleRenderable);
        }

        public void Remove(NineSlice nineSlice)
        {
            mNineSlices.Remove(nineSlice);
            Renderer.RemoveRenderable(nineSlice);
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
            // maybe we need to support nine-slice animation at some point?
        }
    }
}
