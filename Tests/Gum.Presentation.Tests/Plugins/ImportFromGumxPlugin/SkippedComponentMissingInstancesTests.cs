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
        listBoxSwatches.ShouldContain("InputBackground");
        listBoxSwatches.ShouldContain("Warning");
        listBoxSwatches.ShouldContain("InputBackgroundDark");

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
        styles.DefaultState!.GetValue("InputBackground.FillRed").ShouldBe(themeStyles.DefaultState!.GetValue("InputBackground.FillRed"));
        styles.DefaultState!.GetValue("InputBackground.Parent").ShouldBe("Colors");
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
        styles.DefaultState!.SetValue("InputBackground.FillRed", 201, "int");
        styles.DefaultState!.SetValue("InputBackgroundDark.FillRed", 202, "int");
        project.ApplyAllVariableReferences();

        listBox.DefaultState!.GetValue("Background.Red").ShouldBe(201);
        listBox.Categories.SelectMany(c => c.States).Single(s => s.Name == "Disabled")
            .GetValue("Background.Red").ShouldBe(202);
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

    [Fact]
    public async Task SkippedComponent_ThatIsNotStyles_GainsTheInstanceAnImportedElementReads()
    {
        ComponentSave palette = Component("Palette", Instance("Accent", "ColoredRectangle"));
        Set(palette.DefaultState!, "Accent.Red", 9, "int");
        ComponentSave button = Component("Button");
        References(button.DefaultState!, owner: null, "Color = Components/Palette.Accent.Color");

        await ImportKeeping(new[] { Component("Palette") }, new[] { palette, button });

        ComponentSave kept = Load(DestinationPath("Palette"));
        kept.Instances.ShouldHaveSingleItem().Name.ShouldBe("Accent");
        kept.DefaultState!.GetValue("Accent.Red").ShouldBe(9);
    }

    [Fact]
    public async Task SkippedComponent_KeepsAnExistingParentAsItIs_WhenAnAddedInstanceIsAttachedToIt()
    {
        ComponentSave palette = Component("Palette", Instance("Colors", "Container"), Instance("Accent", "ColoredRectangle"));
        Set(palette.DefaultState!, "Colors.X", 0f, "float");
        Set(palette.DefaultState!, "Accent.Parent", "Colors", "string");
        ComponentSave button = Component("Button");
        References(button.DefaultState!, owner: null, "Color = Components/Palette.Accent.Color");
        ComponentSave userPalette = Component("Palette", Instance("Colors", "Container"));
        Set(userPalette.DefaultState!, "Colors.X", 5f, "float");

        await ImportKeeping(new[] { userPalette }, new[] { palette, button });

        ComponentSave kept = Load(DestinationPath("Palette"));
        kept.Instances.Select(i => i.Name).ShouldBe(new[] { "Colors", "Accent" });
        kept.DefaultState!.GetValue("Colors.X").ShouldBe(5f);
        kept.DefaultState!.Variables.Count(v => v.Name == "Colors.X").ShouldBe(1);
        kept.DefaultState!.GetValue("Accent.Parent").ShouldBe("Colors");
    }

    [Fact]
    public async Task SkippedComponent_AlsoGainsWhatAnAddedInstanceItselfReads()
    {
        ComponentSave palette = Component("Palette", Instance("Base", "ColoredRectangle"), Instance("Accent", "ColoredRectangle"),
            Instance("Shade", "ColoredRectangle"));
        References(palette.DefaultState!, owner: "Accent", "Color = Base.Color", "Alpha = Components/Palette.Shade.Alpha");
        ComponentSave button = Component("Button");
        References(button.DefaultState!, owner: null, "Color = Components/Palette.Accent.Color");

        await ImportKeeping(new[] { Component("Palette") }, new[] { palette, button });

        Load(DestinationPath("Palette")).Instances.Select(i => i.Name).OrderBy(n => n)
            .ShouldBe(new[] { "Accent", "Base", "Shade" });
    }

    [Fact]
    public async Task SkippedComponents_GainTheInstancesAReferenceReadsThroughSeveralLevels()
    {
        ComponentSave box = Component("Box", Instance("Deep", "ColoredRectangle"));
        ComponentSave panel = Component("Panel", Instance("Inner", "Box"));
        ComponentSave button = Component("Button");
        References(button.DefaultState!, owner: null, "X = Components/Panel.Inner.Deep.X");

        await ImportKeeping(new[] { Component("Box"), Component("Panel") }, new[] { box, panel, button });

        Load(DestinationPath("Panel")).Instances.ShouldHaveSingleItem().Name.ShouldBe("Inner");
        Load(DestinationPath("Box")).Instances.ShouldHaveSingleItem().Name.ShouldBe("Deep");
    }

    [Fact]
    public async Task SkippedComponent_GainsTheAddedInstancesCategoryStateValues_InStatesItHas()
    {
        ComponentSave palette = Component("Palette", Instance("Accent", "ColoredRectangle"));
        palette.Categories.Add(Category(palette, "Theme", "Dark"));
        Set(palette.Categories[0].States[0], "Accent.Red", 9, "int");
        ComponentSave button = Component("Button");
        References(button.DefaultState!, owner: null, "Color = Components/Palette.Accent.Color");
        ComponentSave userPalette = Component("Palette");
        userPalette.Categories.Add(Category(userPalette, "Theme", "Dark"));
        Set(userPalette.Categories[0].States[0], "Width", 3f, "float");

        await ImportKeeping(new[] { userPalette }, new[] { palette, button });

        StateSave dark = Load(DestinationPath("Palette")).Categories.Single().States.Single();
        dark.GetValue("Accent.Red").ShouldBe(9);
        dark.GetValue("Width").ShouldBe(3f);
    }

    // The per-element error check (GUM0009) runs as each element registers, so the kept component must
    // already have what the element reads by then, in memory too.
    [Fact]
    public async Task SkippedComponent_HasTheAddedInstanceInMemory_BeforeTheReaderIsRegistered()
    {
        ComponentSave palette = Component("Palette", Instance("Accent", "ColoredRectangle"));
        ComponentSave button = Component("Button");
        References(button.DefaultState!, owner: null, "Color = Components/Palette.Accent.Color");
        ComponentSave inMemoryPalette = Component("Palette");
        bool? hadAccentAtRegistration = null;

        await ImportKeeping(new[] { Component("Palette") }, new[] { palette, button }, inMemory: inMemoryPalette,
            onRegister: _ => hadAccentAtRegistration = inMemoryPalette.GetInstance("Accent") != null);

        hadAccentAtRegistration.ShouldBe(true);
    }

    private async Task ImportKeeping(ComponentSave[] userVersions, ComponentSave[] imported, ComponentSave? inMemory = null,
        Action<ComponentSave>? onRegister = null)
    {
        foreach (ComponentSave user in userVersions)
        {
            string path = DestinationPath(user.Name);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            user.Save(path, useCompactFormat: true);
        }
        GumProjectSave source = new GumProjectSave();
        source.Components.AddRange(imported);
        GumProjectSave destination = new GumProjectSave { FullFileName = Path.Combine(_projectDir, "Test.gumx"), Version = source.Version };
        if (inMemory != null)
        {
            destination.Components.Add(inMemory);
        }
        string sourceDir = Path.Combine(_projectDir, "Source");
        Directory.CreateDirectory(sourceDir);

        ImportSelections selections = new()
        {
            DirectComponents = imported.ToList(),
            TransitiveComponents = new(),
            DirectScreens = new(),
            Behaviors = new(),
            Standards = new(),
        };
        ImportResult result = await CreateService(destination, onRegister).ImportAsync(selections, source, sourceDir,
            destinationSubfolder: "", conflictResolution: ConflictResolution.Skip);
        result.ConflictingElements.ShouldBeEmpty();
    }

    private string DestinationPath(string componentName) =>
        Path.Combine(_projectDir, "Components", componentName + ".gucx");

    private static ComponentSave Component(string name, params InstanceSave[] instances)
    {
        ComponentSave component = new ComponentSave { Name = name, BaseType = "Container" };
        component.States.Add(new StateSave { Name = "Default", ParentContainer = component });
        foreach (InstanceSave instance in instances)
        {
            instance.ParentContainer = component;
            component.Instances.Add(instance);
        }
        return component;
    }

    private static InstanceSave Instance(string name, string baseType) => new InstanceSave { Name = name, BaseType = baseType };

    private static StateSaveCategory Category(ComponentSave owner, string name, string stateName) =>
        new StateSaveCategory { Name = name, States = { new StateSave { Name = stateName, ParentContainer = owner } } };

    private static void Set(StateSave state, string name, object value, string type) =>
        state.Variables.Add(new VariableSave { Name = name, Value = value, Type = type, SetsValue = true });

    private static void References(StateSave state, string? owner, params string[] lines)
    {
        VariableListSave<string> list = new VariableListSave<string>
        {
            Type = "string",
            Name = owner == null ? "VariableReferences" : owner + ".VariableReferences",
        };
        list.Value.AddRange(lines);
        state.VariableLists.Add(list);
    }

    private async Task<ImportResult> Import(GumProjectSave theme, ComponentSave themeStyles, ComponentSave themeListBox)
    {
        GumProjectSave destination = new GumProjectSave { FullFileName = Path.Combine(_projectDir, "Test.gumx"), Version = theme.Version };
        GumxImportService service = CreateService(destination, onRegister: null);

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

    private GumxImportService CreateService(GumProjectSave destination, Action<ComponentSave>? onRegister)
    {
        Mock<IProjectState> projectState = new();
        projectState.Setup(x => x.GumProjectSave).Returns(destination);
        projectState.Setup(x => x.ProjectDirectory).Returns(_projectDir + Path.DirectorySeparatorChar);
        Mock<IImportLogic> importLogic = new();
        importLogic.Setup(x => x.ImportComponent(It.IsAny<ComponentSave>(), It.IsAny<bool>()))
            .Callback((ComponentSave component, bool _) =>
            {
                onRegister?.Invoke(component);
                _registered.Add(component);
            })
            .Returns((ComponentSave component, bool _) => component);
        return new GumxImportService(importLogic.Object, projectState.Object, Mock.Of<IFileCommands>(),
            new GumxSourceService(), new ImportVersionGate(), Mock.Of<IFileWatchIgnoreList>());
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
