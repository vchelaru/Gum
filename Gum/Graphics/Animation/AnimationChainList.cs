using System;
using System.Collections.Generic;
using System.Text;
using Texture2D = Microsoft.Xna.Framework.Graphics.Texture2D;

namespace Gum.Graphics.Animation
{
    public enum TimeMeasurementUnit
    {
        Undefined,
        Millisecond,
        Second
    }

    #region XML Docs
    /// <summary>
    /// A list of AnimationChains.
    /// </summary>
    /// <remarks>
    /// This class is often used by IAnimationChainAnimatables to store a list of
    /// AnimationChains.  Since the AnimationChainList provides a string indexer, it
    /// is common to get a reference to an AnimationChain by its name and set it as the
    /// IAnimationChainAnimatable's current AnimationChain.
    /// </remarks>
    #endregion
    public class AnimationChainList : List<AnimationChain>, IDisposable, IEquatable<AnimationChainList>
    {
        #region Fields

        string mName;

        bool mFileRelativeTextures;

        TimeMeasurementUnit mTimeMeasurementUnit = Gum.Graphics.Animation.TimeMeasurementUnit.Second;

        #endregion

        #region Properties

        #region XML Docs
        /// <summary>
        /// Gets and sets whether the AnimationChainList will save the 
        /// Texture2Ds that its AnimationFrames reference with names relative
        /// to the .achx.  Otherwise, this property is not used during runtime.
        /// </summary>
        #endregion
        public bool FileRelativeTextures
        {
            get { return mFileRelativeTextures; }
            set { mFileRelativeTextures = value; }
        }

        #region XML Docs
        /// <summary>
        /// Gets or sets the TimeMeasurementUnit.  This defaults to TimeMeasurementUnit.Millisecond and
        /// should not be changed.  It is included for compatability with older versions of FlatRedBall.
        /// </summary>
        #endregion
        public TimeMeasurementUnit TimeMeasurementUnit
        {
            get { return mTimeMeasurementUnit; }
            set { mTimeMeasurementUnit = value; }
        }

        #region XML Docs
        /// <summary>
        /// Gets and sets the intance's name.
        /// </summary>
        #endregion
        public string Name
        {
            get { return mName; }
            set { mName = value; }
        }

        #region XML Docs
        /// <summary>
        /// Gets the AnimationChain by name.  Returns null if no AnimationChain is found.
        /// </summary>
        /// <param name="animationChainName">The name of the AnimationChain to return</param>
        /// <returns>Reference to the AnimationChain with the specified name.</returns> 
        #endregion
        public AnimationChain this[string animationChainName]
        {
            get
            {
                for (int i = this.Count - 1; i > -1; i--)
                {
                    AnimationChain animationChain = this[i];

                    if (animationChain.Name == animationChainName)
                    {
                        return animationChain;
                    }
                }

                //nothing found, return null
                return null;
            }
        }

        #endregion

        #region Methods

        #region Constructors

        #region XML Docs
        /// <summary>
        /// Instantiates a new AnimationChainList.
        /// </summary>
        #endregion
        public AnimationChainList()
            : base()
        { }

        #region XML Docs
        /// <summary>
        /// Instantiates a new AnimationChainList.
        /// </summary>
        /// <param name="capacity">Sets the initial capacity to reduce memory allocation when subsequently calling Add.</param>
        #endregion
        public AnimationChainList(int capacity)
            : base(capacity)
        { }

        #endregion

        #region Public Methods

        public AnimationChainList Clone()
        {
            AnimationChainList newList = new AnimationChainList(this.Count);

            foreach (AnimationChain chain in this)
            {
                newList.Add(chain.Clone());
            }

            newList.mName = mName;
            newList.mFileRelativeTextures = mFileRelativeTextures;
            newList.mTimeMeasurementUnit = mTimeMeasurementUnit;

            return newList;
        }

		void IDisposable.Dispose()
		{
			// No need to do anything here - all referenced textures are
			// contained in the same ContentManager, and this should only be
			// called when unloading a ContentManager.
		}

		public void SetAllTexture(Texture2D textureToSet)
		{
			int i = 0;
			int j = 0;
			int frameCount = 0;

			for (i = 0; i < this.Count; i++)
			{
				AnimationChain ac = this[i];

				frameCount = ac.Count;

				for (j = 0; j < frameCount; j++)
				{
					ac[j].Texture = textureToSet;
					ac[j].TextureName = textureToSet.Name;
				}

			}

		}

        public override string ToString()
        {
            return this.Name;
        }

		#endregion

		#endregion

        #region IEquatable<AnimationChainList> Members

        bool IEquatable<AnimationChainList>.Equals(AnimationChainList other)
        {
            return this == other;
        }

        #endregion
    }
}
