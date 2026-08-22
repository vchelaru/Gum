using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text.Json.Nodes;
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

        [UnconditionalSuppressMessage("Trimming", "IL2026",
            Justification = "Deserializes AnimationChainListSave, which GumCommon's ILLink.Descriptors.xml preserves in full (preserve=\"all\") under Gum.Content.AnimationChain.*.")]
        public static AnimationChainListSave FromFile(string fileName)
        {
            AnimationChainListSave? toReturn = null;

            if (IsJsonFileName(fileName))
            {
                // JsonNode-based parsing does no reflection/codegen, so it works on Android/iOS
                // (ManualDeserialization) the same as any other platform — no separate branch needed.
                using (Stream stream = FileManager.GetStreamForFile(fileName))
                {
                    toReturn = ParseJson(JsonNode.Parse(stream)!.AsObject());
                }
            }
            else if (ManualDeserialization)
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

        #region JSON (.achj)

        /// <summary>True when <paramref name="fileName"/> has a .achj (JSON) extension; false for .achx or anything else.</summary>
        private static bool IsJsonFileName(string fileName) =>
            fileName.EndsWith(".achj", StringComparison.OrdinalIgnoreCase);

        // Property names match FlatRedBall2's AnimationChain.Common .achj writer (camelCase) so a
        // file authored by the FlatRedBall Animation Editor loads into Gum without a conversion
        // step. Per-frame shapes (#4479) are the one FRB2 addition Gum's AnimationFrameSave
        // doesn't model yet — simply not read; JsonObject's indexer returns null for a missing
        // key, so those files still load, just without that data.
        private static AnimationChainListSave ParseJson(JsonObject root)
        {
            AnimationChainListSave result = new AnimationChainListSave();

            if (root["fileRelativeTextures"] is JsonValue frt)
                result.FileRelativeTextures = frt.GetValue<bool>();

            if (root["timeMeasurementUnit"] is JsonValue tmu)
                result.TimeMeasurementUnit = Enum.Parse<TimeMeasurementUnit>(tmu.GetValue<string>()!);

            if (root["coordinateType"] is JsonValue ct)
                result.CoordinateType = Enum.Parse<TextureCoordinateType>(ct.GetValue<string>()!);

            if (root["animationChains"] is JsonArray chainsArray)
            {
                foreach (JsonNode? chainNode in chainsArray)
                {
                    JsonObject chainObj = chainNode!.AsObject();
                    AnimationChainSave chain = new AnimationChainSave
                    {
                        Name = chainObj["name"]?.GetValue<string>() ?? string.Empty
                    };

                    if (chainObj["frames"] is JsonArray framesArray)
                    {
                        foreach (JsonNode? frameNode in framesArray)
                            chain.Frames.Add(ParseFrameJson(frameNode!.AsObject()));
                    }

                    result.AnimationChains.Add(chain);
                }
            }

            return result;
        }

        private static AnimationFrameSave ParseFrameJson(JsonObject frameObj)
        {
            return new AnimationFrameSave
            {
                TextureName = frameObj["textureName"]?.GetValue<string>() ?? string.Empty,
                FrameLength = FloatProperty(frameObj, "frameLength"),
                LeftCoordinate = FloatProperty(frameObj, "leftCoordinate"),
                RightCoordinate = FloatProperty(frameObj, "rightCoordinate", 1f),
                TopCoordinate = FloatProperty(frameObj, "topCoordinate"),
                BottomCoordinate = FloatProperty(frameObj, "bottomCoordinate", 1f),
                FlipHorizontal = BoolProperty(frameObj, "flipHorizontal"),
                FlipVertical = BoolProperty(frameObj, "flipVertical"),
                FlipDiagonal = BoolProperty(frameObj, "flipDiagonal"),
                RelativeX = FloatProperty(frameObj, "relativeX"),
                RelativeY = FloatProperty(frameObj, "relativeY"),
                Alpha = IntProperty(frameObj, "alpha"),
                Red = IntProperty(frameObj, "red"),
                Green = IntProperty(frameObj, "green"),
                Blue = IntProperty(frameObj, "blue"),
                ColorOperation = ColorOperationProperty(frameObj, "colorOperation"),
            };
        }

        private static float FloatProperty(JsonObject parent, string name, float defaultValue = 0f) =>
            parent[name] is JsonValue value ? value.GetValue<float>() : defaultValue;

        private static bool BoolProperty(JsonObject parent, string name, bool defaultValue = false) =>
            parent[name] is JsonValue value ? value.GetValue<bool>() : defaultValue;

        private static int? IntProperty(JsonObject parent, string name) =>
            parent[name] is JsonValue value ? value.GetValue<int>() : (int?)null;

        private static AnimationFrameColorOperation? ColorOperationProperty(JsonObject parent, string name) =>
            parent[name] is JsonValue value ? Enum.Parse<AnimationFrameColorOperation>(value.GetValue<string>()!) : null;

        #endregion

        #endregion
    }
}
