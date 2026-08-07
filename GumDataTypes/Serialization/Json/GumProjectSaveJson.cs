using System.Collections.Generic;
using System.Linq;

namespace Gum.DataTypes.Serialization.Json;

/// <summary>
/// JSON-serializable shape of <see cref="GumProjectSave"/>. Mirrors exactly the surface the XML
/// serializer covers today (every non-<c>[XmlIgnore]</c> property) — <see cref="GumProjectSave.Screens"/>,
/// <see cref="GumProjectSave.Components"/>, <see cref="GumProjectSave.StandardElements"/>,
/// <see cref="GumProjectSave.Behaviors"/>, and <see cref="GumProjectSave.ElementAnimations"/> are all
/// populated later from the reference lists below, not part of this DTO. The legacy
/// <c>LocalizationFile</c>/<c>LocalizationFilesArray</c> XML back-compat shim has no JSON equivalent —
/// <see cref="GumProjectSave.LocalizationFiles"/> serializes as a plain list.
/// </summary>
internal sealed class GumProjectSaveJson
{
    public string FontRanges { get; set; } = "";
    public int FontSpacingVertical { get; set; }
    public int FontSpacingHorizontal { get; set; }
    public bool UseFontCharacterFile { get; set; }
    public bool AutoSizeFontOutputs { get; set; }
    public FontGeneratorType FontGenerator { get; set; }
    public int Version { get; set; }
    public int DefaultCanvasWidth { get; set; }
    public int DefaultCanvasHeight { get; set; }
    public List<CustomCanvasSize>? CustomCanvasSizes { get; set; }
    public bool ShowOutlines { get; set; }
    public bool ShowCanvasOutline { get; set; }
    public bool ShowRuler { get; set; }
    public bool ShowCheckerBackground { get; set; }
    public string TextureFilter { get; set; } = "";
    public bool ConvertVariablesOnUnitTypeChange { get; set; }
    public bool RestrictToUnitValues { get; set; }
    public bool ShowGrid { get; set; }
    public bool SnapToGrid { get; set; }
    public int GridSize { get; set; }
    public bool RestrictFileNamesForAndroid { get; set; }
    public List<GuideRectangle> Guides { get; set; } = new List<GuideRectangle>();
    public List<string> FavoriteComponents { get; set; } = new List<string>();
    public string ParentProjectRoot { get; set; } = "";
    public List<string> LocalizationFiles { get; set; } = new List<string>();
    public bool ShowLocalizationInGum { get; set; }
    public int CurrentLanguageIndex { get; set; }
    public List<ElementReferenceJson> ScreenReferences { get; set; } = new List<ElementReferenceJson>();
    public List<ElementReferenceJson> ComponentReferences { get; set; } = new List<ElementReferenceJson>();
    public List<ElementReferenceJson> StandardElementReferences { get; set; } = new List<ElementReferenceJson>();
    public List<BehaviorReferenceJson> BehaviorReferences { get; set; } = new List<BehaviorReferenceJson>();
    public string SinglePixelTextureFile { get; set; } = "";
    public int? SinglePixelTextureTop { get; set; }
    public int? SinglePixelTextureLeft { get; set; }
    public int? SinglePixelTextureRight { get; set; }
    public int? SinglePixelTextureBottom { get; set; }
    public List<CustomPropertySaveJson> CustomProperties { get; set; } = new List<CustomPropertySaveJson>();
}

internal static class GumProjectSaveJsonMapper
{
    public static GumProjectSaveJson ToJson(GumProjectSave source)
    {
        return new GumProjectSaveJson
        {
            FontRanges = source.FontRanges,
            FontSpacingVertical = source.FontSpacingVertical,
            FontSpacingHorizontal = source.FontSpacingHorizontal,
            UseFontCharacterFile = source.UseFontCharacterFile,
            AutoSizeFontOutputs = source.AutoSizeFontOutputs,
            FontGenerator = source.FontGenerator,
            Version = source.Version,
            DefaultCanvasWidth = source.DefaultCanvasWidth,
            DefaultCanvasHeight = source.DefaultCanvasHeight,
            CustomCanvasSizes = source.CustomCanvasSizes,
            ShowOutlines = source.ShowOutlines,
            ShowCanvasOutline = source.ShowCanvasOutline,
            ShowRuler = source.ShowRuler,
            ShowCheckerBackground = source.ShowCheckerBackground,
            TextureFilter = source.TextureFilter,
            ConvertVariablesOnUnitTypeChange = source.ConvertVariablesOnUnitTypeChange,
            RestrictToUnitValues = source.RestrictToUnitValues,
            ShowGrid = source.ShowGrid,
            SnapToGrid = source.SnapToGrid,
            GridSize = source.GridSize,
            RestrictFileNamesForAndroid = source.RestrictFileNamesForAndroid,
            Guides = new List<GuideRectangle>(source.Guides),
            FavoriteComponents = new List<string>(source.FavoriteComponents),
            ParentProjectRoot = source.ParentProjectRoot,
            LocalizationFiles = new List<string>(source.LocalizationFiles),
            ShowLocalizationInGum = source.ShowLocalizationInGum,
            CurrentLanguageIndex = source.CurrentLanguageIndex,
            ScreenReferences = source.ScreenReferences.Select(ElementReferenceJsonMapper.ToJson).ToList(),
            ComponentReferences = source.ComponentReferences.Select(ElementReferenceJsonMapper.ToJson).ToList(),
            StandardElementReferences = source.StandardElementReferences.Select(ElementReferenceJsonMapper.ToJson).ToList(),
            BehaviorReferences = source.BehaviorReferences.Select(BehaviorReferenceJsonMapper.ToJson).ToList(),
            SinglePixelTextureFile = source.SinglePixelTextureFile,
            SinglePixelTextureTop = source.SinglePixelTextureTop,
            SinglePixelTextureLeft = source.SinglePixelTextureLeft,
            SinglePixelTextureRight = source.SinglePixelTextureRight,
            SinglePixelTextureBottom = source.SinglePixelTextureBottom,
            CustomProperties = source.CustomProperties.Select(CustomPropertySaveJsonMapper.ToJson).ToList(),
        };
    }

    public static GumProjectSave FromJson(GumProjectSaveJson dto)
    {
        GumProjectSave result = new GumProjectSave
        {
            FontRanges = dto.FontRanges,
            FontSpacingVertical = dto.FontSpacingVertical,
            FontSpacingHorizontal = dto.FontSpacingHorizontal,
            UseFontCharacterFile = dto.UseFontCharacterFile,
            AutoSizeFontOutputs = dto.AutoSizeFontOutputs,
            FontGenerator = dto.FontGenerator,
            Version = dto.Version,
            DefaultCanvasWidth = dto.DefaultCanvasWidth,
            DefaultCanvasHeight = dto.DefaultCanvasHeight,
            CustomCanvasSizes = dto.CustomCanvasSizes,
            ShowOutlines = dto.ShowOutlines,
            ShowCanvasOutline = dto.ShowCanvasOutline,
            ShowRuler = dto.ShowRuler,
            ShowCheckerBackground = dto.ShowCheckerBackground,
            TextureFilter = dto.TextureFilter,
            ConvertVariablesOnUnitTypeChange = dto.ConvertVariablesOnUnitTypeChange,
            RestrictToUnitValues = dto.RestrictToUnitValues,
            ShowGrid = dto.ShowGrid,
            SnapToGrid = dto.SnapToGrid,
            GridSize = dto.GridSize,
            RestrictFileNamesForAndroid = dto.RestrictFileNamesForAndroid,
            Guides = new List<GuideRectangle>(dto.Guides),
            FavoriteComponents = new List<string>(dto.FavoriteComponents),
            ParentProjectRoot = dto.ParentProjectRoot,
            ShowLocalizationInGum = dto.ShowLocalizationInGum,
            CurrentLanguageIndex = dto.CurrentLanguageIndex,
            SinglePixelTextureFile = dto.SinglePixelTextureFile,
            SinglePixelTextureTop = dto.SinglePixelTextureTop,
            SinglePixelTextureLeft = dto.SinglePixelTextureLeft,
            SinglePixelTextureRight = dto.SinglePixelTextureRight,
            SinglePixelTextureBottom = dto.SinglePixelTextureBottom,
        };

        result.LocalizationFiles = new List<string>(dto.LocalizationFiles);
        result.ScreenReferences = dto.ScreenReferences.Select(ElementReferenceJsonMapper.FromJson).ToList();
        result.ComponentReferences = dto.ComponentReferences.Select(ElementReferenceJsonMapper.FromJson).ToList();
        result.StandardElementReferences = dto.StandardElementReferences.Select(ElementReferenceJsonMapper.FromJson).ToList();
        result.BehaviorReferences = dto.BehaviorReferences.Select(BehaviorReferenceJsonMapper.FromJson).ToList();
        result.CustomProperties = dto.CustomProperties.Select(CustomPropertySaveJsonMapper.FromJson).ToList();

        return result;
    }
}
