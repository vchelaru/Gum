using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using ToolsUtilities;

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
            AnimationChainListSave? toReturn = null;

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



        public void Dispose()
        {
            // do nothing, just need this to add it to the loader manager
        }

        #endregion
    }
}
