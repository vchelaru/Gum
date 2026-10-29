using Gum.DataTypes;
using Gum.ProjectServices.CodeGeneration;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CodeOutputPlugin;

/// <summary>
/// The read-only "Resolves to" text of the Code generation page: where a Screen and a Component land
/// for the current folders, so a folder or prefix edit shows its effect without generating. Paths are
/// relative to the Code Project Root, using <c>/</c>, except a custom folder given as an absolute path.
/// </summary>
public static class CodeLocationPreview
{
    private const int KindColumnWidth = 14;

    /// <summary>One line per file: the element kind, which half of the class it is, and its path.</summary>
    public static string Describe(CodeOutputProjectSettings settings)
    {
        if (string.IsNullOrEmpty(settings.CodeProjectRoot))
        {
            return "Set a Code Project Root to see where files go.";
        }

        List<string> lines = new List<string>();
        AddElement(lines, new ScreenSave { Name = "MyScreen" }, "MyScreen", settings);
        AddElement(lines, new ComponentSave { Name = "MyComponent" }, "MyComponent", settings);
        return string.Join("\n", lines);
    }

    private static void AddElement(List<string> lines, ElementSave element, string name, CodeOutputProjectSettings settings)
    {
        string subfolder = CodeGenerationFileLocationsService.GetElementCodeSubfolder(element, settings);
        string kind = element is ScreenSave ? "Screens →" : "Components →";

        // The kind is named on the first line only; both halves of the class share it.
        lines.Add($"{kind.PadRight(KindColumnWidth)}{Join(settings.GeneratedCodeFolder, subfolder, name + ".Generated.cs")}");
        string customFolder = string.IsNullOrEmpty(settings.CustomCodeFolder) ? settings.GeneratedCodeFolder : settings.CustomCodeFolder;
        lines.Add($"{new string(' ', KindColumnWidth)}{Join(customFolder, subfolder, name + ".cs")}");
    }

    private static string Join(params string[] parts)
    {
        string joined = string.Join("/", parts
            .Select(part => part.Replace('\\', '/').Trim('/'))
            .Where(part => part.Length > 0));

        // An absolute folder keeps its leading slash or drive.
        string first = parts[0].Replace('\\', '/');
        if (first.StartsWith("/") && !joined.StartsWith("/"))
        {
            joined = "/" + joined;
        }
        return joined;
    }
}
