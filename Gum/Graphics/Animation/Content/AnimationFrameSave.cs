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

    /// <summary>
    /// How a frame's per-frame color (<see cref="AnimationFrameSave.Red"/>/
    /// <see cref="AnimationFrameSave.Green"/>/<see cref="AnimationFrameSave.Blue"/>) combines with
    /// the sprite's texture, matching FlatRedBall2's <c>ColorOperation</c> enum
    /// (<c>FlatRedBall2.Animation.ColorOperation</c>) authored by the FlatRedBall Animation Editor.
    /// </summary>
    public enum AnimationFrameColorOperation
    {
        /// <summary>Multiply the texture by the color (darken / colorize). White (255) is the identity.</summary>
        Multiply,

        /// <summary>Add the color to the texture (brighten / glow / flash). Black (0) is the identity.
        /// Applied on MonoGame/KNI/FNA (#4792 Gap 2) as a second additive draw pass rather than a
        /// custom shader: the normal draw, then the same sprite drawn again with the additive color
        /// blended on top and destination alpha preserved. Because clamping happens after the
        /// additive blend instead of in one pass, semi-transparent edges over a mid-tone background
        /// can come out a bit hotter than a true single-pass Add would — acceptable for silhouettes
        /// and flashes. raylib, Skia, and NineSlice's equivalent frame-apply path don't implement
        /// this yet and silently drop it (tracked as a follow-up to #4792).</summary>
        Add
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
        /// The red channel (0-255) of the frame's per-frame color. Only combined with the texture
        /// when <see cref="ColorOperation"/> is <see cref="AnimationFrameColorOperation.Multiply"/>;
        /// null (the identity, 255) if unset.
        /// </summary>
        public int? Red;
        public bool ShouldSerializeRed()
        {
            return Red.HasValue;
        }

        /// <summary>The green channel (0-255) of the frame's per-frame color. See <see cref="Red"/>.</summary>
        public int? Green;
        public bool ShouldSerializeGreen()
        {
            return Green.HasValue;
        }

        /// <summary>The blue channel (0-255) of the frame's per-frame color. See <see cref="Red"/>.</summary>
        public int? Blue;
        public bool ShouldSerializeBlue()
        {
            return Blue.HasValue;
        }

        /// <summary>
        /// How <see cref="Red"/>/<see cref="Green"/>/<see cref="Blue"/> combine with the texture, or
        /// null if the frame doesn't author a per-frame color.
        /// </summary>
        public AnimationFrameColorOperation? ColorOperation;
        public bool ShouldSerializeColorOperation()
        {
            return ColorOperation.HasValue;
        }

        /// <summary>
        /// Which rendering technique the frame's sprite should use — <c>Modulate</c> (normal tinted
        /// texture, the default) or <c>ColorTextureAlpha</c> (a flat-color silhouette that uses the
        /// texture only as an alpha mask) — or null if the frame doesn't author one, leaving the
        /// sprite's current technique unchanged. Distinct from <see cref="ColorOperation"/> above:
        /// that one is FlatRedBall's Multiply/Add per-frame tint, this one is Gum's own
        /// <c>RenderingLibrary.Graphics.ColorOperation</c> render-technique selector, which
        /// <c>SpriteRuntime.ColorOperation</c> (MonoGame/KNI/FNA, #4792 Gap 1) exposes but had no
        /// authoring path until this field (#4822). raylib, Skia, and NineSlice don't apply it yet.
        /// </summary>
        public global::RenderingLibrary.Graphics.ColorOperation? RenderColorOperation;
        public bool ShouldSerializeRenderColorOperation()
        {
            return RenderColorOperation.HasValue;
        }

        /// <summary>
        /// Used in XML Serialization of AnimationChains - this should
        /// not explicitly be set by the user.
        /// </summary>
        public string? TextureName;

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
