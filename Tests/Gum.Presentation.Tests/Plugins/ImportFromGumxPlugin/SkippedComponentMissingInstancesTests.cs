using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Logic.FileWatch;
using Gum.Managers;
using Gum.Plugins.ImportPlugin.Manager;
using Gum.Plugins.ImportPlugin.Services;
using Gum.ProjectServices;
using Gum.ToolStates;
using GumRuntime;
using ImportFromGumxPlugin.Services;
using ImportFromGumxPlugin.ViewModels;
using Moq;
using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using ToolsUtilities;

namespace Gum.Presentation.Tests.Plugins.ImportFromGumxPlugin;

/// <summary>
/// Re-importing a Forms control into an older project whose Styles the user keeps (Skip) must not leave
/// the control reading swatches that Styles lacks (#6755). Runs the real Standard theme content through
/// the real <see cref="GumxImportService"/>.
/// </summary>
public class SkippedComponentMissingInstancesTests : BaseTestClass
{
    private const string WarningUserRed = "7";

    private readonly string _projectDir = Path.Combine(Path.GetTempPath(), $"Gum6755_{Guid.NewGuid():N}");
    private readonly string _templateDir = Path.Combine(FindRepoRoot(), "Tools", "Gum.ProjectServices", "Templates", "FormsTemplate");
    private readonly List<ComponentSave> _registered = new();

    public override void Dispose()
    {
        if (Directory.Exists(_projectDir))
        {
            Directory.Delete(_projectDir, recursive: true);
        }
        base.Dispose();
    }

    [Fact]
    public async Task SkippedStyles_GainsTheSwatchesTheImportedListBoxReads_AndKeepsTheUsersOwn()
    {
        GumProjectSave theme = (await new GumxSourceService().LoadProjectAsync(Path.Combine(_templateDir, "GumProject.gumx"))).ShouldNotBeNull();
        ComponentSave themeStyles = theme.Components.Single(c => c.Name == "Styles");
        ComponentSave themeListBox = theme.Components.Single(c => c.Name == "Controls/ListBox");
        List<string> listBoxSwatches = ReadSwatchNames(themeListBox);
        listBoxSwatches.ShouldContain("DarkGray");
        listBoxSwatches.ShouldContain("Warning");

        // The older project: its Styles lacks every swatch the ListBox reads except Warning, which the user recolored.
        string stylesPath = Path.Combine(_projectDir, "Components", "Styles.gucx");
        WriteOlderStyles(stylesPath, listBoxSwatches.Where(s => s != "Warning").ToList());
        ImportResult result = await Import(theme, themeStyles, themeListBox);

        result.ConflictingElements.ShouldBeEmpty();
        ComponentSave styles = Load(stylesPath);
        foreach (string swatch in listBoxSwatches)
        {
            styles.Instances.ShouldContain(i => i.Name == swatch, swatch);
        }
        styles.DefaultState!.GetValue("DarkGray.FillRed").ShouldBe(themeStyles.DefaultState!.GetValue("DarkGray.FillRed"));
        styles.DefaultState!.GetValue("DarkGray.Parent").ShouldBe("Colors");
        styles.DefaultState!.GetValue("Warning.FillRed").ShouldBe(int.Parse(WarningUserRed));
        styles.DefaultState!.Variables.Count(v => v.Name == "Warning.FillRed").ShouldBe(1);

        // Recoloring a restored swatch recolors the control that reads it.
        // The theme stands in for the rest of the project, with the imported Styles and ListBox in it.
        GumProjectSave project = theme;
        ComponentSave listBox = _registered.Single(c => c.Name == "Controls/ListBox");
        project.Components.Remove(themeStyles);
        project.Components.Remove(themeListBox);
        project.Components.Add(styles);
        project.Components.Add(listBox);
        styles.Initialize(StandardElementsManager.Self.GetDefaultStateFor("Component"));
        listBox.Initialize(StandardElementsManager.Self.GetDefaultStateFor("Component"));
        ObjectFinder.Self.GumProjectSave = project;
        styles.DefaultState!.SetValue("DarkGray.FillRed", 201, "int");
        project.ApplyAllVariableReferences();

        listBox.DefaultState!.GetValue("Background.Red").ShouldBe(201);
    }

    [Fact]
    public async Task SkippedStyles_IsLeftAsItIs_WhenItHasEverySwatchTheImportReads()
    {
        GumProjectSave theme = (await new GumxSourceService().LoadProjectAsync(Path.Combine(_templateDir, "GumProject.gumx"))).ShouldNotBeNull();
        ComponentSave themeStyles = theme.Components.Single(c => c.Name == "Styles");
        ComponentSave themeListBox = theme.Components.Single(c => c.Name == "Controls/ListBox");
        string stylesPath = Path.Combine(_projectDir, "Components", "Styles.gucx");
        WriteOlderStyles(stylesPath, new List<string>());
        string before = File.ReadAllText(stylesPath);

        await Import(theme, themeStyles, themeListBox);

        File.ReadAllText(stylesPath).ShouldBe(before);
    }

    private async Task<ImportResult> Import(GumProjectSave theme, ComponentSave themeStyles, ComponentSave themeListBox)
    {
        GumProjectSave destination = new GumProjectSave { FullFileName = Path.Combine(_projectDir, "Test.gumx"), Version = theme.Version };
        Mock<IProjectState> projectState = new();
        projectState.Setup(x => x.GumProjectSave).Returns(destination);
        projectState.Setup(x => x.ProjectDirectory).Returns(_projectDir + Path.DirectorySeparatorChar);
        Mock<IImportLogic> importLogic = new();
        importLogic.Setup(x => x.ImportComponent(It.IsAny<ComponentSave>(), It.IsAny<bool>()))
            .Callback((ComponentSave component, bool _) => _registered.Add(component))
            .Returns((ComponentSave component, bool _) => component);
        GumxImportService service = new(importLogic.Object, projectState.Object, Mock.Of<IFileCommands>(),
            new GumxSourceService(), new ImportVersionGate(), Mock.Of<IFileWatchIgnoreList>());

        ImportSelections selections = new()
        {
            DirectComponents = new() { themeStyles, themeListBox },
            TransitiveComponents = new(),
            DirectScreens = new(),
            Behaviors = new(),
            Standards = new(),
        };
        return await service.ImportAsync(selections, theme, _templateDir, destinationSubfolder: "",
            conflictResolution: ConflictResolution.Skip);
    }

    // The template's own Styles with the given swatches removed and Warning recolored by the user.
    private void WriteOlderStyles(string stylesPath, List<string> removedSwatches)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(stylesPath)!);
        XDocument document = XDocument.Load(Path.Combine(_templateDir, "Components", "Styles.gucx"));
        foreach (XElement element in document.Descendants().ToList())
        {
            string? name = (string?)element.Attribute("Name") ?? element.Element("Name")?.Value;
            if (name != null && removedSwatches.Any(s => name == s || name.StartsWith(s + ".", StringComparison.Ordinal)))
            {
                element.Remove();
            }
            else if (name == "Warning.FillRed")
            {
                element.Elements().Single(e => e.Name.LocalName == "Value").Value = WarningUserRed;
            }
        }
        document.Save(stylesPath);
    }

    private static List<string> ReadSwatchNames(ComponentSave component) =>
        component.AllStates
            .SelectMany(s => s.VariableLists)
            .Where(l => l.GetRootName() == "VariableReferences")
            .SelectMany(l => l.ValueAsIList.OfType<string>())
            .SelectMany(line => Regex.Matches(line, @"Components/Styles\.(\w+)\.").Select(m => m.Groups[1].Value))
            .Distinct()
            .ToList();

    private static ComponentSave Load(string path)
    {
        ComponentSave component = GumFileSerializer.DeserializeElementSave<ComponentSave>(File.ReadAllText(path), GumProjectSave.NativeVersion)!;
        foreach (StateSave state in component.AllStates)
        {
            state.FixEnumerations();
        }
        return component;
    }

    private static string FindRepoRoot()
    {
        string current = AppContext.BaseDirectory;
        for (int i = 0; i < 10; i++)
        {
            if (Directory.Exists(Path.Combine(current, "Gum")) && File.Exists(Path.Combine(current, "GumFull.sln")))
            {
                return current;
            }
            current = Path.GetFullPath(Path.Combine(current, ".."));
        }
        throw new DirectoryNotFoundException("Could not locate the repo root from " + AppContext.BaseDirectory);
    }
}
