using Gum.DataTypes;
using Gum.Mvvm;
using Gum.Settings;
using Gum.Wireframe;
using System;
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

    public Color OutlineColor
    {
        get => Get<Color>();
        set => Set(value);
    }

    public Color GuideLineColor
    {
        get => Get<Color>();
        set => Set(value);
    }

    public Color GuideTextColor
    {
        get => Get<Color>();
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

    public decimal DisplayDensity
    {
        get => Get<decimal>();
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

    public Color CheckerboardColor1
    {
        get => Get<Color>();
        set => Set(value);
    }

    public Color CheckerboardColor2
    {
        get => Get<Color>();
        set => Set(value);
    }

    public string LocalizationFile
    {
        get => Get<string>();
        set => Set(value);
    }

    public int LanguageIndex
    {
        get => Get<int>();
        set => Set(value);
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

    public ProjectPropertiesViewModel()
    {
        OutlineColor = Color.White;
    }

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
            FontRanges = this.gumProject.FontRanges;
            FontSpacingHorizontal = this.gumProject.FontSpacingHorizontal;
            FontSpacingVertical = this.gumProject.FontSpacingVertical;
            UseFontCharacterFile = this.gumProject.UseFontCharacterFile;

            RestrictToUnitValues = this.gumProject.RestrictToUnitValues;
            CanvasHeight = this.gumProject.DefaultCanvasHeight;
            CanvasWidth = this.gumProject.DefaultCanvasWidth;
            RestrictFileNamesForAndroid = this.gumProject.RestrictFileNamesForAndroid;


            CheckerboardColor1 = Color.FromArgb(255, generalSettings.CheckerColor1R, generalSettings.CheckerColor1G, generalSettings.CheckerColor1B);
            CheckerboardColor2 = Color.FromArgb(255, generalSettings.CheckerColor2R, generalSettings.CheckerColor2G, generalSettings.CheckerColor2B);

            OutlineColor = Color.FromArgb(255, generalSettings.OutlineColorR, generalSettings.OutlineColorG, generalSettings.OutlineColorB);
            GuideLineColor = Color.FromArgb(255, generalSettings.GuideLineColorR, generalSettings.GuideLineColorG, generalSettings.GuideLineColorB);
            GuideTextColor = Color.FromArgb(255, generalSettings.GuideTextColorR, generalSettings.GuideTextColorG, generalSettings.GuideTextColorB);

            LocalizationFile = this.gumProject.LocalizationFile;
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
        GraphicalUiElement.ShowLineRectangles = ShowOutlines;
        GraphicalUiElement.CanvasWidth = CanvasWidth;
        GraphicalUiElement.CanvasHeight = CanvasHeight;


        this.gumProject.TextureFilter = TextureFilter.ToString();
        this.gumProject.RestrictToUnitValues = RestrictToUnitValues;
        this.gumProject.DefaultCanvasHeight = CanvasHeight;
        this.gumProject.DefaultCanvasWidth = CanvasWidth;
        this.gumProject.RestrictFileNamesForAndroid = RestrictFileNamesForAndroid;

        generalSettings.CheckerColor1R = CheckerboardColor1.R;
        generalSettings.CheckerColor1G = CheckerboardColor1.G;
        generalSettings.CheckerColor1B = CheckerboardColor1.B;

        generalSettings.CheckerColor2R = CheckerboardColor2.R;
        generalSettings.CheckerColor2G = CheckerboardColor2.G;
        generalSettings.CheckerColor2B = CheckerboardColor2.B;

        generalSettings.OutlineColorR = OutlineColor.R;
        generalSettings.OutlineColorG = OutlineColor.G;
        generalSettings.OutlineColorB = OutlineColor.B;

        generalSettings.GuideLineColorR = GuideLineColor.R;
        generalSettings.GuideLineColorG = GuideLineColor.G;
        generalSettings.GuideLineColorB = GuideLineColor.B;

        generalSettings.GuideTextColorR = GuideTextColor.R;
        generalSettings.GuideTextColorG = GuideTextColor.G;
        generalSettings.GuideTextColorB = GuideTextColor.B;

        this.gumProject.LocalizationFile = LocalizationFile;
        this.gumProject.CurrentLanguageIndex = LanguageIndex;
        this.gumProject.ShowLocalizationInGum = ShowLocalization;
        this.gumProject.FontRanges = FontRanges;
        this.gumProject.UseFontCharacterFile = UseFontCharacterFile;

        this.gumProject.FontSpacingHorizontal = FontSpacingHorizontal;
        this.gumProject.FontSpacingVertical = FontSpacingVertical;

        this.gumProject.SinglePixelTextureFile = SinglePixelTextureFile;
        this.gumProject.SinglePixelTextureTop = SinglePixelTextureTop;
        this.gumProject.SinglePixelTextureBottom = SinglePixelTextureBottom;
        this.gumProject.SinglePixelTextureLeft = SinglePixelTextureLeft;
        this.gumProject.SinglePixelTextureRight = SinglePixelTextureRight;
    }


}
