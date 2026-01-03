using System;
using Gum.Content.AnimationChain;



#if MONOGAME || KNI || XNA4
using Texture2D = Microsoft.Xna.Framework.Graphics.Texture2D;
#endif

using System.Xml.Serialization;

namespace Gum.Graphics.Animation
{

    /// <summary>
    /// Stores information about one frame in a texture-flipping animation.
    /// </summary>
    /// <remarks>
    /// Includes
    /// information about which Texture2D to show, whether the Texture2D should be flipped,
    /// the length of time to show the Texture2D for, texture coordinates (for sprite sheets), and
    /// relative positioning.
    /// </remarks>
    public class AnimationFrame : IEquatable<AnimationFrame>
    {
        #region Fields

        #region XML Docs
        /// <summary>
        /// Empty AnimationFrame.
        /// </summary>
        #endregion
        public static AnimationFrame Empty;

#if MONOGAME || KNI || XNA4
        /// <summary>
        /// The texture that the AnimationFrame will show.
        /// </summary>
        [XmlIgnore]
        public Texture2D Texture;
#endif

        /// <summary>
        /// Whether the texture should be flipped horizontally.
        /// </summary>
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


#if MONOGAME || KNI || XNA4
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


#if MONOGAME || KNI || XNA4
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

    public static class AnimationFrameExtensionMethods
    {

        public static AnimationFrame ToAnimationFrame(this AnimationFrameSave animationFrameSave)
        {
            return animationFrameSave.ToAnimationFrame(true);
        }

        public static AnimationFrame ToAnimationFrame(this AnimationFrameSave animationFrameSave, bool loadTexture)
        {

            return animationFrameSave.ToAnimationFrame(loadTexture, TextureCoordinateType.UV);
        }

        public static AnimationFrame ToAnimationFrame(this AnimationFrameSave animationFrameSave, bool loadTexture, TextureCoordinateType coordinateType)
        {
            AnimationFrame frame = new AnimationFrame();

            #region Set basic variables

            frame.TextureName = animationFrameSave.TextureName;
            frame.FrameLength = animationFrameSave.FrameLength;

            if (loadTexture)
            {
                //if (animationFrameSave.mTextureInstance != null)
                //{
                //    frame.Texture = animationFrameSave.mTextureInstance;
                //}
                // I think we should tolerate frames with a null Texture
                if (!string.IsNullOrEmpty(animationFrameSave.TextureName))
                {
                    //throw new NotImplementedException();
                    //frame.Texture = FlatRedBallServices.Load<Texture2D>(TextureName, contentManagerName);
                    try
                    {
                        var fileName = ToolsUtilities.FileManager.RemoveDotDotSlash(ToolsUtilities.FileManager.RelativeDirectory + animationFrameSave.TextureName);
                        frame.Texture = global::RenderingLibrary.Content.LoaderManager.Self.LoadContent<Microsoft.Xna.Framework.Graphics.Texture2D>(
                            fileName);
                    }
                    catch (System.IO.FileNotFoundException)
                    {
                        if (Wireframe.GraphicalUiElement.MissingFileBehavior == Wireframe.MissingFileBehavior.ThrowException)
                        {
                            string message = $"Error loading texture in animation :\n{animationFrameSave.TextureName}";
                            throw new System.IO.FileNotFoundException(message);
                        }
                        frame.Texture = null;
                    }

                }
                //frame.Texture = FlatRedBallServices.Load<Texture2D>(TextureName, contentManagerName);
            }
            frame.FlipHorizontal = animationFrameSave.FlipHorizontal;
            frame.FlipVertical = animationFrameSave.FlipVertical;

            if (coordinateType == TextureCoordinateType.UV)
            {
                frame.LeftCoordinate = animationFrameSave.LeftCoordinate;
                frame.RightCoordinate = animationFrameSave.RightCoordinate;
                frame.TopCoordinate = animationFrameSave.TopCoordinate;
                frame.BottomCoordinate = animationFrameSave.BottomCoordinate;
            }
            else if (coordinateType == TextureCoordinateType.Pixel)
            {
                // April 16, 2015
                // Victor Chelaru
                // We used to throw this exception, but I don't know why we should, because
                // the Sprite won't show up, and the problem should be discoverable in tools
                // without a crash
                //if (frame.Texture == null)
                //{
                //    throw new Exception("The frame must have its texture loaded to use the Pixel coordinate type");
                //}

                if (frame.Texture != null)
                {
                    frame.LeftCoordinate = animationFrameSave.LeftCoordinate / frame.Texture.Width;
                    frame.RightCoordinate = animationFrameSave.RightCoordinate / frame.Texture.Width;

                    frame.TopCoordinate = animationFrameSave.TopCoordinate / frame.Texture.Height;
                    frame.BottomCoordinate = animationFrameSave.BottomCoordinate / frame.Texture.Height;
                }
            }


            frame.RelativeX = animationFrameSave.RelativeX;
            frame.RelativeY = animationFrameSave.RelativeY;

            #endregion

            return frame;
        }

    }

}
