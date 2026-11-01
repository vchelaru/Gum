using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Shouldly;

namespace Gum.ProjectServices.Tests;

/// <summary>
/// The two framing rectangles on every theme's DemoScreenGum (Background and Background1) must
/// use the same stroke so the screen reads as one design (#7423).
/// </summary>
public class ThemeDemoScreenFramesMatchTests
{
    private static readonly string[] StrokeProperties =
        { "StrokeRed", "StrokeGreen", "StrokeBlue", "StrokeWidth" };

    public static IEnumerable<object[]> DemoScreens()
    {
        string templates = Path.Combine(FindRepoRoot(), "Tools", "Gum.ProjectServices", "Templates");
        return Directory.GetFiles(templates, "DemoScreenGum.gusx", SearchOption.AllDirectories)
            .Select(path => new object[] { Path.GetRelativePath(templates, path) });
    }

    [Theory]
    [MemberData(nameof(DemoScreens))]
    public void DemoScreen_BothFrames_ShouldUseSameStroke(string relativePath)
    {
        string path = Path.Combine(FindRepoRoot(), "Tools", "Gum.ProjectServices", "Templates", relativePath);
        Dictionary<string, string> values = XDocument.Load(path)
            .Descendants("Variable")
            .Select(v => (Name: v.Element("Name")?.Value ?? (string?)v.Attribute("Name") ?? "", Value: v.Element("Value")?.Value ?? ""))
            .ToDictionary(x => x.Name, x => x.Value);

        foreach (string property in StrokeProperties)
        {
            values.TryGetValue("Background." + property, out string? first);
            values.TryGetValue("Background1." + property, out string? second);
            second.ShouldBe(first, $"{relativePath}: {property} differs between Background and Background1");
        }
    }

    [Theory]
    [MemberData(nameof(DemoScreens))]
    public void DemoScreen_BothFrames_ShouldUseSameCornerRadiusAndFill(string relativePath)
    {
        Dictionary<string, string> values = LoadValues(relativePath);

        foreach (string property in new[] { "CornerRadius", "FillRed", "FillGreen", "FillBlue", "IsFilled" })
        {
            values.TryGetValue("Background." + property, out string? first);
            values.TryGetValue("Background1." + property, out string? second);
            second.ShouldBe(first, $"{relativePath}: {property} differs between Background and Background1");
        }
    }

    [Theory]
    [MemberData(nameof(DemoScreens))]
    public void DemoScreen_BothFrames_ShouldReferenceSameStrokeAndFillStyles(string relativePath)
    {
        // The VariableReferences override the literal color values, so they decide what is drawn.
        string path = Path.Combine(FindRepoRoot(), "Tools", "Gum.ProjectServices", "Templates", relativePath);
        XDocument doc = XDocument.Load(path);

        string? Reference(string instance, string prefix) => doc.Descendants("VariableList")
            .Where(l => l.Element("Name")?.Value == instance + ".VariableReferences")
            .SelectMany(l => l.Descendants("string"))
            .Select(s => s.Value)
            .FirstOrDefault(s => s.StartsWith(prefix));

        foreach (string prefix in new[] { "StrokeColor", "FillColor" })
        {
            Reference("Background1", prefix).ShouldBe(Reference("Background", prefix),
                $"{relativePath}: {prefix} reference differs between Background and Background1");
        }
    }

    private static Dictionary<string, string> LoadValues(string relativePath)
    {
        string path = Path.Combine(FindRepoRoot(), "Tools", "Gum.ProjectServices", "Templates", relativePath);
        return XDocument.Load(path)
            .Descendants("Variable")
            .Select(v => (Name: v.Element("Name")?.Value ?? (string?)v.Attribute("Name") ?? "", Value: v.Element("Value")?.Value ?? ""))
            .ToDictionary(x => x.Name, x => x.Value);
    }

    private static string FindRepoRoot()
    {
        string current = AppContext.BaseDirectory;
        for (int i = 0; i < 12; i++)
        {
            if (Directory.Exists(Path.Combine(current, "Tools", "Gum.ProjectServices", "Templates")))
            {
                return current;
            }
            current = Path.GetDirectoryName(current)!;
        }
        throw new InvalidOperationException("Repo root not found");
    }
}
