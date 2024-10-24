using System;
using System.Collections.Generic;
using System.Xml.Serialization;

//TODO: the AnimationChain namespace in the content assembly should probably be renamed to avoid this naming conflict
using Gum.Graphics.Animation;

namespace Gum.Content.AnimationChain
{
    [XmlRoot("AnimationChain")]
    [Serializable]
    public class AnimationChainSave
    {
        #region Fields

        public string Name;

        /// <summary>
        /// This is used if the AnimationChain actually comes from 
        /// a file like a .gif.
        /// </summary>
        public string ParentFile;

        [XmlElementAttribute("Frame")]
        public List<AnimationFrameSave> Frames = new List<AnimationFrameSave>();


        #endregion

        #region Methods

        public AnimationChainSave()
        { }

        //internal static AnimationChainSave FromAnimationChain(AnimationChain animationChain, TimeMeasurementUnit timeMeasurementUnit)
        //{
        //    AnimationChainSave animationChainSave = new AnimationChainSave();
        //    animationChainSave.Frames = new List<AnimationFrameSave>();
        //    animationChainSave.Name = animationChain.Name;

        //    foreach (Anim.AnimationFrame frame in animationChain)
        //    {
        //        AnimationFrameSave save = new AnimationFrameSave(frame);
        //        animationChainSave.Frames.Add(save);
        //    }

        //    if (!string.IsNullOrEmpty(animationChain.ParentGifFileName))
        //    {

        //        animationChainSave.ParentFile = animationChain.ParentGifFileName;
        //    }

        //    return animationChainSave;
        //}

        //public void MakeRelative()
        //{
        //    foreach (AnimationFrameSave afs in Frames)
        //    {

        //        if (!string.IsNullOrEmpty(afs.TextureName) && FileManager.IsRelative(afs.TextureName) == false)
        //        {
        //            afs.TextureName = FileManager.MakeRelative(afs.TextureName);
        //        }
        //    }

        //    if (string.IsNullOrEmpty(ParentFile) == false && FileManager.IsRelative(ParentFile) == false)
        //    {
        //        ParentFile = FileManager.MakeRelative(ParentFile);
        //    }
        //}


        //public Anim.AnimationChain ToAnimationChain(Graphics.Texture.TextureAtlas textureAtlas, TimeMeasurementUnit timeMeasurementUnit)
        //{
        //    return ToAnimationChain(null, textureAtlas, timeMeasurementUnit, TextureCoordinateType.UV);

        //}


        public Gum.Graphics.Animation.AnimationChain ToAnimationChain(string contentManagerName, TimeMeasurementUnit timeMeasurementUnit)
        {
            return ToAnimationChain(contentManagerName, timeMeasurementUnit, TextureCoordinateType.UV);
        }

        public Gum.Graphics.Animation.AnimationChain ToAnimationChain(string contentManagerName, TimeMeasurementUnit timeMeasurementUnit, TextureCoordinateType coordinateType)
        {
            if (!string.IsNullOrEmpty(ParentFile))
            {
                throw new NotImplementedException();
            }
            else
            {
                Gum.Graphics.Animation.AnimationChain animationChain =
                    new Gum.Graphics.Animation.AnimationChain();

                animationChain.Name = Name;

                float divisor = 1;

                if (timeMeasurementUnit == TimeMeasurementUnit.Millisecond)
                    divisor = 1000;

                foreach (AnimationFrameSave save in Frames)
                {
                    // process the AnimationFrame and add it to the newly-created AnimationChain
                    AnimationFrame frame = null;

                    bool loadTexture = true;
                    frame = save.ToAnimationFrame(contentManagerName, loadTexture, coordinateType);

                    frame.FrameLength /= divisor;
                    animationChain.Add(frame);

                }

                return animationChain;
            }
        }

        //        private Anim.AnimationChain ToAnimationChain(string contentManagerName, TextureAtlas textureAtlas,
        //            TimeMeasurementUnit timeMeasurementUnit, TextureCoordinateType coordinateType)
        //        {
        //            if (!string.IsNullOrEmpty(ParentFile))
        //            {

        //            }
        //            else
        //            {
        //                Anim.AnimationChain animationChain =
        //                    new Anim.AnimationChain();

        //                animationChain.Name = Name;

        //                float divisor = 1;

        //                if (timeMeasurementUnit == TimeMeasurementUnit.Millisecond)
        //                    divisor = 1000;

        //                foreach (AnimationFrameSave save in Frames)
        //                {
        //                    // process the AnimationFrame and add it to the newly-created AnimationChain
        //                    AnimationFrame frame = null;
        //                    if (textureAtlas == null)
        //                    {
        //                        bool loadTexture = true;
        //                        frame = save.ToAnimationFrame(contentManagerName, loadTexture, coordinateType);
        //                    }
        //                    else
        //                    {
        //                        frame = save.ToAnimationFrame(textureAtlas);
        //                    }
        //                    frame.FrameLength /= divisor;
        //                    animationChain.Add(frame);

        //                }

        //                return animationChain;
        //            }
        //        }

        public override string ToString()
        {
            return this.Name + " with " + this.Frames.Count + " frames";
        }

        #endregion


        //internal static AnimationChainSave FromXElement(System.Xml.Linq.XElement element)
        //{
        //    AnimationChainSave toReturn = new AnimationChainSave();

        //    foreach (var subElement in element.Elements())
        //    {
        //        switch (subElement.Name.LocalName)
        //        {
        //            case "Name":
        //                toReturn.Name = subElement.Value;
        //                break;
        //            case "Frame":
        //                toReturn.Frames.Add(AnimationFrameSave.FromXElement(subElement));
        //                break;
        //        }
        //    }

        //    return toReturn;
        //}

        //private static uint AsUint(System.Xml.Linq.XElement element)
        //{
        //    return uint.Parse(element.Value, CultureInfo.InvariantCulture);
        //}
    }
}
