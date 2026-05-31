using Gum.DataTypes;
using Gum.DataTypes.Variables;
using RenderingLibrary;
using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using ToolsUtilities;
using Xunit;

namespace MonoGameGum.Tests.DataTypes;

public class GumProjectSaveTests : BaseTestClass
{
    private static GumProjectSave RoundTrip(GumProjectSave project)
    {
        FileManager.XmlSerialize(project, out string xml);
        FileManager.CustomGetStreamFromFile = _ => new MemoryStream(Encoding.UTF8.GetBytes(xml));
        return GumProjectSave.Load("fake.gumx")!;
    }

    private static GumProjectSave LoadFromXml(string xml)
    {
        FileManager.CustomGetStreamFromFile = _ => new MemoryStream(Encoding.UTF8.GetBytes(xml));
        return GumProjectSave.Load("fake.gumx")!;
    }

    [Fact]
    public void LocalizationFile_LegacyElement_AppearsBeforeLocalizationFilesInSerializedXml()
    {
        // Guard against silent reordering of the LocalizationFile / LocalizationFilesArray
        // properties in GumProjectSave. XmlSerializer applies properties in document order,
        // and the list-replacing setter of LocalizationFilesArray must fire AFTER the
        // legacy setter so that a file containing both elements round-trips with the full
        // list (not just the legacy first entry).
        GumProjectSave project = new GumProjectSave();
        project.LocalizationFiles.Add("a.resx");
        project.LocalizationFiles.Add("b.resx");

        FileManager.XmlSerialize(project, out string xml);

        int legacyIndex = xml.IndexOf("<LocalizationFile>", StringComparison.Ordinal);
        int listIndex = xml.IndexOf("<LocalizationFiles>", StringComparison.Ordinal);

        legacyIndex.ShouldBeGreaterThan(-1);
        listIndex.ShouldBeGreaterThan(-1);
        legacyIndex.ShouldBeLessThan(listIndex);
    }

    [Fact]
    public void LocalizationFile_LegacyShim_GetterReturnsFirstEntry()
    {
        GumProjectSave project = new GumProjectSave();
        project.LocalizationFiles.Add("a.resx");
        project.LocalizationFiles.Add("b.resx");

#pragma warning disable CS0618
        project.LocalizationFile.ShouldBe("a.resx");
#pragma warning restore CS0618
    }

    [Fact]
    public void LocalizationFile_LegacyShim_SetterClearsAndAdds()
    {
        GumProjectSave project = new GumProjectSave();
        project.LocalizationFiles.Add("a.resx");
        project.LocalizationFiles.Add("b.resx");

#pragma warning disable CS0618
        project.LocalizationFile = "replaced.csv";
#pragma warning restore CS0618

        project.LocalizationFiles.Count.ShouldBe(1);
        project.LocalizationFiles[0].ShouldBe("replaced.csv");
    }

    [Fact]
    public void LocalizationFile_LegacyShim_SetterWithEmptyClearsList()
    {
        GumProjectSave project = new GumProjectSave();
        project.LocalizationFiles.Add("a.resx");

#pragma warning disable CS0618
        project.LocalizationFile = "";
#pragma warning restore CS0618

        project.LocalizationFiles.Count.ShouldBe(0);
    }

    [Fact]
    public void LocalizationFiles_BothOldAndNewElementsPresent_NewListWins()
    {
        const string xml = """
            <?xml version="1.0" encoding="utf-8"?>
            <GumProjectSave xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
              <LocalizationFile>legacy.csv</LocalizationFile>
              <LocalizationFiles>
                <string>a.resx</string>
                <string>b.resx</string>
              </LocalizationFiles>
            </GumProjectSave>
            """;

        GumProjectSave project = LoadFromXml(xml);

        project.LocalizationFiles.ShouldBe(new[] { "a.resx", "b.resx" });
    }

    [Fact]
    public void LocalizationFiles_LegacySingleFileXml_PopulatesListWithOneEntry()
    {
        const string xml = """
            <?xml version="1.0" encoding="utf-8"?>
            <GumProjectSave xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
              <LocalizationFile>Strings.csv</LocalizationFile>
            </GumProjectSave>
            """;

        GumProjectSave project = LoadFromXml(xml);

        project.LocalizationFiles.Count.ShouldBe(1);
        project.LocalizationFiles[0].ShouldBe("Strings.csv");
    }

    [Fact]
    public void LocalizationFiles_MultiFileXml_PopulatesListWithAllEntries()
    {
        const string xml = """
            <?xml version="1.0" encoding="utf-8"?>
            <GumProjectSave xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
              <LocalizationFiles>
                <string>Strings.resx</string>
                <string>Buttons.resx</string>
                <string>Errors.resx</string>
              </LocalizationFiles>
            </GumProjectSave>
            """;

        GumProjectSave project = LoadFromXml(xml);

        project.LocalizationFiles.ShouldBe(new[] { "Strings.resx", "Buttons.resx", "Errors.resx" });
    }

    [Fact]
    public void LocalizationFiles_RoundTripEmpty_NeitherElementWritten()
    {
        GumProjectSave project = new GumProjectSave();

        FileManager.XmlSerialize(project, out string xml);

        xml.ShouldNotContain("<LocalizationFile>");
        xml.ShouldNotContain("<LocalizationFiles");

        GumProjectSave loaded = RoundTrip(project);
        loaded.LocalizationFiles.Count.ShouldBe(0);
    }

    [Fact]
    public void LocalizationFiles_RoundTripMultiFile_PreservesAllEntries()
    {
        GumProjectSave project = new GumProjectSave();
        project.LocalizationFiles.Add("a.resx");
        project.LocalizationFiles.Add("b.resx");
        project.LocalizationFiles.Add("c.resx");

        GumProjectSave loaded = RoundTrip(project);

        loaded.LocalizationFiles.ShouldBe(new[] { "a.resx", "b.resx", "c.resx" });
    }

    [Fact]
    public void LocalizationFiles_RoundTripSingleFile_PreservesEntry()
    {
        GumProjectSave project = new GumProjectSave();
        project.LocalizationFiles.Add("foo.csv");

        GumProjectSave loaded = RoundTrip(project);

        loaded.LocalizationFiles.Count.ShouldBe(1);
        loaded.LocalizationFiles[0].ShouldBe("foo.csv");
    }

    [Fact]
    public void LocalizationFiles_SerializedMultiFile_EmitsLegacyElementPointingAtFirstPath()
    {
        GumProjectSave project = new GumProjectSave();
        project.LocalizationFiles.Add("first.resx");
        project.LocalizationFiles.Add("second.resx");

        FileManager.XmlSerialize(project, out string xml);

        XDocument doc = XDocument.Parse(xml);
        XElement root = doc.Root!;
        XElement? legacy = root.Elements().FirstOrDefault(e => e.Name.LocalName == "LocalizationFile");
        legacy.ShouldNotBeNull();
        legacy!.Value.ShouldBe("first.resx");

        XElement? list = root.Elements().FirstOrDefault(e => e.Name.LocalName == "LocalizationFiles");
        list.ShouldNotBeNull();
        list!.Elements().Select(e => e.Value).ShouldBe(new[] { "first.resx", "second.resx" });
    }

    [Fact]
    public void Load_ShouldUseFileManagerCustomGetStreamFromFile_IfSet()
    {
        bool wasCalled = false;

        GumProjectSave gumProject = new GumProjectSave();
        FileManager.XmlSerialize(gumProject, out string xml);

        FileManager.CustomGetStreamFromFile = (filePath) =>
        {
            wasCalled = true;
            return new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes(xml));
        };
        string fakeFilePath = "fakeFilePath.gumx";
        Gum.DataTypes.GumProjectSave.Load(fakeFilePath);
        wasCalled.ShouldBeTrue();
    }

    // Repro for issue #2522: when the user installs a CustomGetStreamFromFile
    // hook before GumService.Initialize, SystemManagers.Initialize must not
    // replace it with its own TitleContainer-based hook.
    [Fact]
    public void SystemManagersInitialize_ShouldNotOverwriteUserCustomGetStreamFromFile()
    {
        Func<string, Stream> userHook = _ => new MemoryStream();
        FileManager.CustomGetStreamFromFile = userHook;

        try
        {
            new SystemManagers().Initialize(graphicsDevice: null!, fullInstantiation: false);
        }
        catch
        {
            // Renderer.Initialize requires a GraphicsDevice — that's fine, we only
            // care that the hook wasn't clobbered before it threw.
        }

        FileManager.CustomGetStreamFromFile.ShouldBeSameAs(userHook);
    }

    // Repro for issue #2522: loading a Gum project whose files live only inside a
    // zip (served via CustomGetStreamFromFile) must succeed — no files on disk,
    // the hook must be used for the .gumx and every referenced element.
    [Fact]
    public void Load_ShouldLoadEntireProjectThroughCustomGetStreamFromFile_WhenAllFilesComeFromZip()
    {
        var zipPath = Path.Combine(AppContext.BaseDirectory, "TestContent", "GumProject.zip");
        File.Exists(zipPath).ShouldBeTrue($"Test zip missing at {zipPath}");

        var contentRoot = Path.Combine(AppContext.BaseDirectory, "Content");
        var zipFileBytes = new Dictionary<string, byte[]>();

        using (var zipToOpen = new FileStream(zipPath, FileMode.Open, FileAccess.Read))
        using (var archive = new ZipArchive(zipToOpen, ZipArchiveMode.Read))
        {
            foreach (var entry in archive.Entries)
            {
                if (entry.FullName.EndsWith("/"))
                {
                    continue;
                }

                using var stream = entry.Open();
                using var ms = new MemoryStream();
                stream.CopyTo(ms);

                var fullPath = Path.Combine(contentRoot, entry.FullName);
                zipFileBytes.Add(fullPath.Replace("\\", "/"), ms.ToArray());
            }
        }

        var requestedPaths = new List<string>();
        FileManager.CustomGetStreamFromFile = fullPath =>
        {
            requestedPaths.Add(fullPath);
            var normalized = fullPath.Replace("\\", "/");
            return zipFileBytes.TryGetValue(normalized, out var bytes)
                ? new MemoryStream(bytes)
                : null;
        };

        var originalRelativeDirectory = FileManager.RelativeDirectory;
        FileManager.RelativeDirectory = "Content/";

        try
        {
            var project = GumProjectSave.Load(
                "GumProject/FromZipFileGumProject.gumx",
                out var loadResult);

            loadResult.ErrorMessage.ShouldBeNullOrEmpty();
            loadResult.MissingFiles.ShouldBeEmpty();
            project.ShouldNotBeNull();
            project!.Screens.ShouldNotBeEmpty();
            project.StandardElements.ShouldNotBeEmpty();
        }
        finally
        {
            FileManager.RelativeDirectory = originalRelativeDirectory;
        }
    }

    [Fact]
    public void Load_V1Format_SetsVersionToInitialVersion()
    {
        // V1 files have no <Version> element; Load must explicitly set Version = 1
        const string v1Xml = """
            <?xml version="1.0" encoding="utf-8"?>
            <GumProjectSave xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
              <DefaultCanvasWidth>800</DefaultCanvasWidth>
              <DefaultCanvasHeight>600</DefaultCanvasHeight>
            </GumProjectSave>
            """;

        FileManager.CustomGetStreamFromFile = filePath =>
            new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes(v1Xml));

        var project = GumProjectSave.Load("fake.gumx");

        project.Version.ShouldBe((int)GumProjectSave.GumxVersions.InitialVersion);
    }

    [Fact]
    public void Load_V2Format_SetsVersionToAttributeVersion()
    {
        // V2 files have a <Version>2</Version> element; Load should read it correctly
        const string v2Xml = """
            <?xml version="1.0" encoding="utf-8"?>
            <GumProjectSave xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
              <Version>2</Version>
              <DefaultCanvasWidth>800</DefaultCanvasWidth>
              <DefaultCanvasHeight>600</DefaultCanvasHeight>
            </GumProjectSave>
            """;

        FileManager.CustomGetStreamFromFile = filePath =>
            new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes(v2Xml));

        var project = GumProjectSave.Load("fake.gumx");

        project.Version.ShouldBe((int)GumProjectSave.GumxVersions.AttributeVersion);
    }

    [Fact]
    public void NativeVersion_MatchesShapeVariableExpansion()
    {
        // The v3 slot is reserved for the expanded Circle/Rectangle variable surface
        // landing in #2925/#2927 follow-up PRs. Files saved at v3 use the same XML
        // format as AttributeVersion; the bump only changes the rejection ceiling so
        // tool builds without the new variable definitions refuse to silently drop them.
        GumProjectSave.NativeVersion.ShouldBe((int)GumProjectSave.GumxVersions.ShapeVariableExpansion);
        ((int)GumProjectSave.GumxVersions.ShapeVariableExpansion).ShouldBe(3);
    }

    [Fact]
    public void MigrateCircleRadiusToWidthHeight_ConvertsQualifiedRadius_ToWidthAndHeight()
    {
        var component = new ComponentSave { Name = "MyComponent" };
        var state = new Gum.DataTypes.Variables.StateSave { Name = "Default" };
        state.Variables.Add(new VariableSave { Name = "CircleInstance.Radius", Type = "float", Value = 20.0f, SetsValue = true });
        component.States.Add(state);

        var project = new GumProjectSave();
        project.Components.Add(component);

        var didChange = project.MigrateCircleRadiusToWidthHeight();

        didChange.ShouldBeTrue();
        state.Variables.ShouldNotContain(v => v.Name == "CircleInstance.Radius");
        state.Variables.First(v => v.Name == "CircleInstance.Width").Value.ShouldBe(40.0f);
        state.Variables.First(v => v.Name == "CircleInstance.Height").Value.ShouldBe(40.0f);
    }

    [Fact]
    public void MigrateCircleRadiusToWidthHeight_ConvertsUnqualifiedRadius_OnCircleDerivedElement()
    {
        // A component deriving from Circle can override Radius in its own default state with
        // an unqualified "Radius" variable.
        var component = new ComponentSave { Name = "MyCircle" };
        var state = new Gum.DataTypes.Variables.StateSave { Name = "Default" };
        state.Variables.Add(new VariableSave { Name = "Radius", Type = "float", Value = 8.0f, SetsValue = true });
        component.States.Add(state);

        var project = new GumProjectSave();
        project.Components.Add(component);

        project.MigrateCircleRadiusToWidthHeight();

        state.Variables.ShouldNotContain(v => v.Name == "Radius");
        state.Variables.First(v => v.Name == "Width").Value.ShouldBe(16.0f);
        state.Variables.First(v => v.Name == "Height").Value.ShouldBe(16.0f);
    }

    [Fact]
    public void MigrateCircleRadiusToWidthHeight_LeavesGradientAndCornerRadius_Untouched()
    {
        var component = new ComponentSave { Name = "MyComponent" };
        var state = new Gum.DataTypes.Variables.StateSave { Name = "Default" };
        state.Variables.Add(new VariableSave { Name = "CircleInstance.GradientInnerRadius", Type = "float", Value = 5.0f, SetsValue = true });
        state.Variables.Add(new VariableSave { Name = "CircleInstance.GradientOuterRadius", Type = "float", Value = 10.0f, SetsValue = true });
        state.Variables.Add(new VariableSave { Name = "RoundedInstance.CornerRadius", Type = "float", Value = 3.0f, SetsValue = true });
        component.States.Add(state);

        var project = new GumProjectSave();
        project.Components.Add(component);

        var didChange = project.MigrateCircleRadiusToWidthHeight();

        didChange.ShouldBeFalse();
        state.Variables.ShouldContain(v => v.Name == "CircleInstance.GradientInnerRadius");
        state.Variables.ShouldContain(v => v.Name == "CircleInstance.GradientOuterRadius");
        state.Variables.ShouldContain(v => v.Name == "RoundedInstance.CornerRadius");
        state.Variables.ShouldNotContain(v => v.Name.EndsWith(".Width"));
    }

    // Issue #3009 — Circle/Rectangle dropped the standalone gradient start (Red1/Green1/Blue1/
    // Alpha1); the start is now the active body color. The migration strips orphaned …1 channels
    // from Circle/Rectangle elements and instances, while leaving Arc (which keeps Color1 as an
    // obsolete shim) and Color2 untouched.
    private static GumProjectSave MakeProjectWithShapeStandards()
    {
        GumProjectSave project = new GumProjectSave();
        foreach (string standardName in new[] { "Circle", "Rectangle", "Arc" })
        {
            StandardElementSave standard = new StandardElementSave { Name = standardName };
            standard.States.Add(new Gum.DataTypes.Variables.StateSave { Name = "Default" });
            project.StandardElements.Add(standard);
        }
        return project;
    }

    [Fact]
    public void StripCircleRectangleGradientColor1_RemovesChannels_OnCircleInstance_KeepsColor2()
    {
        GumProjectSave project = MakeProjectWithShapeStandards();
        ComponentSave component = new ComponentSave { Name = "MyComponent" };
        component.Instances.Add(new InstanceSave { Name = "CircleInstance", BaseType = "Circle" });
        Gum.DataTypes.Variables.StateSave state = new Gum.DataTypes.Variables.StateSave { Name = "Default" };
        state.Variables.Add(new VariableSave { Name = "CircleInstance.Red1", Type = "int", Value = 10, SetsValue = true });
        state.Variables.Add(new VariableSave { Name = "CircleInstance.Green1", Type = "int", Value = 20, SetsValue = true });
        state.Variables.Add(new VariableSave { Name = "CircleInstance.Blue1", Type = "int", Value = 30, SetsValue = true });
        state.Variables.Add(new VariableSave { Name = "CircleInstance.Alpha1", Type = "int", Value = 40, SetsValue = true });
        state.Variables.Add(new VariableSave { Name = "CircleInstance.Red2", Type = "int", Value = 50, SetsValue = true });
        component.States.Add(state);
        project.Components.Add(component);
        Gum.Managers.ObjectFinder.Self.GumProjectSave = project;

        bool changed = project.StripCircleRectangleGradientColor1();

        changed.ShouldBeTrue();
        state.Variables.ShouldNotContain(v => v.Name == "CircleInstance.Red1");
        state.Variables.ShouldNotContain(v => v.Name == "CircleInstance.Green1");
        state.Variables.ShouldNotContain(v => v.Name == "CircleInstance.Blue1");
        state.Variables.ShouldNotContain(v => v.Name == "CircleInstance.Alpha1");
        state.Variables.ShouldContain(v => v.Name == "CircleInstance.Red2");
    }

    [Fact]
    public void StripCircleRectangleGradientColor1_RemovesUnqualifiedChannels_OnRectangleStandardElement()
    {
        GumProjectSave project = MakeProjectWithShapeStandards();
        StandardElementSave rectangle = project.StandardElements.First(s => s.Name == "Rectangle");
        Gum.DataTypes.Variables.StateSave state = rectangle.States.First();
        state.Variables.Add(new VariableSave { Name = "Red1", Type = "int", Value = 1, SetsValue = true });
        state.Variables.Add(new VariableSave { Name = "Alpha1", Type = "int", Value = 2, SetsValue = true });
        Gum.Managers.ObjectFinder.Self.GumProjectSave = project;

        bool changed = project.StripCircleRectangleGradientColor1();

        changed.ShouldBeTrue();
        state.Variables.ShouldNotContain(v => v.Name == "Red1");
        state.Variables.ShouldNotContain(v => v.Name == "Alpha1");
    }

    [Fact]
    public void StripCircleRectangleGradientColor1_LeavesArcInstance_Untouched()
    {
        GumProjectSave project = MakeProjectWithShapeStandards();
        ComponentSave component = new ComponentSave { Name = "MyComponent" };
        component.Instances.Add(new InstanceSave { Name = "ArcInstance", BaseType = "Arc" });
        Gum.DataTypes.Variables.StateSave state = new Gum.DataTypes.Variables.StateSave { Name = "Default" };
        state.Variables.Add(new VariableSave { Name = "ArcInstance.Red1", Type = "int", Value = 10, SetsValue = true });
        component.States.Add(state);
        project.Components.Add(component);
        Gum.Managers.ObjectFinder.Self.GumProjectSave = project;

        bool changed = project.StripCircleRectangleGradientColor1();

        changed.ShouldBeFalse();
        state.Variables.ShouldContain(v => v.Name == "ArcInstance.Red1");
    }

    [Fact]
    public void V1GumxFormat_DeserializesScreenReferenceNamesCorrectly()
    {
        const string v1Xml = """
            <?xml version="1.0" encoding="utf-8"?>
            <GumProjectSave xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
              <ScreenReference>
                <ElementType>Screen</ElementType>
                <LinkType>ReferenceOriginal</LinkType>
                <Name>MainMenu</Name>
              </ScreenReference>
            </GumProjectSave>
            """;

        var deserializer = FileManager.GetXmlSerializer(typeof(GumProjectSave));
        GumProjectSave project;
        using (var reader = new StringReader(v1Xml))
        {
            project = (GumProjectSave)deserializer.Deserialize(reader)!;
        }

        project.ScreenReferences.Count.ShouldBe(1);
        project.ScreenReferences[0].Name.ShouldBe("MainMenu");
    }

    [Fact]
    public void V2GumxFormat_DeserializesScreenReferenceNamesCorrectly()
    {
        const string v2Xml = """
            <?xml version="1.0" encoding="utf-8"?>
            <GumProjectSave xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
              <Version>2</Version>
              <ScreenReference Name="MainMenu" />
            </GumProjectSave>
            """;

        var deserializer = GumFileSerializer.GetGumProjectCompactSerializer();
        GumProjectSave project;
        using (var reader = new StringReader(v2Xml))
        {
            project = (GumProjectSave)deserializer.Deserialize(reader)!;
        }

        project.ScreenReferences.Count.ShouldBe(1);
        project.ScreenReferences[0].Name.ShouldBe("MainMenu");
    }
}
