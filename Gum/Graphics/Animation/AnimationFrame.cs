using System;
using System.Collections.Generic;
using System.Text;


#if MONOGAME
using Texture2D = Microsoft.Xna.Framework.Graphics.Texture2D;
#endif

using System.Xml;
using System.Xml.Serialization;

namespace Gum.Graphics.Animation
{
    public enum TextureCoordinateType
    {
        UV,
        Pixel
    }

    /// <summary>
    /// Stores information about one frame in a texture-flipping animation.
    /// </summary>
    /// <remarks>
    /// Includes
    /// information about which Texture2D to show, whether the Texture2D should be flipped,
    /// the length of time to show the Texture2D for, texture coordinates (for sprite sheets), and
    /// relative positioning.
    /// </remarks>
    public class AnimationFrame :  IEquatable<AnimationFrame>
    {
        #region Fields

        #region XML Docs
        /// <summary>
        /// Empty AnimationFrame.
        /// </summary>
        #endregion
        public static AnimationFrame Empty;

#if MONOGAME
        /// <summary>
        /// The texture that the AnimationFrame will show.
        /// </summary>
        [XmlIgnore]
        public Texture2D Texture;
#endif

        #region XML Docs
        /// <summary>
        /// Whether the texture should be flipped horizontally.
        /// </summary>
        #endregion
        public bool FlipHorizontal;

        #region XML Docs
        /// <summary>
        /// Whether the texture should be flipped on the vertidally.
        /// </summary>
        #endregion
        public bool FlipVertical;

        #region XML Docs
        /// <summary>
        /// Used in XML Serialization of AnimationChains - this should
        /// not explicitly be set by the user.
        /// </summary>
        #endregion
        public string TextureName;

        #region XML Docs
        /// <summary>
        /// The amount of time in seconds the AnimationFrame should be shown for.
        /// </summary>
        #endregion
        public float FrameLength;

        /// <summary>
        /// The left coordinate in texture coordinates of the AnimationFrame.  Default is 0. 
        /// This value is in texture coordinates, not pixels. A value of 1 represents the right-side
        /// of the texture.
        /// </summary>
        public float LeftCoordinate;

        /// <summary>
        /// The right coordinate in texture coordinates of the AnimationFrame.  Default is 1.
        /// This value is in texture coordinates, not pixels. A value of 1 represents the right-side
        /// of the texture.
        /// </summary>
        public float RightCoordinate = 1;

        /// <summary>
        /// The top coordinate in texture coordinates of the AnimationFrame.  Default is 0.
        /// This value is in texture coordinates, not pixels. A value of 1 represents the bottom
        /// of the texture;
        /// </summary>
        public float TopCoordinate;

        /// <summary>
        /// The bottom coordinate in texture coordinates of the AnimationFrame.  Default is 1.
        /// This value is in texture coordinates, not pixels. A value of 1 represents the bottom
        /// of the texture;
        /// </summary>
        public float BottomCoordinate = 1;

        #region XML Docs
        /// <summary>
        /// The relative X position of the object that is using this AnimationFrame.  This
        /// is only applied if the IAnimationChainAnimatable's UseAnimationRelativePosition is
        /// set to true.
        /// </summary>
        #endregion
        public float RelativeX;

        #region XML Docs
        /// <summary>
        /// The relative Y position of the object that is using this AnimationFrame.  This
        /// is only applied if the IAnimationChainAnimatable's UseAnimationRelativePosition is
        /// set to true.
        /// </summary>
        #endregion
        public float RelativeY;

        #endregion

        #region Properties

        //public List<Instruction> Instructions
        //{
        //    get;
        //    private set;
        //}

        #endregion

        #region Methods

        #region Constructors

        #region XML Docs
        /// <summary>
        /// Creates a new AnimationFrame.
        /// </summary>
        #endregion
        public AnimationFrame() 
        {
            //Instructions = new List<Instruction>();
        }


#if MONOGAME
        /// <summary>
        /// Creates a new AnimationFrame.
        /// </summary>
        /// <param name="texture">The Texture2D to use for this AnimationFrame.</param>
        /// <param name="frameLength">The amount of time in seconds that this AnimationFrame will display for when 
        /// it is used in an AnimationChain.</param>
        public AnimationFrame(Texture2D texture, float frameLength)
        {
            Texture = texture;
            FrameLength = frameLength;
            FlipHorizontal = false;
            FlipVertical = false;
            
            //Instructions = new List<Instruction>();

            if (texture != null)
            {
                TextureName = texture.Name;
            }
        }
#endif


        #endregion

        #region Public Methods

        #region XML Docs
        /// <summary>
        /// Creates a new AnimationFrame with identical properties.  The new AnimationFrame
        /// will not belong to the AnimationChain that this AnimationFrameBelongs to unless manually
        /// added.
        /// </summary>
        /// <returns>The new AnimationFrame instance.</returns>
        #endregion
        public AnimationFrame Clone()
        {
            AnimationFrame animationFrame = this.MemberwiseClone() as AnimationFrame;
            return animationFrame;
        }


#if MONOGAME
        /// <summary>
        /// Returns a string representation of this.
        /// </summary>
        /// <returns>String representation of this.</returns>
        public override string ToString()
        {
            if (Texture != null)
                return Texture.Name.ToString();
            else
                return "<EMPTY>";
        }
#endif

        #endregion

        #endregion

        #region IEquatable<AnimationFrame> Members

        bool IEquatable<AnimationFrame>.Equals(AnimationFrame other)
        {
            return this == other;
        }

        #endregion
    }
}
