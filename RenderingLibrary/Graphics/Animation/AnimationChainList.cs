using Gum.Content.AnimationChain;
using System;
using System.Collections.Generic;
using ToolsUtilities;
using Texture2D = Microsoft.Xna.Framework.Graphics.Texture2D;

namespace Gum.Graphics.Animation
{

    /// <summary>
    /// A list of AnimationChains.
    /// </summary>
    /// <remarks>
    /// This class is often used by IAnimationChainAnimatables to store a list of
    /// AnimationChains.  Since the AnimationChainList provides a string indexer, it
    /// is common to get a reference to an AnimationChain by its name and set it as the
    /// IAnimationChainAnimatable's current AnimationChain.
    /// </remarks>
    public class AnimationChainList : List<AnimationChain>, IDisposable, IEquatable<AnimationChainList>
    {
        #region Fields

        string mName;

        bool mFileRelativeTextures;

        TimeMeasurementUnit mTimeMeasurementUnit = TimeMeasurementUnit.Second;

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

        /// <summary>
        /// Instantiates a new AnimationChainList.
        /// </summary>
        public AnimationChainList()
            : base()
        { }

        /// <summary>
        /// Instantiates a new AnimationChainList.
        /// </summary>
        /// <param name="capacity">Sets the initial capacity to reduce memory allocation when subsequently calling Add.</param>
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

    public static class AnimationChainListSaveExtensionMethods
    {
        // todo - the contentManagerName parameter is unused, but FRB codegen depends on it. Need to increment FRB version to remove this, but that's a pain, so making it optional for now
        public static AnimationChainList ToAnimationChainList(this AnimationChainListSave animationChainListSave, string? contentManagerName = null)
        {

            return animationChainListSave.ToAnimationChainList(true);
        }


        public static AnimationChainList ToAnimationChainList(this AnimationChainListSave animationChainListSave, bool throwError)
        {
            animationChainListSave.ToRuntimeErrors.Clear();

            AnimationChainList list = new AnimationChainList();

            list.FileRelativeTextures = animationChainListSave.FileRelativeTextures;
            list.TimeMeasurementUnit = animationChainListSave.TimeMeasurementUnit;
            list.Name = animationChainListSave.FileName;

            string oldRelativeDirectory = FileManager.RelativeDirectory;

            try
            {
                if (animationChainListSave.FileRelativeTextures)
                {
                    FileManager.RelativeDirectory = FileManager.GetDirectory(animationChainListSave.FileName);
                }

                foreach (AnimationChainSave animationChain in animationChainListSave.AnimationChains)
                {
                    try
                    {
                        Gum.Graphics.Animation.AnimationChain newChain = null;

                        newChain = animationChain.ToAnimationChain(animationChainListSave.TimeMeasurementUnit, animationChainListSave.CoordinateType);

                        newChain.IndexInLoadedAchx = list.Count;

                        newChain.ParentAchxFileName = animationChainListSave.FileName;

                        list.Add(newChain);

                    }
                    catch (Exception e)
                    {
                        animationChainListSave.ToRuntimeErrors.Add(e.ToString());
                        if (throwError)
                        {
                            throw new Exception("Error loading AnimationChain", e);
                        }
                    }
                }
            }
            finally
            {
                FileManager.RelativeDirectory = oldRelativeDirectory;
            }

            return list;
        }

    }
}
