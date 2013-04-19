using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace RenderingLibrary.Graphics
{
    public class TimedSpriteSheetAnimation : IAnimation
    {
        #region Fields

        bool mLoops;
        double mTimeIntoAnimation;
        double mLastUpdate;

        #endregion

        public Microsoft.Xna.Framework.Graphics.Texture2D CurrentTexture
        {
            get;
            private set;
        }

        public Rectangle? SourceRectangle
        {
            get;
            private set;
        }

        public bool FlipHorizontal
        {
            get;
            private set;
        }

        public bool FlipVertical
        {
            get;
            private set;
        }

        public List<AnimationFrame> Frames
        {
            get;
            private set;
        }

        public int CurrentFrameIndex
        {
            get
            {
                double timeIntoAnimation = mTimeIntoAnimation;

                int frameIndex = 0;
                if (timeIntoAnimation < 0)
                {
                    throw new ArgumentException("The timeIntoAnimation argument must be 0 or positive");
                }
                else if (this.Frames.Count > 1)
                {
                    bool is0Length = this.AnimationLength == 0;

                    while (timeIntoAnimation >= 0)
                    {
                        double frameTime = Frames[frameIndex].FrameTime;

                        if (timeIntoAnimation < frameTime)
                        {
                            // do nothing
                            break;
                        }
                        else
                        {
                            timeIntoAnimation -= frameTime;

                            frameIndex = (frameIndex + 1) % Frames.Count;

                            if (is0Length)
                            {
                                break;
                            }
                        }
                    }
                }
                else if (this.Frames.Count == 1)
                {
                    frameIndex = 0;
                }

                return frameIndex;
            }
        }

        public double AnimationLength
        {
            get
            {
                double toReturn = 0;


                foreach (AnimationFrame frame in Frames)
                {
                    toReturn += frame.FrameTime;
                }
                return toReturn;
            }
        }

        public TimedSpriteSheetAnimation()
        {
            Frames = new List<AnimationFrame>();
        }

        public void AnimationActivity(double time)
        {
            mTimeIntoAnimation += time - mLastUpdate;
            mLastUpdate = time;
            while (AnimationLength != 0 && mTimeIntoAnimation > AnimationLength)
            {
                mTimeIntoAnimation -= AnimationLength;
            }

            SetCurrentFrameFromTimeIntoAnimation();
        }

        private void SetCurrentFrameFromTimeIntoAnimation()
        {
            int frameIndex = CurrentFrameIndex;
            CurrentTexture = Frames[frameIndex].Texture;
            SourceRectangle = Frames[frameIndex].SourceRectangle;
            FlipHorizontal = Frames[frameIndex].FlipHorizontal;
            FlipVertical = Frames[frameIndex].FlipVertical;
        }
    }
}
