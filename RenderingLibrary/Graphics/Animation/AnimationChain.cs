using Gum.Content.AnimationChain;
using System;
using System.Collections.Generic;
using System.Xml.Linq;
using Texture2D = Microsoft.Xna.Framework.Graphics.Texture2D;




namespace Gum.Graphics.Animation
{
    /// <summary>
    /// Represents a collection of AnimationFrames which can be used to perform
    /// texture flipping animation on IAnimationChainAnimatables such as Sprites.
    /// </summary>
    public partial class AnimationChain : List<AnimationFrame>, IEquatable<AnimationChain>
    {
        #region Fields

        private string mName;

        //private string mParentFileName;
        public int IndexInLoadedAchx = -1;

        #endregion

        #region Properties

        /// <summary>
        /// Sets the frame time to every frame in the animation to the value. For example, assigning a FrameTime of .2 will make every frame in the animation last .2 seconds.
        /// </summary>
        public float FrameTime
        {
            set
            {
                foreach (AnimationFrame frame in this)
                    frame.FrameLength = value;
            }
        }



        #region XML Docs
        /// <summary>
        /// Gets the last AnimationFrame of the AnimationChain or null if 
        /// there are no AnimationFrames.
        /// </summary>
        #endregion
        public AnimationFrame LastFrame
        {
            get 
            {
                if (this.Count == 0)
                {
                    return null;
                }
                else
                {
                    return this[this.Count - 1];
                }
            }
        }


        #region XML Docs
        /// <summary>
        /// The name of the AnimationChain.
        /// </summary>
        #endregion
        public string Name
        {
            get { return mName; }
            set { mName = value; }
        }

        private string mParentAchxFileName;
        public string ParentAchxFileName
        {
            get { return mParentAchxFileName; }
            set { mParentAchxFileName = value; }
        }

        private string mParentGifFileName;
        public string ParentGifFileName
        {
            get { return mParentGifFileName; }
            set { mParentGifFileName = value; }
        }

        /// <summary>
        /// The total duration of the animation in seconds. This is obtained by adding the FrameTime of all contained frames.
        /// </summary>
        public float TotalLength
        {
            get
            {
                float sum = 0;
                for (int i = 0; i < this.Count; i++)
                {
                    AnimationFrame af = this[i];
                    sum += af.FrameLength;
                }

                return sum;
            }
        }

        #endregion

        #region Methods

        #region Constructors

        #region XML Docs
        /// <summary>
        /// Creates an empty AnimationChain.
        /// </summary>
        #endregion
        public AnimationChain()
            : base()
        { }

        #region XML Docs
        /// <summary>
        /// Creates a new AnimationChain with the argument capacity.
        /// </summary>
        /// <param name="capacity">Sets the initial capacity.  Used to reduce memory allocation.</param>
        #endregion
        public AnimationChain(int capacity)
            : base(capacity)
        { }

        #endregion


        #region Public Methods
        public AnimationChain Clone()
        {
            AnimationChain animationChain = new AnimationChain();

            foreach (AnimationFrame animationFrame in this)
            {
                animationChain.Add(animationFrame.Clone());
            }

            animationChain.ParentGifFileName = ParentGifFileName;
            animationChain.ParentAchxFileName = ParentAchxFileName;

            animationChain.IndexInLoadedAchx = IndexInLoadedAchx;

            animationChain.mName = this.mName;

            return animationChain;
        }

        #region XML Docs
        /// <summary>
        /// Searches for and returns the AnimationFrame with its Name matching
        /// the nameToSearchFor argument, or null if none are found.
        /// </summary>
        /// <param name="nameToSearchFor">The name of the AnimationFrame to search for.</param>
        /// <returns>The AnimationFrame with matching name, or null if none exists.</returns>
        #endregion
        public AnimationFrame FindByName(string nameToSearchFor)
        {
            for (int i = 0; i < this.Count; i++)
            {
                AnimationFrame af = this[i];
                if (af.Texture.Name == nameToSearchFor)
                    return af;
            }
            return null;
        }

        #region XML Docs
        /// <summary>
        /// Returns the shortest absolute number of frames between the two argument frame numbers.  This
        /// method moves forward and backward and considers looping.
        /// </summary>
        /// <param name="frame1">The index of the first frame.</param>
        /// <param name="frame2">The index of the second frame.</param>
        /// <returns>The positive or negative number of frames between the two arguments.</returns>
        #endregion
        public int FrameToFrame(int frame1, int frame2)
        {
			int difference = frame2 - frame1;

            if (difference > this.Count / 2.0)
                difference -= this.Count;
            else if (difference < -this.Count / 2.0)
                difference += this.Count;

            return difference;
        }


		public void ReplaceTexture(Texture2D oldTexture, Texture2D newTexture)
		{
			for (int i = 0; i < this.Count; i++)
			{
				if (this[i].Texture == oldTexture)
				{
					this[i].Texture = newTexture;
					this[i].TextureName = newTexture.Name;
				}
			}
		}

        public override string ToString()
        {
            return Name + " (" + Count + ")";
        }

		#endregion

		#endregion

        #region IEquatable<AnimationChain> Members

        bool IEquatable<AnimationChain>.Equals(AnimationChain? other)
        {
            return this == other;
        }

        #endregion
    }

    public static class AnimationChainSaveExtensionMethods
    {
        public static Gum.Graphics.Animation.AnimationChain ToAnimationChain(this AnimationChainSave animationChainSave, TimeMeasurementUnit timeMeasurementUnit)
        {
            return animationChainSave.ToAnimationChain(timeMeasurementUnit, TextureCoordinateType.UV);
        }

        public static Gum.Graphics.Animation.AnimationChain ToAnimationChain(this AnimationChainSave animationChainSave, TimeMeasurementUnit timeMeasurementUnit, TextureCoordinateType coordinateType)
        {
            if (!string.IsNullOrEmpty(animationChainSave.ParentFile))
            {
                throw new NotImplementedException();
            }
            else
            {
                Gum.Graphics.Animation.AnimationChain animationChain =
                    new Gum.Graphics.Animation.AnimationChain();

                animationChain.Name = animationChainSave.Name;

                float divisor = 1;

                if (timeMeasurementUnit == TimeMeasurementUnit.Millisecond)
                    divisor = 1000;

                foreach (AnimationFrameSave save in animationChainSave.Frames)
                {
                    // process the AnimationFrame and add it to the newly-created AnimationChain
                    AnimationFrame frame = null;

                    bool loadTexture = true;
                    frame = save.ToAnimationFrame(loadTexture, coordinateType);

                    frame.FrameLength /= divisor;
                    animationChain.Add(frame);

                }

                return animationChain;
            }
        }

    }

}
