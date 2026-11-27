using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Gum.DataTypes;
using RenderingLibrary.Graphics.Fonts;
using ToolsUtilities;

namespace Gum.ProjectServices.Editing;

/// <summary>
/// One project property <c>setProjectProperty</c> sets: its JSON value type, the values it allows when that
/// is a fixed set, and a short description.
/// </summary>
public record ProjectPropertySchema(string Name, string TypeName, IReadOnlyList<string> AllowedValues, string Description);

/// <summary>
/// The Project Properties a game or <c>gumcli fonts</c> reads that <c>setProjectProperty</c> sets, declared once
/// here so <c>edit --schema</c> and the validation cannot disagree. Values are checked the way the tool's
/// Project Properties and Fonts pages check them (font ranges, file paths relative to the project). Editor-only
/// view settings (outlines, checkerboard, grid) are left out.
/// </summary>
internal static class ProjectPropertySetter
{
    private sealed record Property(string Type, string[] Allowed, string Description, Action<GumProjectSave, JsonElement> Apply);

    private static readonly string[] None = Array.Empty<string>();

    private static readonly Dictionary<string, Property> Properties = new()
    {
        [nameof(GumProjectSave.DefaultCanvasWidth)] = new("int", None, "positive whole number, pixels",
            (project, value) => project.DefaultCanvasWidth = GetPositiveInt(value)),
        [nameof(GumProjectSave.DefaultCanvasHeight)] = new("int", None, "positive whole number, pixels",
            (project, value) => project.DefaultCanvasHeight = GetPositiveInt(value)),
        [nameof(GumProjectSave.TextureFilter)] = new("string", new[] { "Point", "Linear" }, "how textures are sampled",
            (project, value) => project.TextureFilter = GetChoice(value, "TextureFilter", "Point", "Linear")),
        [nameof(GumProjectSave.RestrictToUnitValues)] = new("bool", None, "limit unit values to the ones the runtime supports",
            (project, value) => project.RestrictToUnitValues = GetBool(value)),
        [nameof(GumProjectSave.RestrictFileNamesForAndroid)] = new("bool", None, "require file names Android accepts",
            (project, value) => project.RestrictFileNamesForAndroid = GetBool(value)),
        [nameof(GumProjectSave.EngineProfile)] = new("string", None, "path of the .gumengine profile, empty for none; stored relative to the project",
            (project, value) => project.EngineProfile = ProjectRelativePaths.ToProjectRelativeReference(GetString(value), ProjectDirectory(project))),
        [nameof(GumProjectSave.LocalizationFiles)] = new("string[]", None, "CSV or RESX files, replaces the list; stored relative to the project",
            (project, value) => project.LocalizationFiles = GetStringArray(value)
                .Select(path => ToStoredPath(path, project)).ToList()),
        [nameof(GumProjectSave.CurrentLanguageIndex)] = new("int", None, "whole number from 0, the language column to start in",
            (project, value) => project.CurrentLanguageIndex = GetNonNegativeInt(value)),
        [nameof(GumProjectSave.ShowLocalizationInGum)] = new("bool", None, "show translated text in the tool",
            (project, value) => project.ShowLocalizationInGum = GetBool(value)),
        [nameof(GumProjectSave.FontRanges)] = new("string", None, "project-wide character ranges such as \"32-126,160-255\"",
            (project, value) => project.FontRanges = GetFontRanges(value)),
        [nameof(GumProjectSave.FontGenerator)] = new("string", Enum.GetNames(typeof(FontGeneratorType)), "program that bakes the font atlases",
            (project, value) => project.FontGenerator = Enum.Parse<FontGeneratorType>(
                GetChoice(value, "FontGenerator", Enum.GetNames(typeof(FontGeneratorType))))),
        [nameof(GumProjectSave.FontSpacingHorizontal)] = new("int", None, "whole number from 0, pixels between glyphs",
            (project, value) => project.FontSpacingHorizontal = GetNonNegativeInt(value)),
        [nameof(GumProjectSave.FontSpacingVertical)] = new("int", None, "whole number from 0, pixels between glyphs",
            (project, value) => project.FontSpacingVertical = GetNonNegativeInt(value)),
        [nameof(GumProjectSave.AutoSizeFontOutputs)] = new("bool", None, "fewer PNGs, slower font generation",
            (project, value) => project.AutoSizeFontOutputs = GetBool(value)),
        [nameof(GumProjectSave.SinglePixelTextureFile)] = new("string", None, "image for the tool's canvas solid shapes, empty for none; stored relative to the project",
            (project, value) => project.SinglePixelTextureFile = ToStoredPath(GetString(value), project)),
        [nameof(GumProjectSave.SinglePixelTextureLeft)] = new("int", None, "whole number from 0, or null for the whole image",
            (project, value) => project.SinglePixelTextureLeft = GetOptionalNonNegativeInt(value)),
        [nameof(GumProjectSave.SinglePixelTextureTop)] = new("int", None, "whole number from 0, or null for the whole image",
            (project, value) => project.SinglePixelTextureTop = GetOptionalNonNegativeInt(value)),
        [nameof(GumProjectSave.SinglePixelTextureRight)] = new("int", None, "whole number from 0, or null for the whole image",
            (project, value) => project.SinglePixelTextureRight = GetOptionalNonNegativeInt(value)),
        [nameof(GumProjectSave.SinglePixelTextureBottom)] = new("int", None, "whole number from 0, or null for the whole image",
            (project, value) => project.SinglePixelTextureBottom = GetOptionalNonNegativeInt(value)),
    };

    // Real project properties that are refused on purpose, with what to do instead.
    private static readonly Dictionary<string, string> Refused = new()
    {
        [nameof(GumProjectSave.UseFontCharacterFile)] =
            "it reads the project's .gumfcs file in the tool. Set FontRanges to the ranges you need instead.",
        [nameof(GumProjectSave.FontRangeEntries)] =
            "per-font ranges are edited on the tool's Fonts page. Set FontRanges for the project-wide ranges.",
    };

    /// <summary>Every property, in the order <c>edit --schema</c> lists them.</summary>
    public static IReadOnlyList<ProjectPropertySchema> GetSchemas() =>
        Properties.Select(pair => new ProjectPropertySchema(pair.Key, pair.Value.Type, pair.Value.Allowed, pair.Value.Description)).ToList();

    /// <summary>Sets <paramref name="name"/> on <paramref name="project"/>, or throws an <see cref="EditException"/> saying what to change.</summary>
    public static void Set(GumProjectSave project, string name, JsonElement value)
    {
        if (Refused.TryGetValue(name, out string? why))
        {
            throw new EditException($"{name} is not set by setProjectProperty: {why}");
        }
        if (!Properties.TryGetValue(name, out Property? property))
        {
            throw new EditException($"{name} is not a project property setProjectProperty sets. It sets: {string.Join(", ", Properties.Keys)}.");
        }
        try
        {
            property.Apply(project, value);
        }
        catch (EditException ex)
        {
            throw new EditException($"{name}: {ex.Message}");
        }
    }

    private static string ToStoredPath(string path, GumProjectSave project) =>
        ProjectRelativePaths.ToProjectRelative(path, ProjectDirectory(project));

    private static string? ProjectDirectory(GumProjectSave project) =>
        string.IsNullOrEmpty(project.FullFileName) ? null : FileManager.GetDirectory(project.FullFileName);

    private static int GetPositiveInt(JsonElement value) =>
        value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out int number) && number > 0
            ? number
            : throw new EditException($"{value.GetRawText()} is not a positive whole number.");

    private static int GetNonNegativeInt(JsonElement value) =>
        value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out int number) && number >= 0
            ? number
            : throw new EditException($"{value.GetRawText()} is not a whole number from 0.");

    private static int? GetOptionalNonNegativeInt(JsonElement value) =>
        value.ValueKind == JsonValueKind.Null ? null : GetNonNegativeInt(value);

    private static bool GetBool(JsonElement value) =>
        value.ValueKind is JsonValueKind.True or JsonValueKind.False
            ? value.GetBoolean()
            : throw new EditException($"{value.GetRawText()} is not true or false.");

    private static string GetString(JsonElement value) =>
        value.ValueKind == JsonValueKind.String
            ? value.GetString()!
            : throw new EditException($"{value.GetRawText()} is not a string.");

    private static string GetChoice(JsonElement value, string propertyName, params string[] allowed) =>
        value.ValueKind == JsonValueKind.String && allowed.Contains(value.GetString())
            ? value.GetString()!
            : throw new EditException($"{value.GetRawText()} is not a {propertyName}: use {string.Join(", ", allowed.Select(item => $"\"{item}\""))}.");

    private static List<string> GetStringArray(JsonElement value)
    {
        List<string> items = value.ValueKind == JsonValueKind.Array && value.EnumerateArray().All(item => item.ValueKind == JsonValueKind.String)
            ? value.EnumerateArray().Select(item => item.GetString()!).ToList()
            : throw new EditException($"{value.GetRawText()} is not an array of strings.");
        return items.All(item => item.Length > 0) ? items : throw new EditException("an entry is empty: list file paths, or use [] to clear the list.");
    }

    // The Fonts page fixes a range that only has stray spaces and keeps anything else invalid as typed; an edit
    // refuses the invalid range instead, since nobody sees the Errors tab before the fonts bake.
    private static string GetFontRanges(JsonElement value)
    {
        string ranges = BmfcSave.TryFixRange(GetString(value));
        return BmfcSave.GetIfIsValidRange(ranges)
            ? ranges
            : throw new EditException($"{value.GetRawText()} is not a valid range: use comma-separated codepoints or start-end pairs such as \"32-126,160-255\".");
    }
}
