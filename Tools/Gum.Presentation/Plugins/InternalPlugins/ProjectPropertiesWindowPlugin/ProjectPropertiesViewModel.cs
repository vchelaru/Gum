using Gum.DataTypes;
using Gum.Mvvm;
using Gum.Wireframe;
using System;
using System.Collections.Generic;
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


    /// <summary>
    /// True while the font ranges come from the project's .gumfcs file, so they cannot be typed in.
    /// View state, not a project setting: <see cref="ProjectPropertiesChangeLogic"/> ignores it.
    /// </summary>
    [DependsOn(nameof(UseFontCharacterFile))]
    public bool IsFontRangesReadOnly => UseFontCharacterFile;

    /// <summary>
    /// The languages the loaded localization files define, for the Language choice; empty without
    /// localization. Set by the plugin; view state, not a project setting.
    /// </summary>
    public IReadOnlyList<string> AvailableLanguages
    {
        get => Get<IReadOnlyList<string>>() ?? Array.Empty<string>();
        set => Set(value);
    }

    /// <summary>
    /// Raised by <see cref="NotifyReloaded"/> after this view model is refilled from the project, so
    /// a view that builds its fields from the members (the WPF property grid) can rebuild them.
    /// </summary>
    public event Action? Reloaded;

    /// <summary>Raised when the user closes the Project Properties tab from its own Close button.</summary>
    public event Action? CloseRequested;

    /// <summary>Tells views the view model was refilled from the project. Called by the plugin.</summary>
    public void NotifyReloaded() => Reloaded?.Invoke();

    /// <summary>Asks the plugin to close the tab.</summary>
    public void RequestClose() => CloseRequested?.Invoke();

    public bool IsUpdatingFromModel { get; private set; }

    public void SetFrom(bool autoSave, GumProjectSave gumProject)
    {
        IsUpdatingFromModel = true;

        {
            this.gumProject = gumProject;


            AutoSave = autoSave;
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
