using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using ToolsUtilities;

namespace Gum.DataTypes
{
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

        #endregion

        #region Methods

        public GumProjectSave()
        {
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


        public static GumProjectSave Load(string fileName, out string errors)
        {
            if (System.IO.File.Exists(fileName))
            {
                GumProjectSave gps = FileManager.XmlDeserialize<GumProjectSave>(fileName);

                string projectRootDirectory = FileManager.GetDirectory(fileName);

                gps.PopulateElementSavesFromReferences(projectRootDirectory, out errors);

                gps.FullFileName = fileName;

                return gps;
            }
            else
            {
                errors = "The Gum project file does not exist";
                return null;
            }
        }

        private void PopulateElementSavesFromReferences(string projectRootDirectory, out string errors)
        {
            errors = "";

            Screens.Clear();
            Components.Clear();
            StandardElements.Clear();

            foreach (ElementReference reference in ScreenReferences)
            {
                ScreenSave toAdd = null;
                try
                {
                    toAdd = reference.ToElementSave<ScreenSave>(projectRootDirectory, ScreenExtension, ref errors);
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
                    toAdd = reference.ToElementSave<ComponentSave>(projectRootDirectory, ComponentExtension, ref errors);
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
                    toAdd = reference.ToElementSave<StandardElementSave>(projectRootDirectory, StandardExtension, ref errors);
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
        }

        public void Save(string fileName, bool saveElements)
        {
            FileManager.XmlSerialize(this, fileName);

            string directory = FileManager.GetDirectory(fileName);


            if (saveElements)
            {
                foreach (var screenSave in Screens)
                {
                    screenSave.Save(directory + ElementReference.ScreenSubfolder + "/" + screenSave.Name + "." + ScreenExtension);
                }
                foreach (var componentSave in Components)
                {
                    componentSave.Save(directory + ElementReference.ComponentSubfolder + "/" + componentSave.Name + "." + ComponentExtension);
                }
                foreach (var standardElement in StandardElements)
                {
                    standardElement.Save(directory + ElementReference.StandardSubfolder + "/" + standardElement.Name + "." + StandardExtension);
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
