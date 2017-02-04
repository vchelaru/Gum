using Gum.DataTypes.Behaviors;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using ToolsUtilities;

namespace Gum.DataTypes
{

    public class GumLoadResult
    {
        public string ErrorMessage
        {
            get;
            set;
        }

        public List<string> MissingFiles
        {
            get;
            private set;
        }

        public GumLoadResult()
        {
            MissingFiles = new List<string>();
        }
    }

    /// <summary>
    /// Represents the data stored in a .gumx file. GumProjectSave
    /// instances can be XML Serialized to a .gumx file.
    /// </summary>
    public class GumProjectSave
    {
        #region Fields

        public const string ScreenExtension = "gusx";
        public const string ComponentExtension = "gucx";
        public const string StandardExtension = "gutx";
        public const string ProjectExtension = "gumx";

        public List<CustomPropertySave> CustomProperties = new List<CustomPropertySave>();

        #endregion

        #region Properties

        public int Version { get; set; }

        public int DefaultCanvasWidth
        {
            get;
            set;
        }

        public int DefaultCanvasHeight
        {
            get;
            set;
        }

        public bool ShowOutlines
        {
            get;
            set;
        }

        public bool RestrictToUnitValues
        {
            get;
            set;
        }

        public List<GuideRectangle> Guides
        {
            get;
            set;
        }

        [XmlIgnore]
        public string FullFileName
        {
            get;
            set;
        }

        // A note about References vs. the Lists:
        // The References are the names of the files
        // referenced by the .gumx. The non-reference
        // lists are the loaded elements. If a GumProjectSave
        // is deserialized, only the references are populated. If
        // the Load method is called, then all references will be used
        // to populate the actual instances.


        [XmlIgnore]
        public List<ScreenSave> Screens
        {
            get;
            set;
        }

        [XmlIgnore]
        public List<ComponentSave> Components
        {
            get;
            set;
        }

        [XmlIgnore]
        public List<StandardElementSave> StandardElements
        {
            get;
            set;
        }

        [XmlIgnore]
        public List<BehaviorSave> Behaviors
        {
            get; set;
        } = new List<BehaviorSave>();

        [XmlElement("ScreenReference")]
        public List<ElementReference> ScreenReferences
        {
            get;
            set;
        }

        [XmlElement("ComponentReference")]
        public List<ElementReference> ComponentReferences
        {
            get;
            set;
        }

        [XmlElement("StandardElementReference")]
        public List<ElementReference> StandardElementReferences
        {
            get;
            set;
        }

        [XmlElement("BehaviorReference")]
        public List<BehaviorReference> BehaviorReferences
        {
            get;
            set;
        }

        #endregion

        #region Methods

        public GumProjectSave()
        {
            ShowOutlines = true;
            // I think people want this on by default:
            RestrictToUnitValues = true;

            Guides = new List<GuideRectangle>();

            DefaultCanvasWidth = 800;
            DefaultCanvasHeight = 600;

            Screens = new List<ScreenSave>();
            Components = new List<ComponentSave>();
            StandardElements = new List<StandardElementSave>();

            ScreenReferences = new List<ElementReference>();
            ComponentReferences = new List<ElementReference>();
            StandardElementReferences = new List<ElementReference>();
        }


        public static GumProjectSave Load(string fileName, out GumLoadResult result)
        {
            result = new GumLoadResult();
            if (string.IsNullOrEmpty(fileName))
            {
                result.ErrorMessage = "Passed null file name, could not load GumProjectSave";
                return null;
            }

            GumProjectSave gps = null;

#if ANDROID || IOS || WINDOWS_8
            gps = LoadFromTitleStorage(fileName, result);
#else
            try
            {
                gps = FileManager.XmlDeserialize<GumProjectSave>(fileName);
            }
            catch (FileNotFoundException)
            {
                result.ErrorMessage = "The Gum project file does not exist";
                return null;
            }
            catch (IOException ex)
            {
                result.ErrorMessage = ex.Message;
                return null;
            }
#endif

            string projectRootDirectory = FileManager.GetDirectory(fileName);

            gps.PopulateElementSavesFromReferences(projectRootDirectory, result);
            gps.FullFileName = fileName;

            return gps;
        }

#if ANDROID || IOS || WINDOWS_8
        static GumProjectSave LoadFromTitleStorage(string fileName, GumLoadResult result)
		{
			using (System.IO.Stream stream = Microsoft.Xna.Framework.TitleContainer.OpenStream(fileName))
			{
				GumProjectSave gps = FileManager.XmlDeserializeFromStream<GumProjectSave>(stream);

				string projectRootDirectory = FileManager.GetDirectory(fileName);

				gps.PopulateElementSavesFromReferences(projectRootDirectory, result);

				gps.FullFileName = fileName;

				return gps;
			}
		}
#endif

        private void PopulateElementSavesFromReferences(string projectRootDirectory, GumLoadResult result)
        {
            string errors = "";

            Screens.Clear();
            Components.Clear();
            StandardElements.Clear();
            Behaviors.Clear();

            foreach (ElementReference reference in ScreenReferences)
            {
                ScreenSave toAdd = null;
                try
                {
                    toAdd = reference.ToElementSave<ScreenSave>(projectRootDirectory, ScreenExtension, result);
                }
                catch (Exception e)
                {
                    errors += "\nError loading " + reference.Name + ":\n" + e.Message;
                }
                if (toAdd != null)
                {
                    Screens.Add(toAdd);
                }
            }

            foreach (ElementReference reference in ComponentReferences)
            {
                ComponentSave toAdd = null;
                                
                try
                {
                    toAdd = reference.ToElementSave<ComponentSave>(projectRootDirectory, ComponentExtension, result);
                }
                catch (Exception e)
                {
                    errors += "\nError loading " + reference.Name + ":\n" + e.Message;
                }
                if (toAdd != null)
                {
                    Components.Add(toAdd);
                }
            }

            foreach (ElementReference reference in StandardElementReferences)
            {
                StandardElementSave toAdd = null;
                try
                {
                    toAdd = reference.ToElementSave<StandardElementSave>(projectRootDirectory, StandardExtension, result);
                }
                catch (Exception e)
                {
                    errors += "\nError loading " + reference.Name + ":\n" + e.Message;
                }
                if (toAdd != null)
                {
                    StandardElements.Add(toAdd);
                }
            }

            foreach(var reference in BehaviorReferences)
            {
                BehaviorSave toAdd = null;

                try
                {
                    toAdd = reference.ToBehaviorSave(projectRootDirectory);
                }
                catch (Exception e)
                {
                    errors += "\nError loading " + reference.Name + ":\n" + e.Message;
                }
                if (toAdd != null)
                {
                    Behaviors.Add(toAdd);
                }
            }

            result.ErrorMessage += errors;

        }

#if !WINDOWS_8 && !UWP
        public void Save(string fileName, bool saveElements)
        {
            FileManager.XmlSerialize(this, fileName);

            if (saveElements)
            {
                string directory = FileManager.GetDirectory(fileName);

                foreach (var screenSave in Screens)
                {
                    screenSave.Save(directory + ElementReference.ScreenSubfolder + "/" + screenSave.Name + "." + ScreenExtension);
                }
                foreach (var componentSave in Components)
                {
                    componentSave.Save(directory + ElementReference.ComponentSubfolder + "/" + componentSave.Name + "." + ComponentExtension);
                }
                SaveStandardElements(directory);
            }
        }
#endif

        public void SaveStandardElements(string directory)
        {
            foreach (var standardElement in StandardElements)
            {


                const int maxNumberOfTries = 6;
                const int msBetweenSaves = 100;
                int numberOfTimesTried = 0;

                bool succeeded = false;
                Exception exception = null;

                while (numberOfTimesTried < maxNumberOfTries)
                {
                    try
                    {
                        standardElement.Save(directory + ElementReference.StandardSubfolder + "/" + standardElement.Name + "." + StandardExtension);

                        succeeded = true;
                        break;
                    }
                    catch (Exception e)
                    {
                        exception = e;
#if !WINDOWS8 && !UWP
                        System.Threading.Thread.Sleep(msBetweenSaves);
#endif
                        numberOfTimesTried++;
                    }
                }

                if(!succeeded)
                {
                    throw exception;
                }

            }
        }


        public void ReactToRenamed(ElementSave element, InstanceSave instance, string oldName)
        {
            if (instance == null)
            {
                List<ElementReference> listToSearch = null;

                if (element is ScreenSave)
                {
                    listToSearch = ScreenReferences;
                }
                else if (element is ComponentSave)
                {
                    listToSearch = ComponentReferences;
                }
                else if (element is StandardElementSave)
                {
                    listToSearch = StandardElementReferences;
                }

                foreach (ElementReference reference in listToSearch)
                {
                    if (reference.Name == oldName)
                    {
                        reference.Name = element.Name;
                    }
                }
            }
        }

        #endregion

    }
}
