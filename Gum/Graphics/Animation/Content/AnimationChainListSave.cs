using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using AnimationChainList = Gum.Graphics.Animation.AnimationChainList;
using ToolsUtilities;
using Gum.Graphics.Animation;
using TimeMeasurementUnit = Gum.Graphics.Animation.TimeMeasurementUnit;

namespace Gum.Content.AnimationChain
{
    [XmlType("AnimationChainArraySave")]
    public class AnimationChainListSave : IDisposable
    {
#if ANDROID || IOS
        public static bool ManualDeserialization = true;
#else
        public static bool ManualDeserialization = false;
#endif

        #region Fields

        private List<string> mToRuntimeErrors = new List<string>();

        [XmlIgnore]
        public string FileName
        {
            set { mFileName = value; }
            get { return mFileName; }
        }

        #endregion

        #region Properties

        [XmlIgnore]
        public List<string> ToRuntimeErrors
        {
            get { return mToRuntimeErrors; }
        }

        [XmlIgnore]
        protected string mFileName;

        /// <summary>
        /// Whether files (usually image files) referenced by this object (and .achx) are
        /// relative to the .achx itself. If false, then file references will be stored as absolute. 
        /// If true, then file reference,s will be stored relative to the .achx itself. This value should
        /// be true so that a .achx can be moved to a different file system or computer and still
        /// have valid references.
        /// </summary>
        public bool FileRelativeTextures = true;

        public TimeMeasurementUnit TimeMeasurementUnit;
        public TextureCoordinateType CoordinateType = TextureCoordinateType.UV;

        [XmlElementAttribute("AnimationChain")]
        public List<AnimationChainSave> AnimationChains;

        #endregion

        #region Methods

        #region Constructor

        public AnimationChainListSave() 
        {
            AnimationChains = new List<AnimationChainSave>();
        }

        #endregion

        public static AnimationChainListSave FromFile(string fileName)
        {
            AnimationChainListSave toReturn = null;

            if (ManualDeserialization)
            {
                throw new NotImplementedException();
                //toReturn = DeserializeManually(fileName);
            }
            else
            {
                toReturn =
                    FileManager.XmlDeserialize<AnimationChainListSave>(fileName);
            }

            if (FileManager.IsRelative(fileName))
                fileName = FileManager.MakeAbsolute(fileName);

            toReturn.mFileName = fileName;

            return toReturn;
        }

        /// <summary>
        /// Create a "save" object from a regular animation chain list
        /// </summary>
        //public static AnimationChainListSave FromAnimationChainList(AnimationChainList chainList)
        //{
        //    AnimationChainListSave achlist = new AnimationChainListSave();
        //    achlist.FileRelativeTextures = chainList.FileRelativeTextures;
        //    achlist.TimeMeasurementUnit = chainList.TimeMeasurementUnit;
        //    achlist.mFileName = chainList.Name;

        //    List<AnimationChainSave> newChains = new List<AnimationChainSave>(chainList.Count);
        //    for (int i = 0; i < chainList.Count; i++)
        //    {
        //        AnimationChainSave ach = AnimationChainSave.FromAnimationChain(chainList[i], achlist.TimeMeasurementUnit);
        //        newChains.Add(ach);
                
        //    }
        //    achlist.AnimationChains = newChains;

        //    return achlist;
        //}


		//public List<string> GetReferencedFiles(RelativeType relativeType)
		//{
            
		//	List<string> referencedFiles = new List<string>();

		//	foreach (AnimationChainSave acs in this.AnimationChains)
		//	{
  //              //if(acs.ParentFile 
  //              if (acs.ParentFile != null && acs.ParentFile.EndsWith(".gif"))
  //              {
  //                  referencedFiles.Add(acs.ParentFile);

  //              }
  //              else
  //              {

  //                  foreach (AnimationFrameSave afs in acs.Frames)
  //                  {
  //                      string texture = FileManager.Standardize( afs.TextureName, null, false );

  //                      if (FileManager.GetExtension(texture).StartsWith("gif"))
  //                      {
  //                          texture = FileManager.RemoveExtension(texture) + ".gif";
  //                      }

  //                      if (!string.IsNullOrEmpty(texture) && !referencedFiles.Contains(texture))
  //                      {
  //                          referencedFiles.Add(texture);
  //                      }
  //                  }
  //              }
		//	}


		//	if (relativeType == RelativeType.Absolute)
		//	{
		//		string directory = FileManager.GetDirectory(FileName);

		//		for (int i = 0; i < referencedFiles.Count; i++)
		//		{
		//			referencedFiles[i] = directory + referencedFiles[i];
		//		}
		//	}

		//	return referencedFiles;
		//}


        //public void Save(string fileName)
        //{           
            

        //    if (FileRelativeTextures)
        //    {
        //        MakeRelative(fileName);
        //    }

        //    FileManager.XmlSerialize(this, fileName);
        //}


        public AnimationChainList ToAnimationChainList(string contentManagerName)
        {

            return ToAnimationChainList(contentManagerName, true);
        }


        public AnimationChainList ToAnimationChainList(string contentManagerName, bool throwError)
        {
            mToRuntimeErrors.Clear();

            AnimationChainList list = new AnimationChainList();

            list.FileRelativeTextures = FileRelativeTextures;
            list.TimeMeasurementUnit = TimeMeasurementUnit;
            list.Name = mFileName;

            string oldRelativeDirectory = FileManager.RelativeDirectory;

            try
            {
                if (this.FileRelativeTextures)
                {
                    FileManager.RelativeDirectory = FileManager.GetDirectory(mFileName);
                }

                foreach (AnimationChainSave animationChain in this.AnimationChains)
                {
                    try
                    {
                        Gum.Graphics.Animation.AnimationChain newChain = null;

                        newChain = animationChain.ToAnimationChain(contentManagerName, this.TimeMeasurementUnit, this.CoordinateType);

                        newChain.mIndexInLoadedAchx = list.Count;

                        newChain.ParentAchxFileName = mFileName;

                        list.Add(newChain);

                    }
                    catch (Exception e)
                    {
                        mToRuntimeErrors.Add(e.ToString());
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

        public void Dispose()
        {
            // do nothing, just need this to add it to the loader manager
        }


        //AnimationChainList ToAnimationChainList(string contentManagerName, TextureAtlas textureAtlas, bool throwError)
        //{
        //    mToRuntimeErrors.Clear();

        //    AnimationChainList list = new AnimationChainList();

        //    list.FileRelativeTextures = FileRelativeTextures;
        //    list.TimeMeasurementUnit = TimeMeasurementUnit;
        //    list.Name = mFileName;

        //    string oldRelativeDirectory = FileManager.RelativeDirectory;

        //    try
        //    {
        //        if (this.FileRelativeTextures)
        //        {
        //            FileManager.RelativeDirectory = FileManager.GetDirectory(mFileName);
        //        }

        //        foreach (AnimationChainSave animationChain in this.AnimationChains)
        //        {
        //            try
        //            {
        //                FlatRedBall.Graphics.Animation.AnimationChain newChain = null;

        //                if (textureAtlas == null)
        //                {
        //                    newChain = animationChain.ToAnimationChain(contentManagerName, this.TimeMeasurementUnit, this.CoordinateType);
        //                }
        //                else
        //                {
        //                    newChain = animationChain.ToAnimationChain(textureAtlas, this.TimeMeasurementUnit);
        //                }
        //                newChain.mIndexInLoadedAchx = list.Count;

        //                newChain.ParentAchxFileName = mFileName;

        //                list.Add(newChain);

        //            }
        //            catch (Exception e)
        //            {
        //                mToRuntimeErrors.Add(e.ToString());
        //                if (throwError)
        //                {
        //                    throw new Exception("Error loading AnimationChain", e);
        //                }
        //            }
        //        }
        //    }
        //    finally
        //    {
        //        FileManager.RelativeDirectory = oldRelativeDirectory;
        //    }

        //    return list;


        //}


        //public AnimationChainList ToAnimationChainList(Graphics.Texture.TextureAtlas textureAtlas)
        //{
        //    return ToAnimationChainList(null, textureAtlas, true);
        //}


        //private void MakeRelative(string fileName)
        //{
        //    string oldRelativeDirectory = FileManager.RelativeDirectory;

        //    string newRelativeDirectory = FileManager.GetDirectory(fileName);
        //    FileManager.RelativeDirectory = newRelativeDirectory;

        //    foreach (AnimationChainSave acs in AnimationChains)
        //    {
        //        acs.MakeRelative();

        //    }

        //    FileManager.RelativeDirectory = oldRelativeDirectory;
        //}


        //private static AnimationChainListSave DeserializeManually(string fileName)
        //{
        //    AnimationChainListSave toReturn = new AnimationChainListSave();
        //    System.Xml.Linq.XDocument xDocument = null;

        //    using (var stream = FileManager.GetStreamForFile(fileName))
        //    {
        //        xDocument = System.Xml.Linq.XDocument.Load(stream);
        //    }

        //    System.Xml.Linq.XElement foundElement = null;

        //    foreach (var element in xDocument.Elements())
        //    {
        //        if (element.Name.LocalName == "AnimationChainArraySave")
        //        {
        //            foundElement = element;
        //            break;
        //        }
        //    }

        //    LoadFromElement(toReturn, foundElement);

        //    return toReturn;
        //}





        #endregion
    }
}
