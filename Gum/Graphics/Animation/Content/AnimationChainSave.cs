using System;
using System.Collections.Generic;
using System.Xml.Serialization;

//TODO: the AnimationChain namespace in the content assembly should probably be renamed to avoid this naming conflict

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


        public override string ToString()
        {
            return this.Name + " with " + this.Frames.Count + " frames";
        }

        #endregion
    }
}
