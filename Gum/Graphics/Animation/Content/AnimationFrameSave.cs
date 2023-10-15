using System;
using System.Xml.Serialization;
using Gum.Graphics.Animation;
using Microsoft.Xna.Framework.Graphics;

namespace Gum.Content.AnimationChain
{
#if !UWP
    [Serializable]
#endif
    public class AnimationFrameSave
    {
        #region XML Docs
        /// <summary>
        /// Whether the texture should be flipped horizontally.
        /// </summary>
        #endregion
        public bool FlipHorizontal;
        public bool ShouldSerializeFlipHorizontal()
        {
            return FlipHorizontal == true;
        }

        #region XML Docs
        /// <summary>
        /// Whether the texture should be flipped on the vertidally.
        /// </summary>
        #endregion
        public bool FlipVertical;
        public bool ShouldSerializeFlipVertical()
        {
            return FlipVertical == true;
        }

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

        #region XML Docs
        /// <summary>
        /// The relative X position of the object that is using this AnimationFrame.  This
        /// is only applied if the IAnimationChainAnimatable's UseAnimationRelativePosition is
        /// set to true.
        /// </summary>
        #endregion
        public float RelativeX;
        public bool ShouldSerializeRelativeX()
        {
            return RelativeX != 0;
        }

        #region XML Docs
        /// <summary>
        /// The relative Y position of the object that is using this AnimationFrame.  This
        /// is only applied if the IAnimationChainAnimatable's UseAnimationRelativePosition is
        /// set to true.
        /// </summary>
        #endregion
        public float RelativeY;
        public bool ShouldSerializeRelativeY()
        {
            return RelativeY != 0;
        }


        [XmlIgnore]
#if !UWP
        [NonSerialized]
#endif
        internal Texture2D mTextureInstance;


        public AnimationFrameSave() { }

        public AnimationFrameSave(AnimationFrame template)
        {
            FrameLength = template.FrameLength;
            TextureName = template.TextureName;
            FlipVertical = template.FlipVertical;
            FlipHorizontal = template.FlipHorizontal;

            LeftCoordinate = template.LeftCoordinate;
            RightCoordinate = template.RightCoordinate;
            TopCoordinate = template.TopCoordinate;
            BottomCoordinate = template.BottomCoordinate;

            RelativeX = template.RelativeX;
            RelativeY = template.RelativeY;

            TextureName = template.Texture.Name;
        }


        public AnimationFrame ToAnimationFrame(string contentManagerName)
        {
            return ToAnimationFrame(contentManagerName, true);
        }

        public AnimationFrame ToAnimationFrame(string contentManagerName, bool loadTexture)
        {

            return ToAnimationFrame(contentManagerName, loadTexture, TextureCoordinateType.UV);
        }

        public AnimationFrame ToAnimationFrame(string contentManagerName, bool loadTexture, TextureCoordinateType coordinateType)
        {
            AnimationFrame frame = new AnimationFrame();

            #region Set basic variables

            frame.TextureName = TextureName;
            frame.FrameLength = FrameLength;

            if (loadTexture)
            {
                if (mTextureInstance != null)
                {
                    frame.Texture = mTextureInstance;
                }
                // I think we should tolarte frames with a null Texture
                else if (!string.IsNullOrEmpty(TextureName))
                {
                    //throw new NotImplementedException();
                    //frame.Texture = FlatRedBallServices.Load<Texture2D>(TextureName, contentManagerName);
                    try
                    {
                        var fileName = ToolsUtilities.FileManager.RemoveDotDotSlash(ToolsUtilities.FileManager.RelativeDirectory + TextureName);
                        frame.Texture = global::RenderingLibrary.Content.LoaderManager.Self.LoadContent<Microsoft.Xna.Framework.Graphics.Texture2D>(
                            fileName);
                    }
                    catch (System.IO.FileNotFoundException)
                    {
                        if (Wireframe.GraphicalUiElement.MissingFileBehavior == Wireframe.MissingFileBehavior.ThrowException)
                        {
                            string message = $"Error loading texture in animation :\n{TextureName}";
                            throw new System.IO.FileNotFoundException(message);
                        }
                        frame.Texture = null;
                    }

                }
                //frame.Texture = FlatRedBallServices.Load<Texture2D>(TextureName, contentManagerName);
            }
            frame.FlipHorizontal = FlipHorizontal;
            frame.FlipVertical = FlipVertical;

            if (coordinateType == TextureCoordinateType.UV)
            {
                frame.LeftCoordinate = LeftCoordinate;
                frame.RightCoordinate = RightCoordinate;
                frame.TopCoordinate = TopCoordinate;
                frame.BottomCoordinate = BottomCoordinate;
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
                    frame.LeftCoordinate = LeftCoordinate / frame.Texture.Width;
                    frame.RightCoordinate = RightCoordinate / frame.Texture.Width;

                    frame.TopCoordinate = TopCoordinate / frame.Texture.Height;
                    frame.BottomCoordinate = BottomCoordinate / frame.Texture.Height;
                }
            }
            
            
            frame.RelativeX = RelativeX;
            frame.RelativeY = RelativeY;

            #endregion

            return frame;
        }

        //public AnimationFrame ToAnimationFrame(TextureAtlas textureAtlas)
        //{
        //    AnimationFrame toReturn = ToAnimationFrame(null, false);
        //    var entry = textureAtlas.GetEntryFor(this.TextureName);

        //    if (entry != null)
        //    {

        //        float left;
        //        float right;
        //        float top;
        //        float bottom;


        //        entry.FullToReduced(toReturn.LeftCoordinate, toReturn.RightCoordinate,
        //            toReturn.TopCoordinate, toReturn.BottomCoordinate,
        //            out left, out right, out top, out bottom);

        //        toReturn.LeftCoordinate = left;
        //        toReturn.RightCoordinate = right;
        //        toReturn.TopCoordinate = top;
        //        toReturn.BottomCoordinate = bottom;

        //    }

        //    return toReturn;
        //}


        //internal static AnimationFrameSave FromXElement(System.Xml.Linq.XElement element)
        //{
        //    AnimationFrameSave toReturn = new AnimationFrameSave();

                        
        //    foreach (var subElement in element.Elements())
        //    {
        //        switch (subElement.Name.LocalName)
        //        {
        //            case "FlipHorizontal":
        //                toReturn.FlipHorizontal = SceneSave.AsBool(subElement);
        //                break;
        //            case "FlipVertical":
        //                toReturn.FlipVertical = SceneSave.AsBool(subElement);
        //                break;
        //            case "TextureName":
        //                toReturn.TextureName = subElement.Value;
        //                break;
        //            case "FrameLength":
        //                toReturn.FrameLength = SceneSave.AsFloat(subElement);
        //                break;
        //            case "LeftCoordinate":
        //                toReturn.LeftCoordinate = SceneSave.AsFloat(subElement);
        //                break;
        //            case "RightCoordinate":
        //                toReturn.RightCoordinate = SceneSave.AsFloat(subElement);
        //                break;
        //            case "TopCoordinate":
        //                toReturn.TopCoordinate = SceneSave.AsFloat(subElement);
        //                break;
        //            case "BottomCoordinate":
        //                toReturn.BottomCoordinate = SceneSave.AsFloat(subElement);
        //                break;
        //            case "RelativeX":
        //                toReturn.RelativeX = SceneSave.AsFloat(subElement);
        //                break;
        //            case "RelativeY":
        //                toReturn.RelativeY = SceneSave.AsFloat(subElement);
        //                break;
        //        }
        //    }

        //    return toReturn;
        //}
    }
}
