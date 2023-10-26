using System.Collections.Generic;

namespace RenderingLibrary.Graphics
{
    public class TextManager
    {
        #region Fields

        static TextManager mSelf;

        List<Text> mTexts = new List<Text>();

        #endregion

        #region Properties
        public SystemManagers Managers
        {
            get;
            set;
        }
        public static TextManager Self
        {
            get
            {
                if (mSelf == null)
                {
                    mSelf = new TextManager();
                }
                return mSelf;
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

        public IEnumerable<Text> Texts
        {
            get
            {
                return mTexts;
            }
        }
        #endregion

        public void Add(Text text)
        {
            Add(text, null);
        }

        public void Add(Text text, Layer layer)
        {
            if (layer == null)
            {
                layer = Renderer.LayersWritable[0];
            }
            mTexts.Add(text);
#if !TEST
            layer.Add(text);
#endif
        }

        public void Remove(Text text)
        {
            mTexts.Remove(text);
#if !TEST
            Renderer.RemoveRenderable(text);
#endif
        }



        internal void RenderTextTextures()
        {
            foreach(var text in mTexts)
            {
                if (text.AbsoluteVisible)
                {
                    text.TryUpdateTextureToRender();
                }
            }
        }
    }
}
