using Gum.DataTypes.Behaviors;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using ToolsUtilities;

#if !FRB
using Gum.StateAnimation.SaveClasses;
#endif

namespace Gum.DataTypes;

#region Enums

public enum LinkLoadingPreference
{
    PreferLinked,
    RequireLinked
}

#endregion

#region LoadGumResult class
public class GumLoadResult
{
    /// <summary>
    /// Error message about why the load may have failed. This does not include errors about missing files - for missing files see the MissingFiles list.
    /// </summary>
    /// <remarks>
    /// Errors include situations like a malformed XML file - errors which tools like Gum cannot recover from. Missing files, while resulting
    /// in an incomplete load, do not put the project in an unusable state, so we do not consider them errors.
    /// </remarks>
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
#endregion

/// <summary>
/// Represents the data stored in a .gumx file. GumProjectSave
/// instances can be XML Serialized to a .gumx file.
/// </summary>
public class GumProjectSave
{
    public enum GumxVersions
    {
        InitialVersion = 1,

    }

    #region Fields

    public const string ScreenExtension = "gusx";
    public const string ComponentExtension = "gucx";
    public const string StandardExtension = "gutx";
    public const string ProjectExtension = "gumx";


    public List<CustomPropertySave> CustomProperties = new List<CustomPropertySave>();

    #endregion

    #region Properties

    public string FontRanges { get; set; } = "32-126,160-255";

    public int FontSpacingVertical { get; set; } = 1;
    public int FontSpacingHorizontal { get; set; } = 1;

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

    public bool ShowCanvasOutline { get; set; }

    public bool ShowRuler { get; set; }

    public bool ShowCheckerBackground { get; set; }

    public string TextureFilter { get; set; } = "Point";

    public bool ConvertVariablesOnUnitTypeChange
    {
        get; set;
    }

    public bool RestrictToUnitValues
    {
        get;
        set;
    }

    public bool RestrictFileNamesForAndroid { get; set; } = false;

    public List<GuideRectangle> Guides
    {
        get;
        set;
    }

    /// <summary>
    /// The folder of the root of the parent project if this Gum project is part of a larger project (like a game project or android app).
    /// This is a relative path like "../../"
    /// </summary>
    public string ParentProjectRoot { get; set; }

    public string LocalizationFile { get; set; }
    public bool ShowLocalizationInGum { get; set; } = true;
    public int CurrentLanguageIndex { get; set; }

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

#if !FRB
    [XmlIgnore]
    public List<ElementAnimationsSave> ElementAnimations
    {
        get;
        set;
    } = new List<ElementAnimationsSave>();
#endif

    [XmlIgnore]
    public IEnumerable<ElementSave> AllElements
    {
        get
        {
            foreach (var screen in Screens)
            {
                yield return screen;
            }
            foreach (var component in Components)
            {
                yield return component;
            }
            foreach (var standard in StandardElements)
            {
                yield return standard;
            }
        }
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

    [XmlElement("BehaviorReference")]
    public List<BehaviorReference> BehaviorReferences
    {
        get;
        set;
    }

    public string SinglePixelTextureFile { get; set; }

    public int? SinglePixelTextureTop { get; set; }
    public int? SinglePixelTextureLeft { get; set; }
    public int? SinglePixelTextureRight { get; set; }
    public int? SinglePixelTextureBottom { get; set; }


    #endregion

    #region Methods

    public GumProjectSave()
    {
        ShowOutlines = true;
        ShowCanvasOutline = true;
        ShowRuler = true;
        ShowCheckerBackground = true;

        // I think people want this on by default:
        RestrictToUnitValues = true;

        Guides = new List<GuideRectangle>();

        DefaultCanvasWidth = 800;
        DefaultCanvasHeight = 600;

        Screens = new List<ScreenSave>();
        Components = new List<ComponentSave>();
        StandardElements = new List<StandardElementSave>();
        Behaviors = new List<BehaviorSave>();

        ScreenReferences = new List<ElementReference>();
        ComponentReferences = new List<ElementReference>();
        StandardElementReferences = new List<ElementReference>();
        BehaviorReferences = new List<BehaviorReference>();
    }

    public static GumProjectSave Load(string fileName)
    {
        var toReturn = Load(fileName, out GumLoadResult result);

        if (!string.IsNullOrEmpty(result.ErrorMessage) || result.MissingFiles?.Count > 0)
        {
            var stringBuilder = new StringBuilder();

            if (!string.IsNullOrEmpty(result.ErrorMessage))
            {
                stringBuilder.AppendLine(result.ErrorMessage);
            }

            foreach (var missingFile in result.MissingFiles)
            {
                stringBuilder.AppendLine($"Missing file: {missingFile}");
            }


            throw new Exception(stringBuilder.ToString());
        }
        else
        {
            return toReturn;
        }
    }

    public static GumProjectSave? Load(string fileName, out GumLoadResult result)
    {
        return Load(fileName, LinkLoadingPreference.PreferLinked, out result);
    }

    public static GumProjectSave? Load(string fileName, LinkLoadingPreference linkLoadingPreference, out GumLoadResult result)
    {
        result = new GumLoadResult();
        if (string.IsNullOrEmpty(fileName))
        {
            result.ErrorMessage = "Passed null file name, could not load GumProjectSave";
            return null;
        }

        if (FileManager.IsRelative(fileName))
        {
            fileName = FileManager.RemoveDotDotSlash(FileManager.MakeAbsolute(fileName));
        }


        GumProjectSave gps = null;

        var shouldLoadFromTitleContainer = false;
#if ANDROID || IOS
        shouldLoadFromTitleContainer = true;
#elif NET6_0_OR_GREATER
        // If not using precompiles, it may be a standard .dll which is used everywhere, so we still can check like this:
        shouldLoadFromTitleContainer = System.OperatingSystem.IsAndroid() || System.OperatingSystem.IsBrowser();

#endif


        if (shouldLoadFromTitleContainer)
        {
            using (var stream = FileManager.GetStreamForFile(fileName))
            {
                gps = FileManager.XmlDeserializeFromStream<GumProjectSave>(stream);
            }
        }
        else
        {
            if (!System.IO.File.Exists(fileName))
            {
                result.ErrorMessage = $"Could not find main project file {fileName}";
                return null;
            }
            try
            {
                gps = FileManager.XmlDeserialize<GumProjectSave>(fileName);
            }
            catch (FileNotFoundException)
            {
                result.MissingFiles.Add(fileName);
                return null;
            }
            catch (IOException ex)
            {
                result.ErrorMessage = ex.Message;
                return null;
            }
        }
        string projectRootDirectory = FileManager.GetDirectory(fileName);

        gps.PopulateElementSavesFromReferences(projectRootDirectory, linkLoadingPreference, result);
        gps.FullFileName = fileName.Replace('\\', '/');


        return gps;
    }

    // todo - need to figure out how to get this to work with sub-elements each having their own streams
    //public static GumProjectSave Load(Stream stream)
    //{
    //    GumProjectSave gps = null;
    //    gps = FileManager.XmlDeserializeFromStream<GumProjectSave>(stream);
    //    string projectRootDirectory = FileManager.GetDirectory(fileName);

    //    gps.PopulateElementSavesFromReferences(projectRootDirectory, linkLoadingPreference, result);
    //    // filename is unknown...
    //    //gps.FullFileName = fileName.Replace('\\', '/');
    //    return gps;

    //}

#if ANDROID || IOS
    static GumProjectSave LoadFromTitleStorage(string fileName, LinkLoadingPreference linkLoadingPreference, GumLoadResult result)
		{
			using (System.IO.Stream stream = Microsoft.Xna.Framework.TitleContainer.OpenStream(fileName))
			{
				GumProjectSave gps = FileManager.XmlDeserializeFromStream<GumProjectSave>(stream);

				return gps;
			}
		}
#endif

    /// <summary>
    ///  shortcut function to fetch a specific screen from your gum project.
    /// <code ang="csharp">
    /// //same as :
    /// screen.Find(item => item.Name == "name");
    /// </code>
    ///  
    /// </summary>
    /// <param name="screenName"></param>
    /// <returns></returns>
    public ScreenSave? GetScreenSave(string screenName)
    {
        return Screens.FirstOrDefault(item => item.Name == screenName);
    }


    /// <summary>
    /// shortcu function to fetch a specific component from your gum project
    ///
    /// <code ang="csharp">
    /// //same as :
    /// components.Find(item => item.Name == "name");
    /// </code>
    /// </summary>
    /// <param name="componentName"></param>
    /// <returns></returns>
    public ComponentSave? GetComponentSave(string componentName)
    {
        return Components.FirstOrDefault(item => item.Name == componentName);
    }

    /// <summary>
    /// shortcut function to fetch a specific standard elements from your gum project.
    ///
    /// <code ang="csharp">
    /// //same as :
    /// standards.Find(item => item.Name == "name");
    /// </code>
    /// </summary>
    /// <param name="standardElementName"></param>
    /// <returns></returns>
    public StandardElementSave? GetStandardElementSave(string standardElementName)
    {
        return StandardElements.FirstOrDefault(item => item.Name == standardElementName);
    }
    private void PopulateElementSavesFromReferences(string projectRootDirectory, LinkLoadingPreference linkLoadingPreference, GumLoadResult result)
    {
        string errors = "";

        Screens.Clear();
        Components.Clear();
        StandardElements.Clear();
        Behaviors.Clear();

        foreach (ElementReference reference in ScreenReferences)
        {
            ScreenSave? toAdd = null;
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
            ComponentSave? toAdd = null;

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
            StandardElementSave? toAdd = null;
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

        if (BehaviorReferences != null)
        {
            foreach (var reference in BehaviorReferences)
            {
                BehaviorSave? toAdd = null;

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
        }

        result.ErrorMessage += errors;

    }

    public void ReloadBehavior(BehaviorSave behavior)
    {
        string projectRootDirectory = FileManager.GetDirectory(this.FullFileName);

        var matchingReference = BehaviorReferences.FirstOrDefault(item => item.Name == behavior.Name);

        var newBehaviorSave = matchingReference?.ToBehaviorSave(projectRootDirectory);

        if (newBehaviorSave != null)
        {
            Behaviors.Remove(behavior);
            Behaviors.Add(newBehaviorSave);
        }
    }

    public void ReloadElement(ElementSave element)
    {
        string projectRootDirectory = FileManager.GetDirectory(this.FullFileName);

        var gumLoadResult = new GumLoadResult();

        if (element is ScreenSave)
        {
            var matchingReference = ScreenReferences.FirstOrDefault(item => item.Name == element.Name);

            ScreenSave? newScreen = matchingReference?.ToElementSave<ScreenSave>(
                projectRootDirectory, GumProjectSave.ScreenExtension, gumLoadResult);

            if (newScreen != null)
            {
                if(element is ScreenSave oldScreen)
                {
                    Screens.Remove(oldScreen);
                }
                Screens.Add(newScreen);
            }
        }
        else if (element is ComponentSave)
        {
            var matchingReference = ComponentReferences.FirstOrDefault(item => item.Name == element.Name);

            ComponentSave newComonent = matchingReference?.ToElementSave<ComponentSave>(
                projectRootDirectory, GumProjectSave.ComponentExtension, gumLoadResult);

            if (newComonent != null)
            {
                Components.Remove(element as ComponentSave);
                Components.Add(newComonent);
            }
        }
        else if (element is StandardElementSave)
        {
            var matchingReference = StandardElementReferences.FirstOrDefault(item => item.Name == element.Name);

            StandardElementSave newStandardElement = matchingReference?.ToElementSave<StandardElementSave>(
                projectRootDirectory, GumProjectSave.ComponentExtension, gumLoadResult);

            if (newStandardElement != null)
            {
                StandardElements.Remove(element as StandardElementSave);
                StandardElements.Add(newStandardElement);
            }
        }
    }

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
                    System.Threading.Thread.Sleep(msBetweenSaves);
                    numberOfTimesTried++;
                }
            }

            if (!succeeded)
            {
                throw exception;
            }

        }
    }


    public void ReactToRenamed(ElementSave element, InstanceSave instance, string oldName)
    {
        if (instance == null)
        {
            List<ElementReference>? listToSearch = null;

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

            if(listToSearch != null)
            {
                foreach (ElementReference reference in listToSearch)
                {
                    if (reference.Name == oldName)
                    {
                        reference.Name = element.Name;
                    }
                }
            }
        }
    }

    #endregion

}
