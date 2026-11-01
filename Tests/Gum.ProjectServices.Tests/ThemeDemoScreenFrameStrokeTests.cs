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
public class ThemeDemoScreenFrameStrokeTests
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
