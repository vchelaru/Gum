using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary.Content;
using Microsoft.Xna.Framework;

namespace RenderingLibrary.Graphics
{
    public class TextureFlipAnimation : IAnimation
    {
        #region Fields

        bool mLoops = true;
        double mTimeIntoAnimation;
        double mLastUpdate;

        #endregion

        #region Properties

        public bool Loops
        {
            get { return mLoops; }
            set { mLoops = value; }
        }

        double FrameLength
        {
            get
            {
                return .25f;
            }
        }

        public Texture2D CurrentTexture
        {
            get;
            private set;
        }

        public List<Texture2D> TextureList
        {
            get;
            private set;
        }

        public bool FlipHorizontal
        {
            get { return false; }
        }

        public bool FlipVertical
        {
            get { return false; }
        }

        double AnimationLength
        {
            get
            {
                return TextureList.Count * FrameLength;
            }
        }

        public Rectangle? SourceRectangle
        {
            get { return null; }
        }

        public int CurrentFrameIndex
        {
            get
            {
                int index = (int)(mTimeIntoAnimation / FrameLength);

                if (index > TextureList.Count - 1)
                {
                    index = TextureList.Count - 1;
                }
                return index;
            }
        }

        #endregion

        public static TextureFlipAnimation FromStringList(List<string> list, SystemManagers managers)
        {
            TextureFlipAnimation toReturn = new TextureFlipAnimation();

            foreach (string fileName in list)
            {
                toReturn.TextureList.Add(LoaderManager.Self.LoadContent<Texture2D>(fileName));
            }
            return toReturn;
        }


        public void AnimationActivity(double time)
        {
            mTimeIntoAnimation += time - mLastUpdate;
            mLastUpdate = time;
            while (mLoops && AnimationLength != 0 && mTimeIntoAnimation > AnimationLength)
            {
                mTimeIntoAnimation -= AnimationLength;
            }

            SetCurrentFrameFromTimeIntoAnimation();
        }

        private void SetCurrentFrameFromTimeIntoAnimation()
        {
            int index = CurrentFrameIndex;

            if (index != -1)
            {
                CurrentTexture = TextureList[index];
            }
        }


        public TextureFlipAnimation()
        {
            TextureList = new List<Texture2D>();
        }




    }
}
