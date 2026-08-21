using System;
using System.Xml.Serialization;

namespace Gum.Content.AnimationChain
{
    public enum TextureCoordinateType
    {
        UV,
        Pixel
    }
    public enum TimeMeasurementUnit
    {
        Undefined,
        Millisecond,
        Second
    }

    [Serializable]
    public class AnimationFrameSave
    {
        /// <summary>
        /// Whether the texture should be flipped horizontally.
        /// </summary>
        public bool FlipHorizontal;
        public bool ShouldSerializeFlipHorizontal()
        {
            return FlipHorizontal == true;
        }

        /// <summary>
        /// Whether the texture should be flipped on the vertidally.
        /// </summary>
        public bool FlipVertical;
        public bool ShouldSerializeFlipVertical()
        {
            return FlipVertical == true;
        }

        /// <summary>
        /// Whether the texture should be flipped diagonally (reflected across its main diagonal,
        /// swapping which texture corner lands on which quad corner). Only produces an undistorted
        /// result when the frame's source rectangle is square.
        /// </summary>
        public bool FlipDiagonal;
        public bool ShouldSerializeFlipDiagonal()
        {
            return FlipDiagonal == true;
        }

        /// <summary>
        /// The alpha (0-255) to scale the frame's render alpha by. Null means the frame doesn't
        /// author an alpha and playback should leave the current alpha unchanged.
        /// </summary>
        public int? Alpha;
        public bool ShouldSerializeAlpha()
        {
            return Alpha.HasValue;
        }

        /// <summary>
        /// Used in XML Serialization of AnimationChains - this should
        /// not explicitly be set by the user.
        /// </summary>
        public string TextureName;

        /// <summary>
        /// The amount of time in seconds the AnimationFrame should be shown for.
        /// </summary>
        public float FrameLength;

        /// <summary>
        /// The left coordinate in texture coordinates of the AnimationFrame.  Default is 0.
        /// This may be in UV coordinates or pixel coordinates.
        /// </summary>
        public float LeftCoordinate;

        /// <summary>
        /// The right coordinate in texture coordinates of the AnimationFrame.  Default is 1.
        /// This may be in UV coordinates or pixel coordinates.
        /// </summary>
        public float RightCoordinate = 1;

        /// <summary>
        /// The top coordinate in texture coordinates of the AnimationFrame.  Default is 0.
        /// This may be in UV coordinates or pixel coordinates.
        /// </summary>
        public float TopCoordinate;

        /// <summary>
        /// The bottom coordinate in texture coordinates of the AnimationFrame.  Default is 1.
        /// This may be in UV coordinates or pixel coordinates.
        /// </summary>
        public float BottomCoordinate = 1;

        /// <summary>
        /// The relative X position of the object that is using this AnimationFrame.  This
        /// is only applied if the IAnimationChainAnimatable's UseAnimationRelativePosition is
        /// set to true.
        /// </summary>
        public float RelativeX;
        public bool ShouldSerializeRelativeX()
        {
            return RelativeX != 0;
        }

        /// <summary>
        /// The relative Y position of the object that is using this AnimationFrame.  This
        /// is only applied if the IAnimationChainAnimatable's UseAnimationRelativePosition is
        /// set to true.
        /// </summary>
        public float RelativeY;
        public bool ShouldSerializeRelativeY()
        {
            return RelativeY != 0;
        }



        public AnimationFrameSave() { }

        //public AnimationFrameSave(AnimationFrame template)
        //{
        //    FrameLength = template.FrameLength;
        //    TextureName = template.TextureName;
        //    FlipVertical = template.FlipVertical;
        //    FlipHorizontal = template.FlipHorizontal;

        //    LeftCoordinate = template.LeftCoordinate;
        //    RightCoordinate = template.RightCoordinate;
        //    TopCoordinate = template.TopCoordinate;
        //    BottomCoordinate = template.BottomCoordinate;

        //    RelativeX = template.RelativeX;
        //    RelativeY = template.RelativeY;

        //    TextureName = template.Texture.Name;
        //}
    }
}
