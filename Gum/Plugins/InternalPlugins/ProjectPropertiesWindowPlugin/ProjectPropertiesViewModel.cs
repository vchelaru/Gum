using Gum.DataTypes;
using Gum.Mvvm;
using Gum.Settings;
using Gum.Wireframe;
using System;
using System.Collections.Generic;
using Gum.ToolStates;
using ToolsUtilities;
using Color = System.Drawing.Color;
using Matrix = System.Numerics.Matrix4x4;

namespace Gum.Plugins.PropertiesWindowPlugin;

public enum TextureFilter
{
    Linear,
    Point,
}
public class ProjectPropertiesViewModel : ViewModel
{

    GeneralSettingsFile generalSettings;
    GumProjectSave gumProject;

    public bool AutoSave
    {
        get => Get<bool>(); 
        set => Set(value); 
    }

    public bool ShowOutlines
    {
        get => Get<bool>(); 
        set => Set(value); 
    }

    public TextureFilter TextureFilter
    {
        get => Get<TextureFilter>();
        set => Set(value);
    }

    public bool ShowCanvasOutline
    {
        get => Get<bool>();
        set => Set(value);
    }

    public bool ShowCheckerBackground
    {
        get => Get<bool>();
        set => Set(value);
    }

    public bool RestrictToUnitValues
    {
        get => Get<bool>();
        set => Set(value); 
    }

    public int CanvasWidth
    {
        get => Get<int>();
        set => Set(value); 
    }

    public int CanvasHeight
    {
        get => Get<int>();
        set => Set(value);
    }


    public bool RestrictFileNamesForAndroid
    {
        get => Get<bool>(); 
        set => Set(value); 
    }

    public bool RenderTextCharacterByCharacter
    {
        get => Get<bool>(); 
        set => Set(value); 
    }

    public List<string> LocalizationFiles
    {
        get
        {
            // Initialize lazily so mutations (e.g. list.Add(...)) on a freshly-constructed
            // VM persist. Returning a new list each call would let caller changes fall
            // on the floor.
            var list = Get<List<string>>();
            if (list == null)
            {
                list = new List<string>();
                SetWithoutNotifying(list);
            }
            return list;
        }
        set => Set(value);
    }

    public int LanguageIndex
    {
        get => Get<int>();
        set => Set(value);
    }

    public string LanguageName
    {
        get => Get<string>();
        set => Set(value);
    }

    /// <summary>
    /// Syncs LanguageName from the current LanguageIndex without triggering property-change side effects.
    /// </summary>
    public void UpdateLanguageNameFromIndex(IReadOnlyList<string> languages)
    {
        IsUpdatingFromModel = true;
        LanguageName = LanguageIndex > 0 && LanguageIndex <= languages.Count
            ? languages[LanguageIndex - 1]
            : string.Empty;
        IsUpdatingFromModel = false;
    }

    public bool ShowLocalization
    {
        get => Get<bool>();
        set => Set(value);
    }

    public string FontRanges
    {
        get => Get<string>();
        set => Set(value);
    }

    public int FontSpacingHorizontal
    {
        get => Get<int>();
        set => Set(value);
    }
    public int FontSpacingVertical
    {
        get => Get<int>();
        set => Set(value);
    }

    public bool AutoSizeFontOutputs
    {
        get => Get<bool>();
        set => Set(value);
    }

    public FontGeneratorType FontGenerator
    {
        get => Get<FontGeneratorType>();
        set => Set(value);
    }

    public string SinglePixelTextureFile
    {
        get => Get<string>();
        set => Set(value);
    }

    public int? SinglePixelTextureTop
    {
        get => Get<int?>();
        set => Set(value);
    }

    public int? SinglePixelTextureLeft
    {
        get => Get<int?>();
        set => Set(value);
    }

    public int? SinglePixelTextureRight
    {
        get => Get<int?>();
        set => Set(value);
    }

    public int? SinglePixelTextureBottom
    {
        get => Get<int?>();
        set => Set(value);
    }
    
    public bool UseFontCharacterFile
    {
        get => Get<bool>();
        set => Set(value);
    }


    public bool IsUpdatingFromModel { get; private set; }

    public void SetFrom(GeneralSettingsFile generalSettings, GumProjectSave gumProject)
    {
        IsUpdatingFromModel = true;

        {
            this.generalSettings = generalSettings;
            this.gumProject = gumProject;


            AutoSave = this.generalSettings.AutoSave;
            ShowOutlines = this.gumProject.ShowOutlines;
            try
            {
                TextureFilter =  (TextureFilter)Enum.Parse(typeof(TextureFilter),this.gumProject.TextureFilter);
            }
            catch
            {
                TextureFilter = TextureFilter.Point;
            }
            ShowCanvasOutline = this.gumProject.ShowCanvasOutline;
            ShowCheckerBackground = this.gumProject.ShowCheckerBackground;
            FontRanges = this.gumProject.FontRanges;
            FontSpacingHorizontal = this.gumProject.FontSpacingHorizontal;
            FontSpacingVertical = this.gumProject.FontSpacingVertical;
            UseFontCharacterFile = this.gumProject.UseFontCharacterFile;
            AutoSizeFontOutputs = this.gumProject.AutoSizeFontOutputs;
            FontGenerator = this.gumProject.FontGenerator;

            RestrictToUnitValues = this.gumProject.RestrictToUnitValues;
            CanvasHeight = this.gumProject.DefaultCanvasHeight;
            CanvasWidth = this.gumProject.DefaultCanvasWidth;
            RestrictFileNamesForAndroid = this.gumProject.RestrictFileNamesForAndroid;

            LocalizationFiles = new List<string>(this.gumProject.LocalizationFiles);
            LanguageIndex = this.gumProject.CurrentLanguageIndex;
            ShowLocalization = this.gumProject.ShowLocalizationInGum;

            SinglePixelTextureFile = gumProject.SinglePixelTextureFile;
            SinglePixelTextureLeft = gumProject.SinglePixelTextureLeft;
            SinglePixelTextureRight = gumProject.SinglePixelTextureRight;
            SinglePixelTextureTop = gumProject.SinglePixelTextureTop;
            SinglePixelTextureBottom = gumProject.SinglePixelTextureBottom;
        }

        IsUpdatingFromModel = false;

    }

    public void ApplyToModelObjects()
    {
        this.generalSettings.AutoSave = AutoSave;
        this.gumProject.ShowOutlines = ShowOutlines;
        this.gumProject.ShowCanvasOutline = ShowCanvasOutline;
        this.gumProject.ShowCheckerBackground = ShowCheckerBackground;
        GraphicalUiElement.ShowLineRectangles = ShowOutlines;
        GraphicalUiElement.CanvasWidth = CanvasWidth;
        GraphicalUiElement.CanvasHeight = CanvasHeight;


        this.gumProject.TextureFilter = TextureFilter.ToString();
        this.gumProject.RestrictToUnitValues = RestrictToUnitValues;
        this.gumProject.DefaultCanvasHeight = CanvasHeight;
        this.gumProject.DefaultCanvasWidth = CanvasWidth;
        this.gumProject.RestrictFileNamesForAndroid = RestrictFileNamesForAndroid;

        this.gumProject.LocalizationFiles = new List<string>(LocalizationFiles);
        this.gumProject.CurrentLanguageIndex = LanguageIndex;
        this.gumProject.ShowLocalizationInGum = ShowLocalization;
        this.gumProject.FontRanges = FontRanges;
        this.gumProject.UseFontCharacterFile = UseFontCharacterFile;

        this.gumProject.FontSpacingHorizontal = FontSpacingHorizontal;
        this.gumProject.FontSpacingVertical = FontSpacingVertical;
        this.gumProject.AutoSizeFontOutputs = AutoSizeFontOutputs;
        this.gumProject.FontGenerator = FontGenerator;

        this.gumProject.SinglePixelTextureFile = SinglePixelTextureFile;
        this.gumProject.SinglePixelTextureTop = SinglePixelTextureTop;
        this.gumProject.SinglePixelTextureBottom = SinglePixelTextureBottom;
        this.gumProject.SinglePixelTextureLeft = SinglePixelTextureLeft;
        this.gumProject.SinglePixelTextureRight = SinglePixelTextureRight;
    }


}
