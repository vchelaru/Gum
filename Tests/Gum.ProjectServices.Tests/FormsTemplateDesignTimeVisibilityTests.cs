using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Shouldly;

namespace Gum.ProjectServices.Tests;

/// <summary>
/// The Forms controls show the caret, selection and scroll bar thumb only while they are
/// needed, by setting Visible at runtime. The Default state is what the tool canvas draws,
/// so a Default state that shows them paints stray bars on every TextBox and ListBox (#7376).
/// </summary>
public class FormsTemplateDesignTimeVisibilityTests
{
    public static IEnumerable<object[]> TemplateRoots()
    {
        string templates = Path.Combine(FindRepoRoot(), "Tools", "Gum.ProjectServices", "Templates");
        yield return new object[] { "FormsTemplate" };
        foreach (string dir in Directory.GetDirectories(Path.Combine(templates, "FormsThemes")))
        {
            yield return new object[] { Path.Combine("FormsThemes", Path.GetFileName(dir)) };
        }
    }

    [Theory]
    [MemberData(nameof(TemplateRoots))]
    public void DefaultState_TextBoxAndPasswordBox_HidesCaretAndSelection(string template)
    {
        foreach (string file in FindComponents(template, "TextBox.gucx", "PasswordBox.gucx"))
        {
            DefaultStateVisible(file, "CaretInstance").ShouldBe(false, file);
            DefaultStateVisible(file, "SelectionInstance").ShouldBe(false, file);
        }
    }

    [Theory]
    [MemberData(nameof(TemplateRoots))]
    public void DefaultState_ListBoxAndItemsControl_HidesScrollBar(string template)
    {
        foreach (string file in FindComponents(template, "ListBox.gucx", "ItemsControl.gucx"))
        {
            DefaultStateVisible(file, "VerticalScrollBarInstance").ShouldBe(false, file);
        }
    }

    private static List<string> FindComponents(string template, params string[] names)
    {
        string root = Path.Combine(FindRepoRoot(), "Tools", "Gum.ProjectServices", "Templates", template, "Components");
        List<string> files = Directory.GetFiles(root, "*.gucx", SearchOption.AllDirectories)
            .Where(f => names.Contains(Path.GetFileName(f)))
            .ToList();
        files.ShouldNotBeEmpty();
        return files;
    }

    private static bool? DefaultStateVisible(string file, string instance)
    {
        XElement defaultState = XDocument.Load(file).Root!
            .Elements("State").First(s => s.Element("Name")?.Value == "Default");
        XElement? variable = defaultState.Elements("Variable")
            .FirstOrDefault(v => (string?)v.Attribute("Name") == instance + ".Visible");
        return variable == null ? null : bool.Parse(variable.Element("Value")!.Value);
    }

    private static string FindRepoRoot()
    {
        string current = AppContext.BaseDirectory;
        while (!Directory.Exists(Path.Combine(current, "Tools", "Gum.ProjectServices", "Templates")))
        {
            current = Path.GetDirectoryName(current) ?? throw new InvalidOperationException("Repo root not found");
        }
        return current;
    }
}
